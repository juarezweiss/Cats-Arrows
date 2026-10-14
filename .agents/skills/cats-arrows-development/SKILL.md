---
name: cats-arrows-development
description: Develop, review, test, plan, or release work for the Cats & Arrows Unity mobile puzzle repository. Use for feature implementation, bug fixes, puzzle generation/solver changes, mobile input and UI work, game-design changes, testing, task decomposition, PR preparation, release planning, analytics, monetization, or marketing work that must follow this repository's traceability, solvability, accessibility, context-hygiene, and production-safety rules.
---

# Cats & Arrows Development

## Operating model
- Treat root `AGENTS.md` as global governance and safety policy.
- Treat the current GitHub Issue or PR as the primary task packet.
- Use `docs/CONTEXT_MAP.md` to select task-specific context. Do not read all project documentation by default.
- Use this Skill as a workflow/router, not as a copy of project knowledge.
- Use `.codex/agents/` for role-specific behavior. Do not duplicate specialist instructions here.
- Keep durable decisions in Issues, source-of-truth docs, ADRs, tests, or code rather than conversation history.

## Workflow
1. Read root `AGENTS.md` and the current Issue/PR.
2. Consult `docs/CONTEXT_MAP.md`; load only the sources required for this task.
3. Confirm acceptance criteria from repository state. Ask only about unresolved decisions that cannot be derived safely.
4. Delegate only when specialist expertise, independent review, or parallel analysis materially improves the result. Prefer zero or one specialist for routine work.
5. Make the smallest coherent change that satisfies the task.
6. Add or update focused tests.
7. Run the narrowest reliable validation first, then any broader gate required by `docs/TESTING.md`, the Issue, or merge/release policy.
8. Update durable documentation only when behavior, architecture, or an approved decision changed.
9. Keep the completion report terse and follow the communication contract in `AGENTS.md`.
10. Stop before production actions unless explicitly instructed in the current task.

## Route by task

### Puzzle generation, solving, uniqueness, or difficulty
- Read `references/puzzle-workflow.md`.
- Load only the puzzle/game sources selected by `docs/CONTEXT_MAP.md`.
- Use `puzzle_engineer` for non-trivial solver/generator work.
- Use `qa_reviewer` when puzzle correctness, uniqueness, determinism, or solvability guarantees could regress.
- Never reduce context or testing at the expense of the documented solvability contract.

### Unity gameplay or state changes
- Route context through `docs/CONTEXT_MAP.md`.
- Use `unity_engineer` when implementation is non-trivial.
- Keep domain truth outside presentation code where required by repository architecture.
- Use focused tests before broad Unity validation.

### Mobile input, gestures, or interaction
- Read `references/mobile-interaction.md`.
- Route additional UX/architecture context through `docs/CONTEXT_MAP.md`.
- Use `unity_engineer`; add `ux_designer` only when interaction or accessibility decisions need independent review.

### UI, UX, accessibility, or art-direction work
- Route only the relevant UX/art sources through `docs/CONTEXT_MAP.md`.
- Use `ux_designer` for non-trivial player-facing interaction/accessibility decisions.
- Do not involve unrelated specialists for visual/local changes.

### Product or game-design change
- Use `game_designer` when the task changes rules, progression, difficulty intent, or player-facing behavior rather than merely implementing an approved decision.
- Record approved rule changes in the appropriate source-of-truth document before or with implementation.
- Do not bury new game rules only in code or conversation history.

### Bug fix, refactor, or maintenance
- Prefer direct execution without delegation when the issue is local and acceptance criteria are clear.
- Reproduce bugs before fixing when practical.
- Add regression coverage when behavior failed previously.
- Avoid loading unrelated design/product context.

### Monetization
- Route context through `docs/CONTEXT_MAP.md`; load `docs/MONETIZATION.md` only when relevant.
- Use `monetization_designer` for product/economy/ad decisions, not routine adapter implementation.
- Keep provider SDKs behind adapters and do not activate production monetization.

### Marketing or growth
- Route context through `docs/CONTEXT_MAP.md`; load only the relevant marketing/analytics sources.
- Use `growth_marketer` for explicit growth/ASO/acquisition work.
- Tie recommendations to testable hypotheses and measurable metrics.

### Release, CI, signing, or build pipeline
- Route context through `docs/CONTEXT_MAP.md`; load release/testing sources as required.
- Use `release_engineer` for non-trivial pipeline/release work.
- Preserve human control of `main`, store submission, production credentials, and production activation.
- Broaden context deliberately for signing/release work; do not optimize tokens at the expense of release safety.

### Review-only work
- Read the PR/Issue, changed files, and directly relevant tests/docs.
- Do not load the entire repository context unless the change crosses multiple domains.
- Report findings that affect correctness, acceptance criteria, risk, or maintainability; omit narration and generic praise.

## Delegation discipline
When delegating, send only:
- objective;
- Issue/PR number;
- relevant paths, if known;
- explicit question or expected output.

Do not paste large requirements, repo history, or documents into a subagent prompt when the agent can retrieve them from the repository.

Use additional specialists only when they provide distinct value. Avoid multi-agent fan-out for routine implementation, small UI changes, simple bugs, documentation edits, or mechanical refactors.

## Context discipline
- Prefer repository state over conversational recollection.
- Reuse context already loaded during the current task when still current.
- Do not summarize files merely because they were read.
- Do not restate Issue acceptance criteria in plans or completion reports unless a status distinction matters.
- Link/reference durable sources instead of duplicating them.
- Start fresh task context after major milestones when prior decisions are already persisted.
- Expand context whenever uncertainty could affect puzzle correctness, architecture, persistence, monetization, analytics, release/signing, or other high-risk decisions.

## Definition of done
Treat a task as done only when:
- acceptance criteria are satisfied;
- relevant focused tests pass, or unavailable infrastructure is explicitly reported;
- required broader gates pass before merge/release;
- puzzle invariants remain valid when affected;
- player-facing accessibility/interaction impact is considered when affected;
- durable docs/ADR are updated only when behavior or architecture changed;
- Issue/PR traceability is present;
- meaningful risk/rollback information is captured where needed;
- no production boundary was crossed without explicit human instruction.
