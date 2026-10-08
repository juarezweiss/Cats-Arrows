# Issue #6 — Portrait board review

Status: owner approved the display-only implementation and local commit.
Branch: `feat/6-native-board`. Push and merge are separate next steps.

## What this adds

A native UI Toolkit board binds an existing `GameSession` and displays its visible
clues/player marks. Row and column totals share the exact tile pitch. Arrows have
eight vector orientations; Empty, X, Cat, Arrow and Protected use distinct shapes.
The board fits the reported safe area, with width-based panel scaling and a
compact cell-size cap. Localized text covers English, Brazilian Portuguese and
Spanish and extends the existing resource catalog.

The review scene generates a real admitted Easy board with seed 14. It does not
show hidden solution cats. The symbol gallery is a separate presentation fixture,
not manually authored gameplay content. There is no tap/drag mutation or action
control yet; these belong to Issues #7/#8. Protected cells can be supplied by a
session, but automatic protection is not derived in this issue.

## Review locally

Open `UnityProject/Assets/CatsAndArrows/Scenes/BoardReview.unity` in the pinned Unity
editor and enter Play. The scene is explicit; build settings and unrelated scenes
are not changed. The generated runtime theme is under `Assets/UI Toolkit`.

For a screenshot-capable local macOS review player, close the GUI editor first:

```bash
CATS_ARROWS_BOARD_REVIEW_OUTPUT="$PWD/Builds/Issue6Board/BoardReview.app" \
"$(bash scripts/unity.sh)" -batchmode -nographics \
  -projectPath "$PWD/UnityProject" \
  -executeMethod CatsAndArrows.UI.Editor.BoardReviewBuild.Build \
  -logFile "$PWD/.unity-results/issue6-review-build.log" -quit

Builds/Issue6Board/BoardReview.app/Contents/MacOS/UnityProject \
  --board-width=390 --board-height=844 --board-locale=pt-BR \
  --board-safe-top=50 --board-safe-bottom=34 \
  --board-capture="$PWD/.unity-results/issue6-captures/board-pt-BR-390x844.png" \
  -logFile "$PWD/.unity-results/issue6-board-pt-BR.log"
```

The two optional safe-inset arguments simulate screenshot pixel insets only;
normal device presentation uses `Screen.safeArea`. `--board-gallery=1` switches
to the isolated symbol gallery. Capture mode exits after writing a real rendered
PNG. Builds, logs and screenshots stay in ignored local directories.

## Validation

Pinned native Unity 6000.3.25f1 compilation passed (`compile.ICdHwH`). The finalized
suite passed **144/144 EditMode tests**, zero failed/skipped/inconclusive
(`editmode.FnW9mJ/test-results.xml`). Repository validation and whitespace checks
pass. Tests cover detached snapshots/no solution leakage, all eight arrows,
protected/completed presentation, safe-area conversion and clue alignment,
representative portrait geometry, future 7x7/10x10 geometry and localization.

The first graphical launch exposed unresolved/NaN dimensions during initial Bind.
The view now waits for resolved geometry, with regression tests for pre-panel
Bind/Refresh in all three locales and clearing a stale board on generation failure.

The final native macOS review build passed (`issue6-review-build-complete.log`).
Six real Unity player PNG captures exited successfully, with mounted panel and
finite board geometry: English 320×568 and 390×844, Brazilian Portuguese 320×568
and 390×844, Spanish 430×932, and a 390×844 symbol gallery covering all eight arrows.
Captures are in `.unity-results/issue6-captures/`; per-capture logs use
`issue6-final-*.log`. Safe insets were simulated as documented above.

Rendered review exposed a missing panel asset reference after scene replacement;
the build helper now reloads it after creating the scene and verifies the saved
reference. The small portrait header overlap was removed by keeping three header
rows; completion replaces the introduction instead of adding another row. A
regression verifies completion, rebind and failure transitions. The final UX
review found no blocking visual issue in any of the six regenerated captures.
QA approved the display-only implementation for owner review with no remaining blockers.
Borders use #7E8C83, with approximately 3.14:1 contrast against the outer background;
main ink against tiles is approximately 11.94:1. Shape distinctions remain the
primary non-color state indicator.

Actual phone UI/touch and owner color-vision comfort remain follow-up checks;
geometry tests and desktop renders do not establish them. Localized tooltips are
context metadata, not a claim of native VoiceOver support. Future 7x7/10x10 layout
support does not mean those difficulties or comfortable small-cell touch are shipped.

## Risk and rollback

Changes are presentation-only; no domain/gameplay behavior changes. Built-in
Unity UI and screenshot modules are enabled from the existing pinned editor;
there are no paid assets or new third-party services. Review scene, panel/theme,
UI assembly and tests are isolated on the issue branch. Removing those additions and reverting
the module/test-assembly references restores the previous presentation-free state.
