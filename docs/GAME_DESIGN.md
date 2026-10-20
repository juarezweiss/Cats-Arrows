# Game Design Specification

## Board
A rectangular square grid. Current target sizes:
- Easy: 5x5, maximum 2 solution paws per row/column.
- Medium: 6x6, maximum 3 solution paws per row/column; zero-paw lines are permitted.
- Hard: 7x7, 1–3 solution paws per row/column; no zero-paw lines and at least
  one row or column with exactly 3 solution paws on every generated board.

Issue #35's approved pilot supersedes #32's original Hard max4 cap with max3.
Medium's counts and both sizes remain unchanged. Medium/Hard remove redundant
arrows only after complete no-guess/unique proof. Medium favors fewer combined
arrow and derived-hatched cells. Issue #37 removes initial hatches in Hard, so
Hard favors fewer arrows only. Indicative #35 bands (Medium 3–9 arrows/~21% fixed
cells, Hard 7–13/~27%) describe that earlier pilot, not per-board acceptance limits
or calibrated difficulty scores. No additional deductions are introduced. Easy
generation and the fixed Easy tutorial are unchanged.

## Play and difficulty selection — Issue #43

The menu offers Play, Tutorial and languages, with no difficulty
choice. Ordinary Play and Next board choose uniformly from Easy/Medium/Hard, except
that a difficulty is excluded after two consecutive successful starts in it.
The last difficulty and run length persist across app restarts. Abandoning a
successfully started board still counts; generation failure does not. Retry keeps
its pending difficulty and advances only the puzzle seed. Menu, language changes,
Clear and tutorial entry do not consume or reset this history.

First-ever Play presents the fixed Easy tutorial. That first-use gate persists,
so subsequent Plays request random ordinary boards even after a restart. Manual
Tutorial remains available and does not consume a fresh installation's first
Play. Existing tutorial-seen installations migrate once as returning users.
Tutorial Skip/Play uses the same ordinary-board selector, without counting the
tutorial fixture. This adds no board save/resume or generation rule.

## Solution paws
- A normal cell contains at most one solution paw.
- Arrow cells can never contain solution paws.
- The player does not see solution paws directly.

## Row and column totals
Every row and column displays the exact number of solution paws in that line. Column totals are centered over their columns; row totals are centered beside their rows.

Easy and Medium permit at most one zero-paw row and at most one zero-paw column.
Hard permits none. Medium may include 3-paw lines but need not contain one; Hard
must contain a 3-paw row or column. These admission rules never act as hidden
constraints inside the clue-only solver or uniqueness proof.

## Arrow clues
An arrow occupies a grid cell and cannot be marked. The initial MVP supports eight directions: Up, Down, Left, Right, UpLeft, UpRight, DownLeft and DownRight (Issue #14).

Meaning: **at least one solution paw exists in the direction the arrow points.** For a horizontal arrow, consider cells in the same row beyond the arrow. For a vertical arrow, consider cells in the same column beyond the arrow. For a diagonal arrow, advance one row and one column per step along its exact 45-degree ray. All rays continue through other arrow cells to the edge without wrapping; those arrow cells are ineligible for paws. The origin never counts. Multiple arrows may share a paw; a ray may contain more than one paw. Diagonals introduce no diagonal count clues.

Example: a row has total 2 and contains a Right arrow. At least one of the two solution paws is somewhere to the right of the arrow. The second paw may be on either side if all other constraints permit it.

Issue #47 adds a presentation-only aid on active ordinary boards: hold an arrow
for 350 ms to outline its full forward path, using the tutorial's outline
style. The path includes intervening arrows and marked/protected cells, continuing
to the edge; it reveals no hidden paw information. Release or cancellation removes
the outline immediately. Arrow holds never mark cells or become drag marking.
Completed-board interaction remains unchanged. During the fixed tutorial's guided steps,
this aid is enabled only for the focused R1C4 DownLeft arrow-hold lesson before the
diagonal-paw deduction; the independent same-board stage enables normal holds on any arrow.

## Player cell states
Interactive normal cells cycle on a discrete tap:
`Empty -> X -> Paw -> Empty`

X means "I believe no paw is here." Paw means "I believe a paw is here." The game does not punish a wrong intermediate hypothesis.

## Drag-to-mark
A press-and-drag gesture may travel horizontally or vertically from its origin. It changes only eligible cells that are currently Empty, marking them X. Existing X/Paw cells and all protected cells are ignored by drag. Discrete tapping must still cycle all interactive cell states normally.

Direction locks to horizontal or vertical once movement clearly establishes the axis. A cell is processed at most once per gesture.

## Useless/protected cells
Some clue combinations can prove cells impossible before player interaction. Initial rule carried from the prototype: when a line has exactly one solution paw and a cardinal arrow in that line guarantees the paw lies in the pointed direction, cells on the opposite side of that arrow can be represented as protected/impossible cells.

This cardinal-only rule applies to Easy, Medium and the fixed Easy tutorial; it must not be applied to diagonal arrows, which cross different rows and columns. This is a derived presentation aid, not a replacement for the general solver. The solver is the source of truth for whether a cell is logically impossible.

Hard has no initial hatches (Issue #37). Even a provable NotPaw cell behind a
cardinal arrow in a one-paw line remains Empty and interactive until the player
marks it or requests an optional Hint. No solver consequences become automatic
protection. Hatches are optional, so this fix introduces no independent hatch
count or selection rule. An empty hatch set is deterministic for every seed;
the existing clue-only uniqueness and no-guess proofs require no hatch premises.

Protected cells are non-interactive and must have a non-color visual treatment.

## Hint

Future design #29 gates hints with rewarded ads only when monetization is explicitly
enabled; Remove All Ads restores direct unlimited access. Current free trial
behavior remains unchanged. See [MONETIZATION.md](MONETIZATION.md) for access policy;
clue-derived truth, one-fact application, immediate wins and hint-free solvability
are invariant in both modes.
Hint is unlimited in the current free MVP. It should reveal or explain one valid logical deduction from the current board/player state. It must not be required for solvability.

The approved MVP uses an explanatory clue-derived hint, correcting a conflicting player mark with an explanation, rather than relying on player hypotheses as proof. For example ("Column 4 is complete, so these cells cannot contain paws") rather than simply revealing a random solution paw.

## Win condition
Win immediately when the set of player-marked Paw cells exactly equals the solution-paw set.

The state of every non-solution normal cell is irrelevant: it may be Empty or X. Extra player Paw marks prevent victory.

On victory:
- show the localized “All paws found!” inline beneath the difficulty;
- keep the completed board visible with a subtle grey treatment;
- the completed board is read-only;
- Hint and Clear are disabled;
- Next board and Menu remain available.

Issue #44 adds an ordinary-board presentation sequence, including final-Hint
wins. The grid becomes slightly transparent, bilateral confetti ends at three
seconds and centered lowercase “you found all the paws” fades in. Its fade-out
begins four seconds after victory; after it finishes, lowercase Next board,
Back to completed board and Menu buttons fade in. The short fades take 250 ms
each. Back restores the same static finished grid at full opacity, preserving
marks and the completion lock. Next uses #43's persistent difficulty selector.
Navigation, backgrounding and rebind dismiss the sequence without replaying it
on return. Existing controls remain available. Neither tutorial stage uses this
sequence, and completion still belongs to GameSession immediately at the last paw.

## Open design questions
- Broader automatic protection beyond the approved cardinal one-paw rule remains deferred.
- Final solver-based thresholds for Easy/Medium/Hard.
- Daily puzzle vs purely infinite mode.


## Issue #8 Easy loop

The original menu offered Easy and English/Brazilian Portuguese/Spanish
selection. The current menu uses Play and an always-available Tutorial (#72).
Language preference persists; the live board stays in memory through
Menu and background/foreground, while process termination need not resume it.
New Game requests a fresh seed, preserving Easy. Normal app launch selects a fresh
initial seed; an explicit review seed can reproduce a diagnostic run.

Each Hint applies exactly one canonical conclusion from a human proof derived
only from visible clues. Player hypotheses never enter that proof. The earliest
unmatched fact is applied (Paw or X), including correcting a conflicting mark,
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
one clue-proven fact immediately with no explanation UI. It may mark Paw or X,
correct a conflicting player mark and finish the board. Completed boards remain
readonly with Hint/Clear disabled; no Last hint action is shown. Unlimited
assistance, clue-only proof and exact-paw victory remain unchanged. The previous
modal/Last hint presentation is retained only as rollback.

## Issue #26 interactive tutorial

The owner approved one fixed authored Easy 5×5 onboarding board on 2026-10-13.
Ordinary games remain seeded procedural puzzles. Both tutorial axes contain
one zero-total line, one two-total line and three one-total lines; the five-paw
fixture includes cardinal and diagonal arrows and derived protected cells.
The fixed fixture must pass the same human-solvability and uniqueness contract.

The lesson teaches goal, sequential Empty/X/Paw demonstrations, separate hatching
and arrow introductions, a full tap cycle, zero-column
and zero-row drags, cardinal singleton paws, exact diagonal rays and two-paw
quotas. It uses ordinary GameSession taps/drags and immediate paw-set victory.
Only current targets accept guided input. Existing X intersections count; arrows
and hatches never require X. No cleanup action follows the final paw.

First-ever Play presents the tutorial once across restarts; Skip starts a fresh
ordinary board using Issue #43's persistent difficulty selector. A permanent
Tutorial menu action always restarts the same fixed Easy board/flow, independent
of ordinary difficulty history. Manual replay does not consume a fresh first-Play
gate; already-seen installations migrate as returning users.
The tutorial is an explicit exception to previously deferred tutorial frameworks;
no generic framework or change to ordinary puzzle generation is included.

Owner device-review refinement (2026-10-13): guided completion immediately resets
the same fixture to Empty for an independent attempt, without step targets or
assistance. Only that independent win shows tutorial completion and Play for
random boards. Skip remains available as the explicit bypass. Demonstration X/Paw
are temporary presentation samples, not player placements or hidden answers.
