# Cats & Arrows - Codex Operating Rules

## Mission
Build and ship a cozy, accessible mobile logic puzzle game in Unity while keeping every change traceable, testable, reversible, and human-controlled at production boundaries.

## Non-negotiable governance
- Do not start implementation without a task/issue identifier or an explicitly approved bootstrap task.
- Do not merge, tag, publish, deploy, or release to production autonomously.
- `main` is production/release history. Human approval is mandatory for every merge to `main`.
- `develop` is the integration branch for lower environments. Agents may prepare and merge work into `develop` only when explicitly asked in the current task and all required checks pass.
- Never force-push `main` or `develop`.
- Never delete release tags.
- Keep one concern per branch/PR whenever practical.
- Prefer reversible changes, feature flags, and configuration over destructive migrations.
- Never add secrets, signing keys, store credentials, ad-network credentials, or API keys to the repository.

## Work orchestration
The primary Codex thread acts as orchestrator/PM. Delegate bounded work to the custom agents in `.codex/agents/` when specialization or parallel analysis materially helps.

For implementation tasks:
1. Read the issue/task and relevant docs.
2. Confirm acceptance criteria and affected systems.
3. Ask read-only specialists to analyze in parallel when useful.
4. Produce a short implementation plan.
5. Make the smallest coherent change.
6. Add/update tests.
7. Run validation.
8. Ask `qa_reviewer` to review risky logic or interaction changes.
9. Update task traceability and documentation when behavior changed.
10. Stop at a review-ready state unless the user explicitly requested lower-environment merge/deployment.

Avoid concurrent write-heavy subagents on overlapping files. Parallelize research, review, puzzle analysis, UX analysis, and test analysis before parallelizing code edits.

## Source of truth
- Product intent: `docs/PRODUCT.md`
- Game rules: `docs/GAME_DESIGN.md`
- Puzzle guarantees: `docs/PUZZLE_SOLVABILITY.md`
- Architecture: `docs/ARCHITECTURE.md`
- UX/accessibility: `docs/UX.md`
- Art direction: `docs/ART_DIRECTION.md`
- Testing: `docs/TESTING.md`
- Release/rollback: `docs/RELEASE.md`
- Monetization: `docs/MONETIZATION.md`
- Marketing: `docs/MARKETING.md`
- Analytics: `docs/ANALYTICS.md`
- Current priorities: `planning/ROADMAP.md` and GitHub Issues

If these sources disagree, do not silently choose. Surface the conflict and prefer the most recently approved explicit product decision.

## Task traceability
Use GitHub Issues as the authoritative work item. Reference issue numbers in branches, commits, PRs, tests where useful, and release notes.

Branch naming:
- `feat/<issue>-short-name`
- `fix/<issue>-short-name`
- `chore/<issue>-short-name`
- `docs/<issue>-short-name`

Commit style:
- `feat(scope): ...`
- `fix(scope): ...`
- `test(scope): ...`
- `docs(scope): ...`
- `chore(scope): ...`

Every PR must state:
- issue/task
- behavior changed
- acceptance criteria status
- tests run
- screenshots/video for UI changes when available
- risk and rollback notes

## Unity rules
- Target Unity 6.3 LTS unless an ADR changes this.
- Keep puzzle domain logic independent of MonoBehaviour/UI where practical.
- Prefer deterministic pure C# for puzzle generation, validation, solving, and game-state transitions.
- UI consumes domain state; UI must not own puzzle truth.
- Random puzzle generation must accept an explicit seed and preserve it for reproduction.
- A generated puzzle is invalid unless the solver validates it under the documented solvability contract.
- Do not hide logic in Unity scene objects when it belongs in code or ScriptableObjects.
- Avoid singleton proliferation and global mutable state.

## Game-specific invariants
- A cell cannot be both an arrow cell and a solution cat cell.
- Arrow cells are non-interactive.
- Each arrow means at least one solution cat exists in the arrow's pointing direction within the relevant line/path defined by game rules.
- Row/column clue totals must exactly match the solution.
- Player cells cycle `Empty -> X -> Cat -> Empty` on discrete tap unless the completed-board state disables interaction.
- Drag-mark affects eligible empty cells only and must not break normal discrete tap cycling.
- A win occurs immediately when the player's cat placements exactly equal the solution cat set; non-solution cells may be Empty or X.
- Completed boards are read-only.
- Hints are optional assistance, not required for a valid generated puzzle.
- Accessibility cannot rely on color alone.

## Testing expectations
For puzzle-domain changes, add deterministic EditMode tests covering normal cases, edge cases, invalid states, and seed reproducibility.

For interaction/UI changes, test at least:
- tap cycle
- drag marking
- protected cells
- completed-board lock
- clear/new-game behavior
- portrait mobile layout at representative aspect ratios

Run `scripts/validate.sh` before declaring a task complete. If Unity is unavailable, run the repository checks that are available and explicitly report which Unity checks were not executed.

## Production safety
Agents may prepare lower-environment builds and PRs when asked. Agents must not autonomously:
- merge to `main`
- create production releases
- submit iOS/Android store builds
- enable production ad traffic
- change production monetization settings
- rotate production credentials

Those actions require explicit human instruction in the current conversation/task.
