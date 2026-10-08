using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CatsAndArrows.Core;
using NUnit.Framework;

namespace CatsAndArrows.Tests
{
    // Issue #4: generation admits boards through the real Issue #3 assessment.
    public sealed class GenerationTests
    {
        [Test]
        public void SplitMix64MatchesFrozenUnsignedGoldenVectors()
        {
            var inputs = new[] { 0UL, 1UL, ulong.MaxValue };
            var vectors = new[]
            {
                new[] { 0xE220A8397B1DCDAFUL, 0x6E789E6AA1B965F4UL, 0x06C45D188009454FUL, 0xF88BB8A8724C81ECUL },
                new[] { 0x910A2DEC89025CC1UL, 0xBEEB8DA1658EEC67UL, 0xF893A2EEFB32555EUL, 0x71C18690EE42C90BUL },
                new[] { 0xE4D971771B652C20UL, 0xE99FF867DBF682C9UL, 0x382FF84CB27281E9UL, 0x6D1DB36CCBA982D2UL }
            };
            for (var i = 0; i < inputs.Length; i++)
            {
                var random = new SplitMix64Random(inputs[i]);
                foreach (var expected in vectors[i]) Assert.That(random.NextUInt64(), Is.EqualTo(expected));
                Assert.That(random.DrawsUsed, Is.EqualTo(4));
            }
            Assert.That(SplitMix64Random.SeedToState(0), Is.EqualTo(0UL));
            Assert.That(SplitMix64Random.SeedToState(-1), Is.EqualTo(4294967295UL));
            Assert.That(SplitMix64Random.SeedToState(int.MinValue), Is.EqualTo(2147483648UL));
            Assert.That(SplitMix64Random.SeedToState(int.MaxValue), Is.EqualTo(2147483647UL));
        }

        [Test]
        public void IntegerRandomDrawsRespectBoundsAndZeroBudgetConsumesNothing()
        {
            var random = new SplitMix64Random(0);
            Assert.That(random.TryNextInt(5, 0, out _), Is.False);
            Assert.That(random.DrawsUsed, Is.Zero);
            foreach (var bound in new[] { 1, 2, 5, 257, int.MaxValue })
            for (var i = 0; i < 50; i++)
            {
                Assert.That(random.TryNextInt(bound, 10, out var value), Is.True);
                Assert.That(value, Is.InRange(0, bound - 1));
            }
        }

        [Test]
        public void RejectedRandomSamplesConsumeTheSpecifiedDrawBudget()
        {
            var zeroFirstSample = unchecked(0UL - 0x9E3779B97F4A7C15UL);
            var bounded = new SplitMix64Random(zeroFirstSample);
            Assert.That(bounded.TryNextInt(3, 1, out _), Is.False);
            Assert.That(bounded.DrawsUsed, Is.EqualTo(1));
            var sufficient = new SplitMix64Random(zeroFirstSample);
            Assert.That(sufficient.TryNextInt(3, 2, out var value), Is.True);
            Assert.That(value, Is.InRange(0, 2));
            Assert.That(sufficient.DrawsUsed, Is.EqualTo(2));
            Assert.Throws<ArgumentOutOfRangeException>(() => sufficient.TryNextInt(0, 1, out _));
            Assert.Throws<ArgumentOutOfRangeException>(() => sufficient.TryNextInt(3, -1, out _));
        }

        [Test]
        public void CanonicalLogicalFingerprintsMatchIndependentlyCalculatedHashes()
        {
            var cats = new[] { new BoardCoordinate(0, 0), new BoardCoordinate(1, 1) };
            var arrows = new[] { new ArrowClue(new BoardCoordinate(0, 1), Direction.Down),
                new ArrowClue(new BoardCoordinate(1, 0), Direction.Right) };
            var puzzle = new PuzzleDefinition(2, 10, "fixture-a", cats, arrows);
            var reversed = new PuzzleDefinition(2, -1, "fixture-b", cats.Reverse(), arrows.Reverse());
            const string solution = "58dd44527dccd2ecfe6f8aad575c0cfa30c902343cb97aed87521ec1c15702f7";
            const string board = "b2ee0f5bdb32ffa9d4062c44045fe229a7afb98fece0aae263d091b8074d91fc";
            Assert.That(GenerationFingerprints.ForSolution(puzzle), Is.EqualTo(solution));
            Assert.That(GenerationFingerprints.ForBoard(puzzle), Is.EqualTo(board));
            Assert.That(GenerationFingerprints.ForSolution(reversed), Is.EqualTo(solution));
            Assert.That(GenerationFingerprints.ForBoard(reversed), Is.EqualTo(board));
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(int.MinValue)]
        [TestCase(int.MaxValue)]
        public void ExtremeSignedSeedsReproduceAcrossInterleavedCallsAndCultures(int seed)
        {
            var generator = new SeededEasyPuzzleGenerator();
            var first = generator.Generate(DifficultyProfile.Easy, seed);
            Assert.That(first.IsSuccess, Is.True, "Extreme seed must produce a proven board with default limits.");
            AssertAccepted(first, DifficultyProfile.Easy, seed);
            TestContext.Out.WriteLine("GENERATION_SEED " + seed.ToString(CultureInfo.InvariantCulture) + " " +
                first.Metadata.BoardFingerprint + " " + first.Metadata.SolutionFingerprint + " attempt=" + first.Metadata.AcceptedAttempt);
            var goldens = new Dictionary<int, string[]>
            {
                [0] = new[] { "c27794e072fa90622c8f4f053373d0b36b25c0380c787a2dd44a5b002be8b59f", "cfe51f5b10a1c644653ea6199f4908dbf109fa1fb01f457190477412a8d6030c" },
                [-1] = new[] { "83bdf5c791aff18504c7930140563038fadf88f151574d7ae8481efc5ff59c1a", "29d27832e2f4e8e56c0d9563d68b426110fba090e34a7107002621b4e05a8f66" },
                [int.MinValue] = new[] { "b8a1c833206cd0b03748c234186f18034a8644b786e4f9c681d9700babdb091a", "6b75a5581daefcf974e9b68c43fe420f8af4228e47ba202c8392ab71980d9d1c" },
                [int.MaxValue] = new[] { "f1555c7814feefc9ca6f292353353bc0c50e30ec8469aa99380fc53804d21135", "f660d27fc165598b6d408f4422bb5714a1bd1b4f1b1c2b3f02cd6d4cfd1e781a" }
            };
            // Frozen from the native, independently assessed v2 receipts. A changed
            // seed mapping or logical board requires an intentional versioned review.
            CollectionAssert.AreEqual(goldens[seed], new[] { first.Metadata.BoardFingerprint, first.Metadata.SolutionFingerprint });
            Assert.That(first.Metadata.AcceptedAttempt, Is.Zero);

            var fingerprint = ResultFingerprint(first);
            var culture = CultureInfo.CurrentCulture; var uiCulture = CultureInfo.CurrentUICulture;
            try
            {
                foreach (var locale in new[] { "en", "pt-BR", "es" })
                {
                    CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(locale);
                    CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(locale);
                    generator.Generate(DifficultyProfile.Easy, seed ^ 1234567);
                    var repeated = generator.Generate(DifficultyProfile.Easy, seed, first.Metadata.Options);
                    Assert.That(ResultFingerprint(repeated), Is.EqualTo(fingerprint));
                    Assert.That(ResultFingerprint(new SeededEasyPuzzleGenerator().Generate(DifficultyProfile.Easy, seed, first.Metadata.Options)), Is.EqualTo(fingerprint));
                }
            }
            finally { CultureInfo.CurrentCulture = culture; CultureInfo.CurrentUICulture = uiCulture; }
        }

        [Test]
        public void ConcurrentRequestsOnOneInstanceMatchSequentialResults()
        {
            var generator = new SeededEasyPuzzleGenerator();
            var seeds = new[] { 0, 1, -1, int.MaxValue };
            var sequential = seeds.Select(seed => ResultFingerprint(generator.Generate(DifficultyProfile.Easy, seed))).ToArray();
            var tasks = seeds.Select(seed => Task.Run(() => generator.Generate(DifficultyProfile.Easy, seed))).ToArray();
            Task.WaitAll(tasks);
            for (var i = 0; i < tasks.Length; i++)
                Assert.That(ResultFingerprint(tasks[i].Result), Is.EqualTo(sequential[i]));
        }

        [Test]
        public void UnsupportedAndInvalidProfilesReturnNoPuzzle()
        {
            var generator = new SeededEasyPuzzleGenerator();
            foreach (var profile in new[] { DifficultyProfile.Medium, DifficultyProfile.Hard,
                new DifficultyProfile("custom", 5, 2), new DifficultyProfile("easy", 5, 1),
                new DifficultyProfile("easy", 5, 2, 0, 1) })
            {
                var result = generator.Generate(profile, 0);
                Assert.That(result.Status, Is.EqualTo(GenerationStatus.UnsupportedProfile));
                AssertNoPuzzle(result);
            }
            foreach (var profile in new[] { null, new DifficultyProfile("easy", 5, 2, -1, 1), new DifficultyProfile("easy", 5, 2, 1, -1) })
            {
                var result = generator.Generate(profile, 0);
                Assert.That(result.Status, Is.EqualTo(GenerationStatus.InvalidProfile));
                AssertNoPuzzle(result);
            }
        }

        [Test]
        public void ZeroWorkBudgetsTerminateWithoutAdmittingUnprovenBoards()
        {
            var options = new[]
            {
                new GenerationOptions(maxAttempts: 0), new GenerationOptions(maxLayoutNodes: 0),
                new GenerationOptions(maxLayoutNodesPerAttempt: 0), new GenerationOptions(maxRandomDraws: 0),
                new GenerationOptions(maxHumanConstraintEvaluations: 0), new GenerationOptions(maxHumanConstraintEvaluationsPerCall: 0),
                new GenerationOptions(maxVerifierNodes: 0), new GenerationOptions(maxVerifierNodesPerCall: 0)
            };
            var generator = new SeededEasyPuzzleGenerator();
            foreach (var limit in options)
            {
                var first = generator.Generate(DifficultyProfile.Easy, 0, limit);
                var second = generator.Generate(DifficultyProfile.Easy, 0, limit);
                Assert.That(first.Status, Is.EqualTo(GenerationStatus.Exhausted));
                AssertNoPuzzle(first);
                AssertWithinBudgets(first, limit);
                Assert.That(ResultFingerprint(second), Is.EqualTo(ResultFingerprint(first)));
            }
        }

        [Test]
        public void SmallPositiveBudgetsBoundFailedRequestsToo()
        {
            // Exercise each positive bound separately: a tiny random allowance must
            // not mask layout or solver-budget paths in the same request.
            var limits = new[]
            {
                new GenerationOptions(maxLayoutNodesPerAttempt: 1),
                new GenerationOptions(maxLayoutNodes: 1),
                new GenerationOptions(maxHumanConstraintEvaluationsPerCall: 1),
                new GenerationOptions(maxHumanConstraintEvaluations: 1),
                new GenerationOptions(maxRandomDraws: 3)
            };
            var generator = new SeededEasyPuzzleGenerator();
            foreach (var options in limits)
            foreach (var seed in new[] { 0, -1, int.MinValue, int.MaxValue })
            {
                var result = generator.Generate(DifficultyProfile.Easy, seed, options);
                Assert.That(result.Status, Is.EqualTo(GenerationStatus.Exhausted));
                AssertNoPuzzle(result);
                AssertWithinBudgets(result, options);
            }
        }

        [Test]
        public void UnassistedStalledCandidatesAndIncompleteProofsNeverEscapeAsSuccess()
        {
            var generator = new SeededEasyPuzzleGenerator();
            var stalledEvidence = false; var incompleteEvidence = false;
            for (var seed = 0; seed < 16; seed++)
            {
                var noArrows = generator.Generate(DifficultyProfile.Easy, seed,
                    new GenerationOptions(maxAttempts: 1, maxArrows: 0));
                Assert.That(noArrows.Status, Is.EqualTo(GenerationStatus.Exhausted));
                AssertNoPuzzle(noArrows);
                stalledEvidence |= noArrows.Diagnostics.RejectionCounts.TryGetValue(GenerationRejectionReason.HumanStalled, out var stalls) && stalls > 0;
                var noProof = generator.Generate(DifficultyProfile.Easy, seed,
                    new GenerationOptions(maxAttempts: 1, maxVerifierNodesPerCall: 0));
                Assert.That(noProof.Status, Is.EqualTo(GenerationStatus.Exhausted));
                AssertNoPuzzle(noProof);
                incompleteEvidence |= noProof.Diagnostics.RejectionCounts.TryGetValue(GenerationRejectionReason.VerificationIncomplete, out var incomplete) && incomplete > 0;
            }
            Assert.That(stalledEvidence, Is.True, "Real clue-only candidates must produce a recorded human stall.");
            Assert.That(incompleteEvidence, Is.True, "A real human-complete candidate must fail its bounded uniqueness proof.");
        }

        [Test]
        public void NegativeOptionsAreRejectedRatherThanExpandingWork()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new GenerationOptions(maxAttempts: -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new GenerationOptions(maxLayoutNodes: -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new GenerationOptions(maxLayoutNodesPerAttempt: -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new GenerationOptions(maxRandomDraws: -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new GenerationOptions(maxHumanConstraintEvaluations: -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new GenerationOptions(maxHumanConstraintEvaluationsPerCall: -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new GenerationOptions(maxVerifierNodes: -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new GenerationOptions(maxVerifierNodesPerCall: -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new GenerationOptions(maxArrows: -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new GenerationOptions(arrowTrialsPerStage: -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new GenerationOptions(maxArrows: 11));
            Assert.Throws<ArgumentOutOfRangeException>(() => new GenerationOptions(arrowTrialsPerStage: 9));
        }

        [Test]
        public void PreCancelledRequestReturnsNoPuzzleAndConsumesNoWork()
        {
            using (var source = new CancellationTokenSource())
            {
                source.Cancel();
                var result = new SeededEasyPuzzleGenerator().Generate(DifficultyProfile.Easy, 42, cancellationToken: source.Token);
                Assert.That(result.Status, Is.EqualTo(GenerationStatus.Cancelled));
                AssertNoPuzzle(result);
                Assert.That(result.Diagnostics.Attempts, Is.Zero);
                Assert.That(result.Diagnostics.LayoutNodesVisited, Is.Zero);
                Assert.That(result.Diagnostics.RandomDraws, Is.Zero);
                Assert.That(result.Diagnostics.HumanConstraintEvaluations, Is.Zero);
                Assert.That(result.Diagnostics.VerifierNodesVisited, Is.Zero);
            }
        }

        [Test]
        public void AcceptedBoardAndDiagnosticsCannotBeMutatedThroughReturnedCollections()
        {
            var result = new SeededEasyPuzzleGenerator().Generate(DifficultyProfile.Easy, 0);
            Assert.That(result.IsSuccess, Is.True);
            var fingerprint = ResultFingerprint(result);
            var cats = result.Puzzle.SolutionCats as ICollection<BoardCoordinate>;
            Assert.That(cats, Is.Not.Null);
            Assert.That(cats.IsReadOnly, Is.True);
            Assert.Throws<NotSupportedException>(() => cats.Clear());
            var arrows = result.Puzzle.Arrows as IDictionary<BoardCoordinate, Direction>;
            Assert.That(arrows, Is.Not.Null);
            Assert.That(arrows.IsReadOnly, Is.True);
            Assert.Throws<NotSupportedException>(() => arrows.Clear());
            var rejections = result.Diagnostics.RejectionCounts as IDictionary<GenerationRejectionReason, int>;
            Assert.That(rejections, Is.Not.Null);
            Assert.That(rejections.IsReadOnly, Is.True);
            Assert.Throws<NotSupportedException>(() => rejections.Clear());
            Assert.That(ResultFingerprint(result), Is.EqualTo(fingerprint));
        }

        [Test]
        public void FixedThousandSeedCorpusAdmitsOnlyIndependentlyValidProvenBoards()
        {
            var generator = new SeededEasyPuzzleGenerator();
            var options = new GenerationOptions();
            var attempts = new List<double>(); var layouts = new List<double>(); var humans = new List<double>();
            var verifiers = new List<double>(); var draws = new List<double>(); var elapsed = new List<double>();
            var arrows = new List<double>(); var cats = new List<double>(); var allocations = new List<double>();
            var readAllocation = DetectAllocationCounter();
            generator.Generate(DifficultyProfile.Easy, -123456789, options); // Warm editor/JIT outside the measured corpus.
            var logicalBoards = new HashSet<string>(); var solutionLayouts = new HashSet<string>();
            var statuses = new Dictionary<string, int>(); var rejected = new Dictionary<string, int>();
            var successes = 0;
            var diagonalDirections = new HashSet<Direction>();
            var boardsWithDiagonals = 0;
            for (var seed = 0; seed < 1000; seed++)
            {
                var beforeBytes = readAllocation?.Invoke() ?? 0;
                var startTicks = Stopwatch.GetTimestamp();
                var result = generator.Generate(DifficultyProfile.Easy, seed, options);
                var elapsedMs = (Stopwatch.GetTimestamp() - startTicks) * 1000.0 / Stopwatch.Frequency;
                if (readAllocation != null) allocations.Add(readAllocation() - beforeBytes);
                Increment(statuses, result.Status.ToString(), 1);
                attempts.Add(result.Diagnostics.Attempts); layouts.Add(result.Diagnostics.LayoutNodesVisited);
                humans.Add(result.Diagnostics.HumanConstraintEvaluations); verifiers.Add(result.Diagnostics.VerifierNodesVisited);
                draws.Add(result.Diagnostics.RandomDraws); elapsed.Add(elapsedMs);
                foreach (var reason in result.Diagnostics.RejectionCounts) Increment(rejected, reason.Key.ToString(), reason.Value);
                AssertWithinBudgets(result, options);
                if (!result.IsSuccess)
                {
                    Assert.That(result.Status, Is.EqualTo(GenerationStatus.Exhausted));
                    AssertNoPuzzle(result);
                    continue;
                }
                successes++;
                var diagonals = result.Puzzle.Arrows.Values.Where(d => (int)d >= 4).ToArray();
                if (diagonals.Length > 0) boardsWithDiagonals++;
                foreach (var direction in diagonals) diagonalDirections.Add(direction);
                AssertAccepted(result, DifficultyProfile.Easy, seed);
                logicalBoards.Add(LogicalBoard(result.Puzzle));
                solutionLayouts.Add(SolutionLayout(result.Puzzle));
                arrows.Add(result.Puzzle.Arrows.Count); cats.Add(result.Puzzle.SolutionCats.Count);
            }
            TestContext.Out.WriteLine("GENERATION_CORPUS " + "{" +
                "\"profile\":\"easy\",\"seedFirst\":0,\"seedLast\":999,\"runs\":1000," +
                "\"success\":" + successes + ",\"failure\":" + (1000 - successes) +
                ",\"uniqueLogicalBoards\":" + logicalBoards.Count + ",\"uniqueSolutionLayouts\":" + solutionLayouts.Count +
                ",\"statuses\":" + CountsJson(statuses) + ",\"rejections\":" + CountsJson(rejected) +
                ",\"attempts\":" + DistributionJson(attempts) + ",\"layoutNodes\":" + DistributionJson(layouts) +
                ",\"humanEvaluations\":" + DistributionJson(humans) + ",\"verifierNodes\":" + DistributionJson(verifiers) +
                ",\"randomDraws\":" + DistributionJson(draws) + ",\"arrows\":" + DistributionJson(arrows) +
                ",\"cats\":" + DistributionJson(cats) + ",\"timingContext\":\"warm-editor-generation-only\"" +
                ",\"elapsedMsInformational\":" + DistributionJson(elapsed) +
                ",\"allocationCounterAvailable\":" + (readAllocation == null ? "false" : "true") +
                ",\"allocatedBytesInformational\":" + DistributionJson(allocations) + "}");
            CollectionAssert.AreEquivalent(new[] { Direction.UpLeft, Direction.UpRight, Direction.DownLeft, Direction.DownRight }, diagonalDirections,
                "Actual accepted boards must exercise all four diagonal orientations.");
            Assert.That(boardsWithDiagonals, Is.GreaterThan(0), "Diagonal support must reach playable generated boards.");
            TestContext.Out.WriteLine("GENERATION_DIAGONALS boards=" + boardsWithDiagonals + " orientations=" + diagonalDirections.Count);
            Assert.That(successes, Is.EqualTo(1000), "The fixed v2 corpus must succeed within default work limits.");
            Assert.That(logicalBoards.Count, Is.EqualTo(1000), "The fixed corpus must retain its distinct logical boards.");
            Assert.That(solutionLayouts.Count, Is.GreaterThanOrEqualTo(950), "The v2 corpus must retain varied hidden layouts.");
        }

        private static Func<long> DetectAllocationCounter()
        {
            var method = typeof(GC).GetMethod("GetAllocatedBytesForCurrentThread", Type.EmptyTypes);
            if (method == null) return null;
            try
            {
                var read = (Func<long>)Delegate.CreateDelegate(typeof(Func<long>), method);
                var before = read();
                var probe = new byte[4096];
                probe[0] = 1;
                var after = read();
                GC.KeepAlive(probe);
                return after > before ? read : null;
            }
            catch (NotSupportedException) { return null; }
            catch (NotImplementedException) { return null; }
        }

        private static void AssertAccepted(GenerationResult result, DifficultyProfile profile, int seed)
        {
            var puzzle = result.Puzzle;
            Assert.That(result.Status, Is.EqualTo(GenerationStatus.Success));
            Assert.That(puzzle, Is.Not.Null);
            Assert.That(result.Assessment.IsAccepted, Is.True);
            Assert.That(puzzle.Size, Is.EqualTo(profile.Size)); Assert.That(puzzle.Seed, Is.EqualTo(seed));
            Assert.That(result.Metadata.Seed, Is.EqualTo(seed));
            Assert.That(result.Metadata.GeneratorVersion, Is.EqualTo("easy-generator-v2"));
            Assert.That(puzzle.GeneratorVersion, Is.EqualTo(result.Metadata.GeneratorVersion));
            Assert.That(puzzle.Arrows.Count, Is.LessThanOrEqualTo(result.Metadata.Options.MaxArrows));
            Assert.That(puzzle.Size * puzzle.Size - puzzle.SolutionCats.Count - puzzle.Arrows.Count, Is.GreaterThanOrEqualTo(5));
            Assert.That(result.Metadata.ProfileVersion, Is.EqualTo("easy-profile-v1"));
            Assert.That(result.Metadata.RandomVersion, Is.EqualTo("splitmix64-v1"));
            Assert.That(result.Metadata.RulesetVersion, Is.EqualTo("human-v2"));
            Assert.That(result.Metadata.VerifierVersion, Is.EqualTo("exhaustive-v2"));
            Assert.That(result.Metadata.OptionsVersion, Is.EqualTo("generation-options-v1"));
            Assert.That(result.Metadata.ProfileId, Is.EqualTo(profile.Id));
            Assert.That(result.Metadata.AcceptedAttempt, Is.InRange(0, result.Diagnostics.Attempts - 1));
            Assert.That(result.Metadata.BoardFingerprint, Is.EqualTo(GenerationFingerprints.ForBoard(puzzle)));
            Assert.That(result.Metadata.SolutionFingerprint, Is.EqualTo(GenerationFingerprints.ForSolution(puzzle)));
            var solution = new HashSet<BoardCoordinate>(puzzle.SolutionCats);
            foreach (var cat in solution)
            {
                Assert.That(cat.Row, Is.InRange(0, profile.Size - 1)); Assert.That(cat.Column, Is.InRange(0, profile.Size - 1));
                Assert.That(puzzle.Arrows.ContainsKey(cat), Is.False);
            }
            var rowCounts = Enumerable.Range(0, profile.Size).Select(r => solution.Count(c => c.Row == r)).ToArray();
            var colCounts = Enumerable.Range(0, profile.Size).Select(c => solution.Count(p => p.Column == c)).ToArray();
            Assert.That(rowCounts.All(c => c <= profile.MaxCatsPerLine), Is.True);
            Assert.That(colCounts.All(c => c <= profile.MaxCatsPerLine), Is.True);
            Assert.That(rowCounts.Count(c => c == 0), Is.LessThanOrEqualTo(profile.MaxZeroRows));
            Assert.That(colCounts.Count(c => c == 0), Is.LessThanOrEqualTo(profile.MaxZeroColumns));
            foreach (var arrow in puzzle.Arrows)
            {
                Assert.That(arrow.Key.Row, Is.InRange(0, profile.Size - 1)); Assert.That(arrow.Key.Column, Is.InRange(0, profile.Size - 1));
                Assert.That(Enum.IsDefined(typeof(Direction), arrow.Value), Is.True);
                Assert.That(solution.Any(cat => PointsAt(arrow.Key, arrow.Value, cat)), Is.True, "Every arrow must be true from coordinates alone.");
            }
            var clues = PuzzleClues.FromPuzzle(puzzle);
            CollectionAssert.AreEqual(rowCounts, clues.RowTotals); CollectionAssert.AreEqual(colCounts, clues.ColumnTotals);
            var independentAssessment = PuzzleSolvabilityAssessment.Assess(puzzle, profile);
            Assert.That(independentAssessment.IsAccepted, Is.True);
            Assert.That(independentAssessment.Solver.Human.Status, Is.EqualTo(HumanSolverStatus.Solved));
            Assert.That(independentAssessment.Solver.Uniqueness.Status, Is.EqualTo(UniquenessStatus.Unique));
            CollectionAssert.AreEquivalent(solution, independentAssessment.Solver.Uniqueness.Solutions.Single());
            CollectionAssert.AreEquivalent(solution, independentAssessment.Solver.Human.Assignments.Where(c => c.State == CellKnowledge.Cat).Select(c => c.Coordinate));
        }

        private static void AssertNoPuzzle(GenerationResult result)
        {
            Assert.That(result.IsSuccess, Is.False); Assert.That(result.Puzzle, Is.Null); Assert.That(result.Assessment, Is.Null);
            Assert.That(result.Metadata.AcceptedAttempt, Is.EqualTo(-1));
            Assert.That(result.Metadata.BoardFingerprint, Is.Null); Assert.That(result.Metadata.SolutionFingerprint, Is.Null);
        }

        private static void AssertWithinBudgets(GenerationResult result, GenerationOptions options)
        {
            Assert.That(result.Diagnostics.Attempts, Is.InRange(0, options.MaxAttempts));
            Assert.That(result.Diagnostics.LayoutNodesVisited, Is.InRange(0, options.MaxLayoutNodes));
            Assert.That(result.Diagnostics.HumanConstraintEvaluations, Is.InRange(0, options.MaxHumanConstraintEvaluations));
            Assert.That(result.Diagnostics.VerifierNodesVisited, Is.InRange(0, options.MaxVerifierNodes));
            Assert.That(result.Diagnostics.RandomDraws, Is.InRange(0, options.MaxRandomDraws));
            Assert.That(result.Diagnostics.LayoutNodesVisited, Is.LessThanOrEqualTo((long)result.Diagnostics.Attempts * options.MaxLayoutNodesPerAttempt));
            Assert.That(result.Diagnostics.ArrowTrials, Is.InRange(0L, (long)result.Diagnostics.Attempts * options.MaxArrows * options.ArrowTrialsPerStage));
        }

        private static bool PointsAt(BoardCoordinate origin, Direction direction, BoardCoordinate cat) =>
            direction == Direction.Up && cat.Column == origin.Column && cat.Row < origin.Row ||
            direction == Direction.Down && cat.Column == origin.Column && cat.Row > origin.Row ||
            direction == Direction.Left && cat.Row == origin.Row && cat.Column < origin.Column ||
            direction == Direction.Right && cat.Row == origin.Row && cat.Column > origin.Column ||
            direction == Direction.UpLeft && cat.Row < origin.Row && cat.Column < origin.Column && origin.Row - cat.Row == origin.Column - cat.Column ||
            direction == Direction.UpRight && cat.Row < origin.Row && cat.Column > origin.Column && origin.Row - cat.Row == cat.Column - origin.Column ||
            direction == Direction.DownLeft && cat.Row > origin.Row && cat.Column < origin.Column && cat.Row - origin.Row == origin.Column - cat.Column ||
            direction == Direction.DownRight && cat.Row > origin.Row && cat.Column > origin.Column && cat.Row - origin.Row == cat.Column - origin.Column;

        private static string SolutionLayout(PuzzleDefinition puzzle) => string.Join(";", puzzle.SolutionCats.OrderBy(c => c.Row).ThenBy(c => c.Column).Select(c => c.Row + "," + c.Column));
        private static string LogicalBoard(PuzzleDefinition puzzle) => puzzle.Size + ":" + SolutionLayout(puzzle) + ":" +
            string.Join(";", puzzle.Arrows.OrderBy(a => a.Key.Row).ThenBy(a => a.Key.Column).Select(a => a.Key.Row + "," + a.Key.Column + "," + (int)a.Value));
        private static string ResultFingerprint(GenerationResult result) => result.Status + ":" + result.Metadata.BoardFingerprint + ":" +
            result.Metadata.SolutionFingerprint + ":" + result.Metadata.AcceptedAttempt + ":" + result.Diagnostics.Attempts + ":" +
            result.Diagnostics.LayoutNodesVisited + ":" + result.Diagnostics.HumanConstraintEvaluations + ":" + result.Diagnostics.VerifierNodesVisited + ":" +
            result.Diagnostics.RandomDraws + ":" + result.Diagnostics.ArrowTrials + ":" +
            string.Join(";", result.Diagnostics.RejectionCounts.OrderBy(k => k.Key).Select(k => k.Key + "=" + k.Value));
        private static void Increment(Dictionary<string, int> counts, string key, int count) => counts[key] = (counts.TryGetValue(key, out var old) ? old : 0) + count;
        private static string CountsJson(Dictionary<string, int> counts) => "{" + string.Join(",", counts.OrderBy(c => c.Key, StringComparer.Ordinal).Select(c => "\"" + c.Key + "\":" + c.Value)) + "}";
        private static string DistributionJson(List<double> samples)
        {
            if (samples.Count == 0) return "null";
            var sorted = samples.OrderBy(v => v).ToArray();
            double Percentile(double p) => sorted[Math.Max(0, (int)Math.Ceiling(sorted.Length * p) - 1)];
            string F(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
            return "{\"min\":" + F(sorted.First()) + ",\"p50\":" + F(Percentile(.5)) + ",\"p95\":" + F(Percentile(.95)) +
                ",\"max\":" + F(sorted.Last()) + ",\"mean\":" + F(sorted.Average()) + "}";
        }
    }
}
