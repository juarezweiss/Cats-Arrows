#if UNITY_EDITOR || PAWS_APPLE_IAP_REVIEW
using System;
using System.IO;
using CatsAndArrows.Monetization;
using CatsAndArrows.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace CatsAndArrows.Purchasing.Apple.Review
{
    // Issue #64: explicit, separate development export. Never attached to the production scene.
    [RequireComponent(typeof(UIDocument))]
    public sealed class AppleIapReviewBootstrap : MonoBehaviour
    {
        [SerializeField] private string appleProductId;
        [SerializeField] private bool simulateAskToBuy;
        private GameShellView shell;
        private ApplePurchaseService purchases;

        private void Start()
        {
            if (!Debug.isDebugBuild || Application.platform != RuntimePlatform.IPhonePlayer)
                throw new InvalidOperationException("Apple IAP review requires an explicit iOS development export.");
            var document = GetComponent<UIDocument>();
            if (document.panelSettings == null || document.panelSettings.themeStyleSheet == null)
                throw new InvalidOperationException("Apple IAP review scene is missing its panel or runtime theme.");
            Debug.Log("APPLE_IAP_REVIEW_STARTUP panel-ready");
            var store = new FileMonetizationStateStore(Path.Combine(Application.persistentDataPath, "apple-iap-review", "state.bin"));
            var recoveredPending = store.TryLoad(out var recovered) && recovered != null && recovered.IsValid() && recovered.PendingPurchase;
            purchases = new ApplePurchaseService(appleProductId, ApplePurchaseEnvironment.SandboxReview, recoveredPending, simulateAskToBuy);
            var coordinator = new MonetizationCoordinator(true, store,
                new UnavailableAdService(), purchases);
            Debug.Log("APPLE_IAP_REVIEW_STARTUP coordinator-created");
            shell = new GameShellView(unchecked((int)DateTime.UtcNow.Ticks), monetization: coordinator);
            var root = document.rootVisualElement;
            root.style.position = Position.Absolute;
            root.style.left = root.style.right = root.style.top = root.style.bottom = 0;
            root.style.flexGrow = 1;
            root.Add(shell);
            Debug.Log("APPLE_IAP_REVIEW_STARTUP shell-mounted");
        }
        private void Update() { purchases?.Tick(); shell?.TickMonetization(); shell?.UpdateSafeArea(); }
        private void OnApplicationFocus(bool focused)
        {
            shell?.SetApplicationFocus(focused);
            if (focused) shell?.RefreshMonetization(); else shell?.SuspendMonetizationDelivery();
        }
        private void OnApplicationPause(bool paused)
        {
            shell?.SetApplicationPaused(paused);
            if (paused) shell?.SuspendMonetizationDelivery(); else shell?.RefreshMonetization();
        }
        private void OnDestroy() { shell?.DisposeMonetization(); purchases?.Dispose(); }
    }
}
#endif
