using System;
using CatsAndArrows.Monetization;

namespace CatsAndArrows.Advertising
{
    // #67: only a locally declared adult may construct the Google binding.
    // Existing native surfaces retain their guard until actual closure on downgrade.
    public sealed class AudienceAdService : IAdService, IAdPrivacyService, IDisposable
    {
        private readonly IAudiencePreference audience;
        private readonly Func<IGoogleAdsBridge> createBridge;
        private readonly Func<bool> isOwned;
        private readonly Action<Action> dispatch;
        private readonly Func<double> clock;
        private AdMobAdService inner;
        private bool started, disposed, paused, retiring, attempted;
        public AudienceAdService(IAudiencePreference audience, Func<IGoogleAdsBridge> createBridge,
            Func<bool> isOwned, Action<Action> dispatch, Func<double> clock = null)
        {
            this.audience = audience ?? throw new ArgumentNullException(nameof(audience));
            this.createBridge = createBridge ?? throw new ArgumentNullException(nameof(createBridge));
            this.isOwned = isOwned ?? throw new ArgumentNullException(nameof(isOwned));
            this.dispatch = dispatch ?? throw new ArgumentNullException(nameof(dispatch));
            this.clock = clock;
            audience.Changed += AudienceChanged;
        }
        private bool Eligible => !disposed && audience.Current == AudienceBand.Adult;
        public event Action Changed;
        public bool IsPresenting => inner?.IsPresenting == true;
        public bool IsPrivacyFormPresenting => inner?.IsPrivacyFormPresenting == true;
        public bool PrivacyOptionsRequired => Eligible && !retiring && inner?.PrivacyOptionsRequired == true;
        public void Start() { if (disposed) return; started = true; Reconcile(); }
        private void AudienceChanged() { Reconcile(); Changed?.Invoke(); }
        private void Reconcile()
        {
            if (disposed || !started) return;
            if (!Eligible && inner == null) attempted = false;
            if (!Eligible && inner != null) retiring = true;
            if (retiring && inner != null)
            {
                inner.SetPaused(true);
                if (inner.IsPresenting || inner.IsPrivacyFormPresenting) return;
                inner.Changed -= InnerChanged; inner.Dispose(); inner = null; retiring = attempted = false;
            }
            if (Eligible && inner == null && !attempted)
            {
                attempted = true;
                try { inner = new AdMobAdService(true, createBridge(), isOwned, dispatch, clock); }
                catch { return; } // Infrastructure unavailable leaves direct eligible hints usable.
                inner.Changed += InnerChanged;
                inner.SetPaused(paused); inner.Start();
            }
        }
        private void InnerChanged() { Reconcile(); Changed?.Invoke(); }
        public AdAvailability GetAvailability(AdPlacement placement) => Eligible && !retiring ?
            inner?.GetAvailability(placement) ?? AdAvailability.Unavailable : AdAvailability.Unavailable;
        public void Show(long requestId, AdPlacement placement, Action<AdSignal> receive)
        {
            if (receive == null) throw new ArgumentNullException(nameof(receive));
            if (!Eligible || retiring || inner == null) receive(new AdSignal(AdSignalKind.InfrastructureUnavailable));
            else inner.Show(requestId, placement, receive);
        }
        public void Cancel(long requestId) => inner?.Cancel(requestId);
        public void ShowPrivacyOptions(Action<bool> complete)
        {
            if (complete == null) throw new ArgumentNullException(nameof(complete));
            if (!Eligible || retiring || inner == null) complete(false); else inner.ShowPrivacyOptions(complete);
        }
        public void SetPaused(bool value)
        { paused = value; inner?.SetPaused(value || retiring || !Eligible); }
        public void Tick() { inner?.Tick(); Reconcile(); }
        public void Dispose()
        {
            if (disposed) return;
            disposed = true; audience.Changed -= AudienceChanged;
            if (inner != null) { inner.Changed -= InnerChanged; inner.Dispose(); }
            Changed = null;
        }
    }
}
