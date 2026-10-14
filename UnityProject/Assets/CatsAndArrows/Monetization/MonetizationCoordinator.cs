using System;
using CatsAndArrows.Gameplay;

namespace CatsAndArrows.Monetization
{
    // Issue #30: serialized application-thread orchestration, no Unity/vendor types.
    public sealed class MonetizationCoordinator : IDisposable
    {
        private sealed class AdOperation
        {
            public long Id;
            public AdPlacement Placement;
            public GameSession Session;
            public long BoardId;
            public long MarkRevision;
            public double Started;
            public bool AllowDelivery = true;
            public bool TimedOut;
            public bool Rewarded;
            public Action Proceed;
        }
        private readonly IMonetizationStateStore store;
        private readonly IAdService ads;
        private readonly IPurchaseService purchases;
        private readonly Func<double> clock;
        private MonetizationState state = new MonetizationState();
        private GameSession session;
        private long boardId;
        private bool ordinary;
        private bool dirty;
        private bool suppressProgression;
        private bool storageLoaded;
        private bool disposed;
        private AdOperation ad;
        private long purchaseRequest;
        private double purchaseStarted;
        private long refreshGeneration;
        private bool pendingResolved;
        private bool finishing;
        public const double OperationTimeoutSeconds = 30;

        public bool Enabled { get; }
        public bool IsOwned => Enabled && state.Access == AccessState.Owned;
        public bool IsBusy => Enabled && (ad != null || ads.IsPresenting || purchaseRequest != 0);
        public bool HasEarnedHint => Enabled && !IsOwned && state.RewardOperation != 0;
        public bool CanPurchase => Enabled && !IsOwned && pendingResolved && state.Access != AccessState.Unknown &&
            !state.PendingPurchase && purchaseRequest == 0 && Offer.Available;
        public MonetizationStatus Status { get; private set; }
        public PurchaseOffer Offer { get { try { return Enabled ? purchases.GetOffer() : new PurchaseOffer(false); }
            catch { return new PurchaseOffer(false); } } }
        public MonetizationState Snapshot => state.Copy();
        public event Action Changed;
        public event Action<HintDelivery> HintDelivered;

        public MonetizationCoordinator(bool enabled, IMonetizationStateStore store, IAdService ads,
            IPurchaseService purchases, Func<double> clock = null)
        {
            Enabled = enabled;
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            this.ads = ads ?? throw new ArgumentNullException(nameof(ads));
            this.purchases = purchases ?? throw new ArgumentNullException(nameof(purchases));
            this.clock = clock ?? (() => System.Diagnostics.Stopwatch.GetTimestamp() / (double)System.Diagnostics.Stopwatch.Frequency);
            if (!Enabled) return; // Disabled does not touch provider/storage services.
            storageLoaded = store.TryLoad(out var loaded) && loaded != null && loaded.IsValid();
            if (storageLoaded) state = loaded.Copy();
            else { suppressProgression = true; Status = MonetizationStatus.StorageRecovery; }
            if (state.Access == AccessState.Owned)
            {
                bool verified;
                try { verified = purchases.VerifyCachedOwnership(state.StoreNamespace, state.EvidenceReference); }
                catch { verified = false; }
                if (!verified) state.Access = AccessState.Unknown;
            }
            purchases.Updated += OnStoreUpdate;
            RefreshPurchases();
            if (IsOwned) FinishTransaction();
        }

        public void BindSession(GameSession newSession, bool ordinary = true)
        {
            if (disposed) return;
            if (ReferenceEquals(session, newSession) && this.ordinary == ordinary) return;
            CancelHintDelivery();
            session = newSession; this.ordinary = ordinary;
            boardId = 0;
            if (!Enabled || !ordinary || session == null || session.IsCompleted) return;
            if (state.NextBoardInstance == long.MaxValue) { suppressProgression = true; SetStatus(MonetizationStatus.StorageRecovery); return; }
            boardId = state.NextBoardInstance++;
            dirty = true; Persist(); Notify();
        }

        public void RecordCompletion()
        {
            if (!Enabled || disposed || !ordinary || session == null || !session.IsCompleted || boardId == 0 ||
                boardId <= state.LastCountedBoardInstance) return;
            if (state.OrdinaryBoardsCompleted == long.MaxValue) { suppressProgression = true; SetStatus(MonetizationStatus.StorageRecovery); return; }
            state.OrdinaryBoardsCompleted++;
            state.LastCountedBoardInstance = boardId;
            if (IsOwned) state.HandledProgressionMilestone = state.OrdinaryBoardsCompleted / 4;
            dirty = true; Persist(); Notify();
        }

        public void BeginNextBoard(Action proceed)
        {
            if (proceed == null) throw new ArgumentNullException(nameof(proceed));
            if (disposed || IsBusy) return;
            RecordCompletion();
            if (!Enabled) { proceed(); return; }
            if (dirty) Persist();
            if (IsOwned) FinishTransaction();
            var milestone = state.OrdinaryBoardsCompleted / 4;
            if (milestone <= state.HandledProgressionMilestone) { proceed(); return; }
            state.HandledProgressionMilestone = milestone;
            dirty = true;
            var committed = Persist();
            if (!committed || suppressProgression || IsOwned || state.Access == AccessState.Unknown ||
                Availability(AdPlacement.Progression) != AdAvailability.Ready) { proceed(); return; }
            StartAd(AdPlacement.Progression, proceed);
        }

        public void RequestHint()
        {
            if (disposed || IsBusy || session == null || !ordinary || session.IsCompleted) return;
            var prepared = session.PrepareHint();
            if (prepared.Status != HintStatus.Applied) return;
            if (!Enabled || IsOwned) { DeliverHint(session); return; }
            if (HasEarnedHint) { RedeemReward(); return; }
            if (state.Access == AccessState.Unknown) { SetStatus(MonetizationStatus.RestoreRequired); return; }
            if (dirty && !Persist()) return;
            if (!storageLoaded) { SetStatus(MonetizationStatus.StorageRecovery); return; }
            if (Availability(AdPlacement.Hint) != AdAvailability.Ready) { SetStatus(MonetizationStatus.AdUnavailable); return; }
            StartAd(AdPlacement.Hint, null);
        }

        private AdAvailability Availability(AdPlacement placement)
        { try { return ads.GetAvailability(placement); } catch { return AdAvailability.Unavailable; } }

        private void StartAd(AdPlacement placement, Action proceed)
        {
            var id = AllocateOperation();
            if (id == 0) { proceed?.Invoke(); return; }
            ad = new AdOperation { Id = id, Placement = placement, Session = session,
                BoardId = boardId, MarkRevision = session?.MarkRevision ?? 0, Started = clock(), Proceed = proceed };
            SetStatus(MonetizationStatus.Busy);
            try { ads.Show(id, placement, signal => OnAdSignal(id, signal)); }
            catch { OnAdSignal(id, new AdSignal(AdSignalKind.Failed)); }
        }

        private void OnAdSignal(long id, AdSignal signal)
        {
            if (disposed) return;
            if (ad == null || ad.Id != id) { Notify(); return; }
            var current = ad;
            if (signal.Kind == AdSignalKind.RewardEarned)
            {
                if (current.Placement != AdPlacement.Hint || current.Rewarded || id <= state.ProcessedRewardOperation) return;
                current.Rewarded = true;
                state.ProcessedRewardOperation = id;
                if (!IsOwned) { state.RewardOperation = id; state.RewardOriginBoard = current.BoardId; }
                dirty = true; Persist(); Notify(); return;
            }
            if (signal.Kind == AdSignalKind.Presented) { Notify(); return; }
            if (signal.Kind != AdSignalKind.Closed && signal.Kind != AdSignalKind.Failed) return;
            // A failure cannot unlock underneath an existing native presentation.
            if (ads.IsPresenting) { current.AllowDelivery = false; Notify(); return; }
            ad = null;
            if (current.Placement == AdPlacement.Progression)
            { SetStatus(MonetizationStatus.None); current.Proceed?.Invoke(); return; }
            var matches = ReferenceEquals(session, current.Session) && boardId == current.BoardId &&
                session != null && session.MarkRevision == current.MarkRevision && !session.IsCompleted && ordinary;
            if (current.AllowDelivery && matches && (current.Rewarded || IsOwned))
            {
                if (IsOwned) DeliverHint(session); else RedeemReward();
            }
            else SetStatus(HasEarnedHint ? MonetizationStatus.EarnedHint : MonetizationStatus.RewardNotEarned);
            Notify();
        }

        private void RedeemReward()
        {
            if (session == null || session.IsCompleted || !ordinary || !HasEarnedHint) return;
            var target = session;
            var result = target.ApplyHint();
            if (result.Status != HintStatus.Applied) { SetStatus(MonetizationStatus.EarnedHint); return; }
            state.RewardOperation = state.RewardOriginBoard = 0;
            dirty = true;
            Persist(); // Working state remains consumed even if commit fails.
            RecordCompletion();
            if (!dirty) SetStatus(MonetizationStatus.None);
            HintDelivered?.Invoke(new HintDelivery(target, result));
            Notify();
        }

        private void DeliverHint(GameSession target)
        {
            var result = target.ApplyHint();
            RecordCompletion();
            HintDelivered?.Invoke(new HintDelivery(target, result));
            Notify();
        }

        public void CancelHintDelivery()
        {
            if (ad == null || ad.Placement != AdPlacement.Hint) return;
            ad.AllowDelivery = false;
            // Do not abandon an earned callback/dismissal; it may still arrive.
            try { ads.Cancel(ad.Id); } catch { }
        }

        public void PurchaseRemoveAds()
        {
            if (disposed || !CanPurchase) { if (Enabled) SetStatus(MonetizationStatus.PurchaseUnavailable); return; }
            BeginPurchase(false);
        }
        public void RestorePurchases()
        {
            if (disposed || !Enabled || purchaseRequest != 0) return;
            BeginPurchase(true);
        }
        private void BeginPurchase(bool restore)
        {
            // Restore is a read-only store lookup and must remain possible when
            // local storage is damaged. The store transaction is the durable ID.
            var id = AllocateOperation(restore);
            if (id == 0) return;
            purchaseRequest = id; purchaseStarted = clock();
            var generation = ++refreshGeneration;
            SetStatus(MonetizationStatus.Busy);
            try
            {
                Action<PurchaseResult> complete = result => {
                    if (disposed || (purchaseRequest != id && (purchaseRequest != 0 || generation != refreshGeneration))) return;
                    purchaseRequest = 0; refreshGeneration++;
                    HandlePurchase(result); Notify();
                };
                if (restore) purchases.Restore(id, complete); else purchases.Purchase(id, complete);
            }
            catch { purchaseRequest = 0; SetStatus(MonetizationStatus.PurchaseUnavailable); }
        }

        public void RefreshPurchases()
        {
            if (disposed || !Enabled) return;
            var generation = ++refreshGeneration;
            try { purchases.Refresh(result => {
                if (disposed || generation != refreshGeneration) return;
                HandlePurchase(result); if (IsOwned) FinishTransaction(); Notify();
            }); }
            catch { SetStatus(MonetizationStatus.PurchaseUnavailable); }
        }
        private void OnStoreUpdate(PurchaseResult result)
        {
            if (disposed) return;
            refreshGeneration++;
            if (result != null && (result.Outcome == PurchaseOutcome.Pending ||
                (result.Verified && (result.Outcome == PurchaseOutcome.Owned || result.Outcome == PurchaseOutcome.Revoked ||
                (result.Outcome == PurchaseOutcome.NotOwned && result.CompleteInventory)))))
                purchaseRequest = 0;
            HandlePurchase(result); Notify();
        }
        private void HandlePurchase(PurchaseResult result)
        {
            if (result == null) { SetStatus(MonetizationStatus.PurchaseUnavailable); return; }
            if (result.PendingResolved) pendingResolved = true;
            if (result.Outcome == PurchaseOutcome.Pending)
            {
                state.PendingPurchase = true;
                state.PendingPurchaseReference = result.TransactionReference ?? "";
                dirty = true; Persist(); SetStatus(MonetizationStatus.PurchasePending); return;
            }
            if (result.PendingResolved && state.PendingPurchase)
            { state.PendingPurchase = false; state.PendingPurchaseReference = ""; dirty = true; }
            if (result.Outcome == PurchaseOutcome.Owned && result.Verified &&
                !string.IsNullOrEmpty(result.StoreNamespace) && !string.IsNullOrEmpty(result.EvidenceReference))
            {
                state.Access = AccessState.Owned; state.StoreNamespace = result.StoreNamespace;
                state.EvidenceReference = result.EvidenceReference;
                state.PendingPurchase = false; state.PendingPurchaseReference = "";
                state.RewardOperation = state.RewardOriginBoard = 0;
                state.HandledProgressionMilestone = state.OrdinaryBoardsCompleted / 4;
                if (!string.IsNullOrEmpty(result.TransactionReference) && result.TransactionReference != state.LastFinishedTransaction)
                    state.UnfinishedTransaction = result.TransactionReference;
                dirty = true;
                if (ad != null) { try { ads.Cancel(ad.Id); } catch { } }
                if (Persist()) { SetStatus(MonetizationStatus.Owned); FinishTransaction(); }
                return;
            }
            if (result.Verified && (result.Outcome == PurchaseOutcome.Revoked ||
                (result.Outcome == PurchaseOutcome.NotOwned && result.CompleteInventory)))
            {
                var wasOwned = IsOwned;
                state.Access = result.Outcome == PurchaseOutcome.Revoked ? AccessState.Revoked : AccessState.NotOwned;
                state.StoreNamespace = state.EvidenceReference = "";
                if (wasOwned || result.Outcome == PurchaseOutcome.Revoked)
                    state.HandledProgressionMilestone = state.OrdinaryBoardsCompleted / 4;
                dirty = true;
                if (Persist()) SetStatus(MonetizationStatus.None);
                return;
            }
            if (dirty) Persist();
            if (state.PendingPurchase) SetStatus(MonetizationStatus.PurchasePending);
            else SetStatus(IsOwned ? MonetizationStatus.Owned : MonetizationStatus.PurchaseUnavailable);
        }

        private void FinishTransaction()
        {
            if (finishing || dirty || string.IsNullOrEmpty(state.UnfinishedTransaction)) return;
            var transaction = state.UnfinishedTransaction;
            finishing = true;
            try { purchases.Finish(transaction, success => {
                finishing = false;
                if (disposed || !success || state.UnfinishedTransaction != transaction) return;
                state.LastFinishedTransaction = transaction; state.UnfinishedTransaction = "";
                dirty = true; Persist(); Notify();
            }); }
            catch { finishing = false; }
        }

        public void Tick()
        {
            if (disposed || !Enabled) return;
            if (ad != null && (ad.TimedOut || clock() - ad.Started >= OperationTimeoutSeconds))
            {
                var current = ad;
                if (!current.TimedOut)
                {
                    current.TimedOut = true; current.AllowDelivery = false;
                    try { ads.Cancel(current.Id); } catch { }
                }
                if (ReferenceEquals(ad, current) && !ads.IsPresenting)
                {
                    ad = null;
                    SetStatus(HasEarnedHint ? MonetizationStatus.EarnedHint : MonetizationStatus.AdUnavailable);
                    current.Proceed?.Invoke();
                }
                else if (Status != MonetizationStatus.AdUnavailable) SetStatus(MonetizationStatus.AdUnavailable);
            }
            if (purchaseRequest != 0 && clock() - purchaseStarted >= OperationTimeoutSeconds)
            { purchaseRequest = 0; pendingResolved = false; SetStatus(MonetizationStatus.PurchaseUnavailable); }
        }

        private long AllocateOperation(bool allowReadOnlyWithoutCommit = false)
        {
            if (state.NextOperation == long.MaxValue) { SetStatus(MonetizationStatus.StorageRecovery); return 0; }
            var id = state.NextOperation++; dirty = true;
            return Persist() || allowReadOnlyWithoutCommit ? id : 0;
        }
        private bool Persist()
        {
            if (!dirty) return storageLoaded;
            var next = state.Copy();
            if (state.Revision == long.MaxValue) { SetStatus(MonetizationStatus.StorageRecovery); return false; }
            next.Revision++;
            bool committed;
            try { committed = storageLoaded && store.TryCommit(next, state.Revision); }
            catch { committed = false; }
            if (!committed) { suppressProgression = true; SetStatus(MonetizationStatus.StorageRecovery); return false; }
            state.Revision = next.Revision; dirty = false;
            return true;
        }
        private void SetStatus(MonetizationStatus status) { Status = status; Notify(); }
        private void Notify() => Changed?.Invoke();
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            if (Enabled) purchases.Updated -= OnStoreUpdate;
            if (ad != null) { try { ads.Cancel(ad.Id); } catch { } }
            ad = null; session = null; Changed = null; HintDelivered = null;
        }
    }
}
