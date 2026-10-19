# Current Sprint — iOS tester readiness and first trial

**Primary work:** [#21](https://github.com/juarezweiss/Paws/issues/21) and
[#22](https://github.com/juarezweiss/Paws/issues/22)

**Source baseline:** `develop` at `5b9e06b` (2026-10-19)

**Status:** playable Paws feature work is integrated; distribution and broader
trial evidence remain pending.

## Current product baseline

- Paws is a portrait iOS-first logic game with Easy 5×5, Medium 6×6 and Hard 7×7
  procedural boards. `Play`/`Next board` choose among the three modes with a
  persistent two-repeat limit; first-ever Play uses the fixed Easy tutorial.
- The complete loop includes tap/drag marking, Clear/Menu, immediate unlimited
  Hint, arrow-path hold, idle Hint attention, exact-paw completion, celebration
  and return to the locked completed board in English, pt-BR and Spanish.
- SDK-free monetization architecture and persistence are merged, but normal
  composition is disabled and ad-free. Only explicit Editor/development mock
  review flows exist; no real provider or store configuration is present.
- The latest behavior-changing baseline passed pinned compilation and 473/473
  EditMode tests in PR #56. Later PRs #58/#60 were documentation-only.
- The owner reviewed and approved the current Paws physical-iPhone appearance on
  2026-10-19; this closes the pending PR #54 branding appearance gate.

## Sprint outcomes still required

- [ ] Confirm durable bundle identifier, semantic version/build number and a
  reproducible clean iOS tester build containing only the intended game path.
- [ ] Reinspect the final archive's SDKs, capabilities and data behavior; prepare
  accurate beta metadata and privacy/export answers without inventing contacts.
- [x] Record owner approval of the current Paws physical-iPhone appearance,
  including branding, paw readability and localized visual fit.
- [ ] After trial-build settings change, hand the clean build to the owner for
  task-specific interaction/lifecycle review. Smaller-phone comfort and measured
  Medium/Hard latency remain unverified.
- [ ] Present the exact enrollment/distribution step for explicit approval before
  any purchase, upload or invitation.
- [ ] Collect additional versioned friend-trial responses in #22, summarize only
  repeated evidence and open focused follow-ups after owner prioritization.

## Boundaries

- No Apple Developer Program purchase, subscription, TestFlight upload, tester
  invitation, production merge, tag, store submission or release is authorized.
- Android physical validation remains deferred in #28 and does not block this sprint.
- Real ads/IAP, analytics SDKs, board save/resume, difficulty recalibration and
  speculative feature work are outside this sprint.
- The first recorded friend session is useful qualitative evidence, not proof of
  release readiness, visual direction or calibrated difficulty.
