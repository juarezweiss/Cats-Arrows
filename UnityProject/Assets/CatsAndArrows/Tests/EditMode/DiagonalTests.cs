using System;
using System.Linq;
using CatsAndArrows.Core;
using NUnit.Framework;

namespace CatsAndArrows.Tests
{
    // Issue #14: analytic coordinate comparisons and exhaustive row subsets are
    // independent of the production ray traversal, deductions and search pruning.
    public sealed class DiagonalTests
    {
        private static BoardCoordinate C(int row, int column) => new BoardCoordinate(row, column);
        private static ArrowClue A(int row, int column, Direction direction) => new ArrowClue(C(row, column), direction);
        private static PuzzleClues Clues(int[] rows, int[] columns, params ArrowClue[] arrows) =>
            new PuzzleClues(rows.Length, 14, "issue-14-fixture", rows, columns, arrows);

        [Test]
        public void ExistingDirectionValuesRemainStableAndAllEightRaysMatchAnalyticGeometry()
        {
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3 }, new[] {
                (int)Direction.Up, (int)Direction.Down, (int)Direction.Left, (int)Direction.Right });
            for (var row = 0; row < 5; row++)
            for (var column = 0; column < 5; column++)
            foreach (Direction direction in Enum.GetValues(typeof(Direction)))
            {
                var origin = C(row, column);
                var expected = Enumerable.Range(0, 25).Select(i => C(i / 5, i % 5))
                    .Where(p => SolverTests.PointsAt(origin, direction, p)).ToArray();
                var actual = DirectionGeometry.GetDirectionalSegment(5, origin, direction).ToArray();
                CollectionAssert.AreEquivalent(expected, actual, origin + ":" + direction);
                Assert.That(actual.Distinct().Count(), Is.EqualTo(actual.Length));
                Assert.That(actual.Contains(origin), Is.False);
                for (var i = 1; i < actual.Length; i++)
                    Assert.That(Math.Abs(actual[i].Row - row) + Math.Abs(actual[i].Column - column),
                        Is.GreaterThan(Math.Abs(actual[i - 1].Row - row) + Math.Abs(actual[i - 1].Column - column)));
            }
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                DirectionGeometry.GetDirectionalSegment(5, C(2, 2), (Direction)999).ToArray());
        }

        [TestCase(Direction.DownRight, 0, 0, 1, 1, 3, 3)]
        [TestCase(Direction.DownLeft, 0, 3, 1, 2, 3, 0)]
        [TestCase(Direction.UpRight, 3, 0, 2, 1, 0, 3)]
        [TestCase(Direction.UpLeft, 3, 3, 2, 2, 0, 0)]
        public void DiagonalRaysPassThroughArrowsAndShareOneCat(Direction direction,
            int r0, int c0, int r1, int c1, int catRow, int catColumn)
        {
            var rows = new int[4]; var columns = new int[4];
            rows[catRow] = 1; columns[catColumn] = 1;
            var arrows = new[] { A(r0, c0, direction), A(r1, c1, direction) };
            var clues = Clues(rows, columns, arrows);
            AssertSoundAndUnique(clues, C(catRow, catColumn));
            var puzzle = new PuzzleDefinition(4, 14, "fixture", new[] { C(catRow, catColumn) }, arrows);
            Assert.That(PuzzleValidator.Validate(puzzle, new DifficultyProfile("fixture", 4, 4, 4, 4)).IsValid, Is.True);
        }

        [TestCase(Direction.UpLeft, 0, 0)]
        [TestCase(Direction.UpRight, 0, 2)]
        [TestCase(Direction.DownLeft, 2, 0)]
        [TestCase(Direction.DownRight, 2, 2)]
        [TestCase(Direction.UpLeft, 1, 0)]
        [TestCase(Direction.UpRight, 1, 2)]
        [TestCase(Direction.DownLeft, 0, 0)]
        [TestCase(Direction.DownRight, 0, 2)]
        public void OutwardCornerAndEdgeArrowsAreImpossible(Direction direction, int row, int column)
        {
            var arrow = A(row, column, direction);
            var clues = Clues(new[] { 0, 0, 0 }, new[] { 0, 0, 0 }, arrow);
            Assert.That(DirectionGeometry.GetDirectionalSegment(3, arrow.Coordinate, direction), Is.Empty);
            Assert.That(SolverTests.EnumerateSolutions(clues), Is.Empty);
            Assert.That(new HumanDeductionSolver().Solve(clues).Status, Is.EqualTo(HumanSolverStatus.Contradiction));
            Assert.That(new UniquenessVerifier().Verify(clues).Status, Is.EqualTo(UniquenessStatus.NoSolution));
            var puzzle = new PuzzleDefinition(3, 14, "fixture", Array.Empty<BoardCoordinate>(), new[] { arrow });
            Assert.That(PuzzleValidator.Validate(puzzle, new DifficultyProfile("fixture", 3, 3, 3, 3)).IsValid, Is.False);
        }

        [Test]
        public void DiagonalAtLeastOneAllowsMultipleCatsWithoutExactlyOneDeduction()
        {
            var clues = Clues(new[] { 0, 2, 1, 0 }, new[] { 0, 1, 2, 0 }, A(0, 0, Direction.DownRight));
            AssertSoundAndUnique(clues, C(1, 1), C(1, 2), C(2, 2));
            Assert.That(new HumanDeductionSolver().Solve(clues).Steps.Any(s => s.RuleId == DeductionRules.ArrowSingleton), Is.False);
        }

        [Test]
        public void UnsatisfiedDiagonalSingletonForcesItsOnlyRemainingCandidate()
        {
            var clues = Clues(new[] { 0, 1, 1 }, new[] { 1, 1, 0 }, A(0, 0, Direction.DownRight));
            var result = new HumanDeductionSolver().Solve(clues);
            Assert.That(result.Status, Is.EqualTo(HumanSolverStatus.Solved));
            Assert.That(result.Steps.Any(s => s.RuleId == DeductionRules.ArrowSingleton &&
                s.Conclusions.Any(a => a.Coordinate == C(1, 1) && a.State == CellKnowledge.Cat)), Is.True);
            AssertSoundAndUnique(clues, C(1, 1), C(2, 0));
        }

        [Test]
        public void SatisfiedDiagonalDoesNotForceItsRemainingUnknownCandidate()
        {
            var clues = Clues(new[] { 1, 1, 1, 1 }, new[] { 1, 1, 1, 1 },
                A(0, 1, Direction.DownRight), A(1, 1, Direction.Right), A(1, 3, Direction.Left));
            var oracle = SolverTests.EnumerateSolutions(clues);
            Assert.That(oracle.Count, Is.GreaterThan(1));
            var result = new HumanDeductionSolver().Solve(clues);
            Assert.That(result.Status, Is.EqualTo(HumanSolverStatus.Stalled));
            Assert.That(result.Assignments.Single(a => a.Coordinate == C(1, 2)).State, Is.EqualTo(CellKnowledge.Cat));
            Assert.That(result.Assignments.Single(a => a.Coordinate == C(2, 3)).State, Is.EqualTo(CellKnowledge.Unknown));
            SolverTests.AssertTraceSound(clues, result, oracle);
        }

        [Test]
        public void DiagonalNeverUsesTheUnsoundCardinalLineExclusionRule()
        {
            // A diagonal cat can satisfy the arrow while a cat outside that ray
            // occupies the same row as another ray cell. Cardinal line exclusion
            // would incorrectly remove (2,0), one of the legal solution cats.
            var clues = Clues(new[] { 0, 1, 1, 1 }, new[] { 1, 1, 1, 0 }, A(0, 0, Direction.DownRight));
            var oracle = SolverTests.EnumerateSolutions(clues);
            Assert.That(oracle.Any(s => s.SetEquals(new[] { C(1, 1), C(2, 0), C(3, 2) })), Is.True);
            var result = new HumanDeductionSolver().Solve(clues);
            Assert.That(result.Status, Is.Not.EqualTo(HumanSolverStatus.Contradiction));
            Assert.That(result.Steps.Any(s => s.RuleId == DeductionRules.ArrowLineExclusion), Is.False);
            SolverTests.AssertTraceSound(clues, result, oracle);
            Assert.That(new UniquenessVerifier().Verify(clues).Status, Is.EqualTo(UniquenessStatus.Multiple));
        }

        [Test]
        public void ThreeByThreeDiagonalCluesAgreeWithIndependentExhaustiveOracle()
        {
            foreach (var direction in new[] { Direction.UpLeft, Direction.UpRight, Direction.DownLeft, Direction.DownRight })
            for (var row = 0; row < 3; row++)
            for (var column = 0; column < 3; column++)
            {
                var clues = Clues(new[] { 1, 1, 1 }, new[] { 1, 1, 1 }, A(row, column, direction));
                var oracle = SolverTests.EnumerateSolutions(clues);
                var verifier = new UniquenessVerifier().Verify(clues);
                Assert.That(verifier.Status, Is.EqualTo(oracle.Count == 0 ? UniquenessStatus.NoSolution :
                    oracle.Count == 1 ? UniquenessStatus.Unique : UniquenessStatus.Multiple));
                foreach (var witness in verifier.Solutions)
                    Assert.That(oracle.Any(s => s.SetEquals(witness)), Is.True);
                var human = new HumanDeductionSolver().Solve(clues);
                if (oracle.Count > 0) SolverTests.AssertTraceSound(clues, human, oracle);
                if (human.Status == HumanSolverStatus.Solved) Assert.That(oracle.Count, Is.EqualTo(1));
            }
        }

        private static void AssertSoundAndUnique(PuzzleClues clues, params BoardCoordinate[] expected)
        {
            var oracle = SolverTests.EnumerateSolutions(clues);
            Assert.That(oracle.Count, Is.EqualTo(1));
            CollectionAssert.AreEquivalent(expected, oracle.Single());
            var human = new HumanDeductionSolver().Solve(clues);
            Assert.That(human.Status, Is.EqualTo(HumanSolverStatus.Solved));
            SolverTests.AssertTraceSound(clues, human, oracle);
            var verifier = new UniquenessVerifier().Verify(clues);
            Assert.That(verifier.Status, Is.EqualTo(UniquenessStatus.Unique));
            CollectionAssert.AreEquivalent(expected, verifier.Solutions.Single());
        }
    }
}
