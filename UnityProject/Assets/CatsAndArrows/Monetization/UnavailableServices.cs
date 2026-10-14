using System;

namespace CatsAndArrows.Monetization
{
    // SDK-free safe defaults; never synthesize a reward, price or purchase.
    public sealed class UnavailableAdService : IAdService
    {
        public bool IsPresenting => false;
        public AdAvailability GetAvailability(AdPlacement placement) => AdAvailability.Unavailable;
        public void Show(long requestId, AdPlacement placement, Action<AdSignal> receive) => receive(new AdSignal(AdSignalKind.Failed));
        public void Cancel(long requestId) { }
    }
    public sealed class UnavailablePurchaseService : IPurchaseService
    {
        public event Action<PurchaseResult> Updated { add { } remove { } }
        public PurchaseOffer GetOffer() => new PurchaseOffer(false);
        public bool VerifyCachedOwnership(string storeNamespace, string evidenceReference) => false;
        public void Refresh(Action<PurchaseResult> complete) => complete(new PurchaseResult(PurchaseOutcome.Unavailable));
        public void Purchase(long requestId, Action<PurchaseResult> complete) => complete(new PurchaseResult(PurchaseOutcome.Unavailable));
        public void Restore(long requestId, Action<PurchaseResult> complete) => complete(new PurchaseResult(PurchaseOutcome.Unavailable));
        public void Finish(string transactionReference, Action<bool> complete) => complete(false);
    }
    public sealed class MemoryMonetizationStateStore : IMonetizationStateStore
    {
        private MonetizationState state = new MonetizationState();
        public bool TryLoad(out MonetizationState loaded) { loaded = state.Copy(); return true; }
        public bool TryCommit(MonetizationState next, long expectedRevision)
        {
            if (next == null || !next.IsValid() || state.Revision != expectedRevision || next.Revision != expectedRevision + 1) return false;
            state = next.Copy(); return true;
        }
    }
}
