#if !PAWS_PRODUCTION
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.IO;
using CatsAndArrows.Monetization;

namespace CatsAndArrows.UI
{
    // Opt-in lower-environment simulation. No SDK, network or real store identity.
    // #66: mounted desktop Settings capture only; never calls Google or native UI.
    public sealed class ReviewAdPrivacyService : IAdPrivacyService
    {
        public event Action Changed { add { } remove { } }
        public bool PrivacyOptionsRequired => true;
        public bool IsPrivacyFormPresenting => false;
        public void ShowPrivacyOptions(Action<bool> complete) => complete(true);
    }

    public sealed class ReviewAdService : IAdService
    {
        private readonly Func<double> clock;
        private Action<AdSignal> receive;
        private double closesAt;
        private AdPlacement placement;
        public bool IsPresenting => receive != null;
        public ReviewAdService(Func<double> clock) { this.clock = clock; }
        public AdAvailability GetAvailability(AdPlacement requested) => AdAvailability.Ready;
        public void Show(long requestId, AdPlacement requested, Action<AdSignal> callback)
        {
            if (IsPresenting) { callback(new AdSignal(AdSignalKind.Failed)); return; }
            receive = callback; placement = requested; closesAt = clock() + 0.5;
            callback(new AdSignal(AdSignalKind.Presented));
        }
        public void Cancel(long requestId) { /* Simulated presentation still closes safely on Tick. */ }
        public void Tick()
        {
            if (receive == null || clock() < closesAt) return;
            var callback = receive; receive = null;
            if (placement == AdPlacement.Hint) callback(new AdSignal(AdSignalKind.RewardEarned));
            callback(new AdSignal(AdSignalKind.Closed));
        }
    }

    public sealed class ReviewPurchaseService : IPurchaseService
    {
        private const string Store = "cats-arrows-mock-review";
        private const string Evidence = "mock-remove-all-ads-v1";
        private readonly string ledgerPath;
        public event Action<PurchaseResult> Updated { add { } remove { } }
        public ReviewPurchaseService(string ledgerPath) { this.ledgerPath = ledgerPath; }
        public PurchaseOffer GetOffer() => new PurchaseOffer(true, isReviewOffer: true);
        public bool VerifyCachedOwnership(string storeNamespace, string evidenceReference) =>
            storeNamespace == Store && evidenceReference == Evidence && IsOwned();
        public void Refresh(Action<PurchaseResult> complete) => complete(ReadInventory());
        public void Restore(long requestId, Action<PurchaseResult> complete) => complete(ReadInventory());
        public void Purchase(long requestId, Action<PurchaseResult> complete)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ledgerPath));
                File.WriteAllText(ledgerPath, Evidence);
                complete(Owned());
            }
            catch (IOException) { complete(new PurchaseResult(PurchaseOutcome.Failed)); }
            catch (UnauthorizedAccessException) { complete(new PurchaseResult(PurchaseOutcome.Failed)); }
        }
        public void Finish(string transactionReference, Action<bool> complete) => complete(true);
        private bool IsOwned()
        {
            try { return File.Exists(ledgerPath) && File.ReadAllText(ledgerPath) == Evidence; }
            catch (IOException) { return false; }
            catch (UnauthorizedAccessException) { return false; }
        }
        private PurchaseResult ReadInventory()
        {
            try
            {
                if (File.Exists(ledgerPath)) return File.ReadAllText(ledgerPath) == Evidence ? Owned()
                    : new PurchaseResult(PurchaseOutcome.Failed);
                return new PurchaseResult(PurchaseOutcome.NotOwned, verified: true, completeInventory: true, pendingResolved: true);
            }
            catch (IOException) { return new PurchaseResult(PurchaseOutcome.Unavailable); }
            catch (UnauthorizedAccessException) { return new PurchaseResult(PurchaseOutcome.Unavailable); }
        }
        private static PurchaseResult Owned() => new PurchaseResult(PurchaseOutcome.Owned, verified: true,
            completeInventory: true, pendingResolved: true, storeNamespace: Store,
            evidenceReference: Evidence, transactionReference: "mock-remove-all-ads-transaction");
    }
}
#endif

#endif
