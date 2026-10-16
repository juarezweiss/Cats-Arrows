using System;
using System.Collections.Generic;
using System.Linq;
using CatsAndArrows.Core;
using NUnit.Framework;

namespace CatsAndArrows.Tests
{
    // Issue #35: optional tuning can improve an artifact only after a full proof.
    public sealed class PruningTests
    {
        [TestCase("medium")]
        [TestCase("hard")]
        public void PairedCorpusReducesCombinedOccupancyWithoutRejectingOutsideSoftBands(string id)
        {
            var profile = Profile(id);
            var generator = new SeededPuzzleGenerator();
            var fixedBefore = 0; var fixedAfter = 0; var sawOutsideSoftBand = false;
            for (var seed = 0; seed < 128; seed++)
            {
                var baseline = generator.Generate(profile, seed, GenerationOptions.ForLargeBoard(maxPruningTrials: 0));
                var tuned = generator.Generate(profile, seed);
                Assert.That(baseline.IsSuccess && tuned.IsSuccess, Is.True, id + " seed=" + seed);
                CollectionAssert.AreEquivalent(baseline.Puzzle.SolutionCats, tuned.Puzzle.SolutionCats);
                Assert.That(tuned.Metadata.AcceptedAttempt, Is.EqualTo(baseline.Metadata.AcceptedAttempt));
                Assert.That(tuned.Puzzle.Arrows.All(a => baseline.Puzzle.Arrows.TryGetValue(a.Key, out var direction) && direction == a.Value), Is.True);
                var before = FixedCount(baseline); var after = FixedCount(tuned);
                Assert.That(after, Is.LessThanOrEqualTo(before));
                Assert.That(tuned.Puzzle.Arrows.Count, Is.LessThanOrEqualTo(baseline.Puzzle.Arrows.Count));
                Assert.That(tuned.Diagnostics.PruningOrdersCompleted, Is.EqualTo(2));
                Assert.That(tuned.Diagnostics.PruningBudgetExhausted, Is.False);
                Assert.That(tuned.Diagnostics.PruningTrials, Is.EqualTo(2 * baseline.Puzzle.Arrows.Count));
                // Proves that every preflight/full proof is included in both accounting scopes.
                Assert.That(tuned.Diagnostics.HumanConstraintEvaluations,
                    Is.EqualTo(baseline.Diagnostics.HumanConstraintEvaluations + tuned.Diagnostics.PruningHumanConstraintEvaluations));
                Assert.That(tuned.Diagnostics.VerifierNodesVisited,
                    Is.EqualTo(baseline.Diagnostics.VerifierNodesVisited + tuned.Diagnostics.PruningVerifierNodesVisited));
                Assert.That(tuned.Diagnostics.PruningTrials, Is.LessThanOrEqualTo(48));
                Assert.That(tuned.Diagnostics.PruningHumanConstraintEvaluations, Is.LessThanOrEqualTo(40000));
                Assert.That(tuned.Diagnostics.PruningVerifierNodesVisited, Is.LessThanOrEqualTo(10000));
                var proof = PuzzleSolvabilityAssessment.Assess(tuned.Puzzle, profile);
                Assert.That(proof.IsAccepted, Is.True);
                Assert.That(proof.Solver.Human.Status, Is.EqualTo(HumanSolverStatus.Solved));
                Assert.That(proof.Solver.Uniqueness.Status, Is.EqualTo(UniquenessStatus.Unique));
                var restored = baseline.Puzzle.Arrows.Keys.Except(tuned.Puzzle.Arrows.Keys).ToArray();
                foreach (var cell in restored)
                    Assert.That(proof.Solver.Human.Assignments.Single(a => a.Coordinate == cell).State, Is.EqualTo(CellKnowledge.NotCat));
                var low = id == "medium" ? 3 : 7; var high = id == "medium" ? 9 : 13;
                sawOutsideSoftBand |= tuned.Puzzle.Arrows.Count < low || tuned.Puzzle.Arrows.Count > high;
                fixedBefore += before; fixedAfter += after;
            }
            Assert.That(fixedAfter, Is.LessThan(fixedBefore));
            Assert.That(sawOutsideSoftBand, Is.True, "Central density bands must not become admission thresholds.");
        }

        [TestCase("medium", 0)]
        [TestCase("medium", 32)]
        [TestCase("hard", 0)]
        [TestCase("hard", 32)]
        [TestCase("hard", int.MaxValue)]
        public void TwoOrderResultMatchesIndependentGreedyProofAndTieSelection(string id, int seed)
        {
            var profile = Profile(id);
            var generator = new SeededPuzzleGenerator();
            var baseline = generator.Generate(profile, seed, GenerationOptions.ForLargeBoard(maxPruningTrials: 0));
            var best = baseline.Puzzle;
            var baseArrows = best.Arrows.OrderBy(a => a.Key.Row).ThenBy(a => a.Key.Column).Select(a => a.Key).ToArray();
            foreach (var order in new[] { baseArrows, baseArrows.Reverse().ToArray() })
            {
                var current = baseline.Puzzle;
                foreach (var coordinate in order)
                {
                    var candidate = new PuzzleDefinition(current.Size, current.Seed, current.GeneratorVersion,
                        current.SolutionCats, current.Arrows.Where(a => a.Key != coordinate).Select(a => new ArrowClue(a.Key, a.Value)));
                    if (PuzzleSolvabilityAssessment.Assess(candidate, profile).IsAccepted) current = candidate;
                }
                var currentFixed = FixedCount(current, profile); var bestFixed = FixedCount(best, profile);
                if (currentFixed < bestFixed || (currentFixed == bestFixed && current.Arrows.Count < best.Arrows.Count)) best = current;
            }
            var actual = generator.Generate(profile, seed);
            Assert.That(actual.Metadata.BoardFingerprint, Is.EqualTo(GenerationFingerprints.ForBoard(best)));
        }

        [TestCase("medium")]
        [TestCase("hard")]
        public void EveryOptionalBudgetCanStopWithOnlyAnAlreadyProvedArtifact(string id)
        {
            var profile = Profile(id);
            var generator = new SeededPuzzleGenerator();
            var baseline = generator.Generate(profile, 0, GenerationOptions.ForLargeBoard(maxPruningTrials: 0));
            foreach (var options in new[] {
                GenerationOptions.ForLargeBoard(maxPruningTrials: 0),
                GenerationOptions.ForLargeBoard(maxPruningHumanConstraintEvaluations: 0),
                GenerationOptions.ForLargeBoard(maxPruningVerifierNodes: 0),
                GenerationOptions.ForLargeBoard(maxPruningTrials: 1),
                GenerationOptions.ForLargeBoard(maxPruningHumanConstraintEvaluations: 1),
                GenerationOptions.ForLargeBoard(maxPruningVerifierNodes: 1),
                GenerationOptions.ForLargeBoard(maxHumanConstraintEvaluations: baseline.Diagnostics.HumanConstraintEvaluations + 1),
                GenerationOptions.ForLargeBoard(maxVerifierNodes: baseline.Diagnostics.VerifierNodesVisited + 1) })
            {
                var result = generator.Generate(profile, 0, options);
                Assert.That(result.IsSuccess, Is.True);
                Assert.That(result.Diagnostics.PruningBudgetExhausted, Is.True);
                Assert.That(PuzzleSolvabilityAssessment.Assess(result.Puzzle, profile).IsAccepted, Is.True);
                CollectionAssert.AreEquivalent(result.Puzzle.SolutionCats, baseline.Puzzle.SolutionCats);
                Assert.That(FixedCount(result), Is.LessThanOrEqualTo(FixedCount(baseline)));
                Assert.That(result.Diagnostics.HumanConstraintEvaluations, Is.LessThanOrEqualTo(options.MaxHumanConstraintEvaluations));
                Assert.That(result.Diagnostics.VerifierNodesVisited, Is.LessThanOrEqualTo(options.MaxVerifierNodes));
                Assert.That(result.Diagnostics.PruningTrials, Is.LessThanOrEqualTo(options.MaxPruningTrials));
                Assert.That(result.Diagnostics.PruningHumanConstraintEvaluations, Is.LessThanOrEqualTo(options.MaxPruningHumanConstraintEvaluations));
                Assert.That(result.Diagnostics.PruningVerifierNodesVisited, Is.LessThanOrEqualTo(options.MaxPruningVerifierNodes));
            }
        }

        [Test]
        public void InvalidPruningLimitsCannotExpandOrWrapWork()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => GenerationOptions.ForLargeBoard(maxPruningTrials: -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => GenerationOptions.ForLargeBoard(maxPruningTrials: 49));
            Assert.Throws<ArgumentOutOfRangeException>(() => GenerationOptions.ForLargeBoard(maxPruningHumanConstraintEvaluations: -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => GenerationOptions.ForLargeBoard(maxPruningVerifierNodes: -1));
        }

        private static DifficultyProfile Profile(string id) => id == "medium" ? DifficultyProfile.Medium : DifficultyProfile.Hard;
        private static int FixedCount(GenerationResult result) => result.Puzzle.Arrows.Count +
            (result.Metadata.ProfileId == "hard" ? 0 :
                ClueProtectionRules.Derive(PuzzleClues.FromPuzzle(result.Puzzle), result.Assessment.Solver.Human).Count);
        private static int FixedCount(PuzzleDefinition puzzle, DifficultyProfile profile)
        {
            if (profile.Id == "hard") return puzzle.Arrows.Count;
            var clues = PuzzleClues.FromPuzzle(puzzle);
            return puzzle.Arrows.Count + ClueProtectionRules.Derive(clues, new HumanDeductionSolver().Solve(clues)).Count;
        }
    }
}
