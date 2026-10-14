# Cats & Arrows Context Map

Purpose: route agents to the **minimum project context required for the current task**.

This file is an index, not a checklist. Do not load every document listed here.

## Default context

For every task:

1. Read root `AGENTS.md`.
2. Read the current GitHub Issue, PR, or explicitly assigned task.
3. Use this map to select only the additional context required.
4. Inspect only relevant source files and direct dependencies.
5. Broaden context only when correctness, risk, or an unresolved conflict requires it.

If the repo-local `cats-arrows-development` Skill is active, follow its workflow and load only the reference files it routes for the current task. Do not reload already-understood unchanged context.

## Primary routing

| Task | Load first | Load only if needed |
|---|---|---|
| Product intent, player promise, positioning | `docs/PRODUCT.md` | `planning/MASTER_PROJECT_PLAN.md` for approved scope/budget; `docs/GAME_DESIGN.md` for rule implications |
| Game rules, win conditions, clues, cell states | `docs/GAME_DESIGN.md` | `docs/PUZZLE_SOLVABILITY.md` when solvability is affected; `docs/UX.md` for player interaction |
| Puzzle generation, solver, uniqueness, deductions | `docs/PUZZLE_SOLVABILITY.md`, `docs/GAME_DESIGN.md` | Skill `references/puzzle-workflow.md`; `docs/GENERATION_BENCHMARK.md` for generation performance/difficulty evidence; `docs/ARCHITECTURE.md` when interfaces/layers change |
| Difficulty tuning | `docs/GAME_DESIGN.md`, `docs/PUZZLE_SOLVABILITY.md` | `docs/GENERATION_BENCHMARK.md` for measured generator/solver behavior |
| Gameplay/domain state implementation | `docs/ARCHITECTURE.md`, `docs/GAME_DESIGN.md` | `docs/PUZZLE_SOLVABILITY.md` if puzzle truth/validation changes |
| Mobile tap/drag/input | `docs/UX.md`, `docs/ARCHITECTURE.md` | Skill `references/mobile-interaction.md`; `docs/TOUCH_INPUT_REVIEW.md` only for related review follow-up/regression |
| Board UI/layout/presentation | `docs/UX.md`, `docs/ART_DIRECTION.md` | `docs/BOARD_PRESENTATION_REVIEW.md` or `docs/BOARD_POLISH_REVIEW.md` only when implementing/following those reviews |
| Accessibility | `docs/UX.md` | `docs/ART_DIRECTION.md` for visual treatment; affected gameplay spec if behavior changes |
| Hints / inline guidance | `docs/GAME_DESIGN.md`, `docs/UX.md` | `docs/INLINE_HINT_REVIEW.md` when working from that review; `docs/PUZZLE_SOLVABILITY.md` when hint logic uses solver deductions |
| Tutorial / onboarding | `docs/UX.md`, `docs/GAME_DESIGN.md` | `docs/TUTORIAL_REVIEW.md` when working from that review |
| Core game loop, completion, clear/new game/menu flow | `docs/GAME_DESIGN.md`, `docs/UX.md` | `docs/GAME_LOOP_REVIEW.md` when working from that review; `docs/ARCHITECTURE.md` if state ownership changes |
| Art direction / visual language | `docs/ART_DIRECTION.md` | `docs/UX.md` for usability/accessibility constraints |
| Architecture / structural refactor | `docs/ARCHITECTURE.md` | Relevant ADRs in `docs/decisions/`; affected domain/UX/testing docs only as required |
| Project structure / Unity version / repository layout | `docs/decisions/0001-unity-and-project-structure.md`, `docs/ARCHITECTURE.md` | `docs/RELEASE.md` or `docs/TESTING.md` if build/test behavior changes |
| Tests / regression coverage | `docs/TESTING.md` | Only the specification for the behavior under test |
| CI / build / lower-environment release | `docs/RELEASE.md`, `docs/TESTING.md` | `docs/ARCHITECTURE.md` for project/build structure; relevant ADRs when architecture decisions apply |
| iOS/Android signing or store preparation | `docs/RELEASE.md` | `docs/TESTING.md`; architecture/ADR only when build structure is relevant |
| Monetization / ads / IAP | `docs/MONETIZATION.md` | `docs/ANALYTICS.md` for instrumentation; `docs/UX.md` for player-facing impact; `docs/PRODUCT.md` for product fit |
| Analytics / telemetry | `docs/ANALYTICS.md` | Affected feature spec; `docs/MONETIZATION.md` if measuring monetization |
| Marketing / ASO / acquisition | `docs/MARKETING.md`, `docs/PRODUCT.md` | `docs/ANALYTICS.md` for measurable experiments |
| Roadmap / sequencing / current priorities | `planning/ROADMAP.md`, `planning/CURRENT_SPRINT.md` | GitHub Issues; `planning/MASTER_PROJECT_PLAN.md` only for scope/budget/milestone constraints |
| Scope, budget, purchases, subscriptions | `planning/MASTER_PROJECT_PLAN.md` | `docs/PRODUCT.md` if product scope is involved |
| Documentation-only change | The document being changed | Its direct source-of-truth dependency only if needed to verify consistency |

## Review documents

The following files are **review snapshots or implementation guidance**, not universal source-of-truth documents:

- `docs/BOARD_POLISH_REVIEW.md`
- `docs/BOARD_PRESENTATION_REVIEW.md`
- `docs/GAME_LOOP_REVIEW.md`
- `docs/INLINE_HINT_REVIEW.md`
- `docs/TOUCH_INPUT_REVIEW.md`
- `docs/TUTORIAL_REVIEW.md`
- `docs/GENERATION_BENCHMARK.md`

Load them only when:
- the current Issue/PR explicitly references them;
- the task implements recommendations from that review; or
- a regression needs evidence from the reviewed behavior.

Do not load all review documents to establish general project context.

## ADR routing

`docs/decisions/` contains durable architecture decisions.

Read an ADR only when the task touches the decision it governs. Current known ADR:

- `0001-unity-and-project-structure.md` — Unity/project structure and related repository architecture.

If a new architectural decision is approved, add the ADR here or extend this routing section.

## Planning context

Planning files are not default implementation context.

- `planning/CURRENT_SPRINT.md` — use for current sprint sequencing/status.
- `planning/ROADMAP.md` — use for milestone/order/prioritization.
- `planning/MASTER_PROJECT_PLAN.md` — use for approved project scope, budget, purchases/subscriptions, or milestone constraints.

Do not load `MASTER_PROJECT_PLAN.md` for routine implementation tasks.

## Source precedence

When sources conflict:

1. Explicit current owner decision in the active task/Issue.
2. Approved durable source-of-truth document or ADR.
3. Current GitHub Issue acceptance criteria.
4. Review/benchmark documents as supporting evidence.
5. Existing implementation behavior.

Do not silently resolve material conflicts. Return the conflict as a decision-needed item.

## Context expansion triggers

Broaden beyond the normal route when the task affects:

- puzzle correctness, uniqueness, or no-guess solvability;
- save-data compatibility or migrations;
- architecture boundaries or persistent state;
- monetization behavior;
- analytics schemas/events with downstream dependencies;
- release/signing/store submission;
- production credentials or production configuration;
- multiple systems with conflicting source-of-truth requirements.

Even for these tasks, expand deliberately rather than reading the entire repository.

## Context minimization rules

- Do not read all of `docs/` by default.
- Do not read all GitHub Issues or PRs to reconstruct history.
- Do not read `planning/MASTER_PROJECT_PLAN.md` unless its scope is relevant.
- Do not load review snapshots without a task-specific reason.
- Do not reread unchanged documents within the same task.
- Do not paste large source documents into subagent prompts.
- Delegate with issue/PR number, objective, relevant path, and explicit question.
- Prefer repository links/paths over copied requirements.
- Prefer the smallest source set that can answer the current decision correctly.
- If one source resolves the question, stop loading context.
