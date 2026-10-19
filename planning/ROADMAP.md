# Paws Roadmap

Reconciled against `develop` and owner-approved Issues/PRs on 2026-10-19. Scope,
budget and production gates follow the [Master Project Plan](MASTER_PROJECT_PLAN.md).
Every purchase or subscription still requires separate explicit owner approval.

## Completed — product foundation and playable loop

- Unity 6.3 LTS project, deterministic puzzle model, human-style solver,
  exhaustive uniqueness verification and reproducible generation (#1–#5, #14).
- Portrait mobile board, safe-area layout, tap cycling, axis-locked drag marking,
  immediate clue-derived Hint, Clear/Menu, exact-paw victory and completed-board
  lock (#6–#8, #19, #23).
- English, Brazilian Portuguese and Spanish presentation plus a fixed Easy
  guided/independent first-play tutorial (#26).

## Completed — difficulty, replay and player-facing polish

- Easy 5×5, Medium 6×6 and Hard 7×7 generation with profile-specific rules,
  deterministic pruning and independent solvability/uniqueness admission
  (#32, #35, #37).
- `Play` and `Next board` choose among all three difficulties, prevent more than
  two consecutive successful starts in one difficulty and persist that history;
  first-ever Play routes through the tutorial (#43).
- Hold-to-highlight arrow paths, six-second non-moving Hint attention and the
  timed ordinary-board completion celebration with static-board return
  (#47, #48, #44).
- Player-facing and gameplay terminology is **Paws** with paw artwork, display
  name and icon; current README/repository links are aligned. Compatibility-sensitive
  `CatsAndArrows` project identifiers remain intentionally unchanged
  (#53, #55, #57, #59).

## Completed — monetization foundation, inactive by default

- Provider-agnostic policy and ADR for every-fourth-completion opportunities,
  rewarded hints and one-time Remove All Ads ownership (#29).
- SDK-free coordinator, durable state, purchase/ad ports, unavailable-provider
  defaults and Editor/development-only no-charge mock flows are integrated (#30).
- No provider, ad/IAP SDK, product, price, store configuration, live charge or
  production traffic exists. Normal builds remain free with direct unlimited Hint.

## Current milestone — iOS tester readiness and first trial

- Finish the still-open clean-build and distribution-readiness work in
  [#21](https://github.com/juarezweiss/Paws/issues/21): durable bundle/version/build
  identity, trial-only build contents, final SDK/data inspection, beta metadata
  and enrollment/signing prerequisite review.
- Complete human physical-iPhone appearance review of the Paws branding and paw
  symbol; build/install/launch evidence does not satisfy that gate.
- Continue the qualitative friend-trial tracker in
  [#22](https://github.com/juarezweiss/Paws/issues/22). One unversioned session is
  recorded; broader feedback, repeated-obstacle triage and a visual-direction
  decision remain pending.
- Enrollment, purchase, TestFlight upload, invitations and public release remain
  separate owner-approval gates.

## Next milestones — evidence-gated

1. Produce a versioned, reproducible iOS tester build tied to a commit and hand
   it to the owner for the required physical review.
2. Run the small friend trial and create focused follow-up Issues only for repeated
   blockers or an owner-selected improvement.
3. Prepare an iOS release candidate, store materials and accurate declarations
   after the trial; public merge/submission requires explicit owner approval.
4. Re-estimate later work from trial evidence rather than reusing the obsolete
   pre-playable-project forecast.

## Later or separately approved

- Android physical-device input/comfort validation (#28), deferred and not an
  iOS-trial blocker.
- Board save/resume, calibrated difficulty thresholds, additional locales and
  broader visual refinement when feedback justifies them.
- Real monetization-provider integration or activation, analytics and acquisition
  only through separate tracked decisions, validation and production approval.

Do not delay the first controlled trial for a backend, final art, live monetization
or speculative hardening.
