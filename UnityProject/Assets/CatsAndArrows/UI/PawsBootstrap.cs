using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace CatsAndArrows.UI
{
    // Issue #63: ordinary game composition; no diagnostic arguments or providers.
    [RequireComponent(typeof(UIDocument))]
    public sealed class PawsBootstrap : MonoBehaviour
    {
        private GameShellView shell;
        private bool focused = true;
        private bool paused;

        private void Start()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            root.style.position = Position.Absolute;
            root.style.left = root.style.right = root.style.top = root.style.bottom = 0;
            root.style.flexGrow = 1;
            shell = new GameShellView(unchecked((int)DateTime.UtcNow.Ticks ^ Environment.TickCount),
                useInlineHints: true, enableTutorial: true, monetization: null);
            root.Add(shell);
            shell.SetApplicationFocus(focused);
            shell.SetApplicationPaused(paused);
        }

        private void Update() => shell?.UpdateSafeArea();
        private void OnApplicationFocus(bool value)
        {
            focused = value;
            shell?.SetApplicationFocus(value);
        }
        private void OnApplicationPause(bool value)
        {
            paused = value;
            shell?.SetApplicationPaused(value);
        }
    }
}
