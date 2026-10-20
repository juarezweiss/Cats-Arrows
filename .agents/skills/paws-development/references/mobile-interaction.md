# Mobile Interaction Workflow

## Tap
One completed press/tap produces at most one state transition. Never wire independent touch and mouse callbacks that both mutate the same cell on mobile.

## Drag marking
- start from an eligible cell;
- establish horizontal/vertical axis after a small movement threshold;
- process each traversed coordinate once;
- convert only Empty -> X;
- ignore existing X, Cat, arrow, protected, and completed-board cells;
- releasing ends the gesture without an additional tap transition.

## Validation
Test mouse in editor plus real/simulated touch. Specifically reproduce rapid taps, tap-hold-release, drag across mixed cell states, direction changes, and screen-edge/safe-area cases.
