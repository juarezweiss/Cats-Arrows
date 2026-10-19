using System;
using System.Collections;
using System.IO;
using System.Linq;
using CatsAndArrows.Core;
using Unity.Profiling;
using UnityEngine;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace CatsAndArrows.Benchmarks
{
    // Issue #4 internal diagnostic scene only. No runtime bootstrap or gameplay dependency.
    public sealed class GenerationBenchmark : MonoBehaviour
    {
        private const int Runs = 1000;
        private static readonly ProfilerMarker GenerateMarker = new ProfilerMarker("CatsAndArrows.Benchmark.Generate");
        public string SourceRevision = "unspecified";
        public string ProfileId = "easy";
        private string _status = "Starting generation benchmark";
        private string _outputPath;
        private bool _quit;

        private IEnumerator Start()
        {
            Application.targetFrameRate = 60;
            var arguments = Environment.GetCommandLineArgs();
            _quit = Array.IndexOf(arguments, "-benchmarkQuit") >= 0;
            var outputIndex = Array.IndexOf(arguments, "-benchmarkOutput");
            _outputPath = outputIndex >= 0 && outputIndex + 1 < arguments.Length
                ? arguments[outputIndex + 1]
                : Path.Combine(Application.persistentDataPath, "generation-benchmark-" + DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffZ") + ".json");
            var profileIndex = Array.IndexOf(arguments, "-benchmarkProfile");
            var profileId = profileIndex >= 0 && profileIndex + 1 < arguments.Length ? arguments[profileIndex + 1] : ProfileId;
            var profile = profileId switch
            {
                "easy" => DifficultyProfile.Easy, "medium" => DifficultyProfile.Medium,
                "hard" => DifficultyProfile.Hard, _ => null
            };
            var report = new Report
            {
                utcStarted = DateTime.UtcNow.ToString("O"), sourceRevision = SourceRevision,
                unityVersion = Application.unityVersion, platform = Application.platform.ToString(),
                deviceModel = SystemInfo.deviceModel, processor = SystemInfo.processorType,
                processorCount = SystemInfo.processorCount, systemMemoryMb = SystemInfo.systemMemorySize,
                operatingSystem = SystemInfo.operatingSystem, developmentBuild = Debug.isDebugBuild,
                batchMode = Application.isBatchMode, graphicsDevice = SystemInfo.graphicsDeviceType.ToString(),
                profileId = profileId, targetFrameRate = 60,
                samples = new Sample[Runs]
            };
#if ENABLE_IL2CPP
            report.scriptingBackend = "IL2CPP";
#elif ENABLE_MONO
            report.scriptingBackend = "Mono";
#else
            report.scriptingBackend = "unknown";
#endif
            if (profile == null) { Finish(report, "Unsupported benchmark profile: " + profileId); yield break; }
            IPuzzleGenerator generator = new SeededPuzzleGenerator();
            if (!TryMeasure(generator, profile, 0, false, out report.firstInvocation, out var error))
            { Finish(report, error); yield break; }
            report.generatorVersion = report.firstInvocation.generatorVersion;
            report.profileVersion = report.firstInvocation.profileVersion;
            report.optionsVersion = report.firstInvocation.optionsVersion;
            yield return null;

            // First invocation precedes all generator warm-up. This is not app launch latency.
            if (!TryMeasure(generator, profile, -123456789, false, out _, out error))
            { Finish(report, error); yield break; }
            report.allocationCounterAvailable = AllocationCounterWorks();
            var baseline = new double[60];
            for (var i = 0; i < baseline.Length; i++)
            {
                var before = Stopwatch.GetTimestamp();
                yield return null;
                baseline[i] = MillisecondsSince(before);
            }
            report.baselineCoroutineIntervalMs = Distribution.From(baseline);
            var elapsed = new double[Runs];
            var frameIntervals = new double[Runs];
            var allocated = report.allocationCounterAvailable ? new double[Runs] : null;
            var gcBefore = CollectionCounts();
            var max = -1.0;
            for (var seed = 0; seed < Runs; seed++)
            {
                _status = "Generating seed " + seed + " / 999";
                var frameStart = Stopwatch.GetTimestamp();
                if (!TryMeasure(generator, profile, seed, report.allocationCounterAvailable, out var sample, out error))
                { Finish(report, error); yield break; }
                report.samples[seed] = sample;
                report.completedRuns++;
                if (sample.success) report.successes++; else report.failures++;
                elapsed[seed] = sample.generationMs;
                if (allocated != null) allocated[seed] = sample.allocatedBytes;
                if (sample.generationMs > max) { max = sample.generationMs; report.slowestSeed = seed; }
                yield return null; // Deliberately foreground work: exposes potential frame interference.
                sample.coroutineIntervalMs = frameIntervals[seed] = MillisecondsSince(frameStart);
            }
            var gcAfter = CollectionCounts();
            report.gcCollectionCountApiDelta = new int[gcAfter.Length];
            for (var i = 0; i < gcAfter.Length; i++) report.gcCollectionCountApiDelta[i] = gcAfter[i] - gcBefore[i];
            report.warmGenerationMs = Distribution.From(elapsed);
            report.generationCoroutineIntervalMs = Distribution.From(frameIntervals);
            if (allocated != null) report.allocatedBytes = Distribution.From(allocated);
            Finish(report, null);
        }

        private static bool TryMeasure(IPuzzleGenerator generator, DifficultyProfile profile, int seed,
            bool allocationAvailable, out Sample sample, out string error)
        {
            sample = null; error = null;
            try
            {
                var beforeBytes = allocationAvailable ? GC.GetAllocatedBytesForCurrentThread() : 0;
                var started = Stopwatch.GetTimestamp();
                GenerationResult result;
                using (GenerateMarker.Auto()) result = generator.Generate(profile, seed);
                var elapsed = MillisecondsSince(started);
                var bytes = allocationAvailable ? GC.GetAllocatedBytesForCurrentThread() - beforeBytes : -1;
                sample = new Sample
                {
                    seed = seed, generationMs = elapsed, allocatedBytes = bytes,
                    success = result.IsSuccess, status = result.Status.ToString(),
                    attempts = result.Diagnostics.Attempts, humanEvaluations = result.Diagnostics.HumanConstraintEvaluations,
                    verifierNodes = result.Diagnostics.VerifierNodesVisited
                };
                sample.generatorVersion = result.Metadata.GeneratorVersion;
                sample.profileVersion = result.Metadata.ProfileVersion;
                sample.optionsVersion = result.Metadata.OptionsVersion;
                sample.boardFingerprint = result.Metadata.BoardFingerprint;
                sample.pruningTrials = result.Diagnostics.PruningTrials;
                sample.pruningDeletionsRetained = result.Diagnostics.PruningDeletionsRetained;
                sample.pruningHumanEvaluations = result.Diagnostics.PruningHumanConstraintEvaluations;
                sample.pruningVerifierNodes = result.Diagnostics.PruningVerifierNodesVisited;
                sample.pruningBudgetExhausted = result.Diagnostics.PruningBudgetExhausted;
                if (result.IsSuccess)
                {
                    sample.arrows = result.Puzzle.Arrows.Count;
                    sample.hatches = ClueProtectionRules.ForProfile(profile, PuzzleClues.FromPuzzle(result.Puzzle),
                        result.Assessment.Solver.Human).Count;
                    sample.fixedCellFraction = (double)(sample.arrows + sample.hatches) /
                        (result.Puzzle.Size * result.Puzzle.Size);
                    sample.cats = result.Puzzle.SolutionPaws.Count;
                    sample.deductionSteps = result.Assessment.Solver.Human.Metrics.StepCount;
                    sample.dependencyDepth = result.Assessment.Solver.Human.Metrics.MaximumDependencyDepth;
                    sample.stepsToLastCat = result.Assessment.Solver.Human.Steps
                        .Where(step => step.Conclusions.Any(a => a.State == CellKnowledge.Paw))
                        .Select(step => step.Index + 1).DefaultIfEmpty(0).Max();
                }
                return true;
            }
            catch (Exception exception) { error = exception.ToString(); return false; }
        }

        private static bool AllocationCounterWorks()
        {
            try
            {
                var before = GC.GetAllocatedBytesForCurrentThread();
                var probe = new byte[4096];
                probe[0] = 1;
                var after = GC.GetAllocatedBytesForCurrentThread();
                GC.KeepAlive(probe);
                return after > before;
            }
            catch (NotImplementedException) { return false; }
            catch (NotSupportedException) { return false; }
        }

        private static int[] CollectionCounts()
        {
            var counts = new int[GC.MaxGeneration + 1];
            for (var i = 0; i < counts.Length; i++) counts[i] = GC.CollectionCount(i);
            return counts;
        }

        private static double MillisecondsSince(long started) => (Stopwatch.GetTimestamp() - started) * 1000.0 / Stopwatch.Frequency;

        private void Finish(Report report, string error)
        {
            report.error = error;
            report.complete = error == null && report.completedRuns == Runs;
            try
            {
                var directory = Path.GetDirectoryName(_outputPath);
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
                File.WriteAllText(_outputPath, JsonUtility.ToJson(report, true));
                Debug.Log("GENERATION_BENCHMARK_COMPLETE path=" + _outputPath + " complete=" + report.complete +
                    " success=" + report.successes + " failure=" + report.failures);
                _status = "Benchmark " + (report.complete ? "complete" : "failed") + "\n" + _outputPath;
            }
            catch (Exception exception)
            {
                error = exception.ToString();
                _status = "Could not save benchmark report";
                Debug.LogException(exception);
            }
            if (error != null) Debug.LogError(error);
            if (_quit) Application.Quit(error == null && report.complete && report.failures == 0 ? 0 : 1);
        }

        private void OnGUI()
        {
            // Internal diagnostic information, not a shipped player flow.
            var safe = Screen.safeArea;
            GUI.Label(new Rect(safe.x + 12, Screen.height - safe.yMax + 12, safe.width - 24, 180), _status);
        }

        [Serializable]
        private sealed class Report
        {
            public string schemaVersion = "generation-benchmark-v2";
            public string task = "Issue #4 / #35";
            public string measurement = "first generator invocation then warmed foreground corpus; Generate plus ProfilerMarker scope overhead; excludes app launch";
            public string frameMeasurement = "coroutine resume interval, includes scheduling/render wait; not input latency or GPU timing";
            public string allocationMeasurement = "current thread Generate plus ProfilerMarker scope bytes if known allocation probe advances; -1 unavailable, first invocation unmeasured";
            public string gcMeasurement = "raw GC.CollectionCount API deltas over corpus including harness and frame waits; not evidence of full collection cycles or pauses";
            public string clueMeasurement = "post-Generate metrics excluded from generation timing/allocation; profile hatch policy (Hard none, others accepted proof); canonical step/depth counts are not calibrated player difficulty";
            public string utcStarted, sourceRevision, unityVersion, platform, deviceModel, processor, operatingSystem;
            public string scriptingBackend, graphicsDevice, generatorVersion, profileId, profileVersion, optionsVersion, error;
            public int processorCount, systemMemoryMb, targetFrameRate;
            public int seedFirst = 0, seedLast = 999, requestedRuns = Runs;
            public int completedRuns, successes, failures, slowestSeed;
            public bool developmentBuild, batchMode, allocationCounterAvailable, complete;
            public Sample firstInvocation;
            public Sample[] samples;
            public int[] gcCollectionCountApiDelta;
            public Distribution warmGenerationMs, baselineCoroutineIntervalMs, generationCoroutineIntervalMs;
            public Distribution allocatedBytes = new Distribution { min = -1, p50 = -1, p95 = -1, max = -1, mean = -1 };
        }

        [Serializable]
        private sealed class Sample
        {
            public int seed, attempts, humanEvaluations, verifierNodes;
            // Legacy field names are part of the generation-benchmark-v2 JSON schema.
            public int arrows = -1, cats = -1, deductionSteps = -1, dependencyDepth = -1, stepsToLastCat = -1;
            public int hatches = -1, pruningTrials, pruningDeletionsRetained, pruningHumanEvaluations, pruningVerifierNodes;
            public bool pruningBudgetExhausted;
            public double fixedCellFraction = -1;
            public string generatorVersion, profileVersion, optionsVersion, boardFingerprint;
            public string status;
            public bool success;
            public double generationMs, coroutineIntervalMs;
            public long allocatedBytes;
        }

        [Serializable]
        private sealed class Distribution
        {
            public double min, p50, p95, max, mean;
            public static Distribution From(double[] samples)
            {
                var sorted = (double[])samples.Clone();
                Array.Sort(sorted);
                var sum = 0.0;
                foreach (var value in sorted) sum += value;
                return new Distribution
                {
                    min = sorted[0], p50 = sorted[(int)Math.Ceiling(sorted.Length * .5) - 1],
                    p95 = sorted[(int)Math.Ceiling(sorted.Length * .95) - 1], max = sorted[sorted.Length - 1],
                    mean = sum / sorted.Length
                };
            }
        }
    }
}
