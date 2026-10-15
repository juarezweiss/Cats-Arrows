# Roadmap

Scope and gates follow the [approved Master Project Plan](MASTER_PROJECT_PLAN.md) (2026-10-08).
Every purchase/subscription requires separate explicit owner approval.

## Completed foundation
- Unity/repository bootstrap (#1), immutable puzzle model (#2).
- Human-style solver, exhaustive uniqueness and localization foundation (#3).
- Deterministic bounded Easy generator (#4 / merged PR #12).

## M1 — Playable Easy MVP
- Session rules (#5 / merged PR #13).
- Eight-direction arrows (#14 / merged PR #15), native tests and phone timing.
- Portrait board (#6), taps and horizontal/vertical drag (#7): iPhone verified; Android verification deferred.
- Clear/New Game/Menu, unlimited immediate Hint, narrow cardinal protection, immediate win and completed-board review (#8/#23).
- English, Brazilian Portuguese and Spanish with language selection.
- Owner iPhone loop/visual tests passed; polish integrated in PR #20.
- Medium 6×6 / Hard 7×7 and menu selection (#32 / merged PR #33); native 285/285
  tests passed. Fixed Easy tutorial unchanged; owner Medium/Hard iPhone 17 smoke
  and all three languages passed. Smaller phones and measured Medium/Hard latency remain unverified.
- Prepare a small iOS tester trial (#21); enrollment/distribution are separate approval gates.
- Prepare public iOS release for explicit owner approval.

## Later, guided by feedback
- Android development and its own physical-device checks (#7), explicitly deferred by owner on 2026-10-09.
- Measured difficulty, lightweight save/resume, additional languages.
- Optional monetization experiment under a separate approved scope and budget.
- Analytics or acquisition only when needed for a defined learning question.

Do not delay the first player trial for final art, a backend, speculative hardening or optional SDKs.
