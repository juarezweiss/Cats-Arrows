# Puzzle Solvability Contract

## Core guarantee
No puzzle enters gameplay unless an automated solver proves the intended solution is uniquely derivable under the game's allowed deduction rules without consuming Hint.

The old prototype placed cats/arrows randomly under local constraints. That is insufficient: a locally valid board can still require guessing. The Unity implementation must generate and validate as a pair.

## Architecture
1. Generate a candidate solution layout from a seed.
2. Derive row/column totals.
3. Place only arrow clues that are true for that solution.
4. Run the solver from clues alone.
5. Reject candidates that are contradictory, ambiguous, or exceed the target difficulty band.
6. Store the accepted seed and solver metrics for reproduction.

## Solver responsibilities
The solver should maintain candidate states for every non-arrow cell and repeatedly apply deductions until no progress is possible.

Initial deduction families:
- zero line => all eligible cells in line are not cats;
- line remaining count is zero => remaining unknown cells are not cats;
- line remaining count equals unknown candidates => all candidates are cats;
- arrow directional existence => at least one candidate remains in its pointed segment;
- arrow segment reduced to one candidate => that cell is a cat;
- line/arrow constraint interaction that removes candidates impossible under totals;
- propagation after each solved Cat/NotCat.

Do not silently add solver-only rules that players cannot reasonably infer from the visible rules. New deduction families should be documented and tested.

## Uniqueness
A board is valid only if the constraints admit exactly one complete solution. A separate exhaustive/backtracking verifier may be used to prove uniqueness even if the human-style deduction solver is intentionally limited.

## Issue #3 implementation contract

Both engines consume an immutable snapshot of visible clues, never the stored
solution, player hypotheses, or presentation-protected cells. Rows and columns
are exact counts; each arrow is an at-least-one constraint. Rays continue to the
board edge through other arrows, whose cells remain ineligible for cats. Several
arrows may be satisfied by one cat.

The initial human rule set is deliberately bounded: zero/satisfied lines,
fill-remaining lines, unsatisfied arrow singletons, and elimination outside an
unsatisfied arrow segment when its line needs exactly one more cat. Repeat these
rules deterministically to a fixed point. Disjoint-arrow reservations, hypothetical
placements, and broader subset reasoning are deferred. A unique board that needs
unsupported reasoning is stalled and must be rejected.

Human completion classifies every normal cell and validates every clue. This does
not change the player's cat-set-only victory condition. Invalid input,
contradiction, stall, and work-budget exhaustion are distinct outcomes.

The verifier searches normal-cell assignments independently, checks complete
solutions against the original clues, and stops upon finding two distinct
solutions. Exactly one witness is unique only after all alternatives are
exhausted. A search budget or cancellation cannot prove uniqueness. Profile
limits are admission policy, not hidden constraints in the uniqueness search.

The assessment accepts only a complete human derivation plus an exhaustive unique
result matching the intended solution and valid generation profile. It does not
automatically integrate generation or gameplay in Issue #3.

Deduction records contain stable rule IDs, source clues, premises, and explicit
Cat/NotCat conclusions. Explanation text belongs to localization presentation;
solving and metrics must be identical across locales. Record the solver ruleset
version with reproducibility data. Verifier effort is separate from human
difficulty metrics.

V1 metric definitions: immediate-elimination proportion is the first deduction's
NotCat conclusions divided by the initial non-arrow cell count. Maximum unresolved
candidates per line is the initial maximum eligible row/column cell count, because
unknown counts only decrease. Neither metric measures verifier branching effort.

## Difficulty
Difficulty should ultimately come from solving characteristics rather than grid size alone. Candidate metrics:
- number of deduction steps;
- hardest deduction family required;
- average/maximum candidate branching pressure;
- number of chained propagations;
- initial clue density;
- proportion of immediate eliminations.

The solver must not guess when calculating the human-solvable path.

## Reproducibility
Every generated puzzle exposes/logs:
- seed;
- generator version;
- difficulty profile/version;
- solution hash or equivalent test fingerprint.

A regression test must be able to recreate a reported puzzle exactly.
