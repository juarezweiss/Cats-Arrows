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
    // Issue #32: selection belongs to the next game, tutorial truth stays fixed Easy.
    public sealed class DifficultyShellTests
    {
        private bool hadLanguage;
        private string language;
        [SetUp] public void PreserveLanguage()
        { hadLanguage=PlayerPrefs.HasKey(LanguagePreference.Key);language=PlayerPrefs.GetString(LanguagePreference.Key); }
        [TearDown] public void RestoreLanguage()
        { if(hadLanguage)PlayerPrefs.SetString(LanguagePreference.Key,language);else PlayerPrefs.DeleteKey(LanguagePreference.Key);PlayerPrefs.Save(); }

        [TestCase("en","Easy","Medium","Hard")]
        [TestCase("pt-BR","Fácil","Médio","Difícil")]
        [TestCase("es","Fácil","Medio","Difícil")]
        public void MenuExposesAllThreeLocalizedChoicesAndSelectedState(string locale,string easy,string medium,string hard)
        {
            var shell=new GameShellView(14,locale,enableTutorial:false);
            Assert.That(shell.Q<Button>("menu-difficulty-easy").text,Is.EqualTo(easy));
            Assert.That(shell.Q<Button>("menu-difficulty-medium").text,Is.EqualTo(medium));
            Assert.That(shell.Q<Button>("menu-difficulty-hard").text,Is.EqualTo(hard));
            Assert.That(shell.Q<Button>("menu-difficulty-easy").enabledSelf,Is.False);
            shell.SelectDifficulty("medium");
            Assert.That(shell.Loop.SelectedDifficulty.Id,Is.EqualTo("medium"));
            Assert.That(shell.Q<Button>("menu-difficulty-medium").enabledSelf,Is.False);
            Assert.That(shell.Q<Button>("menu-difficulty-easy").enabledSelf,Is.True);
        }
        [TestCase("easy",5)] [TestCase("medium",6)] [TestCase("hard",7)]
        public void SelectedDifficultyDrivesStartAndNewGameWithoutChangingTapDragOrWin(string id,int size)
        {
            var shell=new GameShellView(14,"en",enableTutorial:false);shell.SelectDifficulty(id);
            Assert.That(shell.StartNewGame(),Is.True);var session=shell.ActiveSession;
            Assert.That(session.Puzzle.Size,Is.EqualTo(size));Assert.That(shell.Loop.SessionDifficulty.Id,Is.EqualTo(id));
            Assert.That(shell.Q<Label>("board-difficulty").text,Does.Contain(size+" × "+size));
            var normal=session.Marks.Keys.First();
            Assert.That(session.Tap(normal),Is.True);Assert.That(session.GetMark(normal),Is.EqualTo(PlayerMark.X));
            Assert.That(session.Tap(normal),Is.True);Assert.That(session.GetMark(normal),Is.EqualTo(PlayerMark.Cat));
            Assert.That(session.Tap(normal),Is.True);Assert.That(session.GetMark(normal),Is.EqualTo(PlayerMark.Empty));
            var row=session.Marks.Keys.Where(c=>c.Row==normal.Row).ToArray();
            Assert.That(session.ApplyDrag(row),Is.EqualTo(row.Length));
            foreach(var arrow in session.Puzzle.Arrows.Keys)Assert.That(session.Tap(arrow),Is.False);
            foreach(var fixedCell in session.ProtectedCells)Assert.That(session.Tap(fixedCell),Is.False);
            shell.ClearBoard();Assert.That(session.Marks.Values,Is.All.EqualTo(PlayerMark.Empty));
            foreach(var cat in session.Puzzle.SolutionCats){session.Tap(cat);session.Tap(cat);}
            Assert.That(session.IsCompleted,Is.True);Assert.That(session.Tap(normal),Is.False);Assert.That(session.Clear(),Is.False);
            Assert.That(shell.StartNewGame(),Is.True);Assert.That(shell.ActiveSession.Puzzle.Size,Is.EqualTo(size));
            Assert.That(shell.ActiveSession.Puzzle.Seed,Is.EqualTo(15));Assert.That(shell.Loop.SelectedDifficulty.Id,Is.EqualTo(id));
        }
        [Test] public void ChoosingNextDifficultyPreservesLiveBoardAndItsHeaderAcrossMenuAndLocale()
        {
            var shell=new GameShellView(14,"en",enableTutorial:false);Assert.That(shell.StartNewGame(),Is.True);
            var first=shell.ActiveSession;shell.ShowMenu();shell.SelectDifficulty("hard");shell.SelectLanguage("es");shell.BackToGame();
            Assert.That(shell.ActiveSession,Is.SameAs(first));Assert.That(first.Puzzle.Size,Is.EqualTo(5));
            Assert.That(shell.Q<Label>("board-difficulty").text,Is.EqualTo("Fácil · 5 × 5"));
            shell.SelectDifficulty("medium");Assert.That(shell.Loop.SelectedDifficulty.Id,Is.EqualTo("hard"),"choice is a menu-only intent");
            Assert.That(shell.StartNewGame(),Is.True);Assert.That(shell.ActiveSession.Puzzle.Size,Is.EqualTo(7));
        }
        [TestCase("medium")] [TestCase("hard")]
        public void FirstPlayAndReplayStayTheExactFixedEasyTutorialAfterHigherSelection(string id)
        {
            var seen=false;var shell=new GameShellView(14,"en",tutorialSeen:()=>seen,markTutorialSeen:()=>seen=true);
            shell.SelectDifficulty(id);shell.StartPlaying();
            Assert.That(shell.Tutorial,Is.Not.Null);Assert.That(shell.ActiveSession.Puzzle.Size,Is.EqualTo(5));
            Assert.That(shell.ActiveSession.Puzzle.Seed,Is.EqualTo(TutorialFixture.Seed));
            Assert.That(GenerationFingerprints.ForBoard(shell.ActiveSession.Puzzle),Is.EqualTo(GenerationFingerprints.ForBoard(TutorialFixture.CreatePuzzle())));
            Assert.That(shell.Loop.RequestedSeed,Is.Null);Assert.That(shell.Q<Label>("board-difficulty").text,Is.EqualTo("Easy · 5 × 5"));
            shell.StartTutorial();Assert.That(shell.Tutorial.Step,Is.EqualTo(TutorialStep.Welcome));
            shell.SkipTutorial();Assert.That(shell.StartNewGame(),Is.True);
            Assert.That(shell.Tutorial,Is.Null);Assert.That(shell.ActiveSession.Puzzle.Size,Is.EqualTo(id=="medium"?6:7));
            Assert.That(shell.ActiveSession.Puzzle.Seed,Is.EqualTo(14));
        }
        [TestCase("medium")] [TestCase("hard")]
        public void FailureAndRetryRetainRequestedDifficultyAndConsumeNextSeed(string id)
        {
            var generator=new RetryGenerator();var loop=new EasyGameLoop(14,generator:generator);
            var shell=new GameShellView(14,"en",loop,enableTutorial:false);shell.SelectDifficulty(id);
            Assert.That(shell.StartNewGame(),Is.False);Assert.That(shell.Screen,Is.EqualTo("failure"));
            Assert.That(loop.SelectedDifficulty.Id,Is.EqualTo(id));Assert.That(shell.Q<Label>("board-difficulty").text,Does.StartWith(id=="medium"?"Medium":"Hard"));
            Assert.That(shell.StartNewGame(),Is.True);Assert.That(loop.RequestedSeed,Is.EqualTo(15));
            Assert.That(loop.SessionDifficulty.Id,Is.EqualTo(id));Assert.That(generator.Requested,Is.EqualTo(new[]{id,id}));
        }
        [Test] public void UnsupportedMenuChoiceCannotChangeCurrentDifficulty()
        { var loop=new EasyGameLoop(14);Assert.Throws<ArgumentException>(()=>loop.SelectDifficulty("unknown"));Assert.That(loop.SelectedDifficulty.Id,Is.EqualTo("easy")); }
        [Test] public void WrongProfileInjectedSuccessCannotReachGameplay()
        {
            var loop=new EasyGameLoop(14,seed=>new SeededEasyPuzzleGenerator().Generate(DifficultyProfile.Easy,seed));
            loop.SelectDifficulty("hard");Assert.That(loop.NewGame(),Is.False);Assert.That(loop.Session,Is.Null);
        }

        // Issue #37: a real generated one-cat-arrow consequence remains a manual
        // player deduction, including the cells formerly hatched behind arrows.
        [Test]
        public void HardConsequencesStayVisibleAsEmptyInteractiveCellsUntilPlayerMarksThem()
        {
            var loop = new EasyGameLoop(0);
            loop.SelectDifficulty("hard");
            var consequenceCellsChecked = 0;
            for (var seed = 0; seed < 16; seed++)
            {
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
            var loop = new EasyGameLoop(0);
            loop.SelectDifficulty("hard");
            var hatchesChecked = 0;
            for (var seed = 0; seed < 16; seed++)
            {
                loop.SelectDifficulty(id);
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
        public void HardMenuChoiceAndReturnNeverReapplyConsequenceHatchesToLiveHardBoard()
        {
            var shell = new GameShellView(0, "en", tutorialSeen: () => true, markTutorialSeen: () => { });
            shell.SelectDifficulty("hard");
            Assert.That(shell.StartNewGame(), Is.True);
            var hard = shell.ActiveSession;
            shell.ShowMenu(); shell.SelectDifficulty("medium"); shell.SelectLanguage("es"); shell.BackToGame();
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
