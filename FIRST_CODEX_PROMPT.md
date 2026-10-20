# First Codex Task

Use this after the bootstrap has been committed and `develop` exists:

> Work on GitHub issue #3 first. Act as the orchestrator/PM defined by AGENTS.md. Read docs/GAME_DESIGN.md, docs/PUZZLE_SOLVABILITY.md, docs/ARCHITECTURE.md, and the paws-development skill. Delegate a design pass to `game_designer` and a solver architecture pass to `puzzle_engineer`; have `qa_reviewer` challenge the resulting rules/edge cases. Wait for those read/reasoning results, then implement the smallest deterministic human-style solver foundation and exhaustive uniqueness verifier needed by #3, with EditMode tests. Do not work on UI, monetization, marketing, or production/release actions. Do not merge to main. Finish with a review-ready summary containing files changed, tests run, unresolved deduction rules, risks, and the recommended next task.

Why #3 before the random generator: generation quality depends on having a solver/verifier that can reject ambiguous or guess-dependent boards. The existing domain contracts from #2 are already bootstrapped and should be reviewed/extended as needed rather than rewritten casually.
