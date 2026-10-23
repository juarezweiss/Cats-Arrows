using System;
using CatsAndArrows.Monetization;

namespace CatsAndArrows.Advertising
{
    // #66: application-thread lifecycle; no puzzle, SDK or Unity state lives here.
    public sealed class AdMobAdService : IAdService, IAdPrivacyService, IDisposable
    {
        private sealed class Slot
        {
            public long Generation;
            public bool Loading;
            public double Deadline;
            public IGoogleAd Ad;
        }
        private sealed class Presentation
        {
            public long Id;
            public AdPlacement Placement;
            public IGoogleAd Ad;
            public Action<AdSignal> Receive;
            public bool Presented;
            public bool Rewarded;
            public bool Terminal;
        }
        private readonly bool enabled;
        private readonly IGoogleAdsBridge bridge;
        private readonly Func<bool> isOwned;
        private readonly Action<Action> dispatch;
        private readonly Func<double> clock;
        private readonly Slot progression = new Slot();
        private readonly Slot hint = new Slot();
        private bool started, disposed, paused, consentReady, initializing, initialized, initializationAttempted;
        private bool consentPending, requiredFormPending, privacyPending, privacyTimedOut;
        private bool previousOwned;
        private long consentGeneration;
        private double consentDeadline, initializationDeadline;
        private Presentation active;
        public const double InfrastructureTimeoutSeconds = 30;
        public event Action Changed;
        public bool IsPresenting => active != null;
        public bool IsPrivacyFormPresenting { get; private set; }
        public bool PrivacyOptionsRequired => enabled && !disposed && started && SafePrivacyRequired();

        public AdMobAdService(bool enabled, IGoogleAdsBridge bridge, Func<bool> isOwned,
            Action<Action> dispatch, Func<double> clock = null)
        {
            this.enabled = enabled;
            this.bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
            this.isOwned = isOwned ?? throw new ArgumentNullException(nameof(isOwned));
            this.dispatch = dispatch ?? throw new ArgumentNullException(nameof(dispatch));
            this.clock = clock ?? (() => System.Diagnostics.Stopwatch.GetTimestamp() /
                (double)System.Diagnostics.Stopwatch.Frequency);
        }

        public void Start()
        {
            if (!enabled || disposed || started) return;
            started = true;
            previousOwned = Owned();
            consentPending = true;
            consentDeadline = clock() + InfrastructureTimeoutSeconds;
            var generation = ++consentGeneration;
            try { bridge.UpdateConsent(success => dispatch(() => ConsentUpdated(generation, success))); }
            catch { ConsentUpdated(generation, false); }
        }

        private void ConsentUpdated(long generation, bool success)
        {
            if (disposed || !consentPending || requiredFormPending || IsPrivacyFormPresenting || generation != consentGeneration) return;
            if (!success) { FinishConsent(generation, false); return; }
            requiredFormPending = true;
            TryShowRequiredForm();
        }

        private void TryShowRequiredForm()
        {
            if (disposed || paused || !consentPending || !requiredFormPending || IsPrivacyFormPresenting) return;
            if (clock() >= consentDeadline)
            {
                requiredFormPending = consentPending = false;
                consentReady = false;
                privacyTimedOut = true;
                ++consentGeneration;
                Changed?.Invoke();
                return;
            }
            requiredFormPending = false;
            var generation = consentGeneration;
            // UMP decides whether a form is required. Its completion is the only
            // safe native-closure evidence, including after a lifecycle pause.
            IsPrivacyFormPresenting = true;
            Changed?.Invoke();
            if (disposed) { IsPrivacyFormPresenting = false; return; }
            if (paused)
            {
                // A synchronous observer can pause before native invocation.
                // No surface exists yet; retain the deferred form instead.
                IsPrivacyFormPresenting = false;
                requiredFormPending = true;
                Changed?.Invoke();
                return;
            }
            try { bridge.LoadAndShowRequiredConsentForm(ok => dispatch(() => FinishConsent(generation, ok))); }
            catch
            {
                // Once a native form call was attempted, an exception alone is
                // not closure evidence. A terminal bridge callback still owns
                // releasing the native-surface guard.
                consentReady = false;
                privacyTimedOut = true;
            }
        }

        private void FinishConsent(long generation, bool success)
        {
            if (disposed || !consentPending || generation != consentGeneration) return;
            consentPending = false;
            requiredFormPending = false;
            IsPrivacyFormPresenting = false;
            consentReady = success && !privacyTimedOut && SafeCanRequest();
            Changed?.Invoke();
            if (consentReady) Prepare();
        }

        public AdAvailability GetAvailability(AdPlacement placement)
        {
            ReconcileOwnership();
            if ((placement != AdPlacement.Progression && placement != AdPlacement.Hint) ||
                !CanAdvertise() || IsPresenting) return AdAvailability.Unavailable;
            var slot = For(placement);
            if (slot.Ad == null) return AdAvailability.Unavailable;
            if (SafeCanShow(slot.Ad)) return AdAvailability.Ready;
            Invalidate(slot);
            return AdAvailability.Unavailable;
        }

        // Explicit preload boundary: launch, resume, successful privacy update or
        // previous ad closure. Failure does not schedule an automatic retry loop.
        public void Prepare()
        {
            ReconcileOwnership();
            if (!CanAdvertise(false) || IsPresenting) return;
            if (!initialized)
            {
                if (initializationAttempted) return;
                initializationAttempted = initializing = true;
                initializationDeadline = clock() + InfrastructureTimeoutSeconds;
                try { bridge.Initialize(success => dispatch(() => Initialized(success))); }
                catch { Initialized(false); }
                return;
            }
            Load(AdPlacement.Progression, progression);
            Load(AdPlacement.Hint, hint);
        }

        private void Initialized(bool success)
        {
            if (disposed || !initializing) return;
            initializing = false;
            initialized = success;
            if (success) Prepare();
            Changed?.Invoke();
        }

        private void Load(AdPlacement placement, Slot slot)
        {
            if (!CanAdvertise() || slot.Loading || slot.Ad != null) return;
            slot.Loading = true;
            slot.Deadline = clock() + InfrastructureTimeoutSeconds;
            var generation = ++slot.Generation;
            try { bridge.Load(placement, ad => dispatch(() => Loaded(slot, generation, ad))); }
            catch { Loaded(slot, generation, null); }
        }

        private void Loaded(Slot slot, long generation, IGoogleAd ad)
        {
            // A stale/duplicate load can carry a native resource. Dispose it but
            // never destroy an object already accepted by a slot/presentation.
            if (disposed || !slot.Loading || generation != slot.Generation)
            { DestroyUnlessRetained(ad); return; }
            slot.Loading = false;
            if (!CanAdvertise() || ad == null || !SafeCanShow(ad)) { DestroyUnlessRetained(ad); }
            else slot.Ad = ad;
            Changed?.Invoke();
        }

        public void Show(long requestId, AdPlacement placement, Action<AdSignal> receive)
        {
            if (receive == null) throw new ArgumentNullException(nameof(receive));
            if (GetAvailability(placement) != AdAvailability.Ready)
            { receive(new AdSignal(AdSignalKind.InfrastructureUnavailable)); return; }
            var slot = For(placement);
            var current = new Presentation { Id = requestId, Placement = placement, Ad = slot.Ad, Receive = receive };
            slot.Ad = null;
            // Guard before native Show, which can present before its managed
            // Presented callback is marshalled back to the application thread.
            active = current;
            Changed?.Invoke();
            // Changed is synchronous application code and can revoke eligibility
            // or dispose this service. No native presentation has started yet,
            // so this boundary can safely settle as infrastructure unavailable.
            if (!ReferenceEquals(active, current)) return;
            if (!CanAdvertise()) { End(current, true); return; }
            try
            {
                current.Ad.Show(() => dispatch(() => Presented(current)),
                    () => dispatch(() => Rewarded(current)),
                    () => dispatch(() => End(current, false)),
                    () => dispatch(() => End(current, true)));
            }
            catch
            {
                // A thrown native call does not prove closure. Keep the guard
                // until the bridge certifies native dismissal/failure.
                current.Receive(new AdSignal(AdSignalKind.InfrastructureUnavailable));
            }
        }

        private void Presented(Presentation current)
        {
            if (!ReferenceEquals(active, current) || current.Terminal || current.Presented) return;
            current.Presented = true;
            if (!disposed) current.Receive(new AdSignal(AdSignalKind.Presented));
            Changed?.Invoke();
        }

        private void Rewarded(Presentation current)
        {
            if (!ReferenceEquals(active, current) || current.Terminal || current.Rewarded ||
                current.Placement != AdPlacement.Hint) return;
            current.Rewarded = true;
            if (!disposed) current.Receive(new AdSignal(AdSignalKind.RewardEarned));
        }

        private void End(Presentation current, bool infrastructureFailure)
        {
            if (!ReferenceEquals(active, current) || current.Terminal) return;
            current.Terminal = true;
            active = null;
            SafeDestroy(current.Ad);
            if (!disposed) current.Receive(new AdSignal(infrastructureFailure ?
                AdSignalKind.InfrastructureUnavailable : AdSignalKind.Closed));
            Changed?.Invoke();
            if (!disposed) Prepare();
        }

        public void Cancel(long requestId)
        {
            // Google exposes no safe programmatic fullscreen close. The
            // coordinator invalidates delivery; native closure still owns unlock.
            if (active != null && active.Id == requestId) return;
        }

        public void SetPaused(bool value)
        {
            if (disposed || paused == value) return;
            paused = value;
            if (!paused) { TryShowRequiredForm(); Prepare(); }
        }

        public void ShowPrivacyOptions(Action<bool> complete)
        {
            if (complete == null) throw new ArgumentNullException(nameof(complete));
            if (!PrivacyOptionsRequired || paused || IsPresenting || IsPrivacyFormPresenting || consentPending || privacyPending)
            { complete(false); return; }
            InvalidateLoads();
            consentReady = false;
            privacyPending = true;
            privacyTimedOut = false;
            consentDeadline = clock() + InfrastructureTimeoutSeconds;
            IsPrivacyFormPresenting = true;
            Changed?.Invoke();
            // Observers may pause/dispose while reacting to the form guard.
            // Since native presentation has not begun, unwind safely here.
            if (disposed)
            {
                privacyPending = false;
                IsPrivacyFormPresenting = false;
                return;
            }
            if (paused)
            {
                privacyPending = false;
                IsPrivacyFormPresenting = false;
                complete(false);
                Changed?.Invoke();
                return;
            }
            var finished = false;
            Action<bool> finish = success =>
            {
                if (disposed || finished) return;
                finished = true;
                privacyPending = false;
                IsPrivacyFormPresenting = false;
                consentReady = success && !privacyTimedOut && SafeCanRequest();
                // A completed form can withdraw consent successfully. UI result
                // describes the form, while consentReady independently gates ads.
                complete(success && !privacyTimedOut);
                Changed?.Invoke();
                if (consentReady) Prepare();
            };
            try { bridge.ShowPrivacyOptions(success => dispatch(() => finish(success))); }
            catch { consentReady = false; privacyTimedOut = true; }
        }

        public void Tick()
        {
            if (!enabled || disposed) return;
            ReconcileOwnership();
            var now = clock();
            if ((consentPending || privacyPending) && now >= consentDeadline)
            {
                consentReady = false;
                privacyTimedOut = true;
                // A UMP update itself has no native surface; safely invalidate.
                // Forms retain their guard until the SDK confirms closure.
                if (!IsPrivacyFormPresenting)
                { requiredFormPending = consentPending = false; ++consentGeneration; }
            }
            if (initializing && now >= initializationDeadline) initializing = false;
            Expire(progression, now);
            Expire(hint, now);
            if (!SafeCanRequest()) InvalidateLoads();
        }

        private void Expire(Slot slot, double now)
        { if (slot.Loading && now >= slot.Deadline) { slot.Loading = false; ++slot.Generation; Changed?.Invoke(); } }
        private void ReconcileOwnership()
        {
            if (!enabled || disposed || !started) return;
            var owned = Owned();
            if (owned) InvalidateLoads();
            var revoked = previousOwned && !owned;
            previousOwned = owned;
            if (revoked) Prepare();
        }
        private bool CanAdvertise(bool requireInitialized = true) => enabled && started && !disposed && !paused &&
            !Owned() && consentReady && !consentPending && !privacyPending && !IsPrivacyFormPresenting &&
            (!requireInitialized || initialized) && SafeCanRequest();
        private bool Owned() { try { return isOwned(); } catch { return true; } }
        private bool SafeCanRequest() { try { return enabled && started && !disposed && bridge.CanRequestAds; } catch { return false; } }
        private bool SafePrivacyRequired() { try { return bridge.PrivacyOptionsRequired; } catch { return false; } }
        private static bool SafeCanShow(IGoogleAd ad) { try { return ad.CanShow; } catch { return false; } }
        private Slot For(AdPlacement placement) => placement == AdPlacement.Progression ? progression : hint;
        private void InvalidateLoads() { Invalidate(progression); Invalidate(hint); }
        private static void Invalidate(Slot slot)
        { ++slot.Generation; slot.Loading = false; var ad = slot.Ad; slot.Ad = null; SafeDestroy(ad); }
        private void DestroyUnlessRetained(IGoogleAd ad)
        {
            if (ad == null || ReferenceEquals(progression.Ad, ad) || ReferenceEquals(hint.Ad, ad) ||
                (active != null && ReferenceEquals(active.Ad, ad))) return;
            SafeDestroy(ad);
        }
        private static void SafeDestroy(IGoogleAd ad) { try { ad?.Destroy(); } catch { } }
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            ++consentGeneration;
            InvalidateLoads();
            // A currently presented native resource is destroyed only by End.
            Changed = null;
        }
    }
}
