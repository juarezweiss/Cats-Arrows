# UX and Accessibility

## Reference behavior
The supplied demo establishes a useful portrait interaction model: difficulty menu, compact centered board, numeric clues outside the grid, arrows inside cells, bottom action controls, and a simple completion modal. Treat it as behavioral context, not final visual art.

## Mobile-first requirements
- Portrait is the primary orientation.
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
- Victory feedback should be warm but brief.
- Hint should explain a deduction where feasible.

## Controls
In-game primary controls: `Hint`, `Clear`, `Menu`, `New Game`.

After completion: `Hint` and `Clear` disabled; board read-only; `New Game` and `Menu` enabled; completion modal includes `Back to the Game`.


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
