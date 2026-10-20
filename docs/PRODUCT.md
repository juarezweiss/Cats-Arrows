# Product Definition

## Product promise
Paws is a calm, compact mobile logic puzzle: infer where all hidden paws are,
mark impossibilities, and finish a board without time pressure or punishment.

## Experience pillars
1. **Pure deduction** — every shipped/generated board is solvable without the Hint button.
2. **Calm interaction** — no timers, lives, failure animations, or penalties for experimentation.
3. **Fast tactile play** — portrait-first sessions, tap cycling, and drag-to-mark for repetitive eliminations.
4. **Readable logic** — clues are aligned with the grid and visually distinct without relying on color alone.
5. **Infinite replayability** — seeded procedural boards can produce effectively unbounded puzzles within validated difficulty bands.

## Localization readiness (Issue #3)

Initial languages are English (`en`), Brazilian Portuguese (`pt-BR`), and Spanish
(`es`). Player-facing text uses stable resource keys, with English as the fallback
for unsupported locales or missing translations. Additional locale resources must
not require changes to puzzle logic. Support explicit language selection and
device-language detection, locale-aware formatting, accented characters, and
layouts that accommodate longer translations as UI is built.

Structured deductions can be rendered in the selected language. The current Hint
flow applies one guaranteed fact immediately without explanation UI (#23);
localization still preserves stable deduction resources for diagnostics or a
separately approved future presentation. Regional Spanish variants can be added later.

## Initial audience
Mobile puzzle players who enjoy Picross/Nonograms, Minesweeper-like deduction, and low-pressure daily/short-session games.

## Approved master plan
[Master Project Plan](../planning/MASTER_PROJECT_PLAN.md) governs scope, acceptance
and budget. Version 1.0 was approved 2026-10-08; the current Paws reconciliation
is tracked in #61. Every purchase or subscription needs separate explicit owner approval.

## MVP
- iOS-first; owner playtest and small tester trial before public release. TestFlight
  enrollment and distribution retain their separate approval gates.
- Easy 5×5, Medium 6×6 and Hard 7×7 ordinary play. `Play` and `Next board`
  choose among them with a persistent limit of two consecutive successful starts
  in one mode (#43). Every generated board retains independent uniqueness and
  no-guess derivation; labels do not imply calibrated difficulty scores.
- Fixed Easy first-play tutorial and menu replay. Tutorial Skip/Play enters the
  same ordinary random-difficulty policy without counting the tutorial (#26/#43).
- Procedural seeded board generation.
- Row and column paw-count clues.
- Eight-direction arrow clues embedded in cells, including exact 45-degree diagonals.
- Empty/X/Paw player marking.
- Horizontal/vertical drag-to-X across eligible empty cells.
- Clear, New Game, Menu, Hint.
- Immediate victory detection.
- Timed ordinary-board celebration and return to the same locked finished board.
- Hold-to-highlight arrow paths and non-moving Hint attention after board inactivity.
- English, Brazilian Portuguese and Spanish; language preference and shape-based readability.
- Current Paws name/paw artwork, guided tutorial and unlimited clue-derived hints
  while monetization remains disabled.

Start the first tester session with the fixed Easy tutorial; subsequent ordinary
boards use the current random-difficulty policy. The approved #35 pilot limits
Hard lines to 1–3 paws and prunes redundant
Medium/Hard arrows with both solvability proofs intact; density ranges remain
soft targets. Current profile rules live in [GAME_DESIGN.md](GAME_DESIGN.md); validation
and physical-device evidence live in [TESTING.md](TESTING.md). Keep the first trial
focused on player understanding, comfortable input and concrete feedback.

## Not MVP
- Accounts/cloud save.
- Multiplayer.
- Competitive leaderboards.
- Live ops backend.
- Production advertising.
- Paid user acquisition.
- Calibrated difficulty scoring, persistent board resume, analytics SDKs and
  extensive polish before player feedback.
