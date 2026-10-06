---
name: cats-arrows-development
description: Develop, review, test, plan, or release work for the Cats & Arrows Unity mobile puzzle repository. Use for feature implementation, bug fixes, puzzle generation/solver changes, mobile input and UI work, game-design changes, testing, task decomposition, PR preparation, release planning, analytics, monetization, or marketing work that must follow this repository's traceability, solvability, accessibility, and production-safety rules.
---

# Cats & Arrows Development

## Workflow
1. Read root `AGENTS.md` plus the task/issue.
2. Load only the relevant product references listed below.
3. Identify acceptance criteria, affected layers, and risks.
4. Delegate read-heavy specialist work to `.codex/agents/` when useful.
5. Keep implementation scoped to one tracked task.
6. Add/update tests before calling work complete.
7. Run `scripts/validate.sh` and Unity tests when the editor is available.
8. Update behavior docs/ADR when a rule or architecture decision changes.
9. Produce a review-ready summary with tests, risks, rollback, and unresolved items.
10. Stop before production actions unless the user explicitly instructs them in the current task.

## Route by task
### Puzzle generation, solving, or difficulty
Read `references/puzzle-workflow.md`, then `docs/GAME_DESIGN.md` and `docs/PUZZLE_SOLVABILITY.md`. Prefer the `puzzle_engineer` agent and request a `qa_reviewer` pass for non-trivial changes.

### Unity gameplay, input, or UI
Read `references/mobile-interaction.md`, `docs/ARCHITECTURE.md`, and `docs/UX.md`. Use `unity_engineer`; use `ux_designer` for interaction/accessibility review.

### Product/game-design change
Use `game_designer` first. Update the relevant source-of-truth doc before or with implementation. Do not bury new game rules only in code.

### Monetization
Read `docs/MONETIZATION.md`; use `monetization_designer`. Keep provider SDKs behind adapters and do not activate production monetization.

### Marketing/growth
Read `docs/MARKETING.md` and `docs/ANALYTICS.md`; use `growth_marketer`. Tie recommendations to explicit hypotheses and measurable metrics.

### Release/CI
Read `docs/RELEASE.md` and `docs/TESTING.md`; use `release_engineer`. Lower-environment work may be prepared when asked. Production merge/release/store submission remains human-controlled.

## Definition of done
A task is done only when:
- acceptance criteria are satisfied;
- relevant automated tests exist and pass, or unavailable test infrastructure is explicitly reported;
- puzzle invariants remain valid;
- mobile/accessibility impact is considered for player-facing changes;
- docs are updated for behavior/architecture changes;
- task/PR traceability is present;
- rollback/risk is stated for meaningful changes;
- no production boundary was crossed without explicit human instruction.
