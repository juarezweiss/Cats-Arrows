# Puzzle Solvability Contract

## Core guarantee
No puzzle enters gameplay unless an automated solver proves the intended solution is uniquely derivable under the game's allowed deduction rules without consuming Hint.

The old prototype placed paws/arrows randomly under local constraints. That is insufficient: a locally valid board can still require guessing. The Unity implementation must generate and validate as a pair.

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
- zero line => all eligible cells in line are not paws;
- line remaining count is zero => remaining unknown cells are not paws;
- line remaining count equals unknown candidates => all candidates are paws;
- arrow directional existence => at least one candidate remains in its pointed segment;
- arrow segment reduced to one candidate => that cell is a paw;
- line/arrow constraint interaction that removes candidates impossible under totals;
- propagation after each solved Paw/NotPaw.

Do not silently add solver-only rules that players cannot reasonably infer from the visible rules. New deduction families should be documented and tested.

## Uniqueness
A board is valid only if the constraints admit exactly one complete solution. A separate exhaustive/backtracking verifier may be used to prove uniqueness even if the human-style deduction solver is intentionally limited.

## Issue #3 implementation contract

Both engines consume an immutable snapshot of visible clues, never the stored
solution, player hypotheses, or presentation-protected cells. Rows and columns
are exact counts; each arrow is an at-least-one constraint. Rays continue to the
board edge through other arrows, whose cells remain ineligible for paws. Several
arrows may be satisfied by one paw.

The initial human rule set is deliberately bounded: zero/satisfied lines,
fill-remaining lines, unsatisfied arrow singletons, and elimination outside an
unsatisfied cardinal arrow segment when its row/column needs exactly one more paw.
Diagonal arrows use exact 45-degree rays but never this cardinal line-exclusion rule. Repeat these
rules deterministically to a fixed point. Disjoint-arrow reservations, hypothetical
placements, and broader subset reasoning are deferred. A unique board that needs
unsupported reasoning is stalled and must be rejected.

Human completion classifies every normal cell and validates every clue. This does
not change the player's paw-set-only victory condition. Invalid input,
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
Paw/NotPaw conclusions. Explanation text belongs to localization presentation;
solving and metrics must be identical across locales. Record the solver ruleset
version with reproducibility data. Verifier effort is separate from human
difficulty metrics.

V1 metric definitions: immediate-elimination proportion is the first deduction's
NotPaw conclusions divided by the initial non-arrow cell count. Maximum unresolved
candidates per line is the initial maximum eligible row/column cell count, because
unknown counts only decrease. Neither metric measures verifier branching effort.

## Issue #4 generation contract

The first generator supports Easy 5x5 only: at most two paws per row/column and at
most one zero row/column. Feasible total paw counts are four through ten. Sample
quota patterns and bounded randomized layouts instead of relying on manually
authored board templates. The distribution is not promised to be uniform.

Enumerate truthful eight-direction arrow options on non-paw cells. Trial additions use
the existing human solver and prefer progress on other cells normal in both the
current and trial board. The newly occupied arrow cell does not count as progress.
Seeded ordering resolves ties; a zero-immediate-gain addition is permitted because
later clues can unlock a chain. Never retry a zero-gain stage without a bound.

Stop at the first complete human derivation, then assess the exact frozen board
against all invariants, exhaustive uniqueness, and the intended paw set. No
partial or unproved board may be returned on failure. Do not mutate clues after
proof. Issue #35 adds bounded clue removal for Medium/Hard only; Easy retains
this original first-complete-board behavior. Extra deduction rules and calibrated
difficulty thresholds remain separate work.

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
five normal non-paw cells even with ten paws. This is a configurable generation
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

## Issue #32 Medium/Hard extension

`SeededPuzzleGenerator` delegates Easy to the original v2 implementation, retaining
its quota sampling, options, signed-seed mapping and golden fingerprints. The
separate larger-board path generates Medium 6×6 / Hard 7×7. Existing caps are
Medium max 3/at-most-one zero row and column, Hard max 4/no zero row or column;
Hard admission additionally requires some row or column total exactly 3. Medium
permits but does not require a 3-total line. These checks use the frozen solution
for admission; the human solver and independent verifier still receive clues only.
Both proofs must complete and agree with the intended paw set before success.

The larger path uses bounded quota/layout search and truthful eight-direction
arrow growth with the same documented deductions. Its cached quota arrays are
private/read-only after initialization; per-call RNG, layouts and work counters
are independent. No new solver families, clue-density targets or calibrated
score thresholds are introduced. Metadata records the actual generator/profile
and effective options versions. Easy defaults/public v1 ceilings remain intact.

Larger-board defaults (`generation-options-large-v1`): 64 attempts, 512 layout nodes
per attempt/32,768 aggregate, 8 arrow trials per stage, 24 arrows, 2,000 human
evaluations per call/200,000 aggregate, 5,000 verifier nodes per call/50,000 aggregate,
100,000 raw draws. These are finite work/clue ceilings, not additional game rules.
When caller-supplied tighter v1 options exhaust, failure is explicit and returns no board;
no unchecked fallback, guessed proof or authored game template is used.

Measured non-Unity desktop corpus: 1,000/1,000 successes per new profile. Peak arrow
count Medium 15 / Hard 23, attempts 1/6, human evaluations 10,040/27,390, verifier nodes 27/63
and draws 1,001/2,248. Warm desktop Mono p95 was 6.15 ms / 14.16 ms; this is implementation
evidence, not phone latency or calibrated player difficulty. Native test/review
evidence is recorded in TESTING.md and the #32 PR.

## Approved #35 clue-pruning pilot

The current tuned path is `difficulty-generator-v2` / `difficulty-profile-v2`.
Hard remains 7×7 with no zero lines and at least one exactly-3 row or column,
but its cap is now 3. Medium remains 6×6/max3/at-most-one zero row and column.
Easy delegates unchanged, retaining its versions, options and frozen seed output.
The #32 v1 results above describe the original implementation, not tuned receipts.
There are no shipped board saves requiring a runtime v1 replay path; reproduce
older receipts with their original source revision and versions.

After complete initial admission, try removing arrows in ascending and descending
coordinate order, each starting from the same proved board. A bounded human-only
preflight rejects stalled deletions; every retained deletion then requires full
human/independent-uniqueness assessment and intended-answer agreement. Compare
proved variants by combined arrows plus derived hatches, then arrow count; retain
the deterministic first candidate on ties. Hatches come from the same existing
cardinal one-paw rule and accepted proof used by gameplay, never additional solver
premises. No new deductions, difficulty scores or density rejection rules apply.

`generation-options-large-v2` retains the 24-arrow search ceiling and original
aggregate human/verifier limits. Its optional pruning phase has ceilings of 48
deletion trials, 40,000 human evaluations and 10,000 verifier nodes, also charged
to the unchanged 200,000/50,000 shared totals. Per-call limits remain 2,000/5,000.
Options record tighter overrides; diagnostics record phase trials, work, completed
orders, retained deletions across both paths and pruning exhaustion. The retained
deletion count is not the final board's arrow-count delta.

If optional pruning exhausts, return only the last/best fully admitted artifact;
do not reject an otherwise valid board for missing a density target. If initial
generation cannot prove a board, fail explicitly without a puzzle. Cancellation
always returns no puzzle. Scoring reuses accepted proofs without hidden solver
calls. All effective options and versions form replay identity.

Soft research observations are Medium 3–9 arrows/~21% combined fixed occupancy
and Hard 7–13/~27%. They are corpus distributions, not universal minima or hard
acceptance limits. The research and limitations are recorded in Issue #35;
implementation validation is recorded separately in TESTING.md.

## Issue #37 Hard hatch policy

Hard uses `hard-generator-v3` / `hard-profile-v3`: no initial protected/hatch cells
are derived or displayed, including NotPaw cells behind one-paw cardinal arrows.
The player may mark these cells with X. Optional Hint remains a single requested
deduction, with no automatic protection. No independent hatches are selected in
this minimal fix; the empty set is deterministic and needs no new random draws.

Hard's bounded two-order pruning therefore compares arrow count alone. All
retained boards still pass the same clue-only human and independent uniqueness
assessment with intended-answer agreement. Existing attempt, pruning, solver,
verifier and random-draw ceilings are unchanged. Cancellation and exhaustion
contracts are unchanged. No new solver rules or difficulty thresholds apply.

Medium retains `difficulty-generator-v2` / `difficulty-profile-v2`, its combined
arrow/derived-hatch scoring, original hatch behavior and frozen replay receipts.
Easy and the fixed Easy tutorial are unchanged. `generation-options-large-v2`
remains the common options identity because the limits have not changed. Earlier
Hard receipts/measurements refer to their original revision; reproduce them there,
not by interpreting a v2 receipt as a v3 board. No shipped board saves need migration.

## Difficulty calibration (deferred)
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
