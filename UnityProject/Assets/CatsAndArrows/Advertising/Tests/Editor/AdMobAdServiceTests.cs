using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CatsAndArrows.Advertising;
using CatsAndArrows.Monetization;
using NUnit.Framework;

namespace CatsAndArrows.Tests
{
    public sealed class AdMobAdServiceTests
    {
        private sealed class FakeAd : IGoogleAd
        {
            public bool CanShow { get; set; } = true;
            public int Shows, Destroys;
            public bool ThrowOnShow;
            public Action Presented, Reward, Closed, Failed;
            public void Show(Action presented, Action rewardEarned, Action closed, Action failed)
            {
                Shows++; Presented = presented; Reward = rewardEarned; Closed = closed; Failed = failed;
                if (ThrowOnShow) throw new InvalidOperationException();
            }
            public void Destroy() { Destroys++; }
        }
        private sealed class FakeBridge : IGoogleAdsBridge
        {
            public bool CanRequestAds { get; set; } = true;
            public bool PrivacyOptionsRequired { get; set; }
            public int Updates, Forms, PrivacyForms, Initializations;
            public bool ThrowOnForm, ThrowOnPrivacy;
            public int Loads => LoadCalls.Count;
            public Action<bool> Update, Form, Privacy, Initialization;
            public readonly List<(AdPlacement Placement, Action<IGoogleAd> Complete)> LoadCalls =
                new List<(AdPlacement, Action<IGoogleAd>)>();
            public void UpdateConsent(Action<bool> complete) { Updates++; Update = complete; }
            public void LoadAndShowRequiredConsentForm(Action<bool> complete)
            { Forms++; Form = complete; if (ThrowOnForm) throw new InvalidOperationException(); }
            public void ShowPrivacyOptions(Action<bool> complete)
            { PrivacyForms++; Privacy = complete; if (ThrowOnPrivacy) throw new InvalidOperationException(); }
            public void Initialize(Action<bool> complete) { Initializations++; Initialization = complete; }
            public void Load(AdPlacement placement, Action<IGoogleAd> complete) { LoadCalls.Add((placement, complete)); }
        }
        private FakeBridge bridge;
        private QueuedAdDispatcher dispatcher;
        private AdMobAdService service;
        private bool owned;
        private double now;
        private readonly List<AdSignalKind> signals = new List<AdSignalKind>();

        [SetUp] public void SetUp()
        {
            bridge = new FakeBridge(); dispatcher = new QueuedAdDispatcher();
            owned = false; now = 0; signals.Clear();
            service = Create();
        }
        [TearDown] public void TearDown() { service.Dispose(); }
        private AdMobAdService Create(bool enabled = true) => new AdMobAdService(enabled, bridge,
            () => owned, dispatcher.Post, () => now);
        private void Drain() => dispatcher.Drain();
        private void StartAndConsent()
        { service.Start(); bridge.Update(true); Drain(); bridge.Form(true); Drain(); }
        private void Initialize()
        { StartAndConsent(); bridge.Initialization(true); Drain(); }
        private FakeAd Ready(AdPlacement placement = AdPlacement.Hint)
        {
            Initialize();
            var ad = new FakeAd();
            bridge.LoadCalls.Find(call => call.Placement == placement).Complete(ad); Drain();
            Assert.That(service.GetAvailability(placement), Is.EqualTo(AdAvailability.Ready));
            return ad;
        }
        private void Show(AdPlacement placement = AdPlacement.Hint) => service.Show(7, placement, signal => signals.Add(signal.Kind));

        [Test] public void DisabledNeverTouchesSdkEvenWhenAllEntryPointsAreCalled()
        {
            service.Dispose(); service = Create(false);
            service.Start(); service.Prepare(); service.SetPaused(true); service.SetPaused(false); service.Tick();
            Show(); service.ShowPrivacyOptions(_ => { });
            Assert.That(service.PrivacyOptionsRequired, Is.False);
            Assert.That(bridge.Updates + bridge.Forms + bridge.Initializations + bridge.Loads + bridge.PrivacyForms, Is.Zero);
            Assert.That(signals, Is.EqualTo(new[] { AdSignalKind.InfrastructureUnavailable }));
        }
        [Test] public void ConsentUpdateRunsOncePerServiceLaunchAndBeforeInitializationOrLoads()
        {
            service.Start(); service.Start(); service.Prepare();
            Assert.That(bridge.Updates, Is.EqualTo(1));
            Assert.That(bridge.Initializations + bridge.Loads, Is.Zero);
            bridge.Update(true); Assert.That(bridge.Forms, Is.Zero); Drain();
            Assert.That(service.IsPrivacyFormPresenting, Is.True);
            Assert.That(bridge.Initializations, Is.Zero);
            bridge.Form(true); Drain();
            Assert.That(service.IsPrivacyFormPresenting, Is.False);
            Assert.That(bridge.Initializations, Is.EqualTo(1));
        }
        [Test] public void DuplicateConsentCallbackCannotOpenAnotherForm()
        {
            service.Start(); bridge.Update(true); bridge.Update(true); Drain();
            Assert.That(bridge.Forms, Is.EqualTo(1));
            bridge.Form(true); bridge.Form(true); Drain();
            Assert.That(bridge.Initializations, Is.EqualTo(1));
        }
        [Test] public void ConsentFailureIsUnavailableWithoutInitialization()
        {
            service.Start(); bridge.Update(false); Drain(); service.Prepare(); Show();
            Assert.That(bridge.Forms + bridge.Initializations + bridge.Loads, Is.Zero);
            Assert.That(signals, Is.EqualTo(new[] { AdSignalKind.InfrastructureUnavailable }));
        }
        [Test] public void RequiredFormFailureDoesNotInitializeOrRequestAds()
        {
            service.Start(); bridge.Update(true); Drain(); bridge.Form(false); Drain(); service.Prepare();
            Assert.That(bridge.Initializations + bridge.Loads, Is.Zero);
        }
        [Test] public void CanRequestAdsFalseGatesSuccessfulConsent()
        {
            bridge.CanRequestAds = false; StartAndConsent();
            Assert.That(bridge.Initializations + bridge.Loads, Is.Zero);
            Assert.That(service.GetAvailability(AdPlacement.Hint), Is.EqualTo(AdAvailability.Unavailable));
        }
        [Test] public void InitializationFailureDoesNotLoop()
        {
            StartAndConsent(); bridge.Initialization(false); Drain();
            for (var i = 0; i < 5; i++) { service.Prepare(); service.Tick(); }
            Assert.That(bridge.Initializations, Is.EqualTo(1)); Assert.That(bridge.Loads, Is.Zero);
        }
        [Test] public void InitializationTimeoutIgnoresLateSuccess()
        {
            StartAndConsent(); now = 31; service.Tick(); bridge.Initialization(true); Drain();
            Assert.That(bridge.Loads, Is.Zero);
        }
        [Test] public void ConsentUpdateTimeoutIgnoresLateSuccess()
        {
            service.Start(); now = 31; service.Tick(); bridge.Update(true); Drain();
            Assert.That(bridge.Forms + bridge.Initializations + bridge.Loads, Is.Zero);
        }
        [Test] public void FormTimeoutNeverPretendsNativeFormClosed()
        {
            service.Start(); bridge.Update(true); Drain(); now = 31; service.Tick();
            Assert.That(service.IsPrivacyFormPresenting, Is.True);
            Assert.That(bridge.Initializations, Is.Zero);
            bridge.Form(true); Drain();
            Assert.That(service.IsPrivacyFormPresenting, Is.False);
            Assert.That(bridge.Initializations, Is.Zero);
        }
        [Test] public void SuccessfulInitializationPreloadsBothFormatsWithoutShowing()
        {
            Initialize();
            Assert.That(bridge.Loads, Is.EqualTo(2));
            Assert.That(bridge.LoadCalls[0].Placement, Is.EqualTo(AdPlacement.Progression));
            Assert.That(bridge.LoadCalls[1].Placement, Is.EqualTo(AdPlacement.Hint));
            Assert.That(service.IsPresenting, Is.False);
        }
        [Test] public void NoFillRemainsUnavailableAndDoesNotRetryOnTick()
        {
            Initialize(); foreach (var load in bridge.LoadCalls) load.Complete(null); Drain();
            for (var i = 0; i < 5; i++) service.Tick();
            Assert.That(bridge.Loads, Is.EqualTo(2)); Show();
            Assert.That(signals, Is.EqualTo(new[] { AdSignalKind.InfrastructureUnavailable }));
        }
        [Test] public void UnknownPlacementCannotShowEitherFormat()
        {
            var ad = Ready(); Show((AdPlacement)999);
            Assert.That(ad.Shows, Is.Zero);
            Assert.That(signals, Is.EqualTo(new[] { AdSignalKind.InfrastructureUnavailable }));
        }
        [Test] public void ExpiredLoadedAdIsDestroyedAndRequiresExplicitPreloadRetry()
        {
            var ad = Ready(); ad.CanShow = false;
            Assert.That(service.GetAvailability(AdPlacement.Hint), Is.EqualTo(AdAvailability.Unavailable));
            Assert.That(ad.Destroys, Is.EqualTo(1)); Assert.That(bridge.Loads, Is.EqualTo(2));
            service.Prepare(); Assert.That(bridge.Loads, Is.EqualTo(3));
        }
        [Test] public void TimedOutLoadDestroysLateResourceAndCannotBecomeReady()
        {
            Initialize(); now = 31; service.Tick();
            var late = new FakeAd(); bridge.LoadCalls[1].Complete(late); Drain();
            Assert.That(late.Destroys, Is.EqualTo(1));
            Assert.That(service.GetAvailability(AdPlacement.Hint), Is.EqualTo(AdAvailability.Unavailable));
        }
        [Test] public void RetriedLoadGenerationRejectsOlderCallback()
        {
            Initialize(); now = 31; service.Tick(); service.Prepare();
            var old = new FakeAd(); var fresh = new FakeAd();
            bridge.LoadCalls[1].Complete(old); bridge.LoadCalls[3].Complete(fresh); Drain(); Show();
            Assert.That(old.Destroys, Is.EqualTo(1)); Assert.That(old.Shows, Is.Zero);
            Assert.That(fresh.Shows, Is.EqualTo(1));
        }
        [Test] public void ConsentGateIsRecheckedBeforeLoadAndShow()
        {
            var ad = Ready(); bridge.CanRequestAds = false;
            Show(); service.Prepare(); service.Tick();
            Assert.That(ad.Shows, Is.Zero); Assert.That(ad.Destroys, Is.EqualTo(1));
            Assert.That(bridge.Loads, Is.EqualTo(2));
        }
        [Test] public void ConsentRevokedDuringLoadDestroysResult()
        {
            Initialize(); bridge.CanRequestAds = false;
            var ad = new FakeAd(); bridge.LoadCalls[1].Complete(ad); Drain();
            Assert.That(ad.Destroys, Is.EqualTo(1));
        }
        [Test] public void VerifiedOwnerStartsConsentButNeverInitializesOrLoadsAds()
        {
            owned = true; StartAndConsent(); service.Prepare(); Show();
            Assert.That(bridge.Initializations + bridge.Loads, Is.Zero);
        }
        [Test] public void OwnershipChangeDestroysLoadedFormatsAndPendingResults()
        {
            Initialize(); var progression = new FakeAd(); bridge.LoadCalls[0].Complete(progression); Drain();
            owned = true; service.Tick();
            var lateHint = new FakeAd(); bridge.LoadCalls[1].Complete(lateHint); Drain();
            Assert.That(progression.Destroys, Is.EqualTo(1)); Assert.That(lateHint.Destroys, Is.EqualTo(1));
            Assert.That(service.GetAvailability(AdPlacement.Progression), Is.EqualTo(AdAvailability.Unavailable));
            Assert.That(service.GetAvailability(AdPlacement.Hint), Is.EqualTo(AdAvailability.Unavailable));
        }
        [Test] public void OwnershipChangeCannotDestroyOrUnlockPresentedNativeAd()
        {
            var ad = Ready(); Show(); owned = true; service.Tick(); service.Cancel(7);
            Assert.That(service.IsPresenting, Is.True); Assert.That(ad.Destroys, Is.Zero);
            ad.Closed(); Drain();
            Assert.That(service.IsPresenting, Is.False); Assert.That(ad.Destroys, Is.EqualTo(1));
            Assert.That(bridge.Loads, Is.EqualTo(2));
        }
        [Test] public void RevokedOwnershipCanPreloadWithoutAutomaticallyShowing()
        {
            owned = true; StartAndConsent(); owned = false; service.Tick();
            Assert.That(bridge.Initializations, Is.EqualTo(1));
            bridge.Initialization(true); Drain(); Assert.That(bridge.Loads, Is.EqualTo(2));
            Assert.That(service.IsPresenting, Is.False);
        }
        [Test] public void PresentationGuardBeginsBeforePresentedCallback()
        {
            var ad = Ready(); Show(); Assert.That(service.IsPresenting, Is.True);
            Assert.That(signals, Is.Empty); ad.Presented(); Assert.That(signals, Is.Empty); Drain();
            Assert.That(signals, Is.EqualTo(new[] { AdSignalKind.Presented }));
        }
        [Test] public void ReentrantOwnershipChangeBeforeNativeShowSuppressesPresentation()
        {
            var ad = Ready();
            service.Changed += () => { if (service.IsPresenting) owned = true; };
            Show();
            Assert.That(ad.Shows, Is.Zero); Assert.That(ad.Destroys, Is.EqualTo(1));
            Assert.That(service.IsPresenting, Is.False);
            Assert.That(signals, Is.EqualTo(new[] { AdSignalKind.InfrastructureUnavailable }));
        }
        [Test] public void ReentrantPauseBeforeNativeShowSuppressesPresentation()
        {
            var ad = Ready();
            service.Changed += () => { if (service.IsPresenting) service.SetPaused(true); };
            Show();
            Assert.That(ad.Shows, Is.Zero); Assert.That(ad.Destroys, Is.EqualTo(1));
            Assert.That(service.IsPresenting, Is.False);
            Assert.That(signals, Is.EqualTo(new[] { AdSignalKind.InfrastructureUnavailable }));
        }
        [Test] public void ReentrantConsentChangeBeforeNativeShowSuppressesPresentation()
        {
            var ad = Ready();
            service.Changed += () => { if (service.IsPresenting) bridge.CanRequestAds = false; };
            Show();
            Assert.That(ad.Shows, Is.Zero); Assert.That(ad.Destroys, Is.EqualTo(1));
            Assert.That(service.IsPresenting, Is.False);
            Assert.That(signals, Is.EqualTo(new[] { AdSignalKind.InfrastructureUnavailable }));
        }
        [Test] public void ReentrantDisposeBeforeNativeShowSuppressesPresentationAndCallbacks()
        {
            var ad = Ready();
            service.Changed += () => { if (service.IsPresenting) service.Dispose(); };
            Show();
            Assert.That(ad.Shows, Is.Zero); Assert.That(ad.Destroys, Is.EqualTo(1));
            Assert.That(service.IsPresenting, Is.False); Assert.That(signals, Is.Empty);
        }
        [Test] public void ConfirmedRewardAndCloseAreDeliveredExactlyOnce()
        {
            var ad = Ready(); Show(); ad.Presented(); ad.Presented(); ad.Reward(); ad.Reward(); ad.Closed(); ad.Closed(); Drain();
            Assert.That(signals, Is.EqualTo(new[] { AdSignalKind.Presented, AdSignalKind.RewardEarned, AdSignalKind.Closed }));
            Assert.That(ad.Destroys, Is.EqualTo(1)); Assert.That(service.IsPresenting, Is.False);
        }
        [Test] public void VoluntaryCloseWithoutRewardNeverMeansInfrastructureFailure()
        {
            var ad = Ready(); Show(); ad.Presented(); ad.Closed(); Drain();
            Assert.That(signals, Is.EqualTo(new[] { AdSignalKind.Presented, AdSignalKind.Closed }));
        }
        [Test] public void RewardAfterSettledNativeClosureCannotMintGrant()
        {
            var ad = Ready(); Show(); ad.Closed(); ad.Reward(); Drain();
            Assert.That(signals, Is.EqualTo(new[] { AdSignalKind.Closed }));
        }
        [Test] public void ShowFailureIsInfrastructureUnavailableAfterSafeNativeClosure()
        {
            var ad = Ready(); Show(); ad.Failed(); Drain();
            Assert.That(service.IsPresenting, Is.False);
            Assert.That(signals, Is.EqualTo(new[] { AdSignalKind.InfrastructureUnavailable }));
        }
        [Test] public void PresentedInfrastructureFailureReleasesOnlyWithNativeFailureEvidence()
        {
            var ad = Ready(); Show(); ad.Presented(); Drain();
            service.Cancel(7); now = 300; service.Tick(); Assert.That(service.IsPresenting, Is.True);
            ad.Failed(); Drain();
            Assert.That(signals, Is.EqualTo(new[] { AdSignalKind.Presented, AdSignalKind.InfrastructureUnavailable }));
            Assert.That(service.IsPresenting, Is.False);
        }
        [Test] public void NativeShowExceptionDoesNotProveSafeClosure()
        {
            var ad = Ready(); ad.ThrowOnShow = true; Show();
            Assert.That(service.IsPresenting, Is.True); Assert.That(ad.Destroys, Is.Zero);
            Assert.That(signals, Is.EqualTo(new[] { AdSignalKind.InfrastructureUnavailable }));
            ad.Closed(); Drain(); Assert.That(service.IsPresenting, Is.False);
        }
        [Test] public void InterstitialRewardCallbackNeverAuthorizesHint()
        {
            var ad = Ready(AdPlacement.Progression); Show(AdPlacement.Progression);
            ad.Presented(); ad.Reward(); ad.Closed(); Drain();
            Assert.That(signals, Is.EqualTo(new[] { AdSignalKind.Presented, AdSignalKind.Closed }));
        }
        [Test] public void LateOldPresentationCallbacksCannotAffectNewRequest()
        {
            var old = Ready(); Show(); old.Closed(); Drain();
            var next = new FakeAd(); bridge.LoadCalls[2].Complete(next); Drain();
            service.Show(8, AdPlacement.Hint, signal => signals.Add(signal.Kind));
            old.Reward(); old.Failed(); old.Presented(); Drain();
            Assert.That(service.IsPresenting, Is.True); Assert.That(signals, Is.EqualTo(new[] { AdSignalKind.Closed }));
            next.Closed(); Drain(); Assert.That(signals, Has.Count.EqualTo(2));
        }
        [Test] public void DuplicateLoadCallbackCannotDestroyAcceptedResource()
        {
            var ad = Ready(); bridge.LoadCalls[1].Complete(ad); Drain(); Show();
            bridge.LoadCalls[1].Complete(ad); Drain();
            Assert.That(ad.Destroys, Is.Zero); Assert.That(ad.Shows, Is.EqualTo(1));
        }
        [Test] public void PrivacyOptionsInvalidateAdsAndRecheckConsentBeforeNewLoads()
        {
            var ad = Ready(); bridge.PrivacyOptionsRequired = true; bool? result = null;
            service.ShowPrivacyOptions(success => result = success);
            Assert.That(service.IsPrivacyFormPresenting, Is.True); Assert.That(ad.Destroys, Is.EqualTo(1));
            Assert.That(service.GetAvailability(AdPlacement.Hint), Is.EqualTo(AdAvailability.Unavailable));
            bridge.CanRequestAds = false; bridge.Privacy(true); Drain();
            Assert.That(result, Is.True); Assert.That(service.IsPrivacyFormPresenting, Is.False);
            Assert.That(bridge.Loads, Is.EqualTo(2));
        }
        [Test] public void SuccessfulPrivacyOptionsPreloadAgainButNeverShow()
        {
            Ready(); bridge.PrivacyOptionsRequired = true; var calls = 0;
            service.ShowPrivacyOptions(success => { Assert.That(success, Is.True); calls++; });
            bridge.Privacy(true); bridge.Privacy(true); Drain();
            Assert.That(calls, Is.EqualTo(1)); Assert.That(bridge.Loads, Is.EqualTo(4));
            Assert.That(service.IsPresenting, Is.False);
        }
        [Test] public void ReentrantPauseBeforeNativePrivacyOptionsSuppressesFormAndCompletesFailure()
        {
            Ready(); bridge.PrivacyOptionsRequired = true; bool? result = null;
            service.Changed += () => { if (service.IsPrivacyFormPresenting) service.SetPaused(true); };
            service.ShowPrivacyOptions(success => result = success);
            Assert.That(bridge.PrivacyForms, Is.Zero); Assert.That(result, Is.False);
            Assert.That(service.IsPrivacyFormPresenting, Is.False); Assert.That(bridge.Loads, Is.EqualTo(2));
        }
        [Test] public void ReentrantDisposeBeforeNativePrivacyOptionsSuppressesFormAndCallback()
        {
            Ready(); bridge.PrivacyOptionsRequired = true; bool? result = null;
            service.Changed += () => { if (service.IsPrivacyFormPresenting) service.Dispose(); };
            service.ShowPrivacyOptions(success => result = success);
            Assert.That(bridge.PrivacyForms, Is.Zero); Assert.That(result, Is.Null);
            Assert.That(service.IsPrivacyFormPresenting, Is.False);
        }
        [Test] public void PrivacyOptionsCannotOpenOverAdOrConsentForm()
        {
            bridge.PrivacyOptionsRequired = true; var ad = Ready(); Show();
            service.ShowPrivacyOptions(success => Assert.That(success, Is.False));
            Assert.That(bridge.PrivacyForms, Is.Zero); ad.Closed(); Drain();
        }
        [Test] public void PrivacyTimeoutDoesNotUnlockNativeFormAndLateClosureRemainsUnavailable()
        {
            Ready(); bridge.PrivacyOptionsRequired = true; bool? result = null;
            service.ShowPrivacyOptions(success => result = success); now = 31; service.Tick();
            Assert.That(service.IsPrivacyFormPresenting, Is.True); Assert.That(result, Is.Null);
            bridge.Privacy(true); Drain(); Assert.That(result, Is.False);
            Assert.That(service.IsPrivacyFormPresenting, Is.False); Assert.That(bridge.Loads, Is.EqualTo(2));
        }
        [Test] public void RequiredNativeFormExceptionDoesNotProveClosure()
        {
            bridge.ThrowOnForm = true; service.Start(); bridge.Update(true); Drain();
            Assert.That(service.IsPrivacyFormPresenting, Is.True);
            bridge.Form(false); Drain(); Assert.That(service.IsPrivacyFormPresenting, Is.False);
            Assert.That(bridge.Initializations, Is.Zero);
        }
        [Test] public void PrivacyNativeFormExceptionDoesNotProveClosure()
        {
            Ready(); bridge.PrivacyOptionsRequired = true; bridge.ThrowOnPrivacy = true;
            bool? result = null; service.ShowPrivacyOptions(success => result = success);
            Assert.That(service.IsPrivacyFormPresenting, Is.True); Assert.That(result, Is.Null);
            bridge.Privacy(false); Drain();
            Assert.That(service.IsPrivacyFormPresenting, Is.False); Assert.That(result, Is.False);
        }
        [Test] public void PausePreventsNewPresentationWithoutInventingNativeClosure()
        {
            var ad = Ready(); Show(); ad.Presented(); Drain(); service.SetPaused(true);
            now = 100; service.Tick(); service.Cancel(7); Assert.That(service.IsPresenting, Is.True);
            ad.Reward(); ad.Closed(); Drain(); Assert.That(service.IsPresenting, Is.False);
            Assert.That(signals, Is.EqualTo(new[] { AdSignalKind.Presented, AdSignalKind.RewardEarned, AdSignalKind.Closed }));
            Assert.That(bridge.Loads, Is.EqualTo(2)); service.SetPaused(false);
            Assert.That(bridge.Loads, Is.EqualTo(4)); Assert.That(service.IsPresenting, Is.False);
        }
        [Test] public void ConsentFormPauseResumeKeepsFormUntilRealClosure()
        {
            service.Start(); bridge.Update(true); Drain(); service.SetPaused(true); service.SetPaused(false);
            Assert.That(service.IsPrivacyFormPresenting, Is.True); Assert.That(bridge.Forms, Is.EqualTo(1));
            bridge.Form(true); Drain(); Assert.That(service.IsPrivacyFormPresenting, Is.False);
        }
        [Test] public void SuccessfulConsentUpdateWhilePausedDefersFormUntilForegroundExactlyOnce()
        {
            service.Start(); service.SetPaused(true); bridge.Update(true); Drain();
            Assert.That(bridge.Updates, Is.EqualTo(1)); Assert.That(bridge.Forms, Is.Zero);
            Assert.That(service.IsPrivacyFormPresenting, Is.False);
            Assert.That(bridge.Initializations + bridge.Loads, Is.Zero);
            service.SetPaused(false); service.SetPaused(false); bridge.Update(true); Drain();
            Assert.That(bridge.Forms, Is.EqualTo(1)); Assert.That(service.IsPrivacyFormPresenting, Is.True);
            bridge.Form(true); Drain(); Assert.That(bridge.Initializations, Is.EqualTo(1));
        }
        [Test] public void StartupWhilePausedStillUpdatesConsentAndDefersRequiredForm()
        {
            service.SetPaused(true); service.Start(); bridge.Update(true); Drain();
            Assert.That(bridge.Updates, Is.EqualTo(1)); Assert.That(bridge.Forms, Is.Zero);
            service.SetPaused(false); Assert.That(bridge.Forms, Is.EqualTo(1));
        }
        [TestCase(true)]
        [TestCase(false)]
        public void DeferredConsentFormHonorsOriginalDeadlineBeforeResume(bool tickWhilePaused)
        {
            service.Start(); service.SetPaused(true); bridge.Update(true); Drain(); now = 31;
            if (tickWhilePaused) service.Tick();
            service.SetPaused(false); service.Prepare();
            Assert.That(bridge.Forms + bridge.Initializations + bridge.Loads, Is.Zero);
            Assert.That(service.IsPrivacyFormPresenting, Is.False);
        }
        [Test] public void DisposeWaitsForNativeClosureAndDoesNotDeliverCallbacks()
        {
            var ad = Ready(); Show(); service.Dispose(); Assert.That(ad.Destroys, Is.Zero);
            ad.Presented(); ad.Reward(); ad.Closed(); Drain();
            Assert.That(signals, Is.Empty); Assert.That(ad.Destroys, Is.EqualTo(1));
        }
        [Test] public void DispatcherMarshalsWorkerCallbacksUntilApplicationDrain()
        {
            service.Start(); Task.Run(() => bridge.Update(true)).GetAwaiter().GetResult();
            Assert.That(bridge.Forms, Is.Zero); Drain(); Assert.That(bridge.Forms, Is.EqualTo(1));
        }
    }
}
