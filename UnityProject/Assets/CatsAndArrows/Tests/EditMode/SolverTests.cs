using System;
using System.Collections.Generic;
using System.Linq;
using CatsAndArrows.Core;
using NUnit.Framework;

namespace CatsAndArrows.Tests
{
    // Issue #3: the oracle deliberately uses its own coordinate comparisons and exhaustive
    // row subsets, rather than production ray geometry, deductions, or search pruning.
    public sealed class SolverTests
    {
        private static BoardCoordinate C(int row, int column) => new BoardCoordinate(row, column);
        private static ArrowClue A(int row, int column, Direction direction) => new ArrowClue(C(row, column), direction);
        private static PuzzleClues Clues(int[] rows, int[] columns, params ArrowClue[] arrows) =>
            new PuzzleClues(rows.Length, 3, "issue-3-fixture", rows, columns, arrows);

        [Test]
        public void PawKnowledgeKeepsLegacyNumericValues()
        {
            Assert.That((int)CellKnowledge.Unknown, Is.Zero);
            Assert.That((int)CellKnowledge.Paw, Is.EqualTo(1));
            Assert.That((int)CellKnowledge.NotPaw, Is.EqualTo(2));
        }

        [Test]
        public void FiveByFiveTargetCompletesAndTraceReplaysWithoutGuessing()
        {
            var clues = Clues(new[] { 1, 1, 1, 1, 1 }, new[] { 1, 1, 1, 1, 1 },
                A(0, 1, Direction.Left), A(1, 2, Direction.Left), A(2, 3, Direction.Left),
                A(3, 4, Direction.Left), A(4, 3, Direction.Right));
            var human = new HumanDeductionSolver().Solve(clues);
            Assert.That(human.Status, Is.EqualTo(HumanSolverStatus.Solved));
            var expected = Enumerable.Range(0, 5).Select(i => C(i, i)).ToArray();
            CollectionAssert.AreEquivalent(expected, human.Assignments.Where(a => a.State == CellKnowledge.Paw).Select(a => a.Coordinate));
            var unique = new UniquenessVerifier().Verify(clues);
            Assert.That(unique.Status, Is.EqualTo(UniquenessStatus.Unique));
            CollectionAssert.AreEquivalent(expected, unique.Solutions.Single());
            AssertTraceSound(clues, human, EnumerateSolutions(clues));
        }

        [Test]
        public void ApprovedRuleVocabularyHasExplicitSoundConclusions()
        {
            var fixtures = new[]
            {
                Clues(new[] { 2, 0 }, new[] { 1, 1 }),
                Clues(new[] { 1, 1, 1 }, new[] { 1, 1, 1 }, A(0, 1, Direction.Right)),
                Clues(new[] { 1, 1, 1, 1 }, new[] { 1, 1, 1, 1 }, A(0, 1, Direction.Right))
            };
            var kinds = new HashSet<string>();
            foreach (var clues in fixtures)
            {
                var result = new HumanDeductionSolver().Solve(clues);
                AssertTraceSound(clues, result, EnumerateSolutions(clues));
                foreach (var step in result.Steps) kinds.Add(step.RuleId);
            }
            CollectionAssert.IsSubsetOf(new[] { "zero-line", "line-satisfied", "fill-remaining-line", "arrow-singleton", "arrow-line-exclusion" }, kinds);
        }

        [Test]
        public void ExhaustiveTwoByTwoCluesAgreeWithIndependentOracle()
        {
            // All row/column totals, with no arrows and each single cardinal or diagonal arrow.
            for (var r0 = 0; r0 <= 2; r0++)
            for (var r1 = 0; r1 <= 2; r1++)
            for (var c0 = 0; c0 <= 2; c0++)
            for (var c1 = 0; c1 <= 2; c1++)
            {
                var variants = new List<ArrowClue[]> { Array.Empty<ArrowClue>() };
                for (var row = 0; row < 2; row++)
                for (var col = 0; col < 2; col++)
                foreach (Direction direction in Enum.GetValues(typeof(Direction)))
                    variants.Add(new[] { A(row, col, direction) });
                foreach (var arrows in variants)
                {
                    var clues = Clues(new[] { r0, r1 }, new[] { c0, c1 }, arrows);
                    var oracle = EnumerateSolutions(clues);
                    var verifier = new UniquenessVerifier().Verify(clues);
                    Assert.That(verifier.Status, Is.EqualTo(oracle.Count == 0 ? UniquenessStatus.NoSolution :
                        oracle.Count == 1 ? UniquenessStatus.Unique : UniquenessStatus.Multiple),
                        $"rows={r0},{r1}, columns={c0},{c1}, arrows={arrows.Length}");
                    Assert.That(verifier.Solutions.Count, Is.EqualTo(Math.Min(2, oracle.Count)));
                    foreach (var witness in verifier.Solutions)
                        Assert.That(oracle.Any(s => new HashSet<BoardCoordinate>(s).SetEquals(witness)), Is.True);
                    var human = new HumanDeductionSolver().Solve(clues);
                    if (oracle.Count > 0) AssertTraceSound(clues, human, oracle);
                    if (human.Status == HumanSolverStatus.Solved) Assert.That(oracle.Count, Is.EqualTo(1));
                }
            }
        }

        [TestCase(Direction.Right)]
        [TestCase(Direction.Down)]
        [TestCase(Direction.Left)]
        [TestCase(Direction.Up)]
        public void RaysContinueThroughArrowsAndOverlappingArrowsShareOnePaw(Direction direction)
        {
            var coordinates = new[] { C(0, 0), C(0, 1), C(0, 3) };
            Func<BoardCoordinate, BoardCoordinate> rotate = p => p;
            if (direction == Direction.Down) rotate = p => C(p.Column, 3 - p.Row);
            if (direction == Direction.Left) rotate = p => C(3 - p.Row, 3 - p.Column);
            if (direction == Direction.Up) rotate = p => C(3 - p.Column, p.Row);
            var paw = rotate(coordinates[2]);
            var rows = new int[4]; var cols = new int[4];
            rows[paw.Row] = 1; cols[paw.Column] = 1;
            var arrows = coordinates.Take(2).Select(p => new ArrowClue(rotate(p), direction)).ToArray();
            var clues = Clues(rows, cols, arrows);
            var result = new HumanDeductionSolver().Solve(clues);
            Assert.That(result.Status, Is.EqualTo(HumanSolverStatus.Solved));
            AssertTraceSound(clues, result, EnumerateSolutions(clues));
            Assert.That(new UniquenessVerifier().Verify(clues).Status, Is.EqualTo(UniquenessStatus.Unique));
        }

        [Test]
        public void AtLeastOneArrowAllowsTwoPawsAndAlreadySatisfiedArrowDoesNotForceAnother()
        {
            var clues = Clues(new[] { 2, 0, 0 }, new[] { 0, 1, 1 }, A(0, 0, Direction.Right));
            var result = new HumanDeductionSolver().Solve(clues);
            Assert.That(result.Status, Is.EqualTo(HumanSolverStatus.Solved));
            Assert.That(result.Assignments.Count(a => a.State == CellKnowledge.Paw), Is.EqualTo(2));
            Assert.That(result.Steps.Any(s => s.RuleId == "arrow-singleton"), Is.False);
            AssertTraceSound(clues, result, EnumerateSolutions(clues));
        }

        [Test]
        public void SatisfiedArrowWithOneUnknownCandidateDoesNotForceThatCandidate()
        {
            var clues = Clues(new[] { 2, 2, 1, 1 }, new[] { 1, 0, 4, 1 }, A(0, 1, Direction.Right));
            var result = new HumanDeductionSolver().Solve(clues);
            Assert.That(result.Status, Is.EqualTo(HumanSolverStatus.Stalled));
            Assert.That(result.Assignments.Single(a => a.Coordinate == C(0, 2)).State, Is.EqualTo(CellKnowledge.Paw));
            Assert.That(result.Assignments.Single(a => a.Coordinate == C(0, 3)).State, Is.EqualTo(CellKnowledge.Unknown));
            Assert.That(new UniquenessVerifier().Verify(clues).Status, Is.EqualTo(UniquenessStatus.Multiple));
            AssertTraceSound(clues, result, EnumerateSolutions(clues));
        }

        [Test]
        public void AmbiguousAndContradictoryCluesAreDistinguished()
        {
            var ambiguous = Clues(new[] { 1, 1 }, new[] { 1, 1 });
            Assert.That(new HumanDeductionSolver().Solve(ambiguous).Status, Is.EqualTo(HumanSolverStatus.Stalled));
            Assert.That(new UniquenessVerifier().Verify(ambiguous).Status, Is.EqualTo(UniquenessStatus.Multiple));
            var impossible = Clues(new[] { 1, 0 }, new[] { 0, 1 }, A(0, 1, Direction.Right));
            Assert.That(new HumanDeductionSolver().Solve(impossible).Status, Is.EqualTo(HumanSolverStatus.Contradiction));
            Assert.That(new UniquenessVerifier().Verify(impossible).Status, Is.EqualTo(UniquenessStatus.NoSolution));
        }

        [Test]
        public void MalformedInputsAreReportedInsteadOfSearching()
        {
            var malformed = new[]
            {
                new PuzzleClues(0, 3, "fixture", Array.Empty<int>(), Array.Empty<int>(), Array.Empty<ArrowClue>()),
                Clues(new[] { -1, 1 }, new[] { 0, 0 }),
                new PuzzleClues(2, 3, "fixture", new[] { 1 }, new[] { 1, 0 }, Array.Empty<ArrowClue>()),
                Clues(new[] { 1, 1 }, new[] { 1, 1 }, A(2, 0, Direction.Down)),
                Clues(new[] { 1, 1 }, new[] { 1, 1 }, A(0, 0, (Direction)999)),
                Clues(new[] { 1, 1 }, new[] { 1, 1 }, A(0, 0, Direction.Down), A(0, 0, Direction.Right))
            };
            foreach (var clues in malformed)
            {
                Assert.That(new HumanDeductionSolver().Solve(clues).Status, Is.EqualTo(HumanSolverStatus.InvalidInput));
                Assert.That(new UniquenessVerifier().Verify(clues).Status, Is.EqualTo(UniquenessStatus.InvalidInput));
            }
        }

        [Test]
        public void SearchBudgetNeverMistakesFirstWitnessForUniqueness()
        {
            var clues = Clues(new[] { 1, 1 }, new[] { 1, 1 });
            var solver = new UniquenessVerifier();
            var before = solver.Verify(clues, new SolverOptions(maxVerifierNodes: 0));
            Assert.That(before.Status, Is.EqualTo(UniquenessStatus.Incomplete));
            Assert.That(before.Solutions, Is.Empty);
            var complete = solver.Verify(clues);
            var foundWitnessWhileIncomplete = false;
            for (var budget = 1; budget < complete.NodesVisited; budget++)
            {
                var limited = solver.Verify(clues, new SolverOptions(maxVerifierNodes: budget));
                if (limited.Solutions.Count == 1)
                {
                    Assert.That(limited.Status, Is.EqualTo(UniquenessStatus.Incomplete));
                    foundWitnessWhileIncomplete = true;
                }
            }
            Assert.That(foundWitnessWhileIncomplete, Is.True);
            Assert.That(new HumanDeductionSolver().Solve(clues, new SolverOptions(maxHumanConstraintEvaluations: 0)).Status,
                Is.EqualTo(HumanSolverStatus.BudgetExceeded));
        }

        [Test]
        public void CluesSnapshotAndResultsAreImmutableAndInputOrderDoesNotAffectTrace()
        {
            var rows = new[] { 1, 1, 1 }; var cols = new[] { 1, 1, 1 };
            var arrows = new List<ArrowClue> { A(0, 1, Direction.Left), A(1, 2, Direction.Left), A(2, 1, Direction.Right) };
            var arrowInput = arrows.ToArray();
            var clues = Clues(rows, cols, arrowInput);
            var reversed = Clues(rows, cols, arrows.AsEnumerable().Reverse().ToArray());
            rows[0] = 0; cols[0] = 0; arrows.Clear(); arrowInput[0] = A(0, 0, Direction.Up);
            var solver = new HumanDeductionSolver();
            var first = solver.Solve(clues);
            var repeat = solver.Solve(clues);
            var reordered = solver.Solve(reversed);
            Assert.That(first.Status, Is.EqualTo(HumanSolverStatus.Solved));
            CollectionAssert.AreEqual(TraceFingerprint(first), TraceFingerprint(repeat));
            CollectionAssert.AreEqual(TraceFingerprint(first), TraceFingerprint(reordered));
            Assert.That(first.Metrics.ConstraintEvaluations, Is.EqualTo(repeat.Metrics.ConstraintEvaluations));
            Assert.That(first.Metrics.MaximumDependencyDepth, Is.EqualTo(reordered.Metrics.MaximumDependencyDepth));
            Assert.That(first.Metrics.StepCount, Is.EqualTo(first.Steps.Count));
            Assert.That(first.Metrics.RuleCounts.Values.Sum(), Is.EqualTo(first.Steps.Count));
            Assert.That(first.Assignments is IList<CellAssignment> list && list.IsReadOnly, Is.True);
            Assert.That(first.Steps is IList<DeductionStep> steps && steps.IsReadOnly, Is.True);
            var unique = new UniquenessVerifier().Verify(clues);
            Assert.That(unique.Solutions is IList<IReadOnlyList<BoardCoordinate>> witnesses && witnesses.IsReadOnly, Is.True);
            Assert.That(unique.Solutions[0] is IList<BoardCoordinate> witness && witness.IsReadOnly, Is.True);
        }

        [Test]
        public void DifferentIntendedAnswersWithIdenticalVisibleCluesHaveIdenticalSolverResults()
        {
            var one = new PuzzleDefinition(2, 77, "fixture", new[] { C(0, 0), C(1, 1) }, Array.Empty<ArrowClue>());
            var two = new PuzzleDefinition(2, 88, "fixture", new[] { C(0, 1), C(1, 0) }, Array.Empty<ArrowClue>());
            var first = PuzzleClues.FromPuzzle(one); var second = PuzzleClues.FromPuzzle(two);
            var solver = new HumanDeductionSolver();
            CollectionAssert.AreEqual(TraceFingerprint(solver.Solve(first)), TraceFingerprint(solver.Solve(second)));
            var verifier = new UniquenessVerifier();
            Assert.That(verifier.Verify(first).Status, Is.EqualTo(UniquenessStatus.Multiple));
            Assert.That(verifier.Verify(second).Status, Is.EqualTo(UniquenessStatus.Multiple));
            Assert.That(verifier.Verify(first).NodesVisited, Is.EqualTo(verifier.Verify(second).NodesVisited));
        }

        [Test]
        public void UniquePuzzleBeyondApprovedDeductionVocabularyStallsAndIsRejected()
        {
            // Crossing disjoint reservations require a rule deliberately deferred from V1.
            // The independent oracle enumerates every legal row subset (42,000 boards).
            var arrows = new[]
            {
                A(0, 2, Direction.Left), A(0, 6, Direction.Down),
                A(2, 0, Direction.Up), A(2, 1, Direction.Right), A(2, 2, Direction.Down),
                A(2, 4, Direction.Down), A(2, 5, Direction.Down), A(2, 6, Direction.Up),
                A(3, 2, Direction.Left), A(3, 4, Direction.Right),
                A(4, 0, Direction.Down), A(4, 1, Direction.Right), A(4, 4, Direction.Right), A(4, 5, Direction.Left),
                A(5, 2, Direction.Left), A(5, 3, Direction.Right),
                A(6, 1, Direction.Right), A(6, 3, Direction.Right), A(6, 5, Direction.Left)
            };
            var clues = Clues(new[] { 1, 2, 1, 2, 3, 3, 3 }, new[] { 2, 1, 2, 3, 1, 2, 4 }, arrows);
            var expected = new[]
            {
                C(0, 0), C(1, 3), C(1, 6), C(2, 3), C(3, 1), C(3, 5),
                C(4, 2), C(4, 3), C(4, 6), C(5, 0), C(5, 5), C(5, 6), C(6, 2), C(6, 4), C(6, 6)
            };
            var oracle = EnumerateSolutions(clues);
            Assert.That(oracle.Count, Is.EqualTo(1));
            CollectionAssert.AreEquivalent(expected, oracle.Single());
            var human = new HumanDeductionSolver().Solve(clues);
            Assert.That(human.Status, Is.EqualTo(HumanSolverStatus.Stalled));
            AssertTraceSound(clues, human, oracle);
            var verifier = new UniquenessVerifier().Verify(clues);
            Assert.That(verifier.Status, Is.EqualTo(UniquenessStatus.Unique));
            CollectionAssert.AreEquivalent(expected, verifier.Solutions.Single());
            var puzzle = new PuzzleDefinition(7, 3, "issue-3-fixture", expected, arrows);
            // Generator profile limits are separate admission policy, not solver constraints.
            var profile = new DifficultyProfile("deduction-boundary-fixture", 7, 4, 0, 0);
            Assert.That(PuzzleValidator.Validate(puzzle, profile).IsValid, Is.True);
            var assessment = PuzzleSolvabilityAssessment.Assess(puzzle, profile);
            Assert.That(assessment.Status, Is.EqualTo(PuzzleAssessmentStatus.HumanRejected));
            Assert.That(assessment.IsAccepted, Is.False);
            Assert.That(assessment.Solver.HasUniqueSolution, Is.True);
        }

        [Test]
        public void CandidateAssessmentAcceptsOnlyHumanCompleteProvenUniqueProfileValidAnswer()
        {
            var paws = Enumerable.Range(0, 5).Select(i => C(i, i)).ToArray();
            var arrows = new[] { A(0, 1, Direction.Left), A(1, 2, Direction.Left),
                A(2, 3, Direction.Left), A(3, 4, Direction.Left), A(4, 3, Direction.Right) };
            var target = new PuzzleDefinition(5, 3, "issue-3-fixture", paws, arrows);
            var accepted = PuzzleSolvabilityAssessment.Assess(target, DifficultyProfile.Easy);
            Assert.That(accepted.Status, Is.EqualTo(PuzzleAssessmentStatus.Accepted));
            Assert.That(accepted.IsAccepted, Is.True);
            Assert.That(accepted.Solver.SolvedWithoutGuessing, Is.True);
            Assert.That(accepted.Solver.HasUniqueSolution, Is.True);
            Assert.That(PuzzleSolvabilityAssessment.Assess(target, DifficultyProfile.Medium).Status,
                Is.EqualTo(PuzzleAssessmentStatus.ProfileRejected));
            Assert.That(PuzzleSolvabilityAssessment.Assess(target, DifficultyProfile.Easy,
                new SolverOptions(maxVerifierNodes: 0)).Status, Is.EqualTo(PuzzleAssessmentStatus.UniquenessRejected));
            Assert.That(PuzzleSolvabilityAssessment.Assess(target, DifficultyProfile.Easy,
                new SolverOptions(maxHumanConstraintEvaluations: 0)).Status, Is.EqualTo(PuzzleAssessmentStatus.HumanRejected));
            var ambiguous = new PuzzleDefinition(5, 3, "issue-3-fixture", paws, Array.Empty<ArrowClue>());
            var rejected = PuzzleSolvabilityAssessment.Assess(ambiguous, DifficultyProfile.Easy);
            Assert.That(rejected.Status, Is.EqualTo(PuzzleAssessmentStatus.HumanRejected));
            Assert.That(rejected.Solver.Human.Status, Is.EqualTo(HumanSolverStatus.Stalled));
            Assert.That(rejected.Solver.Uniqueness.Status, Is.EqualTo(UniquenessStatus.Multiple));
            var falseArrow = new PuzzleDefinition(5, 3, "issue-3-fixture", paws, new[] { A(0, 3, Direction.Right) });
            Assert.That(PuzzleSolvabilityAssessment.Assess(falseArrow, DifficultyProfile.Easy).Status,
                Is.EqualTo(PuzzleAssessmentStatus.InvalidCandidate));
            var overlap = new PuzzleDefinition(5, 3, "issue-3-fixture", paws, new[] { A(0, 0, Direction.Down) });
            Assert.That(PuzzleSolvabilityAssessment.Assess(overlap, DifficultyProfile.Easy).Status,
                Is.EqualTo(PuzzleAssessmentStatus.InvalidCandidate));
        }

        private static string[] TraceFingerprint(HumanSolverResult result) => result.Steps.Select(s =>
            s.Index + ":" + s.RuleId + ":" + string.Join(",", s.Premises.SelectMany(p => p.Cells).Select(a => a.Coordinate + "=" + a.State)) +
            ":" + string.Join(",", s.Conclusions.Select(a => a.Coordinate + "=" + a.State))).ToArray();

        internal static void AssertTraceSound(PuzzleClues clues, HumanSolverResult result, List<HashSet<BoardCoordinate>> oracle)
        {
            Assert.That(oracle, Is.Not.Empty, "Soundness cannot be proven vacuously on inconsistent clues.");
            var state = new Dictionary<BoardCoordinate, CellKnowledge>();
            for (var r = 0; r < clues.Size; r++)
            for (var c = 0; c < clues.Size; c++)
            {
                var p = C(r, c);
                state[p] = clues.Arrows.Any(a => a.Coordinate == p) ? CellKnowledge.NotPaw : CellKnowledge.Unknown;
            }
            var nextIndex = 0;
            foreach (var step in result.Steps)
            {
                Assert.That(step.Index, Is.EqualTo(nextIndex++));
                Assert.That(step.DependencyStepIds.All(i => i >= 0 && i < step.Index), Is.True);
                CollectionAssert.AreEqual(step.Conclusions.Select(c => c.Coordinate), step.AffectedCells);
                Assert.That(step.RuleId, Is.Not.Null.And.Not.Empty);
                Assert.That(step.Sources, Is.Not.Empty);
                Assert.That(step.Conclusions, Is.Not.Empty);
                foreach (var premise in step.Premises)
                {
                    var source = premise.Source;
                    var expectedCells = state.Keys.Where(p => !clues.Arrows.Any(a => a.Coordinate == p)).Where(p =>
                        source.Kind == ConstraintKind.Row ? p.Row == source.Index :
                        source.Kind == ConstraintKind.Column ? p.Column == source.Index :
                        PointsAt(source.Coordinate, source.Direction, p)).ToArray();
                    CollectionAssert.AreEquivalent(expectedCells, premise.Cells.Select(a => a.Coordinate));
                    Assert.That(premise.RequiredPaws, Is.EqualTo(source.Kind == ConstraintKind.Row ? clues.RowTotals[source.Index] :
                        source.Kind == ConstraintKind.Column ? clues.ColumnTotals[source.Index] : 1));
                    foreach (var cell in premise.Cells)
                        Assert.That(state[cell.Coordinate], Is.EqualTo(cell.State));
                }
                foreach (var conclusion in step.Conclusions)
                {
                    Assert.That(state[conclusion.Coordinate], Is.EqualTo(CellKnowledge.Unknown), "Each cell must change only once.");
                    Assert.That(conclusion.State, Is.Not.EqualTo(CellKnowledge.Unknown));
                    foreach (var completion in oracle)
                        Assert.That(completion.Contains(conclusion.Coordinate), Is.EqualTo(conclusion.State == CellKnowledge.Paw), step.RuleId);
                    state[conclusion.Coordinate] = conclusion.State;
                }
            }
            foreach (var assignment in result.Assignments)
                Assert.That(assignment.State, Is.EqualTo(state[assignment.Coordinate]), "Replay must equal returned state.");
        }

        internal static bool PointsAt(BoardCoordinate origin, Direction direction, BoardCoordinate p) =>
            direction == Direction.Up && p.Column == origin.Column && p.Row < origin.Row ||
            direction == Direction.Down && p.Column == origin.Column && p.Row > origin.Row ||
            direction == Direction.Left && p.Row == origin.Row && p.Column < origin.Column ||
            direction == Direction.Right && p.Row == origin.Row && p.Column > origin.Column ||
            direction == Direction.UpLeft && p.Row < origin.Row && p.Column < origin.Column && origin.Row - p.Row == origin.Column - p.Column ||
            direction == Direction.UpRight && p.Row < origin.Row && p.Column > origin.Column && origin.Row - p.Row == p.Column - origin.Column ||
            direction == Direction.DownLeft && p.Row > origin.Row && p.Column < origin.Column && p.Row - origin.Row == origin.Column - p.Column ||
            direction == Direction.DownRight && p.Row > origin.Row && p.Column > origin.Column && p.Row - origin.Row == p.Column - origin.Column;

        internal static List<HashSet<BoardCoordinate>> EnumerateSolutions(PuzzleClues clues)
        {
            var solutions = new List<HashSet<BoardCoordinate>>();
            var chosen = new HashSet<BoardCoordinate>();
            void VisitRow(int row)
            {
                if (row == clues.Size)
                {
                    if (Enumerable.Range(0, clues.Size).Any(c => chosen.Count(p => p.Column == c) != clues.ColumnTotals[c])) return;
                    foreach (var arrow in clues.Arrows)
                    {
                        if (chosen.Contains(arrow.Coordinate)) return;
                        var origin = arrow.Coordinate;
                        var satisfied = chosen.Any(p => PointsAt(origin, arrow.Direction, p));
                        if (!satisfied) return;
                    }
                    solutions.Add(new HashSet<BoardCoordinate>(chosen));
                    return;
                }
                for (var mask = 0; mask < 1 << clues.Size; mask++)
                {
                    var cells = Enumerable.Range(0, clues.Size).Where(c => (mask & 1 << c) != 0).Select(c => C(row, c)).ToArray();
                    if (cells.Length != clues.RowTotals[row] || cells.Any(p => clues.Arrows.Any(a => a.Coordinate == p))) continue;
                    foreach (var p in cells) chosen.Add(p);
                    // Exact column totals cannot recover from an excess in an earlier row.
                    if (Enumerable.Range(0, clues.Size).All(c => chosen.Count(p => p.Column == c) <= clues.ColumnTotals[c]))
                        VisitRow(row + 1);
                    foreach (var p in cells) chosen.Remove(p);
                }
            }
            VisitRow(0);
            return solutions;
        }
    }
}
