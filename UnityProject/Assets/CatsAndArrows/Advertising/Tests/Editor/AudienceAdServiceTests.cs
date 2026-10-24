using System;
using System.Collections.Generic;
using CatsAndArrows.Advertising;
using CatsAndArrows.Monetization;
using NUnit.Framework;

namespace CatsAndArrows.Tests
{
    public sealed class AudienceAdServiceTests
    {
        private sealed class Preference : IAudiencePreference
        {
            public AudienceBand Current { get; private set; }
            public event Action Changed;
            public void Select(AudienceBand band) { Current = band; Changed?.Invoke(); }
        }
        private sealed class Ad : IGoogleAd
        {
            public bool CanShow => true;
            public Action Closed, Reward;
            public int Destroys;
            public void Show(Action presented, Action rewardEarned, Action closed, Action failed)
            { Reward = rewardEarned; Closed = closed; presented(); }
            public void Destroy() { Destroys++; }
        }
        private sealed class Bridge : IGoogleAdsBridge
        {
            public bool CanRequestAds => true;
            public bool PrivacyOptionsRequired => true;
            public Action<bool> Update, Form, Initialized;
            public Action<IGoogleAd> Hint;
            public int Updates, Forms, Initializations, Loads;
            public void UpdateConsent(Action<bool> complete) { Updates++; Update = complete; }
            public void LoadAndShowRequiredConsentForm(Action<bool> complete) { Forms++; Form = complete; }
            public void ShowPrivacyOptions(Action<bool> complete) { Forms++; Form = complete; }
            public void Initialize(Action<bool> complete) { Initializations++; Initialized = complete; }
            public void Load(AdPlacement placement, Action<IGoogleAd> complete)
            { Loads++; if (placement == AdPlacement.Hint) Hint = complete; }
        }
        private sealed class Purchases : IPurchaseService
        {
            public event Action<PurchaseResult> Updated { add { } remove { } }
            public int Restores;
            public PurchaseOffer GetOffer() => new PurchaseOffer(true, "$1");
            public bool VerifyCachedOwnership(string storeNamespace, string evidenceReference) => true;
            public void Refresh(Action<PurchaseResult> complete) => complete(new PurchaseResult(PurchaseOutcome.Unavailable));
            public void Purchase(long requestId, Action<PurchaseResult> complete) => complete(new PurchaseResult(PurchaseOutcome.Cancelled));
            public void Restore(long requestId, Action<PurchaseResult> complete)
            { Restores++; complete(new PurchaseResult(PurchaseOutcome.Unavailable)); }
            public void Finish(string transactionReference, Action<bool> complete) => complete(true);
        }
        [TestCase(AudienceBand.Unknown)]
        [TestCase(AudienceBand.Under13)]
        [TestCase(AudienceBand.Teen)]
        [TestCase(AudienceBand.PreferNotToAnswer)]
        public void RestrictedBandsNeverConstructOrTouchGoogle(AudienceBand band)
        {
            var preference = new Preference(); preference.Select(band);
            var constructions = 0;
            using var service = new AudienceAdService(preference, () => { constructions++; return new Bridge(); }, () => false, action => action());
            service.Start(); service.SetPaused(false); service.Tick(); service.ShowPrivacyOptions(result => Assert.That(result, Is.False));
            service.Show(1, AdPlacement.Hint, signal => Assert.That(signal.Kind, Is.EqualTo(AdSignalKind.InfrastructureUnavailable)));
            Assert.That(service.GetAvailability(AdPlacement.Progression), Is.EqualTo(AdAvailability.Unavailable));
            Assert.That(service.PrivacyOptionsRequired, Is.False);
            Assert.That(constructions, Is.Zero);
        }
        [Test]
        public void AdultStartsUmpBeforeAnyAdInitializationOrRequest()
        {
            var preference = new Preference(); var bridge = new Bridge();
            using var service = new AudienceAdService(preference, () => bridge, () => false, action => action());
            service.Start(); preference.Select(AudienceBand.Adult);
            Assert.That(bridge.Updates, Is.EqualTo(1));
            Assert.That(bridge.Initializations + bridge.Loads, Is.Zero);
            bridge.Update(true); Assert.That(bridge.Forms, Is.EqualTo(1));
            bridge.Form(true); Assert.That(bridge.Initializations, Is.EqualTo(1));
            bridge.Initialized(true); Assert.That(bridge.Loads, Is.EqualTo(2));
        }
        [Test]
        public void DowngradeWhileConsentUpdatePendingStartsNoFormOrSdk()
        {
            var preference = new Preference(); preference.Select(AudienceBand.Adult); var bridge = new Bridge();
            using var service = new AudienceAdService(preference, () => bridge, () => false, action => action());
            service.Start(); preference.Select(AudienceBand.Teen); bridge.Update(true); service.Tick();
            Assert.That(bridge.Forms + bridge.Initializations + bridge.Loads, Is.Zero);
        }
        [Test]
        public void DowngradeRetainsNativeConsentGuardUntilClosureAndDoesNotInitialize()
        {
            var preference = new Preference(); preference.Select(AudienceBand.Adult); var bridge = new Bridge();
            using var service = new AudienceAdService(preference, () => bridge, () => false, action => action());
            service.Start(); bridge.Update(true); preference.Select(AudienceBand.Teen);
            Assert.That(service.IsPrivacyFormPresenting, Is.True);
            Assert.That(service.PrivacyOptionsRequired, Is.False);
            bridge.Form(true); service.Tick();
            Assert.That(service.IsPrivacyFormPresenting, Is.False);
            Assert.That(bridge.Initializations + bridge.Loads, Is.Zero);
        }
        [Test]
        public void DowngradeRetainsAdGuardUntilClosureAndPreventsCatchUpLoads()
        {
            var preference = new Preference(); preference.Select(AudienceBand.Adult); var bridge = new Bridge(); var ad = new Ad();
            var signals = new List<AdSignalKind>();
            using var service = new AudienceAdService(preference, () => bridge, () => false, action => action());
            service.Start(); bridge.Update(true); bridge.Form(true); bridge.Initialized(true); bridge.Hint(ad);
            service.Show(1, AdPlacement.Hint, signal => signals.Add(signal.Kind));
            preference.Select(AudienceBand.PreferNotToAnswer);
            Assert.That(service.IsPresenting, Is.True); Assert.That(ad.Destroys, Is.Zero);
            Assert.That(service.GetAvailability(AdPlacement.Hint), Is.EqualTo(AdAvailability.Unavailable));
            ad.Closed(); service.Tick();
            Assert.That(service.IsPresenting, Is.False); Assert.That(ad.Destroys, Is.EqualTo(1));
            Assert.That(bridge.Loads, Is.EqualTo(2));
            Assert.That(signals, Is.EqualTo(new[] { AdSignalKind.Presented, AdSignalKind.Closed }));
        }
        [Test]
        public void ReAdultWaitsForOldNativeClosureBeforeConstructingAnotherBridge()
        {
            var preference = new Preference(); preference.Select(AudienceBand.Adult); var bridge = new Bridge(); var count = 0;
            using var service = new AudienceAdService(preference, () => { count++; return bridge; }, () => false, action => action());
            service.Start(); bridge.Update(true);
            var closeOld = bridge.Form;
            preference.Select(AudienceBand.Under13); preference.Select(AudienceBand.Adult);
            Assert.That(count, Is.EqualTo(1)); closeOld(true);
            Assert.That(count, Is.EqualTo(2)); Assert.That(bridge.Initializations, Is.Zero);
        }
        [Test]
        public void AudienceChangeDoesNotResetOwnershipCountersOrDisableRestore()
        {
            var preference = new Preference(); var store = new MemoryMonetizationStateStore();
            var state = new MonetizationState { Revision = 1, OrdinaryBoardsCompleted = 7, HandledProgressionMilestone = 1,
                Access = AccessState.Owned, StoreNamespace = "verified", EvidenceReference = "proof" };
            Assert.That(store.TryCommit(state, 0), Is.True);
            var purchases = new Purchases();
            using var service = new AudienceAdService(preference, () => new Bridge(), () => true, action => action());
            using var coordinator = new MonetizationCoordinator(true, store, service, purchases);
            service.Start(); preference.Select(AudienceBand.Adult); preference.Select(AudienceBand.Teen);
            coordinator.RestorePurchases();
            Assert.That(coordinator.IsOwned, Is.True);
            Assert.That(coordinator.Snapshot.OrdinaryBoardsCompleted, Is.EqualTo(7));
            Assert.That(coordinator.Snapshot.HandledProgressionMilestone, Is.EqualTo(1));
            Assert.That(purchases.Restores, Is.EqualTo(1));
        }
    }
}
