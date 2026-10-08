# Product Definition

## Product promise
Cats & Arrows is a calm, compact mobile logic puzzle: infer where all hidden cats are, mark impossibilities, and finish a board without time pressure or punishment.

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

Solver explanations are rendered from structured deductions in the selected
language. The localization foundation does not add a language-selection screen or
Hint UI before those screens exist. Regional Spanish variants can be added later.

## Initial audience
Mobile puzzle players who enjoy Picross/Nonograms, Minesweeper-like deduction, and low-pressure daily/short-session games.

## Approved master plan
[Master Project Plan](../planning/MASTER_PROJECT_PLAN.md), approved 2026-10-08, governs scope, acceptance and budget. Every purchase or subscription needs separate explicit owner approval.

## MVP
- Easy-only 5x5, iOS-first; owner playtest and small TestFlight trial before public release.
- Procedural seeded board generation.
- Row and column cat-count clues.
- Eight-direction arrow clues embedded in cells, including exact 45-degree diagonals.
- Empty/X/Cat player marking.
- Horizontal/vertical drag-to-X across eligible empty cells.
- Clear, New Game, Menu, Hint.
- Immediate victory detection.
- Finished-board review state.
- English, Brazilian Portuguese and Spanish; language preference and shape-based readability.
- Simple replaceable icons and brief rules help; unlimited clue-derived hints.

## Not MVP
- Accounts/cloud save.
- Multiplayer.
- Competitive leaderboards.
- Live ops backend.
- Production advertising.
- Paid user acquisition.
- Medium/Hard, persistent board resume, analytics SDKs and extensive polish before player feedback.
