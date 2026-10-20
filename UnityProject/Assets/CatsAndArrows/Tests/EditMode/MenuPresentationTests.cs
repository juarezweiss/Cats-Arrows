using System.Collections;
using System.Linq;
using CatsAndArrows.Core;
using CatsAndArrows.Gameplay;
using CatsAndArrows.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace CatsAndArrows.Tests
{
    // Issue #76 supersedes #72: mounted navigation, native locale choices and safe targets.
    public sealed class MenuPresentationTests
    {
        private GameObject host;
        private PanelSettings settings;
        private RenderTexture panelTexture;
        private bool hadLanguage;
        private string language;
        [SetUp] public void PreservePreferences()
        {
            hadLanguage = PlayerPrefs.HasKey(LanguagePreference.Key);
            language = PlayerPrefs.GetString(LanguagePreference.Key);
        }
        [TearDown] public void Cleanup()
        {
            if (host != null) Object.DestroyImmediate(host);
            if (settings != null) Object.DestroyImmediate(settings);
            if (panelTexture != null) Object.DestroyImmediate(panelTexture);
            if (hadLanguage) PlayerPrefs.SetString(LanguagePreference.Key, language); else PlayerPrefs.DeleteKey(LanguagePreference.Key);
            PlayerPrefs.Save();
        }
        private VisualElement MountRoot()
        {
            settings = ScriptableObject.CreateInstance<PanelSettings>();
            settings.scaleMode = PanelScaleMode.ConstantPixelSize; settings.scale = 1;
            SetPanelSize(new Vector2(390, 844));
            host = new GameObject("issue76-settings", typeof(UIDocument));
            var document = host.GetComponent<UIDocument>(); document.panelSettings = settings;
            return document.rootVisualElement;
        }
        private void SetPanelSize(Vector2 size)
        {
            if (panelTexture != null) Object.DestroyImmediate(panelTexture);
            // The panel itself must have portrait bounds: sizing only its UIDocument
            // leaves lower targets outside the batch Editor Game view's pointer area.
            panelTexture = new RenderTexture((int)size.x, (int)size.y, 0);
            settings.targetTexture = panelTexture;
        }
        private static IEnumerator Settle() { yield return null; yield return null; }
        private static void Tap(GameShellView shell, string name, bool touch = false)
        {
            var button = shell.Q<Button>(name);
            Assert.That(button, Is.Not.Null, name);
            Assert.That(button.enabledInHierarchy, Is.True, name);
            var position = button.worldBound.center;
            Assert.That(shell.panel.visualTree.layout.Contains(position), Is.True, "inside panel pointer area " + name + " position=" + position + " panel=" + shell.panel.visualTree.layout);
            var picked = shell.panel.Pick(position);
            Assert.That(picked == button || (picked != null && button.Contains(picked)), Is.True, "panel picks " + name);
            if (touch)
            {
                var input = new Touch { fingerId = 0, position = position, phase = TouchPhase.Began };
                using (var down = PointerDownEvent.GetPooled(input, EventModifiers.None))
                { down.target = button; button.SendEvent(down); }
                input.phase = TouchPhase.Ended;
                using (var up = PointerUpEvent.GetPooled(input, EventModifiers.None))
                { up.target = button; button.SendEvent(up); }
            }
            else
            {
                using (var down = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, button = 0, mousePosition = position }))
                { down.target = button; button.SendEvent(down); }
                using (var up = PointerUpEvent.GetPooled(new Event { type = EventType.MouseUp, button = 0, mousePosition = position }))
                { up.target = button; button.SendEvent(up); }
            }
        }
        private static void AssertTarget(Button button, Rect safe)
        {
            var rect = button.worldBound;
            Assert.That(rect.width, Is.GreaterThanOrEqualTo(48), button.name);
            Assert.That(rect.height, Is.GreaterThanOrEqualTo(47.5f), button.name);
            Assert.That(rect.xMin, Is.GreaterThanOrEqualTo(safe.xMin - 1), button.name);
            Assert.That(rect.xMax, Is.LessThanOrEqualTo(safe.xMax + 1), button.name);
            Assert.That(rect.yMin, Is.GreaterThanOrEqualTo(safe.yMin - 1), button.name);
            Assert.That(rect.yMax, Is.LessThanOrEqualTo(safe.yMax + 1), button.name);
            if (!string.IsNullOrEmpty(button.text))
            {
                var measured = button.MeasureTextSize(button.text, button.contentRect.width,
                    VisualElement.MeasureMode.AtMost, 0, VisualElement.MeasureMode.Undefined);
                Assert.That(measured.y, Is.LessThanOrEqualTo(button.contentRect.height + 1), "text fits " + button.name);
            }
        }

        [UnityTest] public IEnumerator PortraitHomeSettingsAndSelectorKeepLocalizedTargetsInsideSafeArea()
        {
            var root = MountRoot();
            foreach (var size in new[] { new Vector2(320, 568), new Vector2(390, 844), new Vector2(430, 932) })
            foreach (var locale in new[] { "en", "pt-BR", "es" })
            {
                SetPanelSize(size);
                root.Clear(); root.style.width = size.x; root.style.height = size.y;
                var shell = new GameShellView(14, locale, PlayTestFactory.Loop(), tutorialSeen: () => false, markTutorialSeen: () => { });
                root.Add(shell); yield return Settle();
                var top = size.x == 320 ? 20 : 59; var bottom = size.x == 320 ? 0 : 34;
                shell.ApplySafeArea(new Rect(0, bottom, size.x, size.y - top - bottom), size);
                yield return Settle();
                var safe = shell.Q("game-safe-frame").worldBound;
                var messages = GameLocalization.CreateCatalog().GetResource(locale).Messages;
                Assert.That(shell.Q<BoardSymbolElement>("menu-paw").Kind, Is.EqualTo(BoardCellKind.Paw));
                Assert.That(shell.Q<Label>("menu-title").text, Is.EqualTo("Paws"));
                Assert.That(shell.Q<Button>("menu-start").text, Is.EqualTo(messages["game.start"]));
                Assert.That(shell.Q("game-menu").Query<Button>().ToList().Select(b => b.name),
                    Is.EqualTo(new[] { "menu-start", "menu-settings" }));
                Assert.That(shell.Q("menu-languages"), Is.Null);
                Assert.That(shell.Q("menu-paw").worldBound.yMax, Is.LessThan(shell.Q("menu-title").worldBound.yMin));
                foreach (var name in new[] { "menu-start", "menu-settings" }) AssertTarget(shell.Q<Button>(name), safe);
                var play = shell.Q<Button>("menu-start");
                var title = shell.Q<Label>("menu-title");
                var titleTextWidth = title.MeasureTextSize(title.text, 0, VisualElement.MeasureMode.Undefined,
                    0, VisualElement.MeasureMode.Undefined).x;
                Assert.That(play.worldBound.width, Is.EqualTo(titleTextWidth).Within(1), "Play matches Paws text width, excluding label padding");
                Assert.That(play.worldBound.center.x, Is.EqualTo(title.worldBound.center.x).Within(1), "Play remains centered");
                Assert.That(play.worldBound.height, Is.EqualTo(52).Within(1));
                Assert.That(play.resolvedStyle.unityFontStyleAndWeight, Is.EqualTo(FontStyle.Bold));
                var gear = shell.Q("menu-settings").worldBound;
                Assert.That(gear.center.x, Is.GreaterThan(safe.center.x));
                Assert.That(gear.yMax, Is.GreaterThan(safe.yMax - 8));
                Tap(shell, "menu-settings", touch: locale == "es"); yield return Settle();
                Assert.That(shell.Screen, Is.EqualTo("settings"));
                Assert.That(shell.BoardView.InteractionEnabled, Is.False);
                Assert.That(shell.Q<Label>("settings-title").text, Is.EqualTo(messages["settings.title"]));
                var rows = shell.Q("settings-list").Children().OfType<Button>().ToArray();
                Assert.That(rows.Select(b => b.name), Is.EqualTo(new[] {
                    "settings-language", "settings-tutorial", "settings-remove-ads", "settings-restore" }));
                Assert.That(rows.Select(b => b.text), Is.EqualTo(new[] {
                    messages["game.language"], messages["tutorial.title"], messages["monetization.remove"], messages["monetization.restore"] }));
                for (var i = 0; i < rows.Length; i++)
                {
                    AssertTarget(rows[i], safe);
                    if (i > 0) Assert.That(rows[i].worldBound.yMin, Is.GreaterThan(rows[i - 1].worldBound.yMax));
                }
                Assert.That(rows[2].enabledSelf, Is.False); Assert.That(rows[3].enabledSelf, Is.False);
                AssertTarget(shell.Q<Button>("settings-back"), safe);
                Tap(shell, "settings-language", touch: locale == "es"); yield return Settle();
                Assert.That(shell.Modal, Is.EqualTo("language"));
                Assert.That(shell.Q("game-menu").enabledInHierarchy, Is.False);
                var options = shell.Q("language-selector").Children().OfType<Button>().Take(3).ToArray();
                Assert.That(options.Select(b => b.text), Is.EqualTo(new[] { "Português", "English", "Español" }));
                foreach (var option in options) AssertTarget(option, safe);
                AssertTarget(shell.Q<Button>("language-back"), safe);
                Assert.That(shell.Q<Button>("language-" + locale).style.borderLeftWidth.value, Is.EqualTo(3));
                Tap(shell, "language-back"); yield return Settle();
                Assert.That(shell.Modal, Is.Null); Assert.That(shell.Locale, Is.EqualTo(locale));
                Tap(shell, "settings-back"); yield return Settle();
                Assert.That(shell.Screen, Is.EqualTo("menu"));
            }
        }

        [UnityTest] public IEnumerator LanguageSelectionPersistsImmediatelyRelocalizesAndEscapeCancels()
        {
            var root = MountRoot(); root.style.width = 390; root.style.height = 844;
            var shell = new GameShellView(14, "en", PlayTestFactory.Loop(), tutorialSeen: () => true, markTutorialSeen: () => { });
            root.Add(shell); yield return Settle();
            foreach (var locale in new[] { "pt-BR", "es", "en" })
            {
                shell.ShowSettings(); yield return Settle();
                Tap(shell, "settings-language"); yield return Settle();
                Tap(shell, "language-" + locale, touch: true); yield return Settle();
                Assert.That(shell.Screen, Is.EqualTo("settings")); Assert.That(shell.Modal, Is.Null);
                Assert.That(shell.Locale, Is.EqualTo(locale)); Assert.That(LanguagePreference.Load(), Is.EqualTo(locale));
                Assert.That(new GameShellView(14, loop: PlayTestFactory.Loop(), tutorialSeen: () => true).Locale, Is.EqualTo(locale));
                var messages = GameLocalization.CreateCatalog().GetResource(locale).Messages;
                Assert.That(shell.Q<Label>("settings-title").text, Is.EqualTo(messages["settings.title"]));
                Assert.That(shell.Q<Button>("settings-back").text, Is.EqualTo(messages["settings.back"]));
                Assert.That(shell.Q<Button>("settings-restore").text, Is.EqualTo(messages["monetization.restore"]));
                Assert.That(shell.Q<Label>("monetization-settings-status").text, Is.EqualTo(messages["monetization.status.PurchaseUnavailable"]));
                Tap(shell, "settings-language"); yield return Settle();
                using (var escape = KeyDownEvent.GetPooled('\0', KeyCode.Escape, EventModifiers.None))
                { escape.target = shell.Q<Button>("language-" + locale); shell.SendEvent(escape); }
                yield return Settle();
                Assert.That(shell.Modal, Is.Null); Assert.That(shell.Locale, Is.EqualTo(locale));
            }
        }

        [Test] public void PlayResumesBoardAndSettingsTutorialExplicitlyReplaysWithoutMutatingHistory()
        {
            var shell = new GameShellView(14, "pt-BR", PlayTestFactory.Loop(), tutorialSeen: () => true, markTutorialSeen: () => { });
            shell.StartNewGame(); var session = shell.ActiveSession;
            var cell = session.Marks.Keys.First(); session.Tap(cell);
            var marks = session.Marks.ToArray(); var history = shell.Loop.DifficultySelection.History;
            shell.ShowMenu(); shell.ShowSettings(); shell.ShowMenu(); shell.StartPlaying();
            Assert.That(shell.ActiveSession, Is.SameAs(session));
            Assert.That(session.Marks, Is.EquivalentTo(marks));
            Assert.That(shell.Loop.DifficultySelection.History, Is.SameAs(history));
            Assert.That(shell.Screen, Is.EqualTo("play"));
            shell.ShowMenu(); shell.ShowSettings(); shell.StartTutorial(); var tutorial = shell.Tutorial;
            shell.ContinueTutorial(); shell.ShowMenu(); shell.StartPlaying();
            Assert.That(shell.Tutorial, Is.SameAs(tutorial));
            Assert.That(shell.Tutorial.Step, Is.Not.EqualTo(TutorialStep.Welcome));
            shell.ShowSettings(); shell.StartTutorial();
            Assert.That(shell.Tutorial, Is.Not.SameAs(tutorial));
            Assert.That(shell.Tutorial.Step, Is.EqualTo(TutorialStep.Welcome));
            Assert.That(shell.Loop.DifficultySelection.History, Is.SameAs(history));
        }
    }
}
