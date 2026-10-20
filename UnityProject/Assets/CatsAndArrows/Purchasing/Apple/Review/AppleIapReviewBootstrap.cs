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
        private GameShellView shell;
        private ApplePurchaseService purchases;

        private void Start()
        {
            if (!Debug.isDebugBuild || Application.platform != RuntimePlatform.IPhonePlayer)
                throw new InvalidOperationException("Apple IAP review requires an explicit iOS development export.");
            purchases = new ApplePurchaseService(appleProductId, ApplePurchaseEnvironment.SandboxReview);
            var coordinator = new MonetizationCoordinator(true,
                new FileMonetizationStateStore(Path.Combine(Application.persistentDataPath, "apple-iap-review", "state.bin")),
                new UnavailableAdService(), purchases);
            shell = new GameShellView(unchecked((int)DateTime.UtcNow.Ticks), monetization: coordinator);
            var root = GetComponent<UIDocument>().rootVisualElement;
            root.style.position = Position.Absolute;
            root.style.left = root.style.right = root.style.top = root.style.bottom = 0;
            root.Add(shell);
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
