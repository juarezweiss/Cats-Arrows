# Testing Strategy

## Issue #32 difficulty coverage

Pinned Unity 6000.3.25f1 compile passed; EditMode `editmode.dIeNZn` passed 285/285
with successful editor exit, zero failed/skipped/inconclusive. Difficulty tests
cover 128 fixed seeds per profile with independent human/uniqueness reassessment,
truthful arrows, quotas/zeros/required 3, frozen signed-seed receipts, explicit
budgets/cancellation/invalid profiles and concurrent/interleaved replay. Easy
goldens and the existing tutorial tests pass; new shell regressions check the
unchanged tutorial fingerprint after Medium/Hard selection, localized choices,
actual/live-vs-next labels, retry, protected input and completed-board invariants.
Separate pure-C# seed 0–999 corpora accepted 1,000/1,000 for each new profile.

Mounted macOS checks passed Easy/Medium/Hard EN 320×568, Medium pt-BR 390×844,
Hard ES 430×932: actual menu pointer selection, Hint/Clear/Menu/NewGame, protected
cells, pointer tap cycle/completion/readonly state, centered grid and 20 new-game
transitions per run. Thirty captures and logs are retained in ignored
`.unity-results/issue32-captures/`; representative images are in
`docs/evidence/issue32/`. These are desktop portrait simulations, not physical
phone comfort/latency evidence. QA found no blocking source defects. No new solver
rules or difficulty-score thresholds were used.

Physical iPhone 17 / iOS 27.0.1, 2026-10-14: the owner confirmed all requested
Medium 6×6 and Hard 7×7 smoke checks passed: edge/rapid/held taps and the tap cycle,
horizontal/vertical drags, Hint/Clear/Menu/back, repeated New Game, hints-to-win
and completed-board lock, background/reopen and portrait lock. No missed/extra
marks, cramped targets, unreadable clues, visible pauses or layout problems were
reported. The owner also confirmed all three languages (English, pt-BR and
Spanish) passed. This is qualitative acceptance on one device; smaller physical
phones and measured Medium/Hard p95 latency are not established by this run.

The Release/IL2CPP arm64 review build used source d3452ba (gameplay identical to
develop 2ca1e14), Unity 6000.3.25f1 and Xcode 27.0 (27A266a). Export, final Xcode
build, signature verification, install and launch passed. Logs/receipt remain in
ignored `.unity-results/issue32-ios-export.log`, `issue32-ios-xcode-final.log`,
`issue32-iphone-install-final.json`, `issue32-iphone-flow-final.log` and
`issue32-device-build.json`. Signed app/symbols remain in
`Builds/Issue32Difficulty/Playable/` and `Symbols/`; generated Xcode intermediates
were removed after preservation. Managed launch arguments did not enable the
diagnostic flow, so no fresh automated phone timing result is claimed.

## Local Unity validation

Bootstrap tooling is tracked by GitHub Issue #1. Use a local Codex task opened on
this checkout to run the Unity editor installed on your machine. Running the Codex
desktop app does not move an existing cloud task onto your Mac.

If fetching a branch fails with `could not read Username for 'https://github.com'`,
authenticate Git on the Mac; the cloud task's Git proxy authentication is not
automatically shared with a local checkout. With GitHub CLI installed, run these
commands yourself in Mac Terminal and complete sign-in through the browser:

```bash
gh auth login --hostname github.com --git-protocol https --web
gh auth setup-git --hostname github.com
```

Then local Codex can fetch/check out the issue branch (or use `gh pr checkout 9`
for this bootstrap PR). Check the working tree before switching and preserve local
changes. Alternatively, use GitHub Desktop's signed-in UI to fetch and switch to
the branch before reopening local Codex. Never paste credentials into chat or copy
cloud tokens into the local Git configuration.

Repository JSON validation accepts plain UTF-8 and Unity-generated UTF-8 BOMs
without rewriting package manifests or assembly definitions. Malformed JSON still
fails validation.

Prerequisites:
- Install and activate the pinned Unity 6.3 LTS editor through Unity Hub. The
  scripts read the exact patch from `UnityProject/ProjectSettings/ProjectVersion.txt`.
- Put Python 3.11+ on PATH for `scripts/validate.sh` (which uses `tomllib`) and result
  checking. Unity Hub does not install this prerequisite; Mac system Python may be
  absent or older. Check with `python3 --version`.
- Close the GUI Editor for this project before batch runs. Unity cannot open the
  same project in two editor processes concurrently.

Run from the repository root, or pass an absolute path to a script from elsewhere:

```bash
bash scripts/unity.sh
bash scripts/validate.sh
bash scripts/compile.sh
bash scripts/test.sh
```

Automatic discovery on macOS uses:
`/Applications/Unity/Hub/Editor/<pinned-version>/Unity.app/Contents/MacOS/Unity`.
Linux Hub installs use `$HOME/Unity/Hub/Editor/<pinned-version>/Editor/Unity`.
The scripts never fall back to a different installed patch.

For a custom Hub editor directory:

```bash
UNITY_HUB_EDITOR_ROOT="/Volumes/Development/Unity Editors" bash scripts/test.sh
```

For another installation layout, explicitly select the editor executable:

```bash
UNITY_EDITOR="/Applications/Unity/Hub/Editor/6000.3.25f1/Unity.app/Contents/MacOS/Unity" bash scripts/test.sh
```

An invalid explicit override fails rather than silently falling back. An explicit
override can select a different editor; inspect its version first and keep any
intentional project-version upgrade in separate work.

Compilation imports the project in batch mode and retains `Editor.log`. EditMode
runs omit `-quit` so the Test Framework controls completion. Each run creates a
fresh directory under ignored `.unity-results/`; test runs retain `Editor.log` and
`test-results.xml`. A stale report cannot satisfy a subsequent run. Editor failures
retain their exit status, and missing/malformed/zero-test, failed, skipped, or
inconclusive NUnit results make the test command fail. Compilation alone does not
prove the tests ran.

The bootstrap has eight original EditMode tests. Issue #5 corrects the ordering in
`ExtraCatPreventsVictory`: the extra cat is placed before completing the solution.
The test checks that each mark was applied, that the extra cat prevents victory,
and that removing it immediately wins. Gameplay keeps immediate victory and the
completed-board lock; the correction changes only the fixture.

The runner contracts themselves can be checked without Unity:

```bash
python3 -m unittest discover -s scripts/tests -v
```

These tests use a process fixture to exercise discovery, quoting, working-directory
independence, fresh results, and failure handling. They do not validate Unity import
or replace the C# EditMode suite.

References: [Unity editor arguments](https://docs.unity3d.com/6000.3/Documentation/Manual/EditorCommandLineArguments.html)
and [Test Framework arguments](https://docs.unity3d.com/6000.3/Documentation/Manual/test-framework/reference-command-line.html).

## Issue #3 solver and localization checks

`SolverTests` includes a profile-valid 5x5 target, individual deduction rules,
trace replay, and an independent exhaustive oracle over 1,377 small-board clue
variants. The oracle enumerates placements without using production geometry or
deduction code, and checks each recorded conclusion against surviving completions.
Fixtures cover cardinal traversal through arrow cells, shared/satisfied arrows,
invalid inputs, contradictory clues, deterministic ordering, immutable results,
and work/node budget boundaries.

A separate constructed 7x7 fixture proves that uniqueness does not imply human
completion: the verifier proves one solution while the approved human rule set
stalls, and admission rejects it. That fixture uses an explicit test-only 7x7
profile permitting four cats per line and no zero lines; it does not relax
Easy/Medium/Hard generation policy.

Malformed sizes, clue lengths, coordinates, or directions are `InvalidInput`.
Well-formed but infeasible constraints, including totals exceeding non-arrow
capacity, are `Contradiction`/`NoSolution`. Finding one witness before the search
budget expires is `Incomplete`, never `Unique`.

`LocalizationTests` checks complete key and placeholder coverage for `en`,
`pt-BR`, and `es`, English fallback, device and explicit selection, accented text,
locale formatting, resource immutability, and extension with another locale.
Actual solver traces and verifier results are checked across the three cultures.
Future UI work must separately verify saved preferences, font coverage, and long
translations at representative portrait aspect ratios; there is no board/settings
UI to exercise yet.

## Issue #5 session regression coverage

`GameSessionTests` covers the tap cycle, extra-cat victory regression, mixed
Empty/X non-solution marks at victory, completed-state tap/drag/Clear rejection,
and Clear preserving puzzle identity and protected eligibility. Mixed drags
include Empty, X, Cat, arrow, protected, duplicate, and outside-board coordinates.
All four board edges, null/empty drag, null puzzle, copied protected input,
duplicate/arrow normalization, and invalid protected coordinates/cats are tested.

Collection mutation attempts before and after victory must throw, while the live
read-only marks view must continue to reflect legitimate session transitions.
A generic zero-cat fixture starts completed; this does not alter generated Easy
policy. Gesture axis locking and release-event suppression belong to Issue #7
and are not claimed as tested by these pure session tests.

Native macOS Unity `6000.3.25f1`, 2026-10-07, Issue #5 branch based on `develop`:
repository validation and compilation passed; **59/59 EditMode tests passed**,
including 25 session cases, with zero failures/skips/inconclusive results.
Logs: `.unity-results/compile.m1cUrF/Editor.log` and
`.unity-results/editmode.dhZFAw/test-results.xml`. That historical run excluded the then-unmerged Issue #4 generator tests;
their separate results are recorded below.

After updating PR #13 from `develop` on 2026-10-08, native compilation and the
combined suite passed: **81/81**, zero failed/skipped/inconclusive. Logs:
`.unity-results/compile.iB1O8C/Editor.log` and
`.unity-results/editmode.s34z2D/test-results.xml`. The merge conflict was limited
to this documentation; both session and generator test records were retained.

## Issue #4 generation checks and cost report

`GenerationTests` locks SplitMix64 golden vectors, signed seed mapping, bounded
rejection sampling, independently calculated SHA-256 fingerprints, and accepted
board/solution fingerprints for seeds `0`, `-1`, `int.MinValue`, and
`int.MaxValue`. It checks reproduction from recorded options across cultures,
instances, interleaved requests, and concurrent requests on one generator.
Separate positive and zero work limits exercise layout, random, human, and
verification exhaustion. Pre-cancellation returns no board and consumes no work.
Returned puzzle and diagnostics collections reject mutation.

The fixed `0..999` Easy corpus independently checks cat positions, row/column
counts, full-ray arrow truth, clue ceilings, and complete admission for every
success. Timing encloses only `Generate`, after an unmeasured warm-up; independent
post-generation checks are excluded. Timing is informational, never a correctness
assertion. The known corpus is a regression set, not a guarantee that every
possible seed succeeds.

Native macOS Unity `6000.3.25f1`, 2026-10-07: repository validation and compilation
passed; all **61 EditMode tests passed**, with zero failed/skipped/inconclusive.
Final corpus results:

| Measurement | Median | 95th percentile | Maximum |
| --- | ---: | ---: | ---: |
| Attempts | 1 | 1 | 2 |
| Layout nodes | 6 | 6 | 12 |
| Human constraint evaluations | 1,634 | 3,309 | 5,770 |
| Verifier nodes | 1 | 1 | 7 |
| Random draws | 156 | 268 | 503 |
| Generation time (ms) | 4.306 | 7.973 | 13.835 |

All 1,000 requests succeeded: 1,000 distinct logical boards and 971 distinct cat
layouts, excluding seed metadata. One layout reached the arrow ceiling and its
next attempt succeeded. Boards had 4–10 cats (mean 6.962) and 2–10 arrows (mean
5.053). The earlier warmed run had median 4.343 ms, p95 8.121 ms, and maximum
17.531 ms; these are observed editor timings, not mobile or cold-start guarantees.

The runtime allocation counter did not advance on a known allocation probe, so
allocation bytes are **unavailable**, not zero. Device profiling remains required
for allocations, New Game latency, frame pacing, and eventual background queue
integration. Measure on representative iOS/Android hardware before deciding on
loading animation duration or background scheduling policy.

## Pyramid

1. Pure domain/EditMode tests: highest volume.
2. Unity integration/PlayMode tests: board/session/UI wiring.
3. Device smoke tests: iOS + Android touch, safe-area, performance, lifecycle.

## Mandatory puzzle tests
- arrow cell can never be solution cat;
- row/column totals equal solution;
- maximum cat counts by profile;
- zero-row/zero-column cap;
- arrow truth for every generated arrow;
- generated puzzle unique;
- human solver completes without guessing/hints;
- same seed + generator version => same logical puzzle;
- invalid candidates rejected.

## Mandatory session tests
- tap: Empty -> X -> Cat -> Empty;
- drag affects Empty only;
- drag never modifies arrow/protected/completed cells;
- drag processes each cell once;
- exact solution triggers win immediately;
- extra player cat prevents win;
- Empty and X are both acceptable outside solution;
- Clear resets player marks only;
- New Game replaces puzzle but preserves selected difficulty;
- completed board is immutable.

## Device matrix for MVP
- recent iPhone simulator/device with notch/Dynamic Island safe area;
- older/smaller iPhone viewport;
- mainstream Android phone;
- at least one tall/narrow Android aspect ratio.

## Issue #14 diagonal regression checks

The v2 suite uses independent signed-displacement ray predicates for all eight
directions and exhaustive small-board placements, rather than production ray
traversal. Focused cases cover exact diagonal geometry, outward edge rays, cats
in the quadrant but off the ray, traversal through arrows, shared/multiple cats,
unsatisfied singletons, satisfied arrows and cardinal-only line exclusion. All
four diagonal directions have explanations and help in en/pt-BR/es.

The generator corpus retains seeds 0..999 and the same numeric work bounds.
Generator board fingerprints are intentionally refreshed for v2; PRNG and extreme
seed solution fingerprints remain unchanged. Native results follow; phone timing is recorded in `GENERATION_BENCHMARK.md`; historical v1 measurements above remain
associated with their original revisions.

Native macOS Unity 6000.3.25f1, 2026-10-08: final compilation passed
(`compile.gPQJd5`); **84/84 EditMode tests passed**, zero failed/skipped/inconclusive
(`editmode.V14TG4/test-results.xml`). Repository validation also passed.

All 1,000 v2 corpus requests succeeded on their first attempt: 1,000 distinct
logical boards, 971 solution layouts, and 917 boards containing diagonals,
covering all four orientations. Human evaluations median/p95/max were
1,343/2,907/5,113; verifier nodes 1/1/11; random draws 214/374/445. Arrow count
median/p95/max was 4/7/10 (mean 4.34), compared with the historical v1 mean 5.053.
Numeric work bounds were not increased. Editor generation timing was
3.811/7.391/11.993 ms median/p95/max in this run, with no allocation counter;
concurrent Xcode work makes these informational editor figures unsuitable for a
controlled performance comparison. Physical-player measurements are the timing
gate. This fixed corpus does not prove all possible seeds succeed.

The first measurement intentionally exposed four changed board fingerprint
expectations. One new multiple-cat fixture was uniquely solvable but human-stalled;
it was replaced with a line-forced multiple-cat fixture so the test measures
at-least-one semantics without asserting unsupported reasoning. The final gate
above includes these corrections and localized diagonal/validator cases.

## Combined session and diagonal integration gate

Before merging PR #15 into `develop` on 2026-10-08, the branch incorporated
PR #13 session changes. Native Unity 6000.3.25f1 compilation passed and the
combined **104/104 EditMode tests passed**, with zero failed/skipped/inconclusive.
Repository validation and whitespace checks passed. Logs:
`.unity-results/compile.mgYXTV/Editor.log` and
`.unity-results/editmode.bpkWOt/test-results.xml`. No conflict or additional
game behavior change was introduced by this integration.


## Issue #6 local portrait presentation gate

Native macOS Unity 6000.3.25f1 compilation passed (`compile.ICdHwH`). The final
**144/144 EditMode tests passed**, zero failed/skipped/inconclusive
(`editmode.FnW9mJ/test-results.xml`). This includes visible snapshot isolation,
eight arrow directions, protected/completed states, safe-area/clue geometry,
pre-panel startup, localized header transitions and stale-board failure clearing.
Repository validation and whitespace checks passed.

A local macOS player rendered six portrait/symbol captures across English,
Brazilian Portuguese and Spanish, including 320×568 compact layouts. The final
visual review found no blocking layout issue. See `BOARD_PRESENTATION_REVIEW.md`
for evidence and reproduction. Simulated safe insets and desktop rendering do
not establish physical-phone touch usability or owner color-vision comfort.
No input adapter or controls are delivered in this display-only issue.


## Issue #7 local input gate

Native Unity 6000.3.25f1 compiled the gesture adapter (`compile.6tFSn5`). Final
**184/184 EditMode tests passed**, zero failed/skipped/inconclusive
(`editmode.ZMRYTJ/test-results.xml`). Tests cover tap/hold/release, duplicate and
secondary pointers, axis locking, sparse movement, mixed/protected/completed
states, cancellation, exact boundaries/gaps and representative portrait layouts.

The local macOS player passed mounted synthetic mouse and touch dispatch checks,
including pointer capture surviving two Refresh calls (`issue7-mounted-input.log`).
The owner played the mouse board to completion and reported correct behavior.
A subsequent completed-board mouse check left state unchanged. Xcode Release
build/signing passed and the app was installed/launched on the iPhone 17;
the owner confirmed requested physical iPhone checks felt correct (tap cycle,
horizontal/vertical drag, release, multiple fingers, background/reopen). Android
device verification remains pending. See `TOUCH_INPUT_REVIEW.md` for steps and limitations.


## Issue #8 local complete-loop gate

Native Unity 6000.3.25f1 compilation passed (`compile.DLTpZy`). **210/210 EditMode
tests passed**, zero failed/skipped/inconclusive (`editmode.PG7Ojs/test-results.xml`).
Coverage includes clue-only one-fact hints/corrections, Clear/rescan, narrow
protection, seeded progression/failure, a 20-seed hints-to-win corpus, resources,
preferences, live-menu resume, modal command guards and completed action locks.

Mounted shell flows passed in all three initial locales. A final mounted pass
verifies panel picking before synthetic button events (`issue8-panel-picking.log`).
UX reviewed 18 Menu/Rules/Play/Hint/Victory/Completed captures without blockers.
QA found no local code/test/render blockers. iOS Release build/sign/install
passed. Owner iPhone playtest passed the full flow, three languages, saved locale,
completed-board locks and background/reopen. Native iPhone 17 Release diagnostic
passed 20 New Game requests: median 65.994 ms, p95 66.966 ms, max 72.543 ms;
first start separately 59.497 ms. Timing includes queued feedback and rendered
readiness after synthetic button action, excluding touch hardware latency.
See `GAME_LOOP_REVIEW.md` for measurements, reproduction and limitations.


## Issue #19 calm portrait board gate

Native Unity compile passed (`compile.PllPPK`) and **219/219 EditMode tests
passed**, zero failed/skipped/inconclusive (`editmode.ZwmUpM`). Three native
macOS flows pass centering, inline completion from final hints and mounted board
pointer taps, action locks and New Game reset. QA/UX reviewed 21 locale/portrait
captures without blockers. Exported iOS Info.plist permits upright portrait only.
Owner iPhone review on 2026-10-09 confirmed centered grid, darker cells, working
orientation lock and improved completion. See `BOARD_POLISH_REVIEW.md`.

## Issue #23 immediate hints

InlineHintTests cover immediate one-fact application, all three locales,
correction of conflicting marks, consecutive hints, reset/menu/locale behavior
and completed-board controls without explanation UI. Existing GameShellTests
exercise the legacy presentation for rollback. Native GameLoopReviewChecks
exercise both configurations with mounted pointer events and fixed-grid checks.
See INLINE_HINT_REVIEW.md for fresh results and owner review status.

## Issue #26 tutorial checks

Deterministic tests cover exact fixture quotas, true arrows/protection, current
human-solver completion and independent exhaustive uniqueness; complete lesson
replay; tap-cycle sequence; off-target input; zero-line overlap; two-cat lessons;
no early victory; final completion lock; fresh replay; first-play/Skip/Menu/locale
routing and ordinary-game regression. Native mounted checks must exercise actual
pointer events through tutorial gates, including swipes across fixed cells and
existing cats, gesture cancellation/second fingers, Skip and replay. Render the
three locales at compact/standard/tall portrait sizes and verify text fit,
stationary grid and non-color highlights. Owner phone walkthrough remains the
acceptance gate for touch comfort.

Owner refinements add separate sample-state rendering/mark-isolation tests,
protected-only and arrow-only highlights, welcome Continue emphasis, a clean
same-fixture session swap, and independent play with wrong-cat recovery before
completion. Native checks must prove the first independent pointer tap works,
not merely test the replacement session directly.
