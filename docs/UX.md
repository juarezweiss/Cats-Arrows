# UX and Accessibility

## Reference behavior
The supplied demo establishes a useful portrait interaction model: compact centered board, numeric clues outside the grid, arrows inside cells, bottom action controls, and visible completed-board review. Current menu behavior follows Issue #43 below. Treat the demo as behavioral context, not final visual art.

## Mobile-first requirements
- Mobile play is locked to upright portrait.
- No double-tap zoom or browser-style gesture behavior applies in native Unity.
- Touch targets should remain comfortably tappable on narrow phones.
- Safe areas must be respected on iPhone and Android cutout devices.
- A normal tap must never be interpreted twice.
- Drag marking must feel deliberate and axis-locked.

## Visual semantics
Do not rely on red/green distinction alone. Cat, arrow, X, protected, and empty states require shape/icon/texture differences in addition to color.

The user specifically found low-contrast red/green states difficult in the prototype. Default palette decisions must be checked with color-vision simulation/contrast tooling before approval.

## Feedback
- No punishment animation for a wrong intermediate mark.
- Use subtle haptic/audio feedback for deliberate state changes if enabled.
- Victory feedback stays inline beneath the difficulty label, with a subtle grey grid treatment and readable symbols.
- Hint should explain a deduction where feasible.

## Controls

Issue #43 replaces the menu difficulty controls with a localized Play action,
alongside Tutorial, How to Play and languages. Play and Next board select
Easy/Medium/Hard through one shared policy, with no more than two consecutive
successful boards of one mode, even across restarts. Failure/retry retains its
pending mode. Clear, Menu/back and language changes keep the live board and its
actual difficulty label; they do not consume history.

First-ever Play opens the fixed Easy tutorial; later Plays request ordinary
random boards. This gate persists across restarts independently of the difficulty
streak. Manual Tutorial stays available and does not consume first Play on fresh
installations. Legacy tutorial-seen installations migrate as returning users;
lesson steps and unfinished boards still are not saved. Tutorial Skip/Play enters
the ordinary selector without counting the tutorial fixture or resetting the streak.

Historical #32 evidence (the difficulty-choice menu is superseded): [localized menu](evidence/issue32/difficulty-menu-pt390.png),
[Medium compact portrait](evidence/issue32/medium-en320.png),
[Hard compact portrait](evidence/issue32/hard-en320.png). Mounted pointer checks
pass; physical comfort on compact screens remains a device-review gate.
In-game primary controls: `Hint`, `Clear`, `Menu`, `Next board`.

After completion: `Hint` and `Clear` disabled; board read-only; `Next board` and `Menu` enabled; completion stays inline so the finished board remains visible. Issue #44's celebration is not part of this change.


## Issue #7 initial gesture contract

Taps commit once on release in the original cell, with movement below 8 logical
panel units. Holding does not repeat. Only an Empty cell can start drag marking;
moving from X/Cat beyond the threshold cancels the tap without changing marks.
The dominant direction locks the drag to the starting row/column. Reversing does
not revisit cells; moving diagonally does not switch axes. Internal gaps are
bridged while dragging, but clues/gaps cannot start a gesture. Leaving the grid
ends the gesture without another tap. Cancellation retains already applied Xs.
A second finger cannot take over or finish the first finger's gesture.

This threshold is an initial value for phone testing, not a physical comfort
claim. Rebind, focus loss and layout changes cancel pending interactions.

## Issue #47 arrow-path hold

On an active ordinary board, holding an arrow for 350 ms outlines its entire
forward cardinal or diagonal ray until release. This reuses the tutorial's dark
cell outlines, preserving symbol contrast and touch geometry without relying on
color. Arrows remain non-markable. A short press does nothing; movement beyond
the existing small jitter tolerance or off the origin cancels the hold without
starting a drag. A second pointer cannot take ownership.

Release, capture/cancel, focus loss/background, navigation, suspension, geometry
changes and rebind remove pending or active hold feedback. The aid does not run
in the fixed tutorial or on completed boards, and does not alter their existing
highlights. How to Play describes the gesture in English, pt-BR and Spanish.

## Issue #48 idle Hint attention

During active ordinary play, six seconds without board interaction gives Hint a
stable inversion of its normal text and background colors, fading in over 200 ms
using native UI Toolkit color transitions. Resets restore normal colors immediately
without a fade-out. It never flashes, moves the grid, or applies a hint automatically.
The existing localized label, font weight, border and touch target remain unchanged.

Any board press, including an arrow or protected-cell attempt, clears attention.
While a pointer is held or dragging, attention stays off; release or cancellation
starts a fresh six-second interval. Hint (even with no applicable deduction),
Clear and a new board also reset the interval. Hover does not count as activity.

Menu, either tutorial stage, completed/missing/loading/failed boards, blocking
overlays, disabled Hint, detached views and background/unfocused play suppress
attention and discard elapsed idle time. Returning to eligible play or rebuilding
localized controls starts a fresh interval.

## Issue #19 owner visual feedback

Center the tile grid itself within the safe board region; the numeric clue gutters
follow the centered grid and remain within bounds. Arrow and protected/hatched
cells share a subtly darker background while retaining distinct shapes/hatching.
Completed boards remain visible with the localized “All cats found!” phrase below
the difficulty, without an automatic overlay. A final Hint can be reviewed via an
optional “Last hint” action; opening its explanation is deliberate. New Game
clears the completion treatment. Upright portrait is mandatory on mobile.

## Issue #29 future monetization controls

Current free-MVP controls remain unchanged. Future enabled monetization #29 uses
`Hint (ads)` for unowned players and `Hint` for verified owners. Main menu includes
`Remove All Ads`, store-localized price, both-removals explanation and a distinct
`Restore Purchases` action. Pending/unavailable/owned status is textual and localized
in en/pt-BR/es. Reward loading suspends gestures; cancellation/errors restore
controls only after native presentation ends. No rewarded ad without an eligible
hint; no automatic mutation of a different board after a stale callback. Confirmed
unused rewards have a recoverable next-hint status.

Progression ads follow deliberate next-board intent after every fourth ordinary
completion, preserving immediate inline win and solved-board viewing. Tutorials
are excluded. Unavailable progression ads never block play; unpaid rewarded hints
may be unavailable offline, while verified paid hints remain direct. Current
ad-free trial remains unchanged. Full failure/cadence policy:
[MONETIZATION.md](MONETIZATION.md).

## Issue #23 immediate hints

The owner approved immediate hints as the ongoing default on 2026-10-10 after
rejecting the optional Why explanation in device review. Hint applies
one guaranteed Cat/X conclusion immediately, briefly outlines the target and
keeps the board playable. There is no Why button, explanation area or final-hint
review action in the default flow. Hint/Clear share the first control row;
Menu/New Game remain below. Completion keeps its calm inline status and readonly
grid. The previous modal/Last hint presentation remains a rollback configuration.

## Issue #26 guided first play

Short localized instructions use the existing top completion-message area without
moving the centered grid. Outlines identify relevant totals, arrows, paths and
current input targets without depending on color alone. Informational stages
use Continue/Start; guided stages provide Skip and Menu instead of normal
Hint/Clear/New Game controls. The three editable states are demonstrated separately, with their names bold.

Swipes start at an eligible Empty cell, skip fixed/existing marks, and advance
the lesson after the gesture ends, never midway through it. A tap-based marking
path can satisfy the same state predicates. The practice stage requires the
actual Empty→X→Cat→Empty sequence. After the guided solve, the same board resets for independent play with a
“Now try it yourself” message. Only independent completion stays visible and
readonly with Play opening a random board. Menu/locale changes retain the active
lesson; selecting Tutorial explicitly restarts it. Starting Tutorial replaces
the prior in-memory game; Skip/Play do not restore it.

First-play history is stored locally after successful presentation, including
manual entry, so interrupted or skipped tutorials do not autoappear again.
Existing installations without that preference see the tutorial on next Start.
Backgrounding retains the in-memory lesson while cancelling gestures; force-quit
does not save tutorial progress. Reinstallation/resetting app data may reset
first-play history. English, Brazilian Portuguese and Spanish are required.

The welcome explicitly directs the player to Continue and outlines that button.
A short editable-cell introduction precedes individual Empty, X and Cat examples
with one highlighted example at a time. Hatching highlights only protected cells; the following
arrow introduction highlights only arrows. Sample marks never enter the session.
