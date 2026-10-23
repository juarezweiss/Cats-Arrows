#if UNITY_EDITOR || PAWS_ADMOB_REVIEW
using System;
using System.Collections.Generic;
using System.Linq;
using CatsAndArrows.Monetization;
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;
using UnityEngine;

namespace CatsAndArrows.Advertising.Google
{
    // #66: AdMob only. This binding cannot accept live IDs or run in a release player.
    public sealed class GoogleAdsBridge : IGoogleAdsBridge
    {
        public const string TestAppId = "ca-app-pub-3940256099942544~1458002511";
        public const string TestInterstitial = "ca-app-pub-3940256099942544/4411468910";
        public const string TestRewarded = "ca-app-pub-3940256099942544/1712485313";
        private readonly ConsentRequestParameters consent;
        public GoogleAdsBridge(DebugGeography geography, IEnumerable<string> testDevices)
        {
            if (!Debug.isDebugBuild || Application.platform != RuntimePlatform.IPhonePlayer)
                throw new InvalidOperationException("AdMob binding requires the explicit iOS development review build.");
            consent = new ConsentRequestParameters
            {
                ConsentDebugSettings = new ConsentDebugSettings
                {
                    DebugGeography = geography,
                    TestDeviceHashedIds = new List<string>(testDevices ?? Array.Empty<string>())
                }
            };
        }
        public bool CanRequestAds => ConsentInformation.CanRequestAds();
        public bool PrivacyOptionsRequired => ConsentInformation.PrivacyOptionsRequirementStatus == PrivacyOptionsRequirementStatus.Required;
        public void UpdateConsent(Action<bool> complete) => ConsentInformation.Update(consent, error => complete(error == null));
        public void LoadAndShowRequiredConsentForm(Action<bool> complete) => ConsentForm.LoadAndShowConsentFormIfRequired(error => complete(error == null));
        public void ShowPrivacyOptions(Action<bool> complete) => ConsentForm.ShowPrivacyOptionsForm(error => complete(error == null));
        public void Initialize(Action<bool> complete)
        {
            if (!CanRequestAds) { complete(false); return; }
            // Owner-approved development privacy policy, 2026-10-23. These switches do
            // not disable all SDK measurement, identifiers or advertising data collection.
            MobileAds.DisableSDKCrashReporting();
            MobileAds.SetiOSAppPauseOnBackground(false); // Keep our native guard/dispatcher active; real app background still suspends delivery.
            MobileAds.DisableMediationInitialization();
            MobileAds.SetRequestConfiguration(new RequestConfiguration
            {
                PublisherFirstPartyIdEnabled = false,
                PublisherPrivacyPersonalizationState = PublisherPrivacyPersonalizationState.Disabled,
                MaxAdContentRating = MaxAdContentRating.G
            });
            MobileAds.Initialize(status =>
            {
                bool ready;
                try { ready = status != null && status.getAdapterStatusMap().Values.Any(adapter => adapter.InitializationState == AdapterState.Ready); }
                catch { ready = false; }
                complete(ready);
            });
        }
        public void Load(AdPlacement placement, Action<IGoogleAd> complete)
        {
            if (!CanRequestAds) { complete(null); return; }
            var request = new AdRequest();
            request.Extras.Add("npa", "1");
            if (placement == AdPlacement.Progression)
                InterstitialAd.Load(TestInterstitial, request, (ad, error) => complete(error == null && ad != null ? new GoogleAd(ad) : null));
            else if (placement == AdPlacement.Hint)
                RewardedAd.Load(TestRewarded, request, (ad, error) => complete(error == null && ad != null ? new GoogleAd(ad) : null));
            else complete(null);
        }
        private sealed class GoogleAd : IGoogleAd
        {
            private readonly InterstitialAd interstitial;
            private readonly RewardedAd rewarded;
            public GoogleAd(InterstitialAd ad) { interstitial = ad; }
            public GoogleAd(RewardedAd ad) { rewarded = ad; }
            public bool CanShow => ConsentInformation.CanRequestAds() && (interstitial?.CanShowAd() ?? rewarded?.CanShowAd() ?? false);
            public void Show(Action presented, Action rewardEarned, Action closed, Action failed)
            {
                if (!CanShow) { failed(); return; }
                // With Google ads (no mediation), reward precedes dismissal. Neither
                // destruction, cancellation nor elapsed time implies native dismissal.
                if (interstitial != null)
                {
                    interstitial.OnAdFullScreenContentOpened += presented;
                    interstitial.OnAdFullScreenContentClosed += closed;
                    interstitial.OnAdFullScreenContentFailed += _ => failed();
                    interstitial.Show();
                }
                else
                {
                    rewarded.OnAdFullScreenContentOpened += presented;
                    rewarded.OnAdFullScreenContentClosed += closed;
                    rewarded.OnAdFullScreenContentFailed += _ => failed();
                    rewarded.Show(_ => rewardEarned());
                }
            }
            public void Destroy() { interstitial?.Destroy(); rewarded?.Destroy(); }
        }
    }
}
#endif
