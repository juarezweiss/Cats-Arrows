using System;
using System.Collections.Generic;
using System.Linq;
using CatsAndArrows.Core;
using CatsAndArrows.Gameplay;
using CatsAndArrows.Monetization;
using NUnit.Framework;

namespace CatsAndArrows.Tests
{
    // Issue #30: provider-independent state machine, actual serialization tested separately.
    public sealed class MonetizationTests
    {
        private FaultStore store;
        private TestAds ads;
        private TestPurchases purchases;
        private MonetizationCoordinator service;
        private double now;
        [SetUp] public void SetUp()
        {
            store = new FaultStore(); ads = new TestAds(); purchases = new TestPurchases();
            now = 0; service = Create();
        }
        [TearDown] public void TearDown() => service?.Dispose();
        private MonetizationCoordinator Create(bool enabled = true) => new MonetizationCoordinator(enabled, store, ads, purchases, () => now);
        private GameSession Bind(bool ordinary = true)
        { var session = TutorialFixture.CreateSession(); service.BindSession(session, ordinary); return session; }
        private void Complete(GameSession session)
        {
            foreach (var cat in session.Puzzle.SolutionCats)
            { while (session.GetMark(cat) != PlayerMark.Cat) session.Tap(cat); }
            Assert.That(session.IsCompleted, Is.True); service.RecordCompletion();
        }
        private void Win(int count)
        { for (var i = 0; i < count; i++) Complete(Bind()); }
        private int Changes(GameSession session, KeyValuePair<BoardCoordinate, PlayerMark>[] before) =>
            before.Count(pair => session.GetMark(pair.Key) != pair.Value);

        [Test] public void DisabledDoesNotTouchProvidersOrStorageAndHintsStayDirect()
        {
            service.Dispose(); var loads = store.Loads; var queries = purchases.Queries;
            service = Create(false); var session = Bind(); var before = session.Marks.ToArray();
            service.RequestHint(); Complete(session); service.BeginNextBoard(() => { });
            Assert.That(Changes(session, before), Is.GreaterThan(0));
            Assert.That(store.Loads, Is.EqualTo(loads)); Assert.That(purchases.Queries, Is.EqualTo(queries));
            Assert.That(ads.Shows.Count, Is.Zero); Assert.That(service.Snapshot.OrdinaryBoardsCompleted, Is.Zero);
        }
        [TestCase(1,0)] [TestCase(3,0)] [TestCase(4,1)] [TestCase(8,2)] [TestCase(12,3)]
        public void EveryFourthOrdinaryCompletionCreatesExactlyOneOpportunity(int completions, int expected)
        {
            for (var i=0; i<completions; i++)
            {
                Complete(Bind()); var before = ads.Shows.Count; var proceeded = 0;
                service.BeginNextBoard(() => proceeded++);
                if (ads.Shows.Count > before) ads.Send(AdSignalKind.Closed);
                Assert.That(proceeded, Is.EqualTo(1));
            }
            Assert.That(ads.Shows.Count, Is.EqualTo(expected));
            Assert.That(service.Snapshot.OrdinaryBoardsCompleted, Is.EqualTo(completions));
        }
        [Test] public void DuplicateCompletionSameSeedNewInstanceAndAbandonAreDistinct()
        {
            var first=Bind(); Complete(first); service.RecordCompletion(); service.BindSession(first); service.RecordCompletion();
            Bind(); // abandoned instance is not a win
            Complete(Bind());
            Assert.That(service.Snapshot.OrdinaryBoardsCompleted, Is.EqualTo(2));
            var snapshot=service.Snapshot; snapshot.OrdinaryBoardsCompleted=100;
            Assert.That(service.Snapshot.OrdinaryBoardsCompleted, Is.EqualTo(2));
        }
        [Test] public void TutorialGuidedIndependentAndInitiallyCompleteSessionsAreExcluded()
        {
            Complete(Bind(false)); Complete(Bind(false));
            var zero=new GameSession(new PuzzleDefinition(2,0,"zero",Array.Empty<BoardCoordinate>(),Array.Empty<ArrowClue>()));
            service.BindSession(zero); service.RecordCompletion();
            Assert.That(service.Snapshot.OrdinaryBoardsCompleted, Is.Zero);
        }
        [Test] public void ClearStartsAndNoBoardDoNotAdvanceCompletion()
        { Bind().Clear(); Bind(); service.BindSession(null); service.RecordCompletion(); Assert.That(service.Snapshot.OrdinaryBoardsCompleted, Is.Zero); }
        [Test] public void UnownedRestartAndResumeRefreshPreserveDueMilestone()
        {
            Win(4); Assert.That(ads.Shows, Is.Empty);
            service.Dispose(); service=Create(); service.RefreshPurchases();
            var proceeded=0; service.BeginNextBoard(()=>proceeded++);
            Assert.That(ads.Shows.Count, Is.EqualTo(1)); Assert.That(proceeded, Is.Zero);
            ads.Send(AdSignalKind.Closed); Assert.That(proceeded, Is.EqualTo(1));
        }
        [Test] public void ConsumedOpportunityDoesNotReplayAfterCrashBeforeImpression()
        {
            Win(4); service.BeginNextBoard(()=>{});
            service.Dispose(); ads=new TestAds(); service=Create(); var proceeded=0;
            service.BeginNextBoard(()=>proceeded++);
            Assert.That(ads.Shows, Is.Empty); Assert.That(proceeded, Is.EqualTo(1));
        }
        [TestCase(AdAvailability.Unavailable)] [TestCase(AdAvailability.Offline)] [TestCase(AdAvailability.Restricted)]
        public void UnavailableProgressionSkipsWithoutDebt(AdAvailability availability)
        {
            Win(4); ads.Availability=availability; var proceeded=0;
            service.BeginNextBoard(()=>proceeded++); ads.Availability=AdAvailability.Ready;
            service.BeginNextBoard(()=>proceeded++);
            Assert.That(ads.Shows, Is.Empty); Assert.That(proceeded, Is.EqualTo(2));
            Assert.That(service.Snapshot.HandledProgressionMilestone, Is.EqualTo(1));
        }
        [Test] public void FourthWinPersistsImmediatelyAndOnlyNextBoardPresents()
        { Win(4); store.TryLoad(out var state); Assert.That(state.OrdinaryBoardsCompleted, Is.EqualTo(4)); Assert.That(ads.Shows, Is.Empty); }
        [Test] public void StorageFailureNeverTrapsNextBoardOrRetriesProgressionThisRun()
        {
            Win(4); store.Fail=_=>true; var proceeds=0; service.BeginNextBoard(()=>proceeds++);
            store.Fail=null; service.BeginNextBoard(()=>proceeds++);
            Assert.That(proceeds,Is.EqualTo(2)); Assert.That(ads.Shows, Is.Empty);
        }
        [Test] public void HintIsPreparedWithoutMutationAndDuplicateRewardsDeliverOnce()
        {
            var session=Bind(); var before=session.Marks.ToArray(); var delivered=0;
            service.HintDelivered+=_=>delivered++; service.RequestHint(); service.RequestHint();
            Assert.That(Changes(session,before),Is.Zero); Assert.That(ads.Shows.Count,Is.EqualTo(1));
            ads.Send(AdSignalKind.RewardEarned); ads.Send(AdSignalKind.RewardEarned);
            Assert.That(Changes(session,before),Is.Zero);
            ads.Send(AdSignalKind.Closed); ads.Send(AdSignalKind.RewardEarned); ads.Send(AdSignalKind.Closed);
            Assert.That(Changes(session,before),Is.EqualTo(1)); Assert.That(delivered,Is.EqualTo(1));
            Assert.That(service.HasEarnedHint,Is.False); Assert.That(service.IsBusy,Is.False);
        }
        [TestCase(AdSignalKind.Closed)] [TestCase(AdSignalKind.Failed)]
        public void ClosedFailedAndLateUnconfirmedRewardDoNotApplyHint(AdSignalKind signal)
        {
            var session=Bind(); var before=session.Marks.ToArray(); service.RequestHint(); ads.Send(signal);
            ads.Send(AdSignalKind.RewardEarned);
            Assert.That(Changes(session,before),Is.Zero); Assert.That(service.HasEarnedHint,Is.False);
        }
        [TestCase(AdAvailability.Offline)] [TestCase(AdAvailability.Unavailable)] [TestCase(AdAvailability.Restricted)]
        public void UnavailableRewardDoesNotGrantFreeHint(AdAvailability availability)
        { var session=Bind();var before=session.Marks.ToArray();ads.Availability=availability;service.RequestHint();Assert.That(Changes(session,before),Is.Zero);Assert.That(ads.Shows,Is.Empty); }
        [Test] public void CompletedAndTutorialBoardsNeverRequestReward()
        { Complete(Bind()); service.RequestHint(); Bind(false);service.RequestHint();Assert.That(ads.Shows,Is.Empty); }
        [Test] public void StaleBoardRewardRequiresExplicitRedemptionOnNewBoard()
        {
            Bind();service.RequestHint();var replacement=Bind();var before=replacement.Marks.ToArray();
            ads.Send(AdSignalKind.RewardEarned);ads.Send(AdSignalKind.Closed);
            Assert.That(Changes(replacement,before),Is.Zero);Assert.That(service.HasEarnedHint,Is.True);
            service.RequestHint();Assert.That(Changes(replacement,before),Is.EqualTo(1));Assert.That(ads.Shows.Count,Is.EqualTo(1));
        }
        [Test] public void MarkRevisionChangesSuppressAutomaticRewardDelivery()
        {
            var session=Bind();service.RequestHint();session.Tap(session.Marks.Keys.First());var before=session.Marks.ToArray();
            ads.Send(AdSignalKind.RewardEarned);ads.Send(AdSignalKind.Closed);
            Assert.That(Changes(session,before),Is.Zero);Assert.That(service.HasEarnedHint,Is.True);
        }
        [Test] public void ConfirmedUnusedRewardSurvivesRestart()
        {
            Bind();service.RequestHint();ads.Send(AdSignalKind.RewardEarned);service.Dispose();
            ads=new TestAds();service=Create();var session=Bind();var before=session.Marks.ToArray();service.RequestHint();
            Assert.That(Changes(session,before),Is.EqualTo(1));Assert.That(ads.Shows,Is.Empty);
        }
        [TestCase(true)] [TestCase(false)]
        public void RewardEarnOrConsumeWriteFailureHonorsRewardUnlocksAndCommitsLatestState(bool earnFailure)
        {
            var session=Bind();var before=session.Marks.ToArray();service.RequestHint();
            if(earnFailure)store.Fail=s=>s.RewardOperation!=0;
            ads.Send(AdSignalKind.RewardEarned);
            if(!earnFailure)store.Fail=s=>s.RewardOperation==0;
            ads.Send(AdSignalKind.Closed);
            Assert.That(Changes(session,before),Is.EqualTo(1));Assert.That(service.IsBusy,Is.False);
            Assert.That(service.HasEarnedHint,Is.False);
            ads.Send(AdSignalKind.RewardEarned);Assert.That(Changes(session,before),Is.EqualTo(1));
            store.Fail=null;service.BeginNextBoard(()=>{});store.TryLoad(out var saved);
            Assert.That(saved.RewardOperation,Is.Zero);Assert.That(saved.ProcessedRewardOperation,Is.GreaterThan(0));
        }
        [Test] public void TimeoutNeverUnlocksBeneathNativeAdAndPollsSafeClosureWithoutCallback()
        {
            var session=Bind();service.RequestHint();ads.Send(AdSignalKind.Presented);now=31;service.Tick();
            Assert.That(service.IsBusy,Is.True);ads.Send(AdSignalKind.RewardEarned);
            ads.Presenting=false;service.Tick();Assert.That(service.IsBusy,Is.False);Assert.That(service.HasEarnedHint,Is.True);
            service.RequestHint();Assert.That(service.HasEarnedHint,Is.False);
        }
        [Test] public void ProgressionTimeoutProceedsOnceAfterSafeClosureWithoutCallback()
        {
            Win(4);var proceeds=0;service.BeginNextBoard(()=>proceeds++);ads.Send(AdSignalKind.Presented);
            now=31;service.Tick();Assert.That(proceeds,Is.Zero);ads.Presenting=false;service.Tick();service.Tick();
            ads.Send(AdSignalKind.Closed);Assert.That(proceeds,Is.EqualTo(1));
        }
        [Test] public void PaidOfflineCacheBypassesBothPlacementsAfterRestart()
        {
            purchases.Emit(TestPurchases.Owned());service.Dispose();purchases.RefreshResult=new PurchaseResult(PurchaseOutcome.Unavailable);
            ads=new TestAds();service=Create();var session=Bind();var before=session.Marks.ToArray();service.RequestHint();
            Assert.That(Changes(session,before),Is.EqualTo(1));Win(4);service.BeginNextBoard(()=>{});
            Assert.That(service.IsOwned,Is.True);Assert.That(ads.Shows,Is.Empty);
        }
        [Test] public void UnverifiedEditableOwnedCacheNeverGrantsAccess()
        {
            purchases.Emit(TestPurchases.Owned());service.Dispose();purchases.CacheValid=false;
            purchases.RefreshResult=new PurchaseResult(PurchaseOutcome.Unavailable);service=Create();var session=Bind();var before=session.Marks.ToArray();
            service.RequestHint();Assert.That(service.IsOwned,Is.False);Assert.That(Changes(session,before),Is.Zero);Assert.That(ads.Shows,Is.Empty);
        }
        [TestCase(PurchaseOutcome.Cancelled)] [TestCase(PurchaseOutcome.Pending)] [TestCase(PurchaseOutcome.Unavailable)] [TestCase(PurchaseOutcome.Failed)]
        public void NonOwnershipPurchaseOutcomesCannotGrant(PurchaseOutcome outcome)
        { service.PurchaseRemoveAds();purchases.CompletePurchase(new PurchaseResult(outcome));Assert.That(service.IsOwned,Is.False); }
        [Test] public void PurchaseButtonUnverifiedOwnedResultCannotGrant()
        { service.PurchaseRemoveAds();purchases.CompletePurchase(new PurchaseResult(PurchaseOutcome.Owned));Assert.That(service.IsOwned,Is.False); }
        [Test] public void VerifiedPurchasePersistsBeforeFinalizationAndSuppressesDueAds()
        {
            Win(4);purchases.BeforeFinish=()=>{store.TryLoad(out var state);Assert.That(state.Access,Is.EqualTo(AccessState.Owned));Assert.That(state.UnfinishedTransaction,Is.EqualTo("transaction"));};
            service.PurchaseRemoveAds();purchases.CompletePurchase(TestPurchases.Owned());service.BeginNextBoard(()=>{});
            Assert.That(service.IsOwned,Is.True);Assert.That(purchases.Finishes,Is.EqualTo(1));Assert.That(ads.Shows,Is.Empty);
            purchases.Emit(TestPurchases.Owned());Assert.That(purchases.Finishes,Is.EqualTo(1));
        }
        [Test] public void PurchaseSaveFailureKeepsInMemoryAccessWithoutFinishingUntilRecovery()
        {
            store.Fail=s=>s.Access==AccessState.Owned;service.PurchaseRemoveAds();purchases.CompletePurchase(TestPurchases.Owned());
            Assert.That(service.IsOwned,Is.True);Assert.That(purchases.Finishes,Is.Zero);
            store.Fail=null;service.BeginNextBoard(()=>{});Assert.That(purchases.Finishes,Is.EqualTo(1));
        }
        [Test] public void UnfinishedTransactionRetriesOnOfflineLaunchAndResume()
        {
            purchases.FinishSuccess=false;purchases.Emit(TestPurchases.Owned());service.Dispose();
            purchases.RefreshResult=new PurchaseResult(PurchaseOutcome.Unavailable);var before=purchases.Finishes;service=Create();
            Assert.That(purchases.Finishes,Is.GreaterThan(before));purchases.FinishSuccess=true;service.RefreshPurchases();
            Assert.That(service.Snapshot.UnfinishedTransaction,Is.Empty);Assert.That(service.IsOwned,Is.True);
        }
        [Test] public void PendingPurchaseSurvivesRestartAndIncompleteQueryThenVerifiedUpdateGrants()
        {
            service.PurchaseRemoveAds();purchases.CompletePurchase(new PurchaseResult(PurchaseOutcome.Pending,transactionReference:"pending"));
            service.Dispose();purchases.RefreshResult=new PurchaseResult(PurchaseOutcome.Unavailable);service=Create();
            Assert.That(service.Snapshot.PendingPurchase,Is.True);Assert.That(service.CanPurchase,Is.False);
            purchases.Emit(TestPurchases.Owned());Assert.That(service.IsOwned,Is.True);Assert.That(service.Snapshot.PendingPurchase,Is.False);
        }
        [Test] public void VerifiedRestoreAfterFreshInstallGrantsAndNetworkErrorsNeverRevoke()
        {
            service.RestorePurchases();purchases.CompleteRestore(TestPurchases.Owned());
            purchases.Emit(new PurchaseResult(PurchaseOutcome.Unavailable));purchases.Emit(new PurchaseResult(PurchaseOutcome.Revoked));
            Assert.That(service.IsOwned,Is.True);
        }
        [Test] public void VerifiedRevocationCreatesNoCatchupDebtOnlySubsequentFourthWin()
        {
            purchases.Emit(TestPurchases.Owned());Win(5);purchases.Emit(new PurchaseResult(PurchaseOutcome.Revoked,verified:true,pendingResolved:true));
            service.BeginNextBoard(()=>{});Assert.That(ads.Shows,Is.Empty);Win(3);service.BeginNextBoard(()=>{});
            Assert.That(ads.Shows.Count,Is.EqualTo(1));
        }
        [Test] public void StaleInventoryRefreshCannotUndoPurchaseOrLaterRevocation()
        {
            purchases.DeferRefresh=true;service.RefreshPurchases();var refresh=purchases.RefreshCallbacks.Last();
            purchases.Emit(TestPurchases.Owned());refresh(TestPurchases.NotOwned());Assert.That(service.IsOwned,Is.True);
            service.RefreshPurchases();refresh=purchases.RefreshCallbacks.Last();
            purchases.Emit(new PurchaseResult(PurchaseOutcome.Revoked,verified:true));refresh(TestPurchases.Owned());Assert.That(service.IsOwned,Is.False);
        }
        [Test] public void PurchaseTimeoutAcceptsLateVerifiedSuccessUnlessSuperseded()
        {
            service.PurchaseRemoveAds();now=31;service.Tick();Assert.That(service.IsBusy,Is.False);
            purchases.CompletePurchase(TestPurchases.Owned());Assert.That(service.IsOwned,Is.True);
        }
        [Test] public void OwnedDuringHintAdDeliversAtMostOneOriginalRequestAfterClosure()
        {
            var session=Bind();var before=session.Marks.ToArray();service.RequestHint();ads.Send(AdSignalKind.Presented);
            purchases.Emit(TestPurchases.Owned());Assert.That(service.IsBusy,Is.True);Assert.That(Changes(session,before),Is.Zero);
            ads.Send(AdSignalKind.RewardEarned);ads.Send(AdSignalKind.Closed);ads.Send(AdSignalKind.Closed);
            Assert.That(Changes(session,before),Is.EqualTo(1));Assert.That(service.HasEarnedHint,Is.False);
        }
        [Test] public void DisposedCallbacksCannotApplyHint()
        {
            var session=Bind();var before=session.Marks.ToArray();service.RequestHint();service.Dispose();
            ads.Send(AdSignalKind.RewardEarned);ads.Send(AdSignalKind.Closed);Assert.That(Changes(session,before),Is.Zero);
        }
        [Test] public void AuthoritativeUpdateResolvesPurchaseAndIgnoresStalePendingCallback()
        {
            service.PurchaseRemoveAds();Assert.That(service.IsBusy,Is.True);
            purchases.Emit(TestPurchases.Owned());Assert.That(service.IsBusy,Is.False);
            purchases.CompletePurchase(new PurchaseResult(PurchaseOutcome.Pending));
            Assert.That(service.IsOwned,Is.True);Assert.That(service.Snapshot.PendingPurchase,Is.False);
        }
        [Test] public void ExplicitRestoreWorksWhenLocalStateCannotBeReadOrWritten()
        {
            service.Dispose();store.LoadFailed=true;purchases.RefreshResult=new PurchaseResult(PurchaseOutcome.Unavailable);
            service=Create();service.RestorePurchases();purchases.CompleteRestore(TestPurchases.Owned());
            Assert.That(service.IsOwned,Is.True);Assert.That(purchases.Finishes,Is.Zero);
            var session=Bind();var before=session.Marks.ToArray();service.RequestHint();Assert.That(Changes(session,before),Is.EqualTo(1));
        }
        [Test] public void FailedPendingWriteStillQueriesStorePendingStateBeforeAllowingRepurchase()
        {
            store.Fail=s=>s.PendingPurchase;service.PurchaseRemoveAds();
            purchases.CompletePurchase(new PurchaseResult(PurchaseOutcome.Pending,transactionReference:"pending"));
            service.Dispose();store.Fail=null;purchases.RefreshResult=new PurchaseResult(PurchaseOutcome.Pending,transactionReference:"pending");
            service=Create();Assert.That(service.CanPurchase,Is.False);Assert.That(service.Snapshot.PendingPurchase,Is.True);
        }

        private sealed class FaultStore : IMonetizationStateStore
        {
            private readonly MemoryMonetizationStateStore inner=new MemoryMonetizationStateStore();
            public Func<MonetizationState,bool> Fail; public int Loads;public bool LoadFailed;
            public bool TryLoad(out MonetizationState state){Loads++;if(LoadFailed){state=null;return false;}return inner.TryLoad(out state);}
            public bool TryCommit(MonetizationState state,long revision)=>!(Fail?.Invoke(state)??false)&&inner.TryCommit(state,revision);
        }
        private sealed class TestAds : IAdService
        {
            public AdAvailability Availability=AdAvailability.Ready;public bool Presenting;public bool IsPresenting=>Presenting;
            public readonly List<AdPlacement> Shows=new List<AdPlacement>();private Action<AdSignal> receive;
            public AdAvailability GetAvailability(AdPlacement placement)=>Availability;
            public void Show(long requestId,AdPlacement placement,Action<AdSignal> callback){Shows.Add(placement);receive=callback;}
            public void Send(AdSignalKind kind){if(kind==AdSignalKind.Presented)Presenting=true;if(kind==AdSignalKind.Closed)Presenting=false;receive(new AdSignal(kind));}
            public void Cancel(long requestId){}
        }
        private sealed class TestPurchases : IPurchaseService
        {
            public event Action<PurchaseResult> Updated;public int Queries,Finishes;public bool CacheValid=true,FinishSuccess=true,DeferRefresh;
            public Action BeforeFinish;public PurchaseResult RefreshResult=NotOwned();
            public readonly List<Action<PurchaseResult>> RefreshCallbacks=new List<Action<PurchaseResult>>();
            private Action<PurchaseResult> purchase,restore;
            public static PurchaseResult NotOwned()=>new PurchaseResult(PurchaseOutcome.NotOwned,true,true,true);
            public static PurchaseResult Owned()=>new PurchaseResult(PurchaseOutcome.Owned,true,true,true,"test","evidence","transaction");
            public PurchaseOffer GetOffer()=>new PurchaseOffer(true,"Test only",true);
            public bool VerifyCachedOwnership(string storeNamespace,string evidenceReference)=>CacheValid&&storeNamespace=="test"&&evidenceReference=="evidence";
            public void Refresh(Action<PurchaseResult> complete){Queries++;RefreshCallbacks.Add(complete);if(!DeferRefresh)complete(RefreshResult);}
            public void Purchase(long requestId,Action<PurchaseResult> complete){purchase=complete;}
            public void Restore(long requestId,Action<PurchaseResult> complete){restore=complete;}
            public void CompletePurchase(PurchaseResult result)=>purchase(result);
            public void CompleteRestore(PurchaseResult result)=>restore(result);
            public void Emit(PurchaseResult result)=>Updated?.Invoke(result);
            public void Finish(string transactionReference,Action<bool> complete){Finishes++;BeforeFinish?.Invoke();complete(FinishSuccess);}
        }
    }
}
