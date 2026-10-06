# Testing Strategy

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
