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
    // Issues #43/#37: random ordinary starts preserve each profile's input policy.
    public sealed class DifficultyShellTests
    {
        private bool hadLanguage;
        private string language;
        [SetUp] public void PreserveLanguage()
        { hadLanguage=PlayerPrefs.HasKey(LanguagePreference.Key);language=PlayerPrefs.GetString(LanguagePreference.Key); }
        [TearDown] public void RestoreLanguage()
        { if(hadLanguage)PlayerPrefs.SetString(LanguagePreference.Key,language);else PlayerPrefs.DeleteKey(LanguagePreference.Key);PlayerPrefs.Save(); }

        [TestCase("en", "Play", "Next board")]
        [TestCase("pt-BR", "Jogar", "Próximo tabuleiro")]
        [TestCase("es", "Jugar", "Siguiente tablero")]
        public void MenuOffersPlayTutorialRulesLanguagesWithoutDifficultyChoice(string locale, string play, string next)
        {
            var shell = PlayTestFactory.Shell(14, locale);
            Assert.That(shell.Q<Button>("menu-start").text, Is.EqualTo(play));
            Assert.That(shell.Q<Button>("menu-tutorial"), Is.Not.Null);
            Assert.That(shell.Q<Button>("menu-rules"), Is.Not.Null);
            foreach (var id in new[] { "easy", "medium", "hard" })
                Assert.That(shell.Q<Button>("menu-difficulty-" + id), Is.Null);
            foreach (var id in new[] { "en", "pt-BR", "es" })
                Assert.That(shell.Q<Button>("language-" + id), Is.Not.Null);
            Assert.That(shell.StartNewGame(), Is.True);
            Assert.That(shell.Q<Button>("action-new-game").text, Is.EqualTo(next));
        }

        [TestCase("easy", 5)] [TestCase("medium", 6)] [TestCase("hard", 7)]
        public void RandomProfilePreservesTapDragProtectedCellsWinAndNextBoard(string id, int size)
        {
            var loop = PlayTestFactory.Loop(14, difficultySelection: PlayTestFactory.Selection(id));
            var shell = PlayTestFactory.Shell(14, "en", loop);
            Assert.That(shell.StartNewGame(), Is.True);
            var session = shell.ActiveSession;
            Assert.That(session.Puzzle.Size, Is.EqualTo(size));
            Assert.That(shell.Q<Label>("board-difficulty").text, Does.Contain(size + " × " + size));
            var normal = session.Marks.Keys.First();
            foreach (var state in new[] { PlayerMark.X, PlayerMark.Cat, PlayerMark.Empty })
            { Assert.That(session.Tap(normal), Is.True); Assert.That(session.GetMark(normal), Is.EqualTo(state)); }
            var row = session.Marks.Keys.Where(c => c.Row == normal.Row).ToArray();
            Assert.That(session.ApplyDrag(row), Is.EqualTo(row.Length));
            foreach (var arrow in session.Puzzle.Arrows.Keys) Assert.That(session.Tap(arrow), Is.False);
            foreach (var cell in session.ProtectedCells) Assert.That(session.Tap(cell), Is.False);
            shell.ClearBoard(); Assert.That(session.Marks.Values, Is.All.EqualTo(PlayerMark.Empty));
            foreach (var cat in session.Puzzle.SolutionCats) { session.Tap(cat); session.Tap(cat); }
            Assert.That(session.IsCompleted, Is.True);
            Assert.That(session.Tap(normal), Is.False); Assert.That(session.Clear(), Is.False);
            Assert.That(shell.StartNewGame(), Is.True);
            Assert.That(shell.ActiveSession, Is.Not.SameAs(session));
            Assert.That(shell.ActiveSession.Puzzle.Seed, Is.EqualTo(15));
            Assert.That(loop.DifficultySelection.History.ConsecutiveStarts, Is.EqualTo(2));
            Assert.That(shell.StartNewGame(), Is.True);
            Assert.That(loop.SessionDifficulty.Id, Is.Not.EqualTo(id));
        }

        [Test] public void MenuLocaleResumeDoesNotConsumeHistoryOrReplaceBoard()
        {
            var shell = PlayTestFactory.Shell(); Assert.That(shell.StartNewGame(), Is.True);
            var session = shell.ActiveSession;
            var history = shell.Loop.DifficultySelection.History;
            shell.ShowMenu(); shell.SelectLanguage("es"); shell.BackToGame();
            Assert.That(shell.ActiveSession, Is.SameAs(session));
            Assert.That(shell.Loop.DifficultySelection.History, Is.SameAs(history));
            Assert.That(shell.Q<Label>("board-difficulty").text, Is.EqualTo("Fácil · 5 × 5"));
        }

        [Test] public void FirstPlayAndManualReplayKeepExactTutorialAndDoNotConsumeOrdinaryRun()
        {
            var seen = false;
            var selection = PlayTestFactory.Selection("hard");
            var shell = PlayTestFactory.Shell(loop: PlayTestFactory.Loop(difficultySelection: selection),
                tutorialSeen: () => seen, markTutorialSeen: () => seen = true);
            shell.StartTutorial(); Assert.That(seen, Is.False);
            shell.ShowMenu(); shell.StartPlaying();
            Assert.That(shell.Tutorial, Is.Not.Null);
            Assert.That(GenerationFingerprints.ForBoard(shell.ActiveSession.Puzzle),
                Is.EqualTo(GenerationFingerprints.ForBoard(TutorialFixture.CreatePuzzle())));
            Assert.That(shell.Loop.RequestedSeed, Is.Null);
            Assert.That(selection.History, Is.Null);
            shell.StartTutorial(); Assert.That(shell.Tutorial.Step, Is.EqualTo(TutorialStep.Welcome));
            shell.SkipTutorial(); Assert.That(shell.StartNewGame(), Is.True);
            Assert.That(shell.ActiveSession.Puzzle.Size, Is.EqualTo(7));
            Assert.That(shell.ActiveSession.Puzzle.Seed, Is.EqualTo(14));
            Assert.That(selection.History.ConsecutiveStarts, Is.EqualTo(1));
        }

        [TestCase("medium")] [TestCase("hard")]
        public void FailureAndRetryRetainPendingDifficultyAndOnlySuccessCommits(string id)
        {
            var generator = new RetryGenerator();
            var selection = PlayTestFactory.Selection(id);
            var loop = PlayTestFactory.Loop(14, generator: generator, difficultySelection: selection);
            var shell = PlayTestFactory.Shell(14, "en", loop);
            Assert.That(shell.StartNewGame(), Is.False);
            Assert.That(shell.Screen, Is.EqualTo("failure"));
            Assert.That(selection.History, Is.Null);
            Assert.That(loop.RequestedDifficulty.Id, Is.EqualTo(id));
            Assert.That(shell.Q<Label>("board-difficulty").text, Does.StartWith(id == "medium" ? "Medium" : "Hard"));
            Assert.That(shell.StartNewGame(), Is.True);
            Assert.That(loop.RequestedSeed, Is.EqualTo(15));
            Assert.That(loop.SessionDifficulty.Id, Is.EqualTo(id));
            Assert.That(generator.Requested, Is.EqualTo(new[] { id, id }));
            Assert.That(selection.History.ConsecutiveStarts, Is.EqualTo(1));
        }

        [Test] public void WrongProfileInjectedSuccessCannotReachGameplayOrCommitHistory()
        {
            var loop = PlayTestFactory.Loop(14, seed => new SeededEasyPuzzleGenerator().Generate(DifficultyProfile.Easy, seed),
                difficultySelection: PlayTestFactory.Selection("hard"));
            Assert.That(loop.NewGame(), Is.False); Assert.That(loop.Session, Is.Null);
            Assert.That(loop.DifficultySelection.History, Is.Null);
        }

        [Test] public void DuplicatePlayCommandsCannotSkipTutorialOrQueueTwoStarts()
        {
            var seen = false;
            var shell = PlayTestFactory.Shell(tutorialSeen: () => seen, markTutorialSeen: () => seen = true);
            shell.StartPlaying(); var tutorial = shell.Tutorial; shell.StartPlaying();
            Assert.That(shell.Tutorial, Is.SameAs(tutorial)); Assert.That(shell.Modal, Is.Null);
            shell.ShowMenu(); shell.StartPlaying(); shell.StartPlaying();
            Assert.That(shell.Modal, Is.EqualTo("loading")); Assert.That(shell.NewGameRequestId, Is.Zero);
            Assert.That(shell.StartNewGame(), Is.True); Assert.That(shell.NewGameRequestId, Is.EqualTo(1));
            Assert.That(shell.Loop.DifficultySelection.History.ConsecutiveStarts, Is.EqualTo(1));
        }

        // Issue #37: a real generated one-cat-arrow consequence remains a manual
        // player deduction, including the cells formerly hatched behind arrows.
        [Test]
        public void HardConsequencesStayVisibleAsEmptyInteractiveCellsUntilPlayerMarksThem()
        {

            var consequenceCellsChecked = 0;
            for (var seed = 0; seed < 16; seed++)
            {
                var loop = PlayTestFactory.Loop(seed, difficultySelection: PlayTestFactory.Selection("hard"));
                Assert.That(loop.NewGame(), Is.True, "Hard seed=" + seed);
                var session = loop.Session;
                var puzzle = session.Puzzle;
                Assert.That(session.ProtectedCells, Is.Empty);
                Assert.That(session.Marks.Count, Is.EqualTo(49 - puzzle.Arrows.Count));
                var before = BoardPresentation.FromSession(session);
                Assert.That(before.Cells.Count(c => c.Kind == BoardCellKind.Protected), Is.Zero);
                var consequences = ClueProtection.Derive(PuzzleClues.FromPuzzle(puzzle));
                foreach (var coordinate in consequences)
                {
                    consequenceCellsChecked++;
                    Assert.That(before.GetCell(coordinate).Kind, Is.EqualTo(BoardCellKind.Empty));
                    Assert.That(before.GetCell(coordinate).IsInteractive, Is.True);
                    Assert.That(session.Tap(coordinate), Is.True);
                    Assert.That(session.GetMark(coordinate), Is.EqualTo(PlayerMark.X));
                    Assert.That(BoardPresentation.FromSession(session).GetCell(coordinate).Kind, Is.EqualTo(BoardCellKind.X));
                }
                session.Clear();
                Assert.That(session.ApplyDrag(consequences), Is.EqualTo(consequences.Count));
                session.Clear();
                Assert.That(session.Marks.Values, Is.All.EqualTo(PlayerMark.Empty));
                foreach (var arrow in puzzle.Arrows.Keys) Assert.That(session.CanInteract(arrow), Is.False);
            }
            Assert.That(consequenceCellsChecked, Is.GreaterThan(0), "Corpus must actually exercise the old hatch rule.");
        }

        [TestCase("easy")]
        [TestCase("medium")]
        public void OtherModesRetainTheirOriginalHatchesAfterHardSelection(string id)
        {

            var hatchesChecked = 0;
            for (var seed = 0; seed < 16; seed++)
            {
                var loop = PlayTestFactory.Loop(seed, difficultySelection: PlayTestFactory.Selection(id));
                Assert.That(loop.NewGame(), Is.True);
                var session = loop.Session;
                var expected = ClueProtection.Derive(PuzzleClues.FromPuzzle(session.Puzzle));
                CollectionAssert.AreEqual(expected, session.ProtectedCells);
                hatchesChecked += expected.Count;
                foreach (var cell in expected)
                {
                    Assert.That(BoardPresentation.FromSession(session).GetCell(cell).Kind, Is.EqualTo(BoardCellKind.Protected));
                    Assert.That(session.Tap(cell), Is.False);
                }
            }
            Assert.That(hatchesChecked, Is.GreaterThan(0));
        }

        [Test]
        public void HardMenuReturnNeverReappliesConsequenceHatchesToLiveHardBoard()
        {
            var shell = PlayTestFactory.Shell(0, "en", PlayTestFactory.Loop(0, difficultySelection: PlayTestFactory.Selection("hard")));
            Assert.That(shell.StartNewGame(), Is.True);
            var hard = shell.ActiveSession;
            shell.ShowMenu(); shell.SelectLanguage("es"); shell.BackToGame();
            Assert.That(shell.ActiveSession, Is.SameAs(hard));
            Assert.That(hard.ProtectedCells, Is.Empty);
            Assert.That(shell.BoardView.Presentation.Cells.Any(c => c.Kind == BoardCellKind.Protected), Is.False);
            shell.RequestHint();
            Assert.That(hard.ProtectedCells, Is.Empty, "Optional assistance must not turn deductions into automatic hatches.");
            shell.ClearBoard();
            Assert.That(hard.Marks.Values, Is.All.EqualTo(PlayerMark.Empty));
            shell.ShowMenu(); shell.StartTutorial();
            CollectionAssert.AreEquivalent(new[] {
                new BoardCoordinate(0, 2), new BoardCoordinate(1, 0), new BoardCoordinate(1, 1),
                new BoardCoordinate(1, 2), new BoardCoordinate(2, 2)
            }, shell.ActiveSession.ProtectedCells);
        }

        private sealed class RetryGenerator:IPuzzleGenerator
        {
            public readonly System.Collections.Generic.List<string> Requested=new System.Collections.Generic.List<string>();
            public GenerationResult Generate(DifficultyProfile profile,int seed,GenerationOptions options=null,System.Threading.CancellationToken cancellationToken=default)
            { Requested.Add(profile.Id);return Requested.Count==1?null:new SeededPuzzleGenerator().Generate(profile,seed,options,cancellationToken); }
        }
    }
}
