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
                Assert.That(result.Metadata.GeneratorVersion, Is.EqualTo(SeededPuzzleGenerator.Version));
                Assert.That(puzzle.GeneratorVersion, Is.EqualTo(result.Metadata.GeneratorVersion));
                Assert.That(result.Metadata.ProfileVersion, Is.EqualTo(SeededPuzzleGenerator.ProfileVersion));
                Assert.That(result.Metadata.OptionsVersion, Is.EqualTo("generation-options-large-v1"));
                Assert.That(result.Metadata.RulesetVersion, Is.EqualTo("human-v2"));
                Assert.That(result.Metadata.BoardFingerprint, Is.EqualTo(GenerationFingerprints.ForBoard(puzzle)));
                WithinBudgets(result);
                boards.Add(result.Metadata.BoardFingerprint);
            }
            Assert.That(sawThree, Is.True);
            Assert.That(sawZero, Is.EqualTo(id == "medium"), "Medium can generate zero lines; Hard cannot.");
            Assert.That(boards.Count, Is.EqualTo(128));
        }

        [TestCase("medium", 0, "f0d16059f54ccbd56559a89bc86ba9f5b6a7e4bce90b3386d7d13e83197cb4a2")]
        [TestCase("medium", -1, "47914097981bfa905e4c74a3aa785ec9fd91636ea5de8e2ec6601206fe3bc739")]
        [TestCase("medium", int.MinValue, "3654eeb7d47f54e59a9e36c4ea0e9d504cf7c3d6862c4639cbc51cd871f4ad97")]
        [TestCase("medium", int.MaxValue, "08bfb7b3bfc67f913632823643370ecf41b46780fa01a47973b9055777afa9c7")]
        [TestCase("hard", 0, "91bc41c827a0db001ba4bacd01ea24ae5898aa92c5baa390f961f16013c7a94e")]
        [TestCase("hard", -1, "69d4c0d94906b79d9c1a199e3016c375a6d6d44edb6c3638ad89c027aa858cba")]
        [TestCase("hard", int.MinValue, "dfb5c51d86cb68a45d4050aa9d2e0a5f8d7e46d3c71bc6b4f365b979f248c456")]
        [TestCase("hard", int.MaxValue, "dd0e01a10a295dd83b134649e251415ca527249fc9f7937423c87ebe9ab03d70")]
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
                new DifficultyProfile("hard", 7, 4, 0, 0), new DifficultyProfile("hard", 7, 4, 0, 0, 2),
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
            r.Diagnostics.VerifierNodesVisited + ":" + r.Diagnostics.RandomDraws + ":" + r.Diagnostics.ArrowTrials;
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
        }
    }
}
