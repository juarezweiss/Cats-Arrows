# Issue #4 generation benchmark

This internal diagnostic player measures the existing seeded Easy generator. It
does not add a game screen or change puzzle admission. The benchmark assembly is
excluded from ordinary players unless `CATS_ARROWS_BENCHMARK` is defined.

Issue #35 extends this opt-in harness to Medium/Hard; Easy remains the default.
Set `CATS_ARROWS_BENCHMARK_PROFILE=medium` or `hard` when building, including for
phones where managed launch arguments may be unavailable. Desktop launches may
override the serialized choice with `-benchmarkProfile medium` or `hard`.
Unsupported profiles fail explicitly. Schema `generation-benchmark-v2` records
actual generator/profile/options versions, fingerprints, arrows/hatches, combined
fixed-cell fraction, canonical proof steps/depth, steps through the last cat and
pruning work/exhaustion. These extra clue metrics are computed after the timed
Generate call, using its accepted proof without an extra solver run. They do not
add new game rules, density rejection thresholds or calibrated difficulty scores.
Earlier results below retain their original generator and harness identities.

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
mobile blocker, or approve a loading/background policy. A later inventory check
confirmed Android/iOS Unity modules in the `PlaybackEngines` directory beside
`Unity.app`, plus Unity's bundled Android tools and full Xcode with its iOS SDK.
The earlier module check inspected only the directory inside `Unity.app` and
missed the sibling directory. The physical iPhone baseline follows below.

## Physical iPhone baseline, 2026-10-07

Source `be9a288`, Unity `6000.3.25f1`, iPhone 17 (`iPhone18,3`), iOS `27.0.1`.
Unity exported a non-development iOS player; Xcode built the `Release`
configuration with IL2CPP and Metal. Development signing was configured in the
ignored exported project, without changing tracked game code or settings.
Installation succeeded. Three fresh process launches ran without a debugger
attached, while the physical phone was connected by USB and the diagnostic app
was in the foreground. These are short runs on one device, not a thermal or
battery-condition sweep. No claim is made about Low Power Mode or thermal state.

| Run | First invocation (ms) | Warm median (ms) | Warm p95 (ms) | Warm maximum (ms) | Successful requests |
| --- | ---: | ---: | ---: | ---: | ---: |
| 1 | 33.6173 | 6.4265 | 12.2116 | 23.7290 | 1,000/1,000 |
| 2 | 29.5815 | 6.2553 | 12.0311 | 20.2426 | 1,000/1,000 |
| 3 | 30.2734 | 6.3591 | 12.2568 | 23.6608 | 1,000/1,000 |

The pooled 3,000 measurements (three repetitions of seeds `0..999`, not 3,000
different seeds) have median **6.3469 ms**, p95 **12.1421 ms**, and maximum
**23.7290 ms**. Seed `371` was slowest in all three runs and required two
attempts. Eight warm generation calls exceeded `1000/60` ms (0.27%); no warm
call exceeded `1000/30` ms. First invocation excludes app launch latency.

Coroutine intervals had median approximately 16.66 ms, p95 16.90–16.94 ms,
and maxima 22.46–27.12 ms. These intervals include scheduling/render waits and
do not measure tap-to-ready latency, missed presents, GPU time, or actual input
responsiveness. The diagnostic scene is much simpler than the eventual game UI.
Allocation counters were unavailable in all three runs; all reported allocation
values are `-1`. Raw `GC.CollectionCount` deltas do not establish pause counts or
durations. Allocation and frame-impact profiling remain separate work.

Xcode build log: `.unity-results/issue4-ios-xcode-build.log`. Full reports were
retrieved from the app data container into `.unity-results/issue4-iphone17-run1.json`,
`issue4-iphone17-run2.json`, and `issue4-iphone17-run3.json`. Report outcomes and
statistics were independently recomputed by QA. Build intermediates were removed
after measurement to reduce local storage; the signed app, export, and reports
are retained.

The measured Easy generator does not show a substantial loading wait on this
iPhone 17. This supports continuing development rather than treating generation
as a demonstrated blocker. Rare calls can exceed a 60 fps CPU frame budget,
supporting advance preparation or background execution when New Game is wired.
Choose that scheduling policy with measurements in the real gameplay scene.
Representative older/midrange Android hardware, the complete New Game path,
allocation/GC profiling, and future larger/different profiles remain unverified.

## Issue #14 diagonal generator, 2026-10-08

`easy-generator-v2` adds diagonal candidates with `human-v2` / `exhaustive-v2`
clue semantics and unchanged profile, PRNG and numeric work limits. The fresh
non-development iOS export and Xcode Release build succeeded, and the app was
installed on the paired iPhone 17. After the owner unlocked the device, three
fresh process launches ran the same 0..999 corpus without a debugger attached.
The historical v1 measurements remain a separate baseline.

Source identity for this local, uncommitted build is
`fa21ed1+issue14-source-ca671115292a38ec4b8cc9e1dca6d7b1f4981ec801a83080b9ae21f5df63c83f`.
The SHA-256 covers sorted paths and bytes of the non-test C# sources under
`Assets/CatsAndArrows`. Logs: `.unity-results/issue14-ios-export.log` and
`.unity-results/issue14-ios-xcode-build.log`. Development signing was restored
only in the ignored Xcode export; no signing settings or credentials entered
tracked source. Native v2 corpus correctness/results are in `TESTING.md`.

| Run | First invocation (ms) | Warm median (ms) | Warm p95 (ms) | Warm maximum (ms) | Successes |
| --- | ---: | ---: | ---: | ---: | ---: |
| 1 | 30.4889 | 6.9006 | 13.1006 | 21.6817 | 1,000/1,000 |
| 2 | 29.5641 | 7.1933 | 13.3472 | 20.5674 | 1,000/1,000 |
| 3 | 29.3051 | 7.3294 | 13.3914 | 20.2230 | 1,000/1,000 |

Pooled 3,000 requests (three repetitions of 1,000 seeds) have median **7.1056 ms**,
p95 **13.3421 ms** and maximum **21.6817 ms**. All completed successfully; 21
warm calls exceeded 16.67 ms (0.7%) and none exceeded 33.33 ms. The slowest seeds
were 127 in runs 1/2 and 874 in run 3. This is encouraging for Easy generation
on this iPhone, with a modest median/p95 increase over the historical cardinal
baseline, without establishing a controlled causal comparison. It does not
measure the full New Game path, older devices, sustained thermal behavior or
larger difficulties. Numeric generation bounds were not increased.

Hardware/runtime: iPhone 17 (iPhone18,3), iOS 27.0.1, Unity 6000.3.25f1,
Release IL2CPP/Metal, non-development/non-batch player. All three reports explicitly
lack allocation counters; raw collection-count deltas and coroutine intervals
retain the limitations described above. First invocation excludes app startup.
No Unity test or Xcode build process overlapped these timed runs.

Full reports: `.unity-results/issue14-iphone17-run1.json`,
`.unity-results/issue14-iphone17-run2.json`, `.unity-results/issue14-iphone17-run3.json`.
Per-seed success, seed order, build provenance and nearest-rank statistics were
independently recomputed by QA from the raw reports. The reused harness retains
its original `task: Issue #4` metadata; v2 generator/source identity identifies
this Issue #14 run. The test corpus separately confirms diagonal
incidence; this timing harness does not record per-board directions. The signed
benchmark remains installed; it is a diagnostic app, not the playable game.
