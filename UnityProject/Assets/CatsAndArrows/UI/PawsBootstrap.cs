using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace CatsAndArrows.UI
{
    // Issues #63/#67: ordinary game composition with injected production services.
    [RequireComponent(typeof(UIDocument))]
    public sealed class PawsBootstrap : MonoBehaviour
    {
        private GameShellView shell;
        private IPawsMonetizationRuntime monetization;
        private bool focused = true;
        private bool paused;

        private void Start()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            root.style.position = Position.Absolute;
            root.style.left = root.style.right = root.style.top = root.style.bottom = 0;
            root.style.flexGrow = 1;
            monetization = GetComponent<IPawsMonetizationRuntime>();
#if PAWS_MONETIZATION
            if (monetization == null) throw new InvalidOperationException("Enabled production scene is missing its monetization runtime.");
#endif
            var coordinator = monetization?.Initialize();
            shell = new GameShellView(unchecked((int)DateTime.UtcNow.Ticks ^ Environment.TickCount),
                useInlineHints: true, enableTutorial: true, monetization: coordinator, adPrivacy: monetization?.Privacy, audience: monetization?.Audience,
                privacyPolicyUrl: monetization?.PrivacyPolicyUrl, supportUrl: monetization?.SupportUrl);
            root.Add(shell);
            shell.SetApplicationFocus(focused);
            shell.SetApplicationPaused(paused);
            monetization?.SetPaused(paused || !focused);
            monetization?.StartProviders();
            if (monetization?.Audience?.Current == CatsAndArrows.Monetization.AudienceBand.Unknown)
                shell.ShowAudienceSelector(true);
        }

        private void Update()
        {
            monetization?.Tick(); shell?.TickMonetization(); shell?.UpdateSafeArea();
        }
        private void OnApplicationFocus(bool value)
        {
            focused = value;
            shell?.SetApplicationFocus(value);
            UpdateLifecycle();
        }
        private void OnApplicationPause(bool value)
        {
            paused = value;
            shell?.SetApplicationPaused(value);
            UpdateLifecycle();
        }
        private void UpdateLifecycle()
        {
            monetization?.SetPaused(paused || !focused);
            if (paused || !focused) shell?.SuspendMonetizationDelivery();
            else shell?.RefreshMonetization();
        }
        private void OnDestroy() { shell?.DisposeMonetization(); monetization?.Dispose(); }
    }
}
