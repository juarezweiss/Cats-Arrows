using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CatsAndArrows.Core;
using NUnit.Framework;

namespace CatsAndArrows.Tests
{
    // Issue #32: profile limits are admission policy, never hidden solver clues.
    public sealed class DifficultyGenerationTests
    {
        [TestCase("medium")]
        [TestCase("hard")]
        public void FixedSeedCorpusSatisfiesProfileAndBothIndependentProofs(string id)
        {
            var profile = Profile(id);
            var generator = new SeededPuzzleGenerator();
            var sawZero = false;
            var sawThree = false;
            var boards = new HashSet<string>();
            for (var seed = 0; seed < 128; seed++)
            {
                var result = generator.Generate(profile, seed);
                Assert.That(result.Status, Is.EqualTo(GenerationStatus.Success), id + " seed=" + seed);
                var puzzle = result.Puzzle;
                Assert.That(puzzle.Size, Is.EqualTo(id == "medium" ? 6 : 7));
                Assert.That(puzzle.Seed, Is.EqualTo(seed));
                Assert.That(PuzzleValidator.Validate(puzzle, profile).IsValid, Is.True);
                var rows = Enumerable.Range(0, puzzle.Size).Select(puzzle.GetRowCatCount).ToArray();
                var columns = Enumerable.Range(0, puzzle.Size).Select(puzzle.GetColumnCatCount).ToArray();
                Assert.That(rows.Concat(columns).Max(), Is.LessThanOrEqualTo(profile.MaxCatsPerLine));
                Assert.That(rows.Count(n => n == 0), Is.LessThanOrEqualTo(profile.MaxZeroRows));
                Assert.That(columns.Count(n => n == 0), Is.LessThanOrEqualTo(profile.MaxZeroColumns));
                var three = rows.Concat(columns).Contains(3);
                if (id == "hard") Assert.That(three, Is.True, "Hard requires a 3-total line on every board.");
                sawZero |= rows.Concat(columns).Contains(0);
                sawThree |= three;
                foreach (var arrow in puzzle.Arrows)
                {
                    Assert.That(puzzle.IsSolutionCat(arrow.Key), Is.False);
                    Assert.That(puzzle.GetDirectionalSegment(arrow.Key, arrow.Value).Any(puzzle.IsSolutionCat), Is.True);
                }
                var independent = PuzzleSolvabilityAssessment.Assess(puzzle, profile);
                Assert.That(independent.IsAccepted, Is.True);
                Assert.That(independent.Solver.Human.Status, Is.EqualTo(HumanSolverStatus.Solved));
                Assert.That(independent.Solver.Uniqueness.Status, Is.EqualTo(UniquenessStatus.Unique));
                CollectionAssert.AreEquivalent(puzzle.SolutionCats, independent.Solver.Uniqueness.Solutions.Single());
                CollectionAssert.AreEquivalent(puzzle.SolutionCats, independent.Solver.Human.Assignments
                    .Where(a => a.State == CellKnowledge.Cat).Select(a => a.Coordinate));
                Assert.That(result.Metadata.GeneratorVersion, Is.EqualTo(id == "hard" ? SeededPuzzleGenerator.HardVersion : SeededPuzzleGenerator.Version));
                Assert.That(puzzle.GeneratorVersion, Is.EqualTo(result.Metadata.GeneratorVersion));
                Assert.That(result.Metadata.ProfileVersion, Is.EqualTo(id == "hard" ? SeededPuzzleGenerator.HardProfileVersion : SeededPuzzleGenerator.ProfileVersion));
                Assert.That(result.Metadata.OptionsVersion, Is.EqualTo("generation-options-large-v2"));
                Assert.That(result.Metadata.RulesetVersion, Is.EqualTo("human-v2"));
                Assert.That(result.Metadata.BoardFingerprint, Is.EqualTo(GenerationFingerprints.ForBoard(puzzle)));
                WithinBudgets(result);
                boards.Add(result.Metadata.BoardFingerprint);
            }
            Assert.That(sawThree, Is.True);
            Assert.That(sawZero, Is.EqualTo(id == "medium"), "Medium can generate zero lines; Hard cannot.");
            Assert.That(boards.Count, Is.EqualTo(128));
        }

        // Medium retains #35 v2 receipts. Hard #37 v3 scores only displayed arrows.
        [TestCase("medium", 0, "3bf0fb47cdfecf4526952ad8e281f4f7c20340b6782722f607bac9529d35cd1b")]
        [TestCase("medium", -1, "fc97730017cb5cbeb789de6db620c2c3febb40ad3003c0677abf0a85f1353a65")]
        [TestCase("medium", int.MinValue, "f14406637fd7f4179f17232e9e8d7b826f1cfd367529986b8a41183558bc1c54")]
        [TestCase("medium", int.MaxValue, "8eeaeff3ba70b253c74a2afc05d3ec6f8bd5ad794751c79c7c44981fe889090c")]
        [TestCase("hard", 0, "b2c71295f015e903a73577aa5c304c72e0004d1ced438641db9bf6269c994e0a")]
        [TestCase("hard", -1, "8c7701abdc5009d6f852a617eefa7a1f99ca8621a77fc0242400e808212fb1e0")]
        [TestCase("hard", int.MinValue, "e249fcdba2874900aeb00238a5baa6d4da96fecb45ed8a4c019ddaf0207e6c17")]
        [TestCase("hard", int.MaxValue, "ea303b7d513e5e2e15de95e119fd882a76fed7dbde2511a9b8b15d20324fecf4")]
        public void SignedSeedsReplayAcrossInstancesAndInterleavedDifficulty(string id, int seed, string fingerprint)
        {
            var generator = new SeededPuzzleGenerator();
            var first = generator.Generate(Profile(id), seed);
            Assert.That(first.IsSuccess, Is.True);
            Assert.That(first.Metadata.BoardFingerprint, Is.EqualTo(fingerprint));
            generator.Generate(Profile(id == "medium" ? "hard" : "medium"), seed);
            var replay = new SeededPuzzleGenerator().Generate(Profile(id), seed, first.Metadata.Options);
            Assert.That(Receipt(replay), Is.EqualTo(Receipt(first)));
        }

        [Test]
        public void HardInitialHatchesIgnoreAllProofConsequencesIncludingOneCatArrowExclusions()
        {
            var sawFormerHatches = false;
            for (var seed = 0; seed < 32; seed++)
            {
                var result = new SeededPuzzleGenerator().Generate(DifficultyProfile.Hard, seed);
                Assert.That(result.IsSuccess, Is.True);
                var clues = PuzzleClues.FromPuzzle(result.Puzzle);
                var proof = result.Assessment.Solver.Human;
                var formerHatches = ClueProtectionRules.Derive(clues, proof);
                sawFormerHatches |= formerHatches.Count > 0;
                Assert.That(ClueProtectionRules.ForProfile(DifficultyProfile.Hard, clues, proof), Is.Empty);
                Assert.That(ClueProtectionRules.ForProfile(DifficultyProfile.Hard, clues, null), Is.Empty,
                    "Hard must not need a solver proof to select hatches.");
                foreach (var coordinate in formerHatches)
                {
                    Assert.That(result.Puzzle.Arrows.ContainsKey(coordinate), Is.False);
                    Assert.That(result.Puzzle.IsSolutionCat(coordinate), Is.False);
                    Assert.That(proof.Assignments.Single(a => a.Coordinate == coordinate).State,
                        Is.EqualTo(CellKnowledge.NotCat));
                }
            }
            Assert.That(sawFormerHatches, Is.True, "Regression must exercise actual cardinal one-cat consequences.");
        }

        [TestCase("easy")]
        [TestCase("medium")]
        public void OtherProfilesRetainExistingConsequenceHatches(string id)
        {
            var profile = id == "easy" ? DifficultyProfile.Easy : DifficultyProfile.Medium;
            var sawHatches = false;
            for (var seed = 0; seed < 32; seed++)
            {
                var result = new SeededPuzzleGenerator().Generate(profile, seed);
                Assert.That(result.IsSuccess, Is.True);
                var clues = PuzzleClues.FromPuzzle(result.Puzzle);
                var oldHatches = ClueProtectionRules.Derive(clues, result.Assessment.Solver.Human);
                sawHatches |= oldHatches.Count > 0;
                CollectionAssert.AreEqual(oldHatches,
                    ClueProtectionRules.ForProfile(profile, clues, result.Assessment.Solver.Human));
            }
            Assert.That(sawHatches, Is.True);
        }

        [Test]
        public void HatchPolicyRequiresProfileAndCluesButOnlyLegacyModesRequireProof()
        {
            var result = new SeededPuzzleGenerator().Generate(DifficultyProfile.Medium, 0);
            var clues = PuzzleClues.FromPuzzle(result.Puzzle);
            Assert.Throws<ArgumentNullException>(() => ClueProtectionRules.ForProfile(null, clues, null));
            Assert.Throws<ArgumentNullException>(() => ClueProtectionRules.ForProfile(DifficultyProfile.Hard, null, null));
            Assert.Throws<ArgumentNullException>(() => ClueProtectionRules.ForProfile(DifficultyProfile.Medium, clues, null));
            Assert.Throws<ArgumentNullException>(() => ClueProtectionRules.ForProfile(DifficultyProfile.Easy, clues, null));
        }

        [Test]
        public void ConcurrentDifficultyRequestsShareNoMutableRandomOrLayoutState()
        {
            var generator = new SeededPuzzleGenerator();
            var profiles = new[] { DifficultyProfile.Easy, DifficultyProfile.Medium, DifficultyProfile.Hard };
            var expected = profiles.Select(p => Receipt(generator.Generate(p, 32))).ToArray();
            var tasks = profiles.Select(p => Task.Run(() => generator.Generate(p, 32))).ToArray();
            Task.WaitAll(tasks);
            for (var i = 0; i < tasks.Length; i++) Assert.That(Receipt(tasks[i].Result), Is.EqualTo(expected[i]));
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(int.MinValue)]
        [TestCase(int.MaxValue)]
        public void EasyDispatchPreservesExistingVersionOptionsAndCompleteReceipt(int seed)
        {
            var old = new SeededEasyPuzzleGenerator().Generate(DifficultyProfile.Easy, seed);
            var current = new SeededPuzzleGenerator().Generate(DifficultyProfile.Easy, seed);
            Assert.That(Receipt(current), Is.EqualTo(Receipt(old)));
            Assert.That(current.Metadata.GeneratorVersion, Is.EqualTo("easy-generator-v2"));
            Assert.That(current.Metadata.ProfileVersion, Is.EqualTo("easy-profile-v1"));
            Assert.That(current.Metadata.OptionsVersion, Is.EqualTo("generation-options-v1"));
            Assert.That(current.Metadata.Options.MaxArrows, Is.EqualTo(10));
        }

        [TestCase("medium")]
        [TestCase("hard")]
        public void AllZeroWorkBudgetsFailWithoutReturningAnUnprovedArtifact(string id)
        {
            var options = new[] { new GenerationOptions(maxAttempts: 0), new GenerationOptions(maxLayoutNodes: 0),
                new GenerationOptions(maxLayoutNodesPerAttempt: 0), new GenerationOptions(maxRandomDraws: 0),
                new GenerationOptions(maxHumanConstraintEvaluations: 0), new GenerationOptions(maxHumanConstraintEvaluationsPerCall: 0),
                new GenerationOptions(maxVerifierNodes: 0), new GenerationOptions(maxVerifierNodesPerCall: 0),
                new GenerationOptions(maxArrows: 0), new GenerationOptions(maxHumanConstraintEvaluationsPerCall: 1) };
            foreach (var option in options)
            {
                var result = new SeededPuzzleGenerator().Generate(Profile(id), 0, option);
                Assert.That(result.Status, Is.EqualTo(GenerationStatus.Exhausted));
                Assert.That(result.Puzzle, Is.Null);
                Assert.That(result.Assessment, Is.Null);
                Assert.That(result.Metadata.AcceptedAttempt, Is.EqualTo(-1));
                WithinBudgets(result);
            }
        }

        [TestCase("medium")]
        [TestCase("hard")]
        public void CancellationConsumesNoWork(string id)
        {
            using (var cancel = new CancellationTokenSource())
            {
                cancel.Cancel();
                var result = new SeededPuzzleGenerator().Generate(Profile(id), 32, cancellationToken: cancel.Token);
                Assert.That(result.Status, Is.EqualTo(GenerationStatus.Cancelled));
                Assert.That(result.Puzzle, Is.Null);
                Assert.That(result.Diagnostics.Attempts, Is.Zero);
                Assert.That(result.Diagnostics.RandomDraws, Is.Zero);
                Assert.That(result.Diagnostics.HumanConstraintEvaluations, Is.Zero);
            }
        }

        [Test]
        public void ModifiedOrUnrecognizedProfilesCannotBypassAdmissionPolicy()
        {
            var generator = new SeededPuzzleGenerator();
            foreach (var p in new[] { new DifficultyProfile("medium", 7, 3), new DifficultyProfile("medium", 6, 4),
                new DifficultyProfile("medium", 6, 3, 2, 1), new DifficultyProfile("hard", 7, 4),
                new DifficultyProfile("hard", 7, 3, 0, 0), new DifficultyProfile("hard", 7, 4, 0, 0, 3), new DifficultyProfile("hard", 7, 4, 0, 0, 2),
                new DifficultyProfile("easy", 5, 2, 1, 1, 2), new DifficultyProfile("custom", 7, 4) })
            {
                var result = generator.Generate(p, 32);
                Assert.That(result.Status, Is.EqualTo(GenerationStatus.UnsupportedProfile));
                Assert.That(result.Puzzle, Is.Null);
            }
            Assert.That(generator.Generate(null, 32).Status, Is.EqualTo(GenerationStatus.InvalidProfile));
            Assert.That(generator.Generate(new DifficultyProfile("hard", 7, 4, -1, 0, 3), 32).Status,
                Is.EqualTo(GenerationStatus.InvalidProfile));
            Assert.Throws<ArgumentOutOfRangeException>(() => new DifficultyProfile("bad", 7, 4, 0, 0, 5));
            Assert.Throws<ArgumentOutOfRangeException>(() => new DifficultyProfile("bad", 7, 4, 0, 0, -1));
        }

        private static DifficultyProfile Profile(string id) => id == "medium" ? DifficultyProfile.Medium : DifficultyProfile.Hard;
        private static string Receipt(GenerationResult r) => r.Status + ":" + r.Metadata.BoardFingerprint + ":" +
            r.Metadata.SolutionFingerprint + ":" + r.Metadata.AcceptedAttempt + ":" + r.Metadata.GeneratorVersion + ":" +
            r.Metadata.ProfileVersion + ":" + r.Metadata.OptionsVersion + ":" + r.Diagnostics.Attempts + ":" +
            r.Diagnostics.LayoutNodesVisited + ":" + r.Diagnostics.HumanConstraintEvaluations + ":" +
            r.Diagnostics.VerifierNodesVisited + ":" + r.Diagnostics.RandomDraws + ":" + r.Diagnostics.ArrowTrials + ":" +
            r.Diagnostics.PruningTrials + ":" + r.Diagnostics.PruningDeletionsRetained + ":" + r.Diagnostics.PruningOrdersCompleted + ":" +
            r.Diagnostics.PruningHumanConstraintEvaluations + ":" + r.Diagnostics.PruningVerifierNodesVisited + ":" + r.Diagnostics.PruningBudgetExhausted;
        private static void WithinBudgets(GenerationResult r)
        {
            var o = r.Metadata.Options;
            Assert.That(r.Diagnostics.Attempts, Is.InRange(0, o.MaxAttempts));
            Assert.That(r.Diagnostics.LayoutNodesVisited, Is.InRange(0, o.MaxLayoutNodes));
            Assert.That(r.Diagnostics.LayoutNodesVisited, Is.LessThanOrEqualTo((long)r.Diagnostics.Attempts * o.MaxLayoutNodesPerAttempt));
            Assert.That(r.Diagnostics.HumanConstraintEvaluations, Is.InRange(0, o.MaxHumanConstraintEvaluations));
            Assert.That(r.Diagnostics.VerifierNodesVisited, Is.InRange(0, o.MaxVerifierNodes));
            Assert.That(r.Diagnostics.RandomDraws, Is.InRange(0, o.MaxRandomDraws));
            Assert.That(r.Diagnostics.ArrowTrials, Is.LessThanOrEqualTo((long)r.Diagnostics.Attempts * o.MaxArrows * o.ArrowTrialsPerStage));
            Assert.That(r.Diagnostics.PruningTrials, Is.InRange(0, o.MaxPruningTrials));
            Assert.That(r.Diagnostics.PruningHumanConstraintEvaluations, Is.InRange(0, o.MaxPruningHumanConstraintEvaluations));
            Assert.That(r.Diagnostics.PruningVerifierNodesVisited, Is.InRange(0, o.MaxPruningVerifierNodes));
        }
    }
}
