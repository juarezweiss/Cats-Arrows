# Issue #4 generation benchmark

This internal diagnostic player measures the existing seeded Easy generator. It
does not add a game screen or change puzzle admission. The benchmark assembly is
excluded from ordinary players unless `CATS_ARROWS_BENCHMARK` is defined.

The first `Generate(Easy, 0)` invocation is recorded before generator warm-up;
this is not application startup or New Game input latency. After warm-up and 60
baseline frames, seeds `0..999` run on the main thread, one request per frame.
JSON includes per-seed outcomes and timings, median/p95/maximum, slowest seed,
first invocation, backend/build/hardware context, collection-count deltas, and
coroutine intervals. Allocation bytes are available only when a known allocation
probe advances the runtime counter. Timing includes the small ProfilerMarker
scope overhead. Frame intervals include scheduling/render waits; collection
counts include harness work. Neither is a direct measure of production UI lag.

## Build and run

Close the project in the GUI Editor. Use the exact editor reported by
`bash scripts/unity.sh`. Build output should use the ignored `Builds/` directory.
For example, from the repository root:

```bash
mkdir -p Builds/Issue4Benchmark .unity-results
CATS_ARROWS_BENCHMARK_OUTPUT="$PWD/Builds/Issue4Benchmark/GenerationBenchmark.app" \
CATS_ARROWS_BENCHMARK_TARGET=StandaloneOSX \
CATS_ARROWS_BENCHMARK_REVISION="$(git rev-parse HEAD)" \
"$(bash scripts/unity.sh)" -batchmode -nographics \
  -projectPath "$PWD/UnityProject" \
  -executeMethod CatsAndArrows.Benchmarks.Editor.GenerationBenchmarkBuild.Build \
  -logFile "$PWD/.unity-results/benchmark-build.log" -quit

Builds/Issue4Benchmark/GenerationBenchmark.app/Contents/MacOS/UnityProject \
  -batchmode -nographics -benchmarkQuit \
  -benchmarkOutput "$PWD/.unity-results/benchmark-macos.json" \
  -logFile "$PWD/.unity-results/benchmark-player.log"
```

The default is a release build. Set `CATS_ARROWS_BENCHMARK_DEVELOPMENT=1` for a
diagnostic build that can connect to Unity Profiler. Use the marker
`CatsAndArrows.Benchmark.Generate`; avoid Deep Profiling for baseline timing.
Repeat release measurements with the profiler disconnected.

For Android, select target `Android` and an absolute `.apk` output path. For iOS,
select target `iOS` and an absolute Xcode export directory. The pinned editor must
have the matching Unity Hub module installed. Android also needs its SDK/NDK/JDK
and a USB-debugging-enabled phone; iOS needs full Xcode and device development
signing. This helper does not install tools, configure signing, or submit builds.
Record the backend from the JSON; the helper uses the existing backend settings.

On phones the report is saved to `Application.persistentDataPath`, and the
diagnostic screen/log shows the exact filename. Android reports can be pulled
from the app's accessible external files directory with `adb pull`; if device
access restrictions prevent this, use a development build's accessible app
container. For iOS, use Xcode Devices and Simulators to download the app container
and retrieve the report from its data directory. Preserve the full JSON, not
only the completion log. `-benchmarkOutput` and `-benchmarkQuit` are optional
desktop command-line flags; phone builds stay on the completed diagnostic screen.

## Mobile decision gate

Run on a physical iPhone and representative older/midrange Android, record their
models and OS versions, and repeat across fresh launches. Establish acceptable
generation latency and frame responsiveness before declaring mobile performance
accepted. Inspect slow seeds/retries and profiler allocations as well as typical
timing. The fixed corpus is not a global worst-case guarantee or sustained thermal
test. The eventual gameplay scene must separately measure tap-to-ready latency,
background preparation, and queue-empty behavior.

Desktop/headless results verify the harness and provide a baseline. They cannot
resolve the mobile performance risk.
