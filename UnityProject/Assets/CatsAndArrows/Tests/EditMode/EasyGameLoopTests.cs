using System;
using System.Collections.Generic;
using System.Linq;
using CatsAndArrows.Core;
using CatsAndArrows.Gameplay;
using NUnit.Framework;

namespace CatsAndArrows.Tests
{
    // Issue #8: seeded admission, clue-derived protection, and explainable assistance.
    public sealed class EasyGameLoopTests
    {
        [TestCase(14)]
        [TestCase(int.MaxValue)]
        public void NewGamesConsumeDistinctSeedsAndKeepEasyDifficulty(int initialSeed)
        {
            var requested = new List<int>();
            var generator = new SeededEasyPuzzleGenerator();
            var loop = PlayTestFactory.Loop(initialSeed, seed =>
            {
                requested.Add(seed);
                return generator.Generate(DifficultyProfile.Easy, seed);
            });
            Assert.That(loop.Session, Is.Null);
            Assert.That(loop.NewGame(), Is.True);
            var first = loop.Session;
            Assert.That(first.Puzzle.Seed, Is.EqualTo(initialSeed));
            Assert.That(loop.NewGame(), Is.True);
            Assert.That(loop.Session, Is.Not.SameAs(first));
            Assert.That(requested, Is.EqualTo(new[] { initialSeed, unchecked(initialSeed + 1) }));
            Assert.That(loop.Session.Puzzle.Seed, Is.EqualTo(unchecked(initialSeed + 1)));
            Assert.That(loop.RequestedDifficulty.Id, Is.EqualTo("easy"));
            Assert.That(loop.RequestedDifficulty.Size, Is.EqualTo(5));
            Assert.That(loop.Session.Marks.Values, Is.All.EqualTo(PlayerMark.Empty));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void FailedGenerationDropsPreviousSessionAndRetryUsesNextSeed(bool returnNull)
        {
            var generator = new SeededEasyPuzzleGenerator();
            var attempts = 0;
            var loop = PlayTestFactory.Loop(14, seed => ++attempts == 2
                ? (returnNull ? null : generator.Generate(DifficultyProfile.Easy, seed, new GenerationOptions(maxAttempts: 0)))
                : generator.Generate(DifficultyProfile.Easy, seed));
            Assert.That(loop.NewGame(), Is.True);
            Assert.That(loop.NewGame(), Is.False);
            Assert.That(loop.Session, Is.Null);
            Assert.That(loop.LastGenerationStatus, Is.EqualTo(GenerationStatus.Exhausted));
            Assert.That(loop.RequestedSeed, Is.EqualTo(15));
            Assert.That(loop.NewGame(), Is.True);
            Assert.That(loop.Session.Puzzle.Seed, Is.EqualTo(16));
        }

        [TestCase(Direction.Left, 1, 0, 1, 2)]
        [TestCase(Direction.Right, 1, 2, 1, 0)]
        [TestCase(Direction.Up, 0, 1, 2, 1)]
        [TestCase(Direction.Down, 2, 1, 0, 1)]
        public void OriginalSingleCatCardinalLineProtectsOnlyItsOppositeNormalCell(
            Direction direction, int catRow, int catColumn, int protectedRow, int protectedColumn)
        {
            var puzzle = new PuzzleDefinition(3, 8, "issue8-fixture",
                new[] { new BoardCoordinate(catRow, catColumn) },
                new[] { new ArrowClue(new BoardCoordinate(1, 1), direction) });
            var protection = ClueProtection.Derive(PuzzleClues.FromPuzzle(puzzle));
            Assert.That(protection, Is.EquivalentTo(new[] { new BoardCoordinate(protectedRow, protectedColumn) }));
            var session = new GameSession(puzzle, protection);
            Assert.That(session.Tap(protection[0]), Is.False);
            Assert.That(session.ApplyDrag(protection), Is.Zero);
            Assert.That(session.Clear(), Is.True);
            Assert.That(session.ProtectedCells, Is.EquivalentTo(protection));
        }

        [Test]
        public void DiagonalArrowDoesNotCreateCardinalOppositeProtection()
        {
            var puzzle = new PuzzleDefinition(3, 8, "issue8-fixture",
                new[] { new BoardCoordinate(0, 0) },
                new[] { new ArrowClue(new BoardCoordinate(1, 1), Direction.UpLeft) });
            Assert.That(ClueProtection.Derive(PuzzleClues.FromPuzzle(puzzle)), Is.Empty);
        }

        [Test]
        public void OriginalTwoCatLineNeverProtectsOppositeSideEvenWhenSolverResolvesIt()
        {
            var puzzle = new PuzzleDefinition(3, 8, "issue8-fixture",
                new[] { new BoardCoordinate(1, 0), new BoardCoordinate(1, 2) },
                new[] { new ArrowClue(new BoardCoordinate(1, 1), Direction.Right) });
            Assert.That(new HumanDeductionSolver().Solve(PuzzleClues.FromPuzzle(puzzle)).Status,
                Is.EqualTo(HumanSolverStatus.Solved));
            Assert.That(ClueProtection.Derive(PuzzleClues.FromPuzzle(puzzle)), Is.Empty);
        }

        [Test]
        public void ClearPreservesSeedCluesProtectionAndRestartsHintProofScan()
        {
            var loop = PlayTestFactory.Loop(14);
            Assert.That(loop.NewGame(), Is.True);
            var session = loop.Session;
            var puzzle = session.Puzzle;
            var protection = session.ProtectedCells.ToArray();
            var first = session.ApplyHint();
            Assert.That(first.Status, Is.EqualTo(HintStatus.Applied));
            Assert.That(session.Clear(), Is.True);
            Assert.That(session.Puzzle, Is.SameAs(puzzle));
            Assert.That(session.ProtectedCells, Is.EquivalentTo(protection));
            Assert.That(session.Marks.Values, Is.All.EqualTo(PlayerMark.Empty));
            var repeated = session.ApplyHint();
            Assert.That(repeated.Assignment.Value.Coordinate, Is.EqualTo(first.Assignment.Value.Coordinate));
            Assert.That(repeated.Assignment.Value.State, Is.EqualTo(first.Assignment.Value.State));
        }

        [Test]
        public void HintUsesClueProofDespiteIncorrectPlayerCatsAndCorrectsOnlyOneFact()
        {
            var puzzle = OneCatPuzzle();
            var session = new GameSession(puzzle);
            var proof = new HumanDeductionSolver().Solve(PuzzleClues.FromPuzzle(puzzle));
            var first = proof.Steps.SelectMany(step => step.Conclusions).First();
            Assert.That(first.State, Is.EqualTo(CellKnowledge.NotCat));
            PlaceCat(session, first.Coordinate);
            var before = session.Marks.ToDictionary(pair => pair.Key, pair => pair.Value);
            var hint = session.ApplyHint();
            Assert.That(hint.Status, Is.EqualTo(HintStatus.Applied));
            Assert.That(hint.Corrected, Is.True);
            Assert.That(hint.Assignment.Value.Coordinate, Is.EqualTo(first.Coordinate));
            Assert.That(session.GetMark(first.Coordinate), Is.EqualTo(PlayerMark.X));
            Assert.That(session.Marks.Count(pair => before[pair.Key] != pair.Value), Is.EqualTo(1));
            Assert.That(hint.Step.Conclusions.Any(assignment => assignment.Coordinate.Equals(first.Coordinate)
                && assignment.State == CellKnowledge.NotCat), Is.True);
        }

        [Test]
        public void HintCorrectsWrongXOnCatAndCompletesImmediatelyThenBecomesReadOnly()
        {
            var session = new GameSession(OneCatPuzzle());
            var cat = new BoardCoordinate(1, 1);
            Assert.That(session.Tap(cat), Is.True);
            HintResult hint = null;
            for (var request = 0; request < 9 && !session.IsCompleted; request++) hint = session.ApplyHint();
            Assert.That(session.IsCompleted, Is.True);
            Assert.That(hint.Assignment.Value.Coordinate, Is.EqualTo(cat));
            Assert.That(hint.Corrected, Is.True);
            Assert.That(session.GetMark(cat), Is.EqualTo(PlayerMark.Cat));
            var completed = session.Marks.ToArray();
            Assert.That(session.ApplyHint().Status, Is.EqualTo(HintStatus.Completed));
            Assert.That(session.Clear(), Is.False);
            Assert.That(session.Marks, Is.EquivalentTo(completed));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void StalledOrInvalidClueProofNeverMutatesPlayerMarks(bool invalid)
        {
            var puzzle = new PuzzleDefinition(2, 1, "issue8-fixture",
                new[] { new BoardCoordinate(0, 0), new BoardCoordinate(1, 1) },
                invalid ? new[] { new ArrowClue(new BoardCoordinate(2, 0), Direction.Left) } : Array.Empty<ArrowClue>());
            var session = new GameSession(puzzle);
            session.Tap(new BoardCoordinate(0, 1));
            var before = session.Marks.ToArray();
            Assert.That(session.ApplyHint().Status, Is.EqualTo(HintStatus.Unavailable));
            Assert.That(session.Marks, Is.EquivalentTo(before));
            Assert.That(ClueProtection.Derive(PuzzleClues.FromPuzzle(puzzle)), Is.Empty);
        }

        [Test]
        public void TwentySeedCorpusHintsAreIndependentProvenFactsAndSolveEveryAdmittedBoard()
        {
            for (var seed = 0; seed < 20; seed++)
            {
                var loop = PlayTestFactory.Loop(seed);
                Assert.That(loop.NewGame(), Is.True, "seed " + seed);
                var session = loop.Session;
                var proof = new HumanDeductionSolver().Solve(PuzzleClues.FromPuzzle(session.Puzzle));
                Assert.That(proof.Status, Is.EqualTo(HumanSolverStatus.Solved));
                foreach (var protectedCell in session.ProtectedCells)
                {
                    Assert.That(session.Puzzle.IsArrow(protectedCell), Is.False);
                    Assert.That(proof.Assignments.Any(a => a.Coordinate.Equals(protectedCell)
                        && a.State == CellKnowledge.NotCat), Is.True);
                }
                for (var request = 0; request < 25 && !session.IsCompleted; request++)
                {
                    var before = session.Marks.ToDictionary(pair => pair.Key, pair => pair.Value);
                    var hint = session.ApplyHint();
                    Assert.That(hint.Status, Is.EqualTo(HintStatus.Applied), "seed " + seed);
                    var assignment = hint.Assignment.Value;
                    Assert.That(proof.Steps.SelectMany(step => step.Conclusions).Any(a =>
                        a.Coordinate.Equals(assignment.Coordinate) && a.State == assignment.State), Is.True);
                    Assert.That(session.Marks.Count(pair => before[pair.Key] != pair.Value), Is.EqualTo(1));
                    Assert.That(session.Puzzle.IsArrow(assignment.Coordinate), Is.False);
                    Assert.That(session.ProtectedCells.Contains(assignment.Coordinate), Is.False);
                }
                Assert.That(session.IsCompleted, Is.True, "seed " + seed);
            }
        }

        private static PuzzleDefinition OneCatPuzzle() => new PuzzleDefinition(3, 1, "issue8-fixture",
            new[] { new BoardCoordinate(1, 1) }, Array.Empty<ArrowClue>());

        private static void PlaceCat(GameSession session, BoardCoordinate cell)
        {
            Assert.That(session.Tap(cell), Is.True);
            Assert.That(session.Tap(cell), Is.True);
        }
    }
}
