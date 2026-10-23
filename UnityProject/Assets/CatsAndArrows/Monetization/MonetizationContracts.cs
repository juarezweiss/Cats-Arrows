using System;
using CatsAndArrows.Gameplay;

namespace CatsAndArrows.Monetization
{
    public enum AdPlacement { Progression, Hint }
    public enum AdAvailability { Ready, Unavailable, Offline, Restricted }
    public enum AdSignalKind { Presented, RewardEarned, Closed, Failed, InfrastructureUnavailable }
    public enum AccessState { Unknown, NotOwned, Owned, Revoked }
    public enum PurchaseOutcome { Owned, NotOwned, Revoked, Pending, Cancelled, Unavailable, Failed }
    public enum MonetizationStatus { None, AdUnavailable, RewardNotEarned, EarnedHint, RestoreRequired, PurchasePending, PurchaseUnavailable, Owned, StorageRecovery, Busy, OfferCodeUnsupported, OfferCodeUnavailable }
    // Sheet closure is not proof of redemption. Ownership comes only through IPurchaseService.
    public enum OfferCodeRedemptionResult { Closed, Unsupported, Unavailable }

    public readonly struct AdSignal
    {
        public AdSignalKind Kind { get; }
        public AdSignal(AdSignalKind kind) { Kind = kind; }
    }

    // Callbacks must be marshalled to the application thread. Closed is sent only
    // after native dismissal AND the provider has settled its reward callbacks.
    // InfrastructureUnavailable is confirmed provider/consent/load/show failure,
    // never a voluntary close. Failed remains ambiguous after presentation (#66).
    public interface IAdService
    {
        AdAvailability GetAvailability(AdPlacement placement);
        // Remains true until native dismissal is safe and queued rewards settle.
        bool IsPresenting { get; }
        void Show(long requestId, AdPlacement placement, Action<AdSignal> receive);
        void Cancel(long requestId);
    }

    // Optional provider privacy capability; SDK consent types stay adapter-private (#66).
    public interface IAdPrivacyService
    {
        event Action Changed;
        bool PrivacyOptionsRequired { get; }
        bool IsPrivacyFormPresenting { get; }
        void ShowPrivacyOptions(Action<bool> complete);
    }

    public sealed class PurchaseOffer
    {
        public bool Available { get; }
        public string LocalizedPrice { get; }
        public bool IsReviewOffer { get; }
        public PurchaseOffer(bool available, string localizedPrice = null, bool isReviewOffer = false)
        { Available = available; LocalizedPrice = localizedPrice; IsReviewOffer = isReviewOffer; }
    }

    public sealed class PurchaseResult
    {
        public PurchaseOutcome Outcome { get; }
        public bool Verified { get; }
        public bool CompleteInventory { get; }
        public bool PendingResolved { get; }
        public string StoreNamespace { get; }
        public string EvidenceReference { get; }
        public string TransactionReference { get; }
        public PurchaseResult(PurchaseOutcome outcome, bool verified = false, bool completeInventory = false,
            bool pendingResolved = false, string storeNamespace = null, string evidenceReference = null,
            string transactionReference = null)
        {
            Outcome = outcome; Verified = verified; CompleteInventory = completeInventory;
            PendingResolved = pendingResolved; StoreNamespace = storeNamespace;
            EvidenceReference = evidenceReference; TransactionReference = transactionReference;
        }
    }

    public interface IPurchaseService
    {
        event Action<PurchaseResult> Updated;
        PurchaseOffer GetOffer();
        bool VerifyCachedOwnership(string storeNamespace, string evidenceReference);
        // Refresh includes pending/deferred purchases and unfinished transactions,
        // not merely restored entitlements. Partial/network failures are not absence.
        void Refresh(Action<PurchaseResult> complete);
        void Purchase(long requestId, Action<PurchaseResult> complete);
        void Restore(long requestId, Action<PurchaseResult> complete);
        void Finish(string transactionReference, Action<bool> complete);
    }

    // Optional platform capability; no code values or store types cross this port (#65).
    public interface IOfferCodeRedemptionService
    {
        void RedeemOfferCode(Action<OfferCodeRedemptionResult> complete);
    }

    public interface IMonetizationStateStore
    {
        bool TryLoad(out MonetizationState state);
        bool TryCommit(MonetizationState state, long expectedRevision);
    }

    public readonly struct HintDelivery
    {
        public GameSession Session { get; }
        public HintResult Result { get; }
        public HintDelivery(GameSession session, HintResult result) { Session = session; Result = result; }
    }
}
