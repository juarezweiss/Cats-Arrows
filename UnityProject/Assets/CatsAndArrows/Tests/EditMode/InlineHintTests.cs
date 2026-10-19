using System.Linq;
using CatsAndArrows.Core;
using CatsAndArrows.Gameplay;
using CatsAndArrows.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace CatsAndArrows.Tests
{
    // Issue #23: each Hint applies one fact immediately and keeps the board playable.
    public sealed class InlineHintTests
    {
        private bool hadPreference;
        private string previousPreference;

        [SetUp]
        public void PreserveLanguage()
        {
            hadPreference = PlayerPrefs.HasKey(LanguagePreference.Key);
            previousPreference = PlayerPrefs.GetString(LanguagePreference.Key);
        }

        [TearDown]
        public void RestoreLanguage()
        {
            if (hadPreference) PlayerPrefs.SetString(LanguagePreference.Key, previousPreference);
            else PlayerPrefs.DeleteKey(LanguagePreference.Key);
            PlayerPrefs.Save();
        }

        [TestCase("en")]
        [TestCase("pt-BR")]
        [TestCase("es")]
        public void HintAppliesOneFactImmediatelyWithoutExplanationOrBlockingBoard(string locale)
        {
            var view = Started(locale);
            var before = view.Loop.Session.Marks.ToDictionary(pair => pair.Key, pair => pair.Value);
            view.RequestHint();
            Assert.That(view.Loop.Session.Marks.Count(pair => before[pair.Key] != pair.Value), Is.EqualTo(1));
            Assert.That(view.Modal, Is.Null);
            Assert.That(view.BoardView.InteractionEnabled, Is.True);
            AssertNoExplanation(view);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void ClearRemovesPriorHintOutlineInBothPresentationModes(bool inline)
        {
            var view = PlayTestFactory.Shell(14, "en", useInlineHints: inline);
            Assert.That(view.StartNewGame(), Is.True);
            view.RequestHint();
            var target = view.ActiveSession.Marks.Keys.Single(coordinate =>
                view.BoardView.Q<VisualElement>("cell-" + coordinate.Row + "-" + coordinate.Column)
                    .style.borderLeftWidth.value == 3);
            if (!inline) view.BackToGame();
            view.ClearBoard();
            Assert.That(view.ActiveSession.Marks.Values, Is.All.EqualTo(PlayerMark.Empty));
            Assert.That(view.BoardView.Q<VisualElement>("cell-" + target.Row + "-" + target.Column)
                .style.borderLeftWidth.value, Is.EqualTo(1));
        }

        [Test]
        public void HintCorrectsAnIncorrectMarkWithoutOpeningAnExplanation()
        {
            var view = Started("en");
            var session = view.Loop.Session;
            var proof = new HumanDeductionSolver().Solve(PuzzleClues.FromPuzzle(session.Puzzle));
            var next = proof.Steps.SelectMany(step => step.Conclusions)
                .First(assignment => session.Marks.ContainsKey(assignment.Coordinate));
            session.Tap(next.Coordinate);
            if (next.State == CellKnowledge.NotPaw) session.Tap(next.Coordinate);
            var before = session.Marks.ToDictionary(pair => pair.Key, pair => pair.Value);
            view.RequestHint();
            Assert.That(session.GetMark(next.Coordinate), Is.EqualTo(next.State == CellKnowledge.Paw ? PlayerMark.Paw : PlayerMark.X));
            Assert.That(session.Marks.Count(pair => before[pair.Key] != pair.Value), Is.EqualTo(1));
            Assert.That(view.Modal, Is.Null);
            AssertNoExplanation(view);
        }

        [Test]
        public void ConsecutiveHintsApplyOneFactEachAndClearAndNewGameResetMarks()
        {
            var view = Started("en");
            var session = view.Loop.Session;
            for (var request = 0; request < 2; request++)
            {
                var before = session.Marks.ToDictionary(pair => pair.Key, pair => pair.Value);
                view.RequestHint();
                Assert.That(session.Marks.Count(pair => before[pair.Key] != pair.Value), Is.EqualTo(1));
                Assert.That(view.Modal, Is.Null);
            }
            Assert.That(session.IsCompleted, Is.False);
            view.ClearBoard();
            Assert.That(view.Loop.Session, Is.SameAs(session));
            Assert.That(session.Marks.Values, Is.All.EqualTo(PlayerMark.Empty));
            AssertNoExplanation(view);
            view.RequestHint();
            Assert.That(session.Marks.Values.Count(mark => mark != PlayerMark.Empty), Is.EqualTo(1));
            Assert.That(view.StartNewGame(), Is.True);
            Assert.That(view.Loop.Session, Is.Not.SameAs(session));
            Assert.That(view.Loop.Session.Marks.Values, Is.All.EqualTo(PlayerMark.Empty));
            Assert.That(view.Q<Button>("action-hint").enabledSelf, Is.True);
            AssertNoExplanation(view);
        }

        [Test]
        public void MenuAndLanguagePreserveHintMarksWithoutAddingExplanation()
        {
            var view = Started("en");
            view.RequestHint();
            var session = view.Loop.Session;
            var marks = session.Marks.ToArray();
            view.ShowMenu();
            view.SelectLanguage("pt-BR"); view.BackToGame();
            Assert.That(view.Loop.Session, Is.SameAs(session));
            Assert.That(view.Locale, Is.EqualTo("pt-BR"));
            Assert.That(session.Marks, Is.EquivalentTo(marks));
            Assert.That(view.Modal, Is.Null);
            Assert.That(view.BoardView.InteractionEnabled, Is.True);
            AssertNoExplanation(view);
            var before = session.Marks.ToDictionary(pair => pair.Key, pair => pair.Value);
            view.RequestHint();
            Assert.That(session.Marks.Count(pair => before[pair.Key] != pair.Value), Is.EqualTo(1));
            Assert.That(view.Modal, Is.Null);
        }

        [Test]
        public void FinalHintLeavesCompletedBoardReadOnlyWithoutLastHintAction()
        {
            var view = Started("en");
            for (var request = 0; request < 25 && !view.Loop.Session.IsCompleted; request++) view.RequestHint();
            var session = view.Loop.Session;
            Assert.That(session.IsCompleted, Is.True);
            Assert.That(view.Modal, Is.Null);
            Assert.That(view.BoardView.InteractionEnabled, Is.False);
            Assert.That(view.Q<Button>("action-hint").enabledSelf, Is.False);
            Assert.That(view.Q<Button>("action-clear").enabledSelf, Is.False);
            AssertNoExplanation(view);
            Assert.That(view.Q<Button>("completion-hint").style.display.value, Is.EqualTo(DisplayStyle.None));
            Assert.That(view.Q<Label>("completion-status").text,
                Is.EqualTo(GameLocalization.CreateCatalog().GetResource("en").Messages["game.victory"]));
            var completed = session.Marks.ToArray();
            view.ShowLastHint(); view.RequestHint(); view.ClearBoard();
            session.Tap(session.Marks.Keys.First());
            Assert.That(view.Modal, Is.Null);
            Assert.That(session.Marks, Is.EquivalentTo(completed));
            view.ShowMenu(); view.BackToGame();
            Assert.That(view.BoardView.InteractionEnabled, Is.False);
            Assert.That(view.Modal, Is.Null);
            Assert.That(view.StartNewGame(), Is.True);
            Assert.That(view.Loop.Session.IsCompleted, Is.False);
            AssertNoExplanation(view);
        }

        [Test]
        public void FailedNewGameAndRetryDoNotLeaveStaleHintOrExplanation()
        {
            var generator = new SeededEasyPuzzleGenerator();
            var attempts = 0;
            var loop = PlayTestFactory.Loop(14, seed => ++attempts == 2 ? null : generator.Generate(DifficultyProfile.Easy, seed));
            var view = PlayTestFactory.Shell(14, "en", loop);
            Assert.That(view.StartNewGame(), Is.True);
            view.RequestHint();
            Assert.That(view.StartNewGame(), Is.False);
            Assert.That(view.Loop.Session, Is.Null);
            Assert.That(view.Screen, Is.EqualTo("failure"));
            Assert.That(view.Modal, Is.EqualTo("failure"));
            view.RequestHint();
            Assert.That(view.Loop.Session, Is.Null);
            Assert.That(view.StartNewGame(), Is.True);
            Assert.That(view.Modal, Is.Null);
            Assert.That(view.Loop.Session.Marks.Values, Is.All.EqualTo(PlayerMark.Empty));
            AssertNoExplanation(view);
            view.RequestHint();
            Assert.That(view.Loop.Session.Marks.Values.Count(mark => mark != PlayerMark.Empty), Is.EqualTo(1));
            Assert.That(view.Modal, Is.Null);
        }

        private static GameShellView Started(string locale)
        {
            var view = PlayTestFactory.Shell(14, locale);
            Assert.That(view.StartNewGame(), Is.True);
            return view;
        }

        private static void AssertNoExplanation(GameShellView view)
        {
            Assert.That(view.Q<Button>("action-why"), Is.Null);
            Assert.That(view.Q<Label>("hint-explanation"), Is.Null);
            Assert.That(view.Q<ScrollView>("hint-explanation-scroll"), Is.Null);
        }
    }
}
