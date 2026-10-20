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
    // Issue #44: presentation timing never owns completion, marks or difficulty history.
    public sealed class CompletionShellTests
    {
        private GameObject host;
        private PanelSettings settings;
        private UIDocument document;
        private GameShellView shell;
        private double now;
        private bool hadLanguage;
        private string language;

        [SetUp] public void PreserveLanguage()
        { hadLanguage = PlayerPrefs.HasKey(LanguagePreference.Key); language = PlayerPrefs.GetString(LanguagePreference.Key); }

        [TearDown] public void TearDown()
        {
            if (host != null) Object.DestroyImmediate(host);
            if (settings != null) Object.DestroyImmediate(settings);
            if (hadLanguage) PlayerPrefs.SetString(LanguagePreference.Key, language); else PlayerPrefs.DeleteKey(LanguagePreference.Key);
            PlayerPrefs.Save();
        }

        private IEnumerator Mount(string locale = "en", EasyGameLoop loop = null)
        {
            now = 0;
            settings = ScriptableObject.CreateInstance<PanelSettings>();
            settings.hideFlags = HideFlags.HideAndDontSave;
            host = new GameObject("issue44-headless-shell", typeof(UIDocument));
            host.hideFlags = HideFlags.HideAndDontSave;
            document = host.GetComponent<UIDocument>(); document.panelSettings = settings;
            document.rootVisualElement.style.width = 390; document.rootVisualElement.style.height = 844;
            shell = new GameShellView(14, locale, loop ?? PlayTestFactory.Loop(), tutorialSeen: () => true,
                markTutorialSeen: () => { }, completionClock: () => now);
            document.rootVisualElement.Add(shell);
            Assert.That(shell.StartNewGame(), Is.True);
            yield return null; yield return null;
            Assert.That(shell.panel, Is.Not.Null);
        }

        private void CompleteWithHints()
        {
            for (var i = 0; i < 100 && !shell.ActiveSession.IsCompleted; i++) shell.RequestHint();
            Assert.That(shell.ActiveSession.IsCompleted, Is.True);
        }
        private void Tick(double time) { now = time; shell.TickCompletion(); }
        private VisualElement Actions => shell.Q("completion-actions");
        private Label Message => shell.Q<Label>("completion-message");
        private void AssertStatic()
        {
            Assert.That(Actions.style.display.value, Is.EqualTo(DisplayStyle.None));
            Assert.That(Message.style.display.value, Is.EqualTo(DisplayStyle.None));
            Assert.That(shell.BoardView.Q("board-grid").style.opacity.value, Is.EqualTo(1));
            Assert.That(shell.BoardView.InteractionEnabled, Is.False);
        }

        [TestCase("en", "you found all the paws", "next board", "back to completed board", "menu")]
        [TestCase("pt-BR", "você encontrou todas as patas", "próximo tabuleiro", "voltar ao tabuleiro concluído", "menu")]
        [TestCase("es", "encontraste todas las patas", "siguiente tablero", "volver al tablero completado", "menú")]
        public void CelebrationResourcesAreLocalizedAndLowercase(string locale, string message, string next, string back, string menu)
        {
            var messages = GameLocalization.CreateCatalog().GetResource(locale).Messages;
            Assert.That(messages["completion.message"], Is.EqualTo(message));
            Assert.That(messages["completion.next"], Is.EqualTo(next));
            Assert.That(messages["completion.back"], Is.EqualTo(back));
            Assert.That(messages["completion.menu"], Is.EqualTo(menu));
            foreach (var key in new[] { "completion.message", "completion.next", "completion.back", "completion.menu" })
                Assert.That(messages[key], Is.EqualTo(messages[key].ToLowerInvariant()));
        }

        [UnityTest] public IEnumerator FinalHintShowsSequentialFadesAndBackRestoresIdenticalLockedBoard()
        {
            yield return Mount();
            CompleteWithHints(); var session = shell.ActiveSession; var marks = session.Marks.ToArray();
            var history = shell.Loop.DifficultySelection.History;
            Assert.That(shell.Modal, Is.Null);
            Assert.That(shell.Q<Button>("action-hint").enabledSelf, Is.False);
            Assert.That(shell.Q<Button>("action-clear").enabledSelf, Is.False);
            Tick(.3);
            Assert.That(Message.style.opacity.value, Is.EqualTo(1));
            Assert.That(shell.BoardView.Q("board-grid").style.opacity.value, Is.LessThan(1));
            Assert.That(Actions.style.display.value, Is.EqualTo(DisplayStyle.None));
            Tick(4.1);
            Assert.That(Message.style.opacity.value, Is.InRange(.01f, .99f));
            Assert.That(Actions.style.display.value, Is.EqualTo(DisplayStyle.None));
            Tick(5);
            Assert.That(Message.style.display.value, Is.EqualTo(DisplayStyle.None));
            Assert.That(Actions.style.opacity.value, Is.EqualTo(1));
            Assert.That(shell.Q<Button>("completion-next").enabledInHierarchy, Is.True);
            shell.BackToCompletedBoard(); AssertStatic();
            shell.ClearBoard(); shell.RequestHint();
            foreach (var cell in session.Marks.Keys) Assert.That(session.Tap(cell), Is.False);
            Assert.That(session.ApplyDrag(session.Marks.Keys), Is.Zero);
            Tick(100); shell.BackToGame(); Tick(101); AssertStatic();
            Assert.That(shell.ActiveSession, Is.SameAs(session));
            Assert.That(session.Marks, Is.EquivalentTo(marks));
            Assert.That(shell.Loop.DifficultySelection.History, Is.SameAs(history));
        }

        [UnityTest] public IEnumerator MenuLocaleFocusPauseDetachAndRebindConsumeCelebrationWithoutReplay()
        {
            yield return Mount();
            foreach (var path in new[] { "menu", "locale", "focus", "pause", "detach", "rebind", "other-board" })
            {
                CompleteWithHints(); Tick(now + .5);
                var session = shell.ActiveSession; var marks = session.Marks.ToArray();
                switch (path)
                {
                    case "menu": shell.ShowMenu(); shell.BackToGame(); break;
                    case "locale": shell.SelectLanguage("es"); shell.BackToGame(); break;
                    case "focus": shell.SetApplicationFocus(false); Tick(now + 30); shell.SetApplicationFocus(true); break;
                    case "pause": shell.SetApplicationPaused(true); Tick(now + 30); shell.SetApplicationPaused(false); break;
                    case "detach": shell.RemoveFromHierarchy(); Tick(now + 30); document.rootVisualElement.Add(shell); yield return null; break;
                    case "rebind": shell.BoardView.Bind(session, shell.Locale); break;
                    case "other-board": shell.BoardView.Bind(new GameSession(TutorialFixture.CreatePuzzle())); shell.BoardView.Bind(session); break;
                }
                shell.TickCompletion(); AssertStatic(); Tick(now + 50); AssertStatic();
                Assert.That(shell.ActiveSession, Is.SameAs(session), path);
                Assert.That(session.Marks, Is.EquivalentTo(marks), path);
                Assert.That(shell.StartNewGame(), Is.True);
                Tick(now + 100);
                Assert.That(shell.ActiveSession.IsCompleted, Is.False);
                Assert.That(Actions.style.display.value, Is.EqualTo(DisplayStyle.None));
            }
        }

        [UnityTest] public IEnumerator DuplicateNotificationsAndEarlyBackCannotSkipOrRestartSequence()
        {
            yield return Mount(); CompleteWithHints(); Tick(1);
            shell.BackToCompletedBoard();
            Assert.That(Message.style.opacity.value, Is.EqualTo(1));
            shell.BackToGame(); Tick(3.999);
            Assert.That(Message.style.opacity.value, Is.EqualTo(1));
            Assert.That(Actions.style.display.value, Is.EqualTo(DisplayStyle.None));
            Tick(5); shell.BackToGame(); Tick(50);
            Assert.That(Actions.style.opacity.value, Is.EqualTo(1));
            Assert.That(Message.style.display.value, Is.EqualTo(DisplayStyle.None));
        }

        [UnityTest] public IEnumerator BothTutorialStagesRemainFreeOfOrdinaryCelebration()
        {
            yield return Mount(); shell.StartTutorial(); var tutorial = shell.Tutorial;
            for (var i = 0; i < 40 && tutorial.Step != TutorialStep.Independent; i++)
            {
                if (tutorial.CanContinue) { shell.ContinueTutorial(); continue; }
                if (tutorial.Step == TutorialStep.DiagonalPaw)
                    Assert.That(tutorial.RecordArrowHold(new BoardCoordinate(0, 3)), Is.True);
                if (tutorial.Step == TutorialStep.Practice)
                    for (var tap = 0; tap < 3; tap++) tutorial.Tap(tutorial.Targets[0]);
                else foreach (var target in tutorial.Targets)
                {
                    var expected = tutorial.Session.Puzzle.IsSolutionPaw(target) ? PlayerMark.Paw : PlayerMark.X;
                    for (var tap = 0; tap < 3 && tutorial.Session.GetMark(target) != expected; tap++) tutorial.Tap(target);
                }
                Assert.That(tutorial.EndGesture(), Is.True);
                shell.BackToGame(); Tick(now + 10);
                Assert.That(Actions.style.display.value, Is.EqualTo(DisplayStyle.None));
            }
            Assert.That(tutorial.Step, Is.EqualTo(TutorialStep.Independent));
            foreach (var paw in tutorial.Session.Puzzle.SolutionPaws) { tutorial.Tap(paw); tutorial.Tap(paw); }
            tutorial.EndGesture(); shell.BackToGame(); Tick(now + 10);
            Assert.That(tutorial.IsComplete, Is.True);
            Assert.That(Message.style.display.value, Is.EqualTo(DisplayStyle.None));
            Assert.That(Actions.style.display.value, Is.EqualTo(DisplayStyle.None));
            Assert.That(shell.Q<Button>("action-play"), Is.Not.Null);
            Assert.That(shell.Loop.DifficultySelection.History.ConsecutiveStarts, Is.EqualTo(1));
        }
    }
}
