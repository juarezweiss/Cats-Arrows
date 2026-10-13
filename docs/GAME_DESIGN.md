# Game Design Specification

## Board
A rectangular square grid. Current target sizes:
- Easy: 5x5, maximum 2 solution cats per row/column.
- Medium: 7x7, maximum 3 solution cats per row/column.
- Hard: 10x10, maximum 4 solution cats per row/column.

Difficulty values are initial design targets, not a substitute for solver-derived difficulty scoring.

## Solution cats
- A normal cell contains at most one solution cat.
- Arrow cells can never contain solution cats.
- The player does not see solution cats directly.

## Row and column totals
Every row and column displays the exact number of solution cats in that line. Column totals are centered over their columns; row totals are centered beside their rows.

Generation should allow at most one zero-cat row and at most one zero-cat column, unless a later difficulty design changes this rule.

## Arrow clues
An arrow occupies a grid cell and is non-interactive. The initial MVP supports eight directions: Up, Down, Left, Right, UpLeft, UpRight, DownLeft and DownRight (Issue #14).

Meaning: **at least one solution cat exists in the direction the arrow points.** For a horizontal arrow, consider cells in the same row beyond the arrow. For a vertical arrow, consider cells in the same column beyond the arrow. For a diagonal arrow, advance one row and one column per step along its exact 45-degree ray. All rays continue through other arrow cells to the edge without wrapping; those arrow cells are ineligible for cats. The origin never counts. Multiple arrows may share a cat; a ray may contain more than one cat. Diagonals introduce no diagonal count clues.

Example: a row has total 2 and contains a Right arrow. At least one of the two solution cats is somewhere to the right of the arrow. The second cat may be on either side if all other constraints permit it.

## Player cell states
Interactive normal cells cycle on a discrete tap:
`Empty -> X -> Cat -> Empty`

X means "I believe no cat is here." Cat means "I believe a cat is here." The game does not punish a wrong intermediate hypothesis.

## Drag-to-mark
A press-and-drag gesture may travel horizontally or vertically from its origin. It changes only eligible cells that are currently Empty, marking them X. Existing X/Cat cells and all protected cells are ignored by drag. Discrete tapping must still cycle all interactive cell states normally.

Direction locks to horizontal or vertical once movement clearly establishes the axis. A cell is processed at most once per gesture.

## Useless/protected cells
Some clue combinations can prove cells impossible before player interaction. Initial rule carried from the prototype: when a line has exactly one solution cat and a cardinal arrow in that line guarantees the cat lies in the pointed direction, cells on the opposite side of that arrow can be represented as protected/impossible cells.

This cardinal-only rule must not be applied to diagonal arrows, which cross different rows and columns. This is a derived presentation aid, not a replacement for the general solver. The solver is the source of truth for whether a cell is logically impossible.

Protected cells are non-interactive and must have a non-color visual treatment.

## Hint
Hint is unlimited. It should reveal or explain one valid logical deduction from the current board/player state. It must not be required for solvability.

The approved MVP uses an explanatory clue-derived hint, correcting a conflicting player mark with an explanation, rather than relying on player hypotheses as proof. For example ("Column 4 is complete, so these cells cannot contain cats") rather than simply revealing a random solution cat.

## Win condition
Win immediately when the set of player-marked Cat cells exactly equals the solution-cat set.

The state of every non-solution normal cell is irrelevant: it may be Empty or X. Extra player Cat marks prevent victory.

On victory:
- show the localized “All cats found!” inline beneath the difficulty;
- keep the completed board visible with a subtle grey treatment;
- do not automatically open a completion screen;
- the completed board is read-only;
- Hint and Clear are disabled;
- New Game and Menu remain available.

## Open design questions
- Broader automatic protection beyond the approved cardinal one-cat rule remains deferred.
- Final solver-based thresholds for Easy/Medium/Hard.
- Daily puzzle vs purely infinite mode.


## Issue #8 Easy loop

The menu offers Easy only, rules help and English/Brazilian Portuguese/Spanish
selection. Language preference persists; the live board stays in memory through
Menu and background/foreground, while process termination need not resume it.
New Game requests a fresh seed, preserving Easy. Normal app launch selects a fresh
initial seed; an explicit review seed can reproduce a diagnostic run.

Each Hint applies exactly one canonical conclusion from a human proof derived
only from visible clues. Player hypotheses never enter that proof. The earliest
unmatched fact is applied (Cat or X), including correcting a conflicting mark,
with its clue explanation and cell coordinate. Clear and later player edits
cause hints to rescan rather than advancing a permanent hint cursor.

Before play, only original-total-one cardinal arrows contribute opposite-side
protection, verified against clue-only solver assignments. Diagonals and totals
of two do not create this automatic protection. Hint can finish the game. The
completed grid stays visible; an optional Last hint action opens that deduction
on request. Hint/Clear are disabled and Menu/New Game remain enabled. The owner’s
Issue #19 decision supersedes the earlier automatic victory-dialog presentation.

## Issue #23 immediate-hint presentation

The owner's 2026-10-10 approval establishes the ongoing default and supersedes
the proposed Why experiment: Hint applies
one clue-proven fact immediately with no explanation UI. It may mark Cat or X,
correct a conflicting player mark and finish the board. Completed boards remain
readonly with Hint/Clear disabled; no Last hint action is shown. Unlimited
assistance, clue-only proof and exact-cat victory remain unchanged. The previous
modal/Last hint presentation is retained only as rollback.

## Issue #26 interactive tutorial

The owner approved one fixed authored Easy 5×5 onboarding board on 2026-10-13.
Ordinary games remain seeded procedural puzzles. Both tutorial axes contain
one zero-total line, one two-total line and three one-total lines; the five-cat
fixture includes cardinal and diagonal arrows and derived protected cells.
The fixed fixture must pass the same human-solvability and uniqueness contract.

The lesson teaches goal, sequential Empty/X/Cat demonstrations, separate hatching
and arrow introductions, a full tap cycle, zero-column
and zero-row drags, cardinal singleton cats, exact diagonal rays and two-cat
quotas. It uses ordinary GameSession taps/drags and immediate cat-set victory.
Only current targets accept guided input. Existing X intersections count; arrows
and hatches never require X. No cleanup action follows the final cat.

First ordinary Start presents the tutorial once; Skip starts a fresh random Easy
board. A permanent Tutorial menu action always restarts the same board/flow.
The tutorial is an explicit exception to previously deferred tutorial frameworks;
no generic framework or change to ordinary puzzle generation is included.

Owner device-review refinement (2026-10-13): guided completion immediately resets
the same fixture to Empty for an independent attempt, without step targets or
assistance. Only that independent win shows tutorial completion and Play for
random boards. Skip remains available as the explicit bypass. Demonstration X/Cat
are temporary presentation samples, not player placements or hidden answers.
