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
