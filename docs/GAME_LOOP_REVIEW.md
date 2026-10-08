# Issue #8 — Complete Easy game loop review

Status: review-ready on `feat/8-game-loop`; owner approved commit and push.
Merge remains pending explicit owner approval.

## Delivered behavior

Menu offers Easy start, scrollable rules, English/Brazilian Portuguese/Spanish
language selection and Back to the Game when a live session exists. Locale alone
persists; the current board stays in memory through Menu/background transitions.
Normal boot picks a fresh initial seed; `--game-seed` pins diagnostic reproduction.
New Game preserves Easy and requests a new seed. Failed admission drops the board
and presents Retry/Menu rather than exposing an unverified puzzle.

The play view uses a compact header and 2×2 Hint/Clear/Menu/New Game controls.
Clear cancels pending input and preserves clues/protection. Hint is unlimited and
applies one guaranteed clue-only conclusion, correcting conflicting player marks
with an explanation and target coordinate. Initial protection is cardinal-only,
original-line-total-one, verified by the solver. No diagonal/two-cat broad
protection or hidden-answer reveal is introduced.

Victory appears immediately after exact-cat completion, including from Hint.
Back to the Game exposes the read-only completed board. Hint/Clear are disabled;
Menu/New Game stay active. A final Hint merges its explanation into one victory
dialog. Rules/hint bodies scroll and actions remain reachable; overlays suspend
board input and cancel capture. Existing deterministic tap/drag remains intact.

## Validation evidence

Native Unity 6000.3.25f1 compilation passed (`compile.DLTpZy`). Final domain/UI
suite: **210/210 EditMode tests passed**, zero failed/skipped/inconclusive
(`editmode.PG7Ojs/test-results.xml`). Tests include one-fact proof-only hints,
conflicting marks, Clear/rescan, protection cardinal/diagonal/two-total cases,
20-seed hints-to-win, seed progression/wrap/failure, three resources/preferences,
menu/resume, modal guards, retry and immediate victory/action locks.

macOS builds passed (`issue8-macos-build.log`, `issue8-macos-picking-build.log`).
Mounted synthetic button flows passed in en/pt-BR/es; final checks also verify
panel picking resolves each visible button (`issue8-panel-picking.log`). Real
rendered PNGs cover Menu, Rules, Play, Hint, Victory and Completed in each locale:
`.unity-results/issue8-captures/en-320x568`, `pt-BR-390x844`, `es-430x932`.
Simulated safe top/bottom insets are respectively 50/34, 50/34 and 59/34 pixels.
UX reviewed all 18 captures and found no overlap, clipping or missing actions.
Main buttons have a 48-panel-unit minimum height; compact actual grid remains
legible. These are desktop renders, not screen-reader or physical comfort claims.

The final compact mounted run measured 20 New Game button requests at median
66.309 ms, p95 66.833 ms, max 66.978 ms; first start 126.552 ms. Timing starts at
the button action, includes its queued loading feedback, generation/session/view
binding, and waits through an end-of-frame after readiness. These are macOS
synthetic-button measurements, not phone results or touchscreen hardware latency.
Capture IO finishes before these ordinary samples.

Fresh iPhone 17 / iOS 27.0.1 Release/IL2CPP diagnostic passed with exit 0:
first start **59.497 ms**; **20 New Game requests**, median **65.994 ms**,
p95 **66.966 ms**, max **72.543 ms**. The measured p95 meets the approved 250 ms
budget. This is the same button-action-through-render boundary using synthetic
button events on the physical phone; it excludes finger/hardware input latency.
Reports: `.unity-results/issue8-iphone-report.txt` and
`issue8-iphone-diagnostic-flow.log`. One device / 20 deterministic requests are a
small MVP gate, not a guarantee for every future device or difficulty.

The iOS export and Xcode Release build/signing passed (`issue8-ios-export.log`,
`issue8-ios-xcode.log`), and the review app installed on the paired iPhone 17 as
`com.juarezweiss.catsarrows.inputreview`. After unlocking, the owner confirmed the actual touch controls and full flow:
Hint/Continue/Clear, Menu/resume, hints-to-victory, completed-board locks, New Game,
all three languages, saved locale after restart and background/reopen. The separate native device timing gate also passed.
Desktop CUA clicks did not establish actual button event delivery; don't infer
manual-pointer success from synthetic callback/picking tests alone.

## Reproduce locally

Open `Assets/CatsAndArrows/Scenes/BoardReview.unity` in the pinned editor and Play.
Normal launch now opens the complete game shell. Existing `--board-*` capture and
input fixtures still explicitly select the earlier board/gallery diagnostic.
Use `--game-loop-check=1 --game-seed=14 --board-locale=en` to run mounted flow and
20-request timing; add an absolute `--game-captures=...` directory for PNGs.
That mode logs results and writes `issue8-game-loop.txt` in persistentDataPath.
It is opt-in diagnostics; normal gameplay doesn't run automatic actions/reports.
On iOS, launch arguments did not activate the diagnostic in this run. Build with
`CATS_ARROWS_GAME_LOOP_AUTORUN=1` to add `CATS_ARROWS_GAME_LOOP_CHECK` only to that
review build; use a separate diagnostic bundle identifier. PlayerSettings defines
are not changed. Review targets remain macOS/iOS/Android via the existing build helper. Android
physical verification remains pending; first delivery is iOS.

## Owner playtest

Start Easy; tap/drag; use Hint and read its explanation; dismiss it and Clear.
Open Menu and return, preserving marks; switch all three languages and read Rules.
Complete a board (Hint may help), inspect it using Back to the Game, confirm
Hint/Clear are disabled, then start New Game. Check that modal screens block board
input and background/reopen retains the live board. Restart to check the saved
language; unfinished boards need not survive termination in this MVP.

## Risk and rollback

No new vendor SDK, purchases, saved-board system or production publication.
Synchronous bounded Easy generation is used; compare phone timing to the approved
250 ms p95 target as future devices/difficulties are added; the current Easy
iPhone run passed without scheduling/queues. Hint correctness relies on the
existing solver contract; no player hypothesis is trusted as proof. Revert Issue
#8 to restore the integrated interactive board. Source, logs and signed review
artifacts stay separate from ignored reproducible build intermediates.

## Local artifacts and storage

Signed playable and diagnostic apps are preserved under
`Builds/Issue8Board/Playable/UnityProject.app` and
`Builds/Issue8Board/Diagnostic/UnityProject.app`. Reproducible Issue #8 Xcode
DerivedData was removed after saving both apps and reports to reclaim storage.
