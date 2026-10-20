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
    // Issue #72: mounted geometry, localized navigation and safe touch targets.
    public sealed class MenuPresentationTests
    {
        private GameObject host;
        private PanelSettings settings;
        [TearDown] public void Cleanup()
        {
            if (host != null) Object.DestroyImmediate(host);
            if (settings != null) Object.DestroyImmediate(settings);
        }

        [UnityTest] public IEnumerator PortraitMenusKeepLocalizedTargetsInsideSafeArea()
        {
            settings = ScriptableObject.CreateInstance<PanelSettings>();
            host = new GameObject("issue72-menu", typeof(UIDocument));
            var document = host.GetComponent<UIDocument>(); document.panelSettings = settings;
            var root = document.rootVisualElement;
            foreach (var size in new[] { new Vector2(320, 568), new Vector2(390, 844), new Vector2(430, 932) })
            foreach (var locale in new[] { "en", "pt-BR", "es" })
            {
                root.Clear(); root.style.width = size.x; root.style.height = size.y;
                var shell = new GameShellView(14, locale, PlayTestFactory.Loop(), tutorialSeen: () => false, markTutorialSeen: () => { });
                root.Add(shell);
                yield return null; yield return null;
                var top = size.x == 320 ? 20 : 59;
                var bottom = size.x == 320 ? 0 : 34;
                shell.ApplySafeArea(new Rect(0, bottom, size.x, size.y - top - bottom), size);
                yield return null; yield return null;
                var safe = shell.Q("game-safe-frame").worldBound;
                Assert.That(shell.Q<BoardSymbolElement>("menu-paw").Kind, Is.EqualTo(BoardCellKind.Paw));
                Assert.That(shell.Q<Label>("menu-title").text, Is.EqualTo("Paws"));
                var messages = GameLocalization.CreateCatalog().GetResource(locale).Messages;
                Assert.That(shell.Q<Button>("menu-start").text, Is.EqualTo(messages["game.start"]));
                Assert.That(shell.Q<Button>("menu-tutorial").text, Is.EqualTo(messages["tutorial.title"]));
                Assert.That(shell.Q("game-menu").Query<Label>().ToList().Any(l => l.text == messages["board.intro"]), Is.False);
                Assert.That(shell.Query<Label>().ToList().Any(l => l.text == messages["game.language"]), Is.False);
                Assert.That(shell.Q("menu-languages").Children().Select(e => e.name),
                    Is.EqualTo(new[] { "language-pt-BR", "language-es", "language-en" }));
                foreach (var name in new[] { "menu-start", "menu-tutorial", "language-pt-BR", "language-es", "language-en" })
                {
                    var button = shell.Q<Button>(name); var rect = button.worldBound;
                    Assert.That(rect.width, Is.GreaterThanOrEqualTo(48), name);
                    Assert.That(rect.height, Is.GreaterThanOrEqualTo(47.5f), name);
                    Assert.That(rect.xMin, Is.GreaterThanOrEqualTo(safe.xMin - 1), name);
                    Assert.That(rect.xMax, Is.LessThanOrEqualTo(safe.xMax + 1), name);
                    Assert.That(rect.yMin, Is.GreaterThanOrEqualTo(safe.yMin - 1), name);
                    Assert.That(rect.yMax, Is.LessThanOrEqualTo(safe.yMax + 1), name);
                    if (name.StartsWith("language-"))
                    {
                        Assert.That(button.enabledSelf, Is.EqualTo(name != "language-" + locale));
                        Assert.That(button.tooltip, Is.Not.Empty);
                    }
                }
                var play = shell.Q("menu-start").worldBound;
                var tutorial = shell.Q("menu-tutorial").worldBound;
                Assert.That(Mathf.Abs(play.y - tutorial.y), Is.LessThan(1));
                Assert.That(play.xMax, Is.LessThan(tutorial.xMin));
                Assert.That(play.yMin, Is.GreaterThan(safe.center.y));
                Assert.That(shell.Q("menu-languages").worldBound.yMin, Is.GreaterThan(play.yMax));
                Assert.That(shell.Q("menu-paw").worldBound.yMax, Is.LessThan(shell.Q("menu-title").worldBound.yMin));
            }
        }

        [Test] public void ResumeAndTutorialRemainAvailableWithoutMutatingBoard()
        {
            var shell = new GameShellView(14, "pt-BR", PlayTestFactory.Loop(), tutorialSeen: () => true, markTutorialSeen: () => { });
            shell.StartNewGame();
            var session = shell.ActiveSession;
            var cell = session.Marks.Keys.First(); session.Tap(cell);
            var marks = session.Marks.ToArray();
            shell.ShowMenu();
            Assert.That(shell.Q<Button>("menu-back"), Is.Not.Null);
            Assert.That(shell.Q<Button>("menu-rules"), Is.Null);
            Assert.That(shell.Q<Button>("menu-tutorial"), Is.Not.Null);
            shell.BackToGame();
            Assert.That(shell.ActiveSession, Is.SameAs(session));
            Assert.That(session.Marks, Is.EquivalentTo(marks));
        }
    }
}
