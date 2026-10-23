using System;
using System.Collections;
using CatsAndArrows.Monetization;
using CatsAndArrows.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace CatsAndArrows.Tests
{
    public sealed class AdPrivacyShellTests
    {
        private GameObject host;
        private PanelSettings panel;
        private RenderTexture texture;
        private GameShellView shell;
        private Privacy privacy;
        private IEnumerator Mount(int width = 390, int height = 844, string locale = "en")
        {
            privacy = new Privacy();
            panel = ScriptableObject.CreateInstance<PanelSettings>();
            panel.hideFlags = HideFlags.HideAndDontSave;
            panel.scaleMode = PanelScaleMode.ConstantPixelSize; panel.scale = 1;
            texture = new RenderTexture(width, height, 0); panel.targetTexture = texture;
            host = new GameObject("issue66-privacy", typeof(UIDocument)); host.hideFlags = HideFlags.HideAndDontSave;
            var document = host.GetComponent<UIDocument>(); document.panelSettings = panel;
            document.rootVisualElement.style.width = width; document.rootVisualElement.style.height = height;
            shell = new GameShellView(14, locale, PlayTestFactory.Loop(), enableTutorial: false,
                tutorialSeen: () => true, markTutorialSeen: () => { }, adPrivacy: privacy);
            document.rootVisualElement.Add(shell); shell.ShowSettings();
            yield return null; yield return null;
        }
        [TearDown] public void Cleanup()
        {
            shell?.DisposeMonetization(); shell = null;
            if (host != null) UnityEngine.Object.DestroyImmediate(host);
            if (panel != null) UnityEngine.Object.DestroyImmediate(panel);
            if (texture != null) UnityEngine.Object.DestroyImmediate(texture);
        }
        [UnityTest] public IEnumerator PrivacyEntryUsesExistingScrollableListAcrossLocalesAndPortraitSizes()
        {
            foreach (var locale in new[] { "en", "pt-BR", "es" })
            foreach (var size in new[] { new Vector2Int(320, 568), new Vector2Int(390, 844), new Vector2Int(430, 932) })
            {
                yield return Mount(size.x, size.y, locale);
                var button = shell.Q<Button>("settings-privacy-options");
                Assert.That(button, Is.Not.Null, locale);
                Assert.That(button.text, Is.EqualTo(locale == "en" ? "Privacy Options" : locale == "pt-BR" ? "Opções de privacidade" : "Opciones de privacidad"));
                Assert.That(button.resolvedStyle.minHeight.value, Is.GreaterThanOrEqualTo(56));
                var scroll = shell.Q<ScrollView>("settings-scroll"); scroll.ScrollTo(button);
                yield return null; yield return null;
                Assert.That(button.worldBound.xMin, Is.GreaterThanOrEqualTo(0));
                Assert.That(button.worldBound.xMax, Is.LessThanOrEqualTo(size.x + 1));
                Assert.That(button.worldBound.yMax, Is.LessThanOrEqualTo(size.y + 1));
                Cleanup();
            }
        }
        [UnityTest] public IEnumerator UnrequiredPrivacyEntryIsHiddenAndProviderNeverInvoked()
        {
            yield return Mount(); privacy.Required = false; privacy.Notify();
            Assert.That(shell.Q<Button>("settings-privacy-options"), Is.Null);
            shell.ShowPrivacyOptions(); Assert.That(privacy.Calls, Is.Zero);
        }
        [UnityTest] public IEnumerator PrivacyNativeFormSerializesActionsAndRestoresSettingsAfterSuccessfulWithdrawal()
        {
            yield return Mount(); shell.ShowPrivacyOptions(); shell.ShowPrivacyOptions();
            Assert.That(privacy.Calls, Is.EqualTo(1));
            Assert.That(shell.Q<Button>("settings-language").enabledInHierarchy, Is.False);
            shell.ShowMenu(); Assert.That(shell.Screen, Is.EqualTo("settings"));
            privacy.Complete(true);
            Assert.That(shell.Modal, Is.Null);
            Assert.That(shell.Q<Button>("settings-language").enabledInHierarchy, Is.True);
            shell.ShowMenu(); Assert.That(shell.Screen, Is.EqualTo("menu"));
        }
        [UnityTest] public IEnumerator PrivacyFailureUsesLocalizedAlertAndRecoversWithoutMutatingBoard()
        {
            yield return Mount(locale: "pt-BR");
            shell.ShowPrivacyOptions(); privacy.Complete(false);
            Assert.That(shell.Modal, Is.EqualTo("notification"));
            StringAssert.Contains("privacidade", shell.Q<Label>("notification-message").text);
            Assert.That(shell.Q<Button>("notification-ok").enabledInHierarchy, Is.True);
        }
        [UnityTest] public IEnumerator NativeFormAndDisposedCallbacksCannotReenableOrNavigateAStaleView()
        {
            yield return Mount(); shell.ShowPrivacyOptions(); shell.DisposeMonetization();
            privacy.Complete(false); privacy.Notify();
            Assert.That(shell.Modal, Is.Null);
            Assert.That(shell.Screen, Is.EqualTo("settings"));
        }
        private sealed class Privacy : IAdPrivacyService
        {
            public bool Required = true;
            public int Calls;
            private Action<bool> finish;
            public event Action Changed;
            public bool PrivacyOptionsRequired => Required;
            public bool IsPrivacyFormPresenting { get; private set; }
            public void ShowPrivacyOptions(Action<bool> complete)
            { Calls++; finish = complete; IsPrivacyFormPresenting = true; Notify(); }
            public void Complete(bool success)
            { IsPrivacyFormPresenting = false; var callback = finish; finish = null; callback(success); Notify(); }
            public void Notify() => Changed?.Invoke();
        }
    }
}
