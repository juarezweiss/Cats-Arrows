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

## Native baseline, 2026-10-07

Source `5c2a0ff` includes generator commit `d079622` and the reviewed harness.
Pinned Unity `6000.3.25f1` built and ran a non-development macOS Mono player on an
Apple M1 MacBook Pro (16 GB), batch mode with Null graphics. Build and player
exited zero. No compilation or test process overlapped this final measurement.

| Measurement | Median | p95 | Maximum |
| --- | ---: | ---: | ---: |
| Warm generation, ms | 6.7926 | 12.1711 | 23.7678 |
| Baseline coroutine interval, ms | 16.7423 | 18.4863 | 18.6276 |
| Generation coroutine interval, ms | 16.6774 | 17.3230 | 33.3643 |

First generator invocation took 54.2943 ms. All 1,000 requests succeeded; the
slowest seed was `371`, which required two attempts. Allocation measurement was
unavailable and every allocation statistic is explicitly `-1`. The raw
`GC.CollectionCount` API delta was `[15572]`; this is not a count or duration of
stop-the-world pauses. Use device Profiler captures to assess actual GC impact.

Logs/report are saved locally under ignored `.unity-results/`:
`issue4-build-5c2a0ff.log`, `issue4-macos-5c2a0ff.log`, and
`issue4-macos-5c2a0ff.json`. Native compilation and all 61 EditMode tests passed
during harness validation (`compile.MKlQ3k`, `editmode.7mn9GQ`). Final build also
compiled the committed reporting fixes. Temporary scenes/settings were cleaned
up and no game code changed for the benchmark.

This confirms a usable harness and exposes occasional foreground intervals
longer than a 60 fps frame. It does not determine mobile loading latency, prove a
mobile blocker, or approve a loading/background policy. On this machine the
pinned editor has only `MacStandaloneSupport`; Android SDK/adb and full Xcode
are absent. Physical-device measurement is pending phone availability and the
corresponding toolchain.
