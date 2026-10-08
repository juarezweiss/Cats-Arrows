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
unsatisfied cardinal arrow segment when its row/column needs exactly one more cat.
Diagonal arrows use exact 45-degree rays but never this cardinal line-exclusion rule. Repeat these
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

## Issue #4 generation contract

The first generator supports Easy 5x5 only: at most two cats per row/column and at
most one zero row/column. Feasible total cat counts are four through ten. Sample
quota patterns and bounded randomized layouts instead of relying on manually
authored board templates. The distribution is not promised to be uniform.

Enumerate truthful eight-direction arrow options on non-cat cells. Trial additions use
the existing human solver and prefer progress on other cells normal in both the
current and trial board. The newly occupied arrow cell does not count as progress.
Seeded ordering resolves ties; a zero-immediate-gain addition is permitted because
later clues can unlock a chain. Never retry a zero-gain stage without a bound.

Stop at the first complete human derivation, then assess the exact frozen board
against all invariants, exhaustive uniqueness, and the intended cat set. No
partial or unproved board may be returned on failure. Do not mutate clues after
proof. Clue removal/minimization, extra deduction rules, and final difficulty
thresholds remain separate work.

Candidate attempts, layout nodes, trial additions, raw random draws, human
constraint evaluations, and verifier nodes have deterministic limits. Per-call
and aggregate solver limits prevent retry multiplication from hiding expensive
work. Exhaustion returns an explicit failure without a puzzle; it does not trigger
unchecked dense clues or a template fallback. Cancellation also returns no puzzle.

Specify the PRNG algorithm and signed-seed mapping, test golden vectors, and avoid
runtime-dependent randomness, culture-dependent fingerprints, and unordered
iteration. Unbiased bounded sampling consumes the raw-draw budget even on rejected
draws. Effective options and profile/ruleset versions are part of replay identity.

The initial clue ceiling is ten arrows, leaving at least fifteen normal cells and
five normal non-cat cells even with ten cats. This is a configurable generation
policy, not a new gameplay rule or calibrated Easy score. Measure acceptance,
duplicates, clue/solution distributions, allocations/work, and latency before
changing the versioned defaults. Desktop timings do not establish mobile waits.

Unchanged numeric defaults (v1 and v2): 64 attempts, 256 layout nodes per attempt (16,384 aggregate), eight
trials per stage, ten arrows, 500 human evaluations per call (200,000 aggregate),
5,000 verifier nodes per call (50,000 aggregate), and 100,000 random draws. These
remain bounds, not target work. The fixed 1,000-seed corpus passed within these
defaults without increasing them; see `docs/TESTING.md` for measured yield/work.

`IPuzzleGenerator.Generate` returns a `GenerationResult` with `Success`,
`InvalidProfile`, `UnsupportedProfile`, `Exhausted`, or `Cancelled`. Only success
contains a puzzle and accepted assessment. Options are immutable, recorded with
the receipt, and enforce the v1 ten-arrow/eight-trial ceilings. Only the exact Easy
profile is supported initially. Cancellation is cooperative at work boundaries
and after bounded solver calls; cancelled requests return no playable artifact.

To reproduce an accepted board, use its seed and effective options with matching
generator/profile/PRNG/ruleset versions, then compare the canonical fingerprint.
Retain earlier generation implementations when adding versions that must replay
saved puzzles. Merely using the same seed with changed defaults is insufficient.

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

## Issue #14 version transition

The approved MVP adds four diagonal directions with the same at-least-one semantics.
The generator, human rules and verifier record v2 provenance; profile, PRNG and
work-limit defaults stay unchanged. Old cardinal enum values remain 0–3. V1 board
receipts and benchmark reports refer to their original Git revisions; v2 is not
an implementation of v1 replay. There are no shipped saved boards requiring a
runtime migration. Fixed seed vectors are refreshed for v2, while independent
small-board enumeration checks geometry and deduction soundness.
