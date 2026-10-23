using System;
using CatsAndArrows.Gameplay;

namespace CatsAndArrows.Monetization
{
    public enum AdPlacement { Progression, Hint }
    public enum AdAvailability { Ready, Unavailable, Offline, Restricted }
    public enum AdSignalKind { Presented, RewardEarned, Closed, Failed }
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
    public interface IAdService
    {
        AdAvailability GetAvailability(AdPlacement placement);
        bool IsPresenting { get; }
        void Show(long requestId, AdPlacement placement, Action<AdSignal> receive);
        void Cancel(long requestId);
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
