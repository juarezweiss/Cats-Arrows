# Issue #7 — Touch interaction review

Status: owner approved the tested implementation, commit and push on
`feat/7-touch-input`. Merge requires separate instruction. Android verification
remains outstanding.

## Behavior

A single UI Toolkit Pointer event path handles mouse and touch. Press does not
change a cell; release in the same tile cycles Empty → X → Cat → Empty once.
Holding does not repeat. An 8-panel-unit Euclidean displacement starts a drag
from Empty, locking to the dominant axis (horizontal on ties). Existing X/Cat
origins can be tapped, but movement beyond the threshold cancels without marking.

A drag marks the origin and every crossed Empty tile on its starting row/column,
including tiles between sparse movement samples. Each coordinate is processed
once; reversal cannot repeat it. Arrows, protected cells and existing marks are
skipped. Release adds no tap. Clues/gaps/outside cannot start a gesture. Leaving
the playable grid cancels, retaining marks already applied. A second pointer
cannot take ownership. Completion immediately locks input.

The stable grid owns pointer capture while child tile visuals refresh. Focus
loss/pause, capture loss, detach, layout changes, rebind and generation failure
cancel pending gestures. Later Clear controls must cancel before clearing.

## Native checks

Unity 6000.3.25f1 compilation passed (`compile.6tFSn5`). After fixing exact tile
boundary hit testing, **184/184 EditMode tests passed**, zero failed/skipped/
inconclusive (`editmode.ZMRYTJ/test-results.xml`). The first run caught fractional
pitch rounding at exact tile minima; hit testing now uses the same rendered
rectangles. An incorrect vertical fixture expected an arrow to become X; it now
uses an eligible column while mixed-state tests retain arrow-skip coverage.

Repository validation passes. The macOS review build passed (`issue7-macos-build.log`).
Mounted-panel synthetic mouse and touch checks passed with exit 0
(`issue7-mounted-input.log`), including capture across multiple refreshes.
The owner played the local mouse board to completion and reported that tap/drag
felt correct. A subsequent real mouse click/drag check left the completed board
unchanged. iOS export passed (`issue7-ios-export.log`). The corrected Xcode Release
build/signing passed (`issue7-ios-xcode-final.log`) and the review app was installed
and launched on the paired iPhone 17. Initial install attempts exposed a transient
connection reset and a global bundle-ID override; the latter was corrected in the
ignored Xcode export only. The owner confirmed all requested physical touch checks felt correct: tap cycling,
horizontal/vertical drag, release suppression, second-finger handling and
background/reopen. This is a qualitative owner test, not automated device replay.
The signed app is retained at `Builds/Issue7Board/UnityProject.app`; the logs remain available. The reproducible Xcode export was pruned while
preparing Issue #8 to maintain build headroom; tracked source can regenerate it. Disposable DerivedData was removed after installation.
Android device verification remains pending. QA found no
blocking code findings after the final boundary/capture checks.
The opt-in `--board-input-check=1` review player dispatches synthetic mouse and
Touch pointer events through a mounted panel, checks actual pointer capture
through multiple Refresh calls, tap cycles, mixed drag, release and cancellation.
Synthetic dispatch is not actual OS touch delivery or phone comfort evidence.

## Local review

Open `Assets/CatsAndArrows/Scenes/BoardReview.unity` in the pinned editor and Play.
It generates admitted Easy seed 14 and supports mouse input. The review builder
also accepts `CATS_ARROWS_BOARD_REVIEW_TARGET=iOS` or `Android`; default is macOS.
Build/export/signing outputs remain ignored, and signing belongs only in the
local Xcode export. No production/store publication is part of this issue.

## Physical acceptance checklist

- Three taps on a normal tile show X, Cat, Empty, one change each.
- Rapid taps and press/hold/release each produce the expected single cycle step.
- Horizontal and vertical drags mark only Empty cells; reverse or turn diagonally
  and verify the original row/column remains locked.
- Cross arrows and existing X/Cat without changing them; release leaves Xs.
- Move off the grid and re-enter: no additional marking until another press.
- Start on a clue/arrow/gap; it does nothing. Add a second finger during a gesture;
  it cannot take over or cause an extra release transition.
- Background/foreground cancels pending input. Solve the board and confirm all
  cells become read-only. Clear/New Game controls belong to Issue #8.

iOS owner verification passed on the iPhone 17. Android device verification is
still outstanding, so the issue's cross-platform acceptance item is not fully
satisfied. Unit tests and synthetic dispatch do not replace that remaining check.

## Risk and rollback

GameSession/domain logic and hidden solution handling are unchanged. Gesture
threshold/edge behavior may need tuning from phone feedback. Revert only Issue #7
UI/controller/test changes to return to the integrated display-only board. No
purchases, external SDKs, persistent board state, controls or new game rules.
