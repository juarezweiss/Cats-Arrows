#if UNITY_EDITOR || PAWS_ADMOB_REVIEW
using System;
using System.IO;
using CatsAndArrows.Advertising.Google;
using CatsAndArrows.Monetization;
using CatsAndArrows.UI;
using GoogleMobileAds.Ump.Api;
using UnityEngine;
using UnityEngine.UIElements;

namespace CatsAndArrows.Advertising.Review
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class AdMobReviewBootstrap : MonoBehaviour
    {
        [SerializeField] private DebugGeography geography = DebugGeography.EEA;
        [SerializeField] private string[] testDevices = Array.Empty<string>();
        private GameShellView shell;
        private AdMobAdService ads;
        private QueuedAdDispatcher dispatcher;
        private bool paused;
        private bool focused = true;
        private void Start()
        {
            if (!Debug.isDebugBuild || Application.platform != RuntimePlatform.IPhonePlayer)
                throw new InvalidOperationException("Use the explicit AdMob iOS development export.");
            var document = GetComponent<UIDocument>();
            if (document.panelSettings == null || document.panelSettings.themeStyleSheet == null)
                throw new InvalidOperationException("AdMob review requires the production panel/theme.");
            var directory = Path.Combine(Application.persistentDataPath, "admob-review");
            dispatcher = new QueuedAdDispatcher();
            MonetizationCoordinator coordinator = null;
            ads = new AdMobAdService(true, new GoogleAdsBridge(geography, testDevices),
                () => coordinator?.IsOwned == true, dispatcher.Post);
            coordinator = new MonetizationCoordinator(true, new FileMonetizationStateStore(Path.Combine(directory, "state.bin")),
                ads, new ReviewPurchaseService(Path.Combine(directory, "mock-ownership.bin")));
            shell = new GameShellView(unchecked((int)DateTime.UtcNow.Ticks), monetization: coordinator, adPrivacy: ads);
            var root = document.rootVisualElement;
            root.style.position = Position.Absolute;
            root.style.left = root.style.right = root.style.top = root.style.bottom = 0;
            root.style.flexGrow = 1; root.Add(shell);
            shell.SetApplicationFocus(focused); shell.SetApplicationPaused(paused);
            ads.SetPaused(paused || !focused); ads.Start();
        }
        private void Update()
        {
            dispatcher?.Drain(); ads?.Tick(); shell?.TickMonetization(); shell?.UpdateSafeArea();
        }
        private void OnApplicationFocus(bool value)
        {
            focused = value; shell?.SetApplicationFocus(value); ads?.SetPaused(paused || !focused);
            if (!value) shell?.SuspendMonetizationDelivery(); else shell?.RefreshMonetization();
        }
        private void OnApplicationPause(bool value)
        {
            paused = value; shell?.SetApplicationPaused(value); ads?.SetPaused(paused || !focused);
            if (value) shell?.SuspendMonetizationDelivery(); else shell?.RefreshMonetization();
        }
        private void OnDestroy() { shell?.DisposeMonetization(); ads?.Dispose(); }
    }
}
#endif
