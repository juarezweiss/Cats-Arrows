using System;
using System.IO;
using CatsAndArrows.Monetization;
using CatsAndArrows.UI;
using UnityEngine;
#if UNITY_EDITOR || PAWS_MONETIZATION
using CatsAndArrows.Advertising;
using CatsAndArrows.Advertising.Google;
using CatsAndArrows.Purchasing.Apple;
#endif

namespace CatsAndArrows.Production
{
    // #67: one production ledger; validation never reads or writes it.
    public sealed class ProductionMonetizationRuntime : MonoBehaviour, IPawsMonetizationRuntime
    {
        private ProductionMonetizationConfig config;
        public string PrivacyPolicyUrl => config?.privacyPolicyUrl;
        public string SupportUrl => config?.supportUrl;
#if UNITY_EDITOR || PAWS_MONETIZATION
        private MonetizationCoordinator coordinator;
        private AudienceAdService ads;
        private LocalAudiencePreference audience;
        private ApplePurchaseService purchases;
        private QueuedAdDispatcher dispatcher;
#endif
        public IAudiencePreference Audience
        {
            get
            {
#if UNITY_EDITOR || PAWS_MONETIZATION
                return audience;
#else
                return null;
#endif
            }
        }
        public IAdPrivacyService Privacy
        {
            get
            {
#if UNITY_EDITOR || PAWS_MONETIZATION
                return ads;
#else
                return null;
#endif
            }
        }
        public MonetizationCoordinator Initialize()
        {
#if PAWS_MONETIZATION
            if (coordinator != null) throw new InvalidOperationException("Production monetization is initialized once.");
            if (Debug.isDebugBuild || Application.platform != RuntimePlatform.IPhonePlayer)
                throw new InvalidOperationException("Monetized release requires a non-development iOS player.");
            var resource = Resources.Load<TextAsset>("PawsMonetization.generated");
            if (resource == null) throw new InvalidOperationException("Missing injected monetization configuration.");
            config = JsonUtility.FromJson<ProductionMonetizationConfig>(resource.text);
            config.Validate();
#if PAWS_MONETIZATION_VALIDATION
            if (config.profile != "validation") throw new InvalidOperationException("Validation profile mismatch.");
            const bool validation = true;
#else
            if (config.profile != "production") throw new InvalidOperationException("Production profile mismatch.");
            const bool validation = false;
#endif
            var directory = validation ? "monetization-validation" : "monetization";
            var store = new FileMonetizationStateStore(Path.Combine(Application.persistentDataPath, directory, "state.bin"));
            var pending = store.TryLoad(out var loaded) && loaded != null && loaded.IsValid() && loaded.PendingPurchase;
            purchases = new ApplePurchaseService(ProductionMonetizationConfig.ProductId,
                validation ? ApplePurchaseEnvironment.SandboxReview : ApplePurchaseEnvironment.Production, pending);
            dispatcher = new QueuedAdDispatcher();
            audience = new LocalAudiencePreference();
            ads = new AudienceAdService(audience, () =>
            {
#if PAWS_MONETIZATION_VALIDATION
                return new GoogleAdsBridge(config.interstitialAdUnitId, config.rewardedAdUnitId,
                config.consentDebugGeography, config.umpTestDeviceIds);
#else
                return new GoogleAdsBridge(config.interstitialAdUnitId, config.rewardedAdUnitId);
#endif
            }, () => coordinator?.IsOwned == true, dispatcher.Post);
            coordinator = new MonetizationCoordinator(true, store, ads, purchases, audience: audience);
            return coordinator;
#else
            // Rollback never opens the ledger or constructs/connects a provider.
            return null;
#endif
        }
        public void StartProviders()
        {
#if UNITY_EDITOR || PAWS_MONETIZATION
            ads?.Start();
#endif
        }
        public void Tick()
        {
#if UNITY_EDITOR || PAWS_MONETIZATION
            dispatcher?.Drain(); purchases?.Tick(); ads?.Tick();
#endif
        }
        public void SetPaused(bool paused)
        {
#if UNITY_EDITOR || PAWS_MONETIZATION
            ads?.SetPaused(paused);
#endif
        }
        public void Dispose()
        {
#if UNITY_EDITOR || PAWS_MONETIZATION
            coordinator?.Dispose(); ads?.Dispose(); purchases?.Dispose();
#endif
        }
    }
}
