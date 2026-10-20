# Paws - Codex Operating Rules

## Mission
Build and ship a cozy, accessible mobile logic puzzle game in Unity while keeping every change traceable, testable, reversible, and human-controlled at production boundaries.

## Non-negotiable governance
- The approved `planning/MASTER_PROJECT_PLAN.md` governs product scope and budget.
- Ask for and receive explicit owner approval before every purchase or subscription, including paid renewals/upgrades/usage charges. Plan or budget approval does not authorize spending.
- Do not start implementation without a task/issue identifier or an explicitly approved bootstrap task.
- Do not merge, tag, publish, deploy, or release to production autonomously.
- `main` is production/release history. Human approval is mandatory for every merge to `main`.
- `develop` is the integration branch for lower environments. Agents may prepare and merge work into `develop` only when explicitly asked in the current task and all required checks pass.
- Never force-push `main` or `develop`.
- Never delete release tags.
- Keep one concern per branch/PR whenever practical.
- Prefer reversible changes, feature flags, and configuration over destructive migrations.
- Never add secrets, signing keys, store credentials, ad-network credentials, or API keys to the repository.

## Context and token discipline
Treat the repository as durable project memory. Conversation history is temporary working context.

- Start with root `AGENTS.md` and the current GitHub issue/task.
- Use `docs/CONTEXT_MAP.md` to decide which additional documents to load. Do not read all of `docs/`, `planning/`, issues, PRs, or source files by default.
- Load only the minimum context needed to complete the current task correctly.
- GitHub Issues are the primary task specification. Prefer issue acceptance criteria over reconstructing requirements from conversation history.
- Treat short prompts such as `Do #51`, `Review PR #42`, or `Fix #27` as sufficient when the referenced issue/PR and repository instructions provide the required context.
- Inspect relevant source files and their direct dependencies instead of asking the orchestrator/user to paste code or project history.
- Do not repeatedly reload unchanged files already inspected in the current task unless their state may have changed.
- Do not restate project history, requirements, or documents merely to show they were read.
- When a decision must survive the current task, write it to the appropriate issue, source-of-truth document, ADR, test, or code comment instead of carrying it only in conversation.
- Prefer a fresh task/thread after a meaningful milestone when durable state has already been captured in the repository.
- If `docs/CONTEXT_MAP.md` is missing or incomplete, use the Source of truth section below as the fallback router.
- Context minimization must not reduce correctness. Broaden context when necessary for high-risk work, especially puzzle correctness, solver/generator changes, save-data migrations, monetization behavior, analytics schemas, release/signing, or architecture changes.

## Work orchestration
The primary Codex thread acts as orchestrator/PM. Delegate bounded work to the custom agents in `.codex/agents/` only when specialization, independent review, or parallel analysis materially improves the outcome.

Do not delegate by default:
- trivial/local change: orchestrator or one implementation agent;
- normal feature: one relevant specialist/implementation agent when useful;
- risky domain or interaction change: relevant specialist plus QA review;
- architecture/product change: specialist analysis, human decision when required, then implementation and QA.

For implementation tasks:
1. Read the issue/task, root instructions, and only the context routed by `docs/CONTEXT_MAP.md`.
2. Confirm acceptance criteria and affected systems. Ask the user only for unresolved decisions that cannot be answered from repository state.
3. Decide whether delegation is actually useful. Prefer zero or one specialist for routine work.
4. For non-trivial or architectural work, produce a short implementation plan. For simple/local changes, proceed directly.
5. Make the smallest coherent change.
6. Add/update tests.
7. Run validation.
8. Ask `qa_reviewer` to review risky logic, puzzle, architecture, persistence, monetization, release, or interaction changes.
9. Update task traceability and durable documentation only when behavior, architecture, or an approved decision changed.
10. Stop at a review-ready state unless the user explicitly requested lower-environment merge/deployment.

When delegating, send the smallest viable brief:
- objective;
- issue/PR number;
- relevant paths, if known;
- explicit question or expected output.

Do not copy large project context into subagent prompts. Specialists should retrieve their own relevant context through `AGENTS.md`, `docs/CONTEXT_MAP.md`, the repo-local Skill, and their specialist instructions.

Avoid concurrent write-heavy subagents on overlapping files. Parallelize research, review, puzzle analysis, UX analysis, and test analysis before parallelizing code edits.

The repo-local Skill defines repeatable workflow routing; specialist agent files define role-specific behavior. Do not duplicate those instructions in this file unless they are global governance rules.

## Source of truth
Use `docs/CONTEXT_MAP.md` as the first routing index for task-specific context.

Fallback source list:
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

Do not load every source above for every task. Load only what the context map/task requires.

If these sources disagree, do not silently choose. Surface the conflict and prefer the most recently approved explicit product decision.

## Task traceability
Use GitHub Issues as the authoritative work item. A well-scoped issue should carry enough context that implementation can start without replaying prior conversations.

When creating or refining an issue, include as applicable:
- goal;
- acceptance criteria;
- relevant area/files;
- dependencies;
- explicit non-goals or boundaries.

Reference issue numbers in branches, commits, PRs, tests where useful, and release notes.

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

Keep PR descriptions factual and compact. Link to the issue/source of truth instead of repeating large requirements verbatim.

## Communication
Default to terse operational responses.

Do not:
- repeat the user's task;
- narrate routine file reads, searches, or implementation steps;
- summarize documents merely because they were inspected;
- paste code unless requested or necessary to explain a blocking issue;
- provide tutorials unless requested;
- restate acceptance criteria already present in the issue;
- produce long completion summaries for routine work.

For routine completion/status responses, use only the fields that apply:

```text
Status: done | blocked | decision-needed
Changed:
- <1-3 short bullets>
Tests: <result>
Blockers/Risks: <only if present>
PR: <number/link if applicable>
```

Target <=120 words for routine agent/orchestrator responses. Exceed this only when a design decision, failure analysis, safety issue, or requested explanation requires it.

Specialist responses should default to:

```text
Finding:
Recommendation:
Risk:
Blocker:
```

Omit empty fields. Return evidence or detail only when it materially affects the decision.

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
- Arrow cells are non-markable; the presentation-only hold-to-highlight aid (#47) must never mutate board marks.
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

Do not run unrelated expensive test suites by default when a narrower reliable validation exists. Use the smallest test scope that provides adequate confidence, then run broader required gates before merge/release as defined by `docs/TESTING.md`.

### Physical iPhone review — owner policy
- Physical iPhone interaction, appearance, lifecycle and comfort tests require human review.
- Do not automate these tests through Device Hub, screen mirroring, remote input, or an alternative device-control tool. Fall back to the owner/human reviewer instead.
- When authorized, agents may build, sign, install, launch and verify technical device readiness. These preparation checks do not satisfy the physical iPhone review gate.
- Hand off the ready build with a short task-specific checklist, then record the human's explicit pass/fail report and its scope. Until that report arrives, report physical review as pending; do not infer a pass from launch success, screenshots or automated checks.
- Desktop/simulated input and automated Unity tests remain separate validation evidence. See `docs/TESTING.md` for the review workflow.

## Local Unity validation (bootstrap #1)
- Use the existing checkout. Do not create a Git worktree unless the user requests one.
- In a local macOS Codex task, run `bash scripts/unity.sh` to discover the exact editor pinned in `UnityProject/ProjectSettings/ProjectVersion.txt`.
- Run `bash scripts/compile.sh` and `bash scripts/test.sh`; both resolve the project independently of the caller's working directory.
- Standard Hub installs are discovered automatically. For custom locations, use `UNITY_HUB_EDITOR_ROOT`; an explicit absolute `UNITY_EDITOR` executable takes precedence. Do not silently switch editor patches or edit the version pin to make discovery pass.
- Require Python 3.11+ for repository validation. Keep Unity activation in the user's Hub; never ask for license/account credentials in chat or store them in the repo.
- Close the GUI Editor for this project before batch validation. Do not add `-quit` to EditMode runs; it can interrupt the Test Framework.
- Logs and NUnit XML are saved per run under ignored `.unity-results/`. Test success requires a successful editor exit and a complete, passing, nonzero NUnit report. Failed, skipped, inconclusive, missing, or malformed results are not a passing gate.
- If this task is attached to Linux/cloud, do not claim access to the user's Mac or successful native Unity validation. Test runner contracts with `python3 -m unittest discover -s scripts/tests -v`, then report the local Unity checks still needed.

## Production safety
Agents may prepare lower-environment builds and PRs when asked. Agents must not autonomously:
- merge to `main`
- create production releases
- submit iOS/Android store builds
- enable production ad traffic
- change production monetization settings
- rotate production credentials

Those actions require explicit human instruction in the current conversation/task.
