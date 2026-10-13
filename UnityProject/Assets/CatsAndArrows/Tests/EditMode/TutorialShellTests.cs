using System;
using System.Linq;
using CatsAndArrows.Core;
using CatsAndArrows.Gameplay;
using CatsAndArrows.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace CatsAndArrows.Tests
{
    public sealed class TutorialShellTests
    {
        private bool hadLanguage, hadSeen;
        private string language;
        private int seenValue;
        [SetUp] public void PreservePreferences()
        {
            hadLanguage = PlayerPrefs.HasKey(LanguagePreference.Key);
            language = PlayerPrefs.GetString(LanguagePreference.Key);
            hadSeen = PlayerPrefs.HasKey(TutorialPreference.Key);
            seenValue = PlayerPrefs.GetInt(TutorialPreference.Key);
        }
        [TearDown] public void RestorePreferences()
        {
            if (hadLanguage) PlayerPrefs.SetString(LanguagePreference.Key, language); else PlayerPrefs.DeleteKey(LanguagePreference.Key);
            if (hadSeen) PlayerPrefs.SetInt(TutorialPreference.Key, seenValue); else PlayerPrefs.DeleteKey(TutorialPreference.Key);
            PlayerPrefs.Save();
        }
        [Test] public void FirstStartRoutesOnceAndSavesSeenOnlyWhenPresented()
        {
            var seen = false; var writes = 0;
            var shell = new GameShellView(14, "en", tutorialSeen: () => seen, markTutorialSeen: () => { seen = true; writes++; });
            Assert.That(writes, Is.Zero);
            Assert.That(shell.Q<Button>("menu-tutorial"), Is.Not.Null);
            shell.StartPlaying();
            Assert.That(shell.Tutorial.Step, Is.EqualTo(TutorialStep.Welcome));
            Assert.That(shell.ActiveSession, Is.SameAs(shell.Tutorial.Session));
            Assert.That(shell.Loop.Session, Is.Null);
            Assert.That(seen, Is.True); Assert.That(writes, Is.EqualTo(1));
            shell.ShowMenu(); shell.StartPlaying();
            Assert.That(shell.Modal, Is.EqualTo("loading"));
            Assert.That(writes, Is.EqualTo(1));
            Assert.That(shell.StartNewGame(), Is.True);
            Assert.That(shell.Tutorial, Is.Null);
            Assert.That(shell.ActiveSession, Is.SameAs(shell.Loop.Session));
        }
        [Test] public void ManualReplayIsFreshAndDoesNotConsumeRandomSeeds()
        {
            var seen = false;
            var shell = new GameShellView(14, "en", tutorialSeen: () => seen, markTutorialSeen: () => seen = true);
            shell.StartTutorial(); var first = shell.Tutorial;
            while (first.CanContinue) shell.ContinueTutorial();
            first.Tap(new BoardCoordinate(0, 0)); first.EndGesture();
            Assert.That(first.Session.GetMark(new BoardCoordinate(0, 0)), Is.EqualTo(PlayerMark.X));
            shell.StartTutorial();
            Assert.That(shell.Tutorial, Is.Not.SameAs(first));
            Assert.That(shell.Tutorial.Step, Is.EqualTo(TutorialStep.Welcome));
            Assert.That(shell.ActiveSession.Marks.Values, Is.All.EqualTo(PlayerMark.Empty));
            Assert.That(shell.NewGameRequestId, Is.Zero);
            Assert.That(shell.StartNewGame(), Is.True);
            Assert.That(shell.Loop.RequestedSeed, Is.EqualTo(14));
        }
        [TestCase("en")][TestCase("pt-BR")][TestCase("es")]
        public void TutorialMessagesLocalizedAndMenuLanguageResumePreservesProgress(string locale)
        {
            var shell = new GameShellView(14, locale, tutorialSeen: () => true, markTutorialSeen: () => { });
            shell.StartTutorial(); while (shell.Tutorial.CanContinue) shell.ContinueTutorial();
            var tutorial = shell.Tutorial; tutorial.Tap(new BoardCoordinate(0, 0)); tutorial.EndGesture();
            var marks = tutorial.Session.Marks.ToArray();
            shell.ShowMenu(); shell.SelectLanguage(locale == "en" ? "pt-BR" : "en"); shell.BackToGame();
            Assert.That(shell.Tutorial, Is.SameAs(tutorial));
            Assert.That(shell.Tutorial.Step, Is.EqualTo(TutorialStep.Practice));
            Assert.That(shell.ActiveSession.Marks, Is.EquivalentTo(marks));
            Assert.That(shell.Q<Button>("action-hint"), Is.Null);
            Assert.That(shell.Q<Button>("action-clear"), Is.Null);
            Assert.That(shell.Q<Button>("action-skip"), Is.Not.Null);
            Assert.That(shell.BoardView.Q<Label>("completion-status").text, Is.Not.Empty);
            var before = shell.ActiveSession.Marks.ToArray(); shell.RequestHint(); shell.ClearBoard();
            Assert.That(shell.ActiveSession.Marks, Is.EquivalentTo(before));
            var catalog = GameLocalization.CreateCatalog();
            var resource = catalog.GetResource(locale).Messages;
            var keys = catalog.GetResource("en").Messages.Keys.Where(key => key.StartsWith("tutorial.")).ToArray();
            Assert.That(keys.Length, Is.GreaterThanOrEqualTo(22));
            foreach (var key in keys) Assert.That(resource[key], Is.Not.Empty, key);
            Assert.That(resource["tutorial.diagonal-cat"], Does.Contain("45°"));
        }
        [Test] public void SequentialDemoSymbolsAreBoldReadOnlyAndFixedKindsAreSeparate()
        {
            var shell = new GameShellView(14, "en", tutorialSeen: () => true, markTutorialSeen: () => { });
            shell.StartTutorial();
            Assert.That(shell.Q<Button>("action-continue").style.borderLeftWidth.value, Is.EqualTo(3));
            while (shell.Tutorial.Step != TutorialStep.EmptyState) shell.ContinueTutorial();
            foreach (var kind in new[] { BoardCellKind.Empty, BoardCellKind.X, BoardCellKind.Cat })
            {
                Assert.That(shell.BoardView.Q<VisualElement>("cell-" + shell.Tutorial.DemoCoordinate.Value.Row + "-" + shell.Tutorial.DemoCoordinate.Value.Column).Q<BoardSymbolElement>().Kind, Is.EqualTo(kind));
                Assert.That(shell.ActiveSession.Marks.Values, Is.All.EqualTo(PlayerMark.Empty));
                Assert.That(shell.BoardView.InteractionEnabled, Is.True);
                Assert.That(shell.Tutorial.CanTap(new BoardCoordinate(0, 0)), Is.False);
                Assert.That(shell.BoardView.Q<Label>("completion-status").text, Does.Contain("<b>"));
                shell.ContinueTutorial();
            }
            Assert.That(shell.Tutorial.Step, Is.EqualTo(TutorialStep.HatchedCells));
            Assert.That(shell.Tutorial.HighlightedCells, Is.EquivalentTo(shell.ActiveSession.ProtectedCells));
            shell.ContinueTutorial();
            Assert.That(shell.Tutorial.Step, Is.EqualTo(TutorialStep.Arrows));
            Assert.That(shell.Tutorial.HighlightedCells, Is.EquivalentTo(shell.ActiveSession.Puzzle.Arrows.Keys));
            shell.ContinueTutorial();
            Assert.That(shell.ActiveSession.Marks.Values, Is.All.EqualTo(PlayerMark.Empty));
        }
        [Test] public void SkipFailureUsesOrdinaryRetryWithoutRepeatingTutorial()
        {
            var seen = false; var attempts = 0;
            var generator = new SeededEasyPuzzleGenerator();
            var loop = new EasyGameLoop(14, seed => ++attempts == 1 ? null : generator.Generate(DifficultyProfile.Easy, seed));
            var shell = new GameShellView(14, "en", loop, tutorialSeen: () => seen, markTutorialSeen: () => seen = true);
            shell.StartPlaying(); shell.SkipTutorial();
            Assert.That(shell.Tutorial, Is.Null);
            Assert.That(shell.Modal, Is.EqualTo("loading"));
            Assert.That(shell.StartNewGame(), Is.False);
            Assert.That(shell.Screen, Is.EqualTo("failure"));
            Assert.That(shell.Q<Button>("modal-retry"), Is.Not.Null);
            Assert.That(seen, Is.True);
            Assert.That(shell.StartNewGame(), Is.True);
            Assert.That(shell.Tutorial, Is.Null);
            Assert.That(shell.ActiveSession, Is.SameAs(loop.Session));
        }
        [Test] public void RollbackDisablesAutomaticAndManualEntryWithoutWritingPreference()
        {
            var writes = 0;
            var shell = new GameShellView(14, "en", enableTutorial: false, tutorialSeen: () => false, markTutorialSeen: () => writes++);
            Assert.That(shell.Q<Button>("menu-tutorial"), Is.Null);
            shell.StartTutorial(); Assert.That(shell.Tutorial, Is.Null);
            shell.StartPlaying(); Assert.That(shell.Modal, Is.EqualTo("loading"));
            Assert.That(writes, Is.Zero);
        }
        [Test] public void SeenPreferenceSurvivesReloadAndDirectRandomPrimitiveDoesNotMarkSeen()
        {
            PlayerPrefs.DeleteKey(TutorialPreference.Key);
            Assert.That(TutorialPreference.HasSeen(), Is.False);
            var writes = 0;
            var shell = new GameShellView(14, "en", markTutorialSeen: () => writes++);
            Assert.That(shell.StartNewGame(), Is.True);
            Assert.That(writes, Is.Zero);
            TutorialPreference.MarkSeen();
            Assert.That(TutorialPreference.HasSeen(), Is.True);
        }
    }
}
