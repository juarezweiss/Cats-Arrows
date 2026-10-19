# Paws — Master Project Plan

**Version:** 1.1 — current-state reconciliation

**Prepared:** 2026-10-19

**Status:** Proposed reconciliation; effective after owner-approved merge of Issue #61

**Prior approval:** version 1.0 approved 2026-10-08

**Owner:** Project owner; Codex coordinates tracked implementation and review

**Currency:** USD

**Pre-revenue cash ceiling:** $500

**Owner availability assumption:** approximately 1–2 hours daily, or 7–14 hours weekly

## 1. Product direction

Paws is a calm, compact mobile deduction game. Players infer hidden paws from
exact row/column totals and eight-direction arrows, mark impossibilities, request
optional assistance and move through reproducible procedural boards without time
pressure, lives or punishment for an incorrect hypothesis.

The product is portrait-first, readable without color alone and intended for
short, thoughtful sessions. Easy 5×5, Medium 6×6 and Hard 7×7 are implemented.
The immediate business goal is learning from a controlled iOS trial, not assuming
revenue or delaying feedback for speculative systems.

## 2. Authority and decision precedence

Use current owner-approved GitHub Issues/PRs first, then the current `develop`
implementation, then [PRODUCT](../docs/PRODUCT.md) and
[GAME_DESIGN](../docs/GAME_DESIGN.md). Consult
[UX](../docs/UX.md), [ARCHITECTURE](../docs/ARCHITECTURE.md),
[MONETIZATION](../docs/MONETIZATION.md), [RELEASE](../docs/RELEASE.md) and
[TESTING](../docs/TESTING.md) only for their governed areas. This plan owns
strategic scope, sequencing, budget and approval boundaries; detailed rules and
technical contracts stay in those sources and their Issues.

Merged owner decisions supersede older descriptions that still use Cats & Arrows,
an Easy-only MVP, explicit difficulty buttons, an unexplained pre-playable state
or estimates for work already complete. Historical `CatsAndArrows` namespaces,
assemblies, folders, persisted keys and fingerprint identifiers remain where
compatibility requires them; they are not player-facing branding.

If sources conflict materially and no later approved decision resolves them, stop
and request an owner decision rather than silently extending scope.

## 3. Current baseline

| Area | Status on `develop` | Evidence / remaining limit |
| --- | --- | --- |
| Brand and semantics | Complete | Paws title, paw artwork, en/pt-BR/es copy, internal gameplay terminology and repository links merged in #53/#55/#57/#59. Human physical-iPhone appearance review of the current branding remains pending. |
| Puzzle foundation | Complete | Deterministic immutable model, eight-direction clues, human deduction, independent uniqueness and versioned replay/generation are integrated. |
| Difficulty profiles | Complete for current rules | Easy 5×5, Medium 6×6 and Hard 7×7 are generated and independently admitted. Medium/Hard pruning and Hard's no-hatch policy are merged (#32/#35/#37). Labels are not a claim of calibrated perceived difficulty. |
| Core game loop | Complete | Tap cycle, axis-locked drag, protected cells, immediate clue-derived Hint, Clear/Menu, exact-paw win and completed-board lock are integrated. |
| Onboarding and replay | Complete | Fixed Easy guided/independent tutorial; first-ever Play gate; random Easy/Medium/Hard selection with persistent two-repeat limit (#26/#43). Unfinished boards are not saved after termination. |
| UX polish | Complete for current tracked scope | 350 ms arrow-path hold, six-second Hint attention and timed ordinary-board celebration/static return are merged (#47/#48/#44). Broader device comfort remains trial evidence, not an inferred pass. |
| Monetization foundation | Implemented but inactive | Approved provider-agnostic policy, durable state and SDK-free services/mocks are merged (#29/#30). Default play is free with direct unlimited Hint. No provider, SDK, store product, price, charge or production activation exists. |
| iOS tester readiness | Open | #21 still requires durable identity/version/build, clean trial artifact, final dependency/data inspection, beta metadata and explicit approval before enrollment/distribution. No TestFlight trial is complete. |
| Player feedback | Open | #22 contains one unversioned qualitative session. Additional sessions, repeated-obstacle synthesis and owner selection of follow-up work remain pending. |
| Android | Deferred | Physical-device validation is open in #28 and does not block the approved iOS-first path. |

The latest behavior-changing baseline passed Unity 6000.3.25f1 compilation and
473/473 EditMode tests in PR #56. Later merged work through `5b9e06b` is
documentation-only. Automated, desktop and build/install evidence does not replace
the required owner-performed physical iPhone interaction and appearance review.
No production release is implied by integration into `develop`.

## 4. Current delivery scope

### Included for the first controlled iOS trial

- Current Paws branding and paw semantics in English, Brazilian Portuguese and Spanish.
- Easy, Medium and Hard procedural boards admitted by the documented deterministic
  solvability and uniqueness contracts.
- First-play tutorial and the current persistent random-difficulty Play/Next policy.
- Portrait board, accessible shape-based states, tap/drag interaction, Hint, Clear,
  Menu, completion celebration and completed-board review.
- A versioned, reproducible iOS tester build tied to a commit, with diagnostic and
  automated review modes excluded.
- Human physical-iPhone review and a small qualitative friend trial recorded in
  #21/#22 before public-release preparation.

### Not required for that trial

- Accounts, cloud sync, multiplayer, competitive leaderboards or a live-ops backend.
- Persistent unfinished-board resume, calibrated difficulty scoring, daily puzzles
  or more languages.
- Real ads/IAP, an analytics SDK, paid acquisition or production monetization.
- Final art, broad device coverage or speculative hardening unrelated to a found risk.

Background/foreground retains the in-memory board and cancels unsafe interaction;
process termination may lose an unfinished board. This is an explicit trial
limitation, not an untracked promise to add persistence.

## 5. Delivery gates

| Gate | Required evidence |
| --- | --- |
| Puzzle/gameplay integrity | Existing deterministic validation, human-solvability, uniqueness, exact-paw victory and interaction invariants continue to pass for all affected profiles. |
| Clean tester build | Approved identity/version/build, one intended game entry point, diagnostics excluded, artifact tied to source and final SDK/capability/data behavior inspected. |
| Physical iPhone review | Owner reports explicit pass/fail and scope for current branding, readability, touch, lifecycle and task-specific interactions. Technical build/install/launch checks alone are insufficient. |
| Small friend trial | Versioned build delivered through an approved route; responses recorded without invented data; progress blockers triaged into focused Issues. |
| iOS release candidate | Trial evidence reviewed, final declarations/materials prepared, known release blockers resolved and rollback/source provenance recorded. |
| Public iOS release | Owner explicitly approves production merge, signing/submission and release in the current task. |
| Android release | Separate #28 physical-device acceptance plus Android-specific release work and owner approval. |
| Monetization activation | Separate provider/product/pricing/privacy decision, sandbox and lifecycle validation, kill-switch/rollback readiness and explicit production approval. |

## 6. Strategic sequence and forecasting

1. Complete #21 clean-build readiness and the pending current-Paws human iPhone review.
2. Run the small #22 trial; distinguish repeated blockers from one-person preferences.
3. Let the owner select the smallest focused improvement or proceed to release-candidate work.
4. Prepare public iOS materials and submission only after the trial gate.
5. Revisit Android, persistence, calibration or monetization through separate Issues
   after evidence establishes which one matters next.

The former 65–120-hour / 7–14-week forecast described work that is now mostly
merged and is retired. No replacement calendar or remaining-hours total has been
approved. Estimate each newly selected milestone from a current Issue and current
evidence; enrollment, tester availability and store review are external elapsed-time
dependencies rather than engineering completion claims.

## 7. Cash budget and spending rules

**The approved pre-revenue incremental-cash ceiling remains $500.** It is a cap,
not a spending target or authorization. The existing planning envelopes remain:

| Envelope | Maximum allocation |
| --- | ---: |
| Apple developer membership | $100 |
| Optional art/audio/store assets | $75 |
| Optional native-language review | $50 |
| Device access or storage | $75 |
| Incremental AI/tools/static hosting | $75 |
| Later Google Play registration | $25 |
| Contingency/early maintenance | $100 |
| **Total ceiling** | **$500** |

The Mac/iPhone and existing shared tools are treated as owned/sunk unless the
owner explicitly changes that accounting. New memberships, renewals, upgrades,
usage charges, assets, contractors, hosting or storage count against the ceiling.
No project purchase or subscription is confirmed in the ledger by this update.
Vendor price and eligibility must be rechecked when a concrete purchase is proposed.

**Ask for and receive explicit owner approval before every purchase or subscription,
including every renewal, upgrade and incremental usage charge.** Plan approval,
an envelope or a prior similar purchase never authorizes a charge. Track actual,
committed and remaining cash. If a required action would exceed the ceiling,
reduce scope, use a free alternative or request a budget revision first.

## 8. Current risks and responses

| Risk | Response |
| --- | --- |
| Trial starts from stale identity/build instructions | Resolve #21 against current Paws settings and actual archive contents; do not reuse the old difficulty-menu or Cats & Arrows checklist without correction. |
| Branding or paw symbol is unclear on a phone | Use the owner-only physical iPhone gate; record device/build and explicit scope. |
| One tester's preferences become roadmap commitments | Collect more versioned sessions, separate observations from interpretation and require owner prioritization. |
| Medium/Hard labels overstate calibrated difficulty | Preserve current proven generation profiles while treating perceived difficulty as trial evidence. |
| Touch or layout regresses on compact devices | Retain focused automated coverage and request human device review; Android remains a separate gate. |
| Monetization implementation is mistaken for activation readiness | Keep default disabled; require separate provider, store, privacy, sandbox, rollback and production approvals. |
| Unfinished progress loss harms calm play | Disclose the limitation and add save/resume only if feedback shows recurring harm. |
| Build artifacts exhaust local storage | Preserve source/current receipts and necessary artifacts; remove only reproducible intermediates through tracked cleanup. |
| Scope or spending expands before learning | Use focused Issues, the $500 cap and per-charge approval; do not bundle unrelated follow-ups. |

## 9. Product learning and later decisions

The first trial is qualitative. Observe whether players understand the goal,
totals and arrows; can use tap/drag and Hint; notice completion; read the paw
symbol; and voluntarily begin another board. Record build/device/language and any
coaching. Do not infer market-wide retention, demographic preference, calibrated
difficulty or revenue viability from a small sample.

After the trial, the owner may choose one focused next step: fix a repeated blocker,
refine visual/onboarding behavior, prepare release, add persistence, calibrate
difficulty, resume Android, or scope a monetization integration. None is automatic.
Analytics or paid acquisition require a defined learning question, success measure,
spend cap and stop condition before approval.

## 10. Ownership and production control

- The owner approves product tradeoffs, each charge, release readiness, production
  merges, signing/submission and monetization activation.
- GitHub Issues are authoritative work items; focused branches/PRs target `develop`.
- `develop` is lower-environment integration. `main`, release tags, stores, live
  traffic and production configuration remain human-controlled.
- Never force-push `main`/`develop`, delete release tags or put credentials,
  signing material, provider keys or store secrets in Git.
- Prefer reversible configuration/adapters and preserve puzzle seeds, versions and
  evidence needed for rollback or reproduction.

## 11. Superseded planning assumptions

This version explicitly retires these older planning statements:

- **Cats & Arrows / hidden cats** → **Paws / hidden paws** for current product semantics.
- **Easy-only current MVP** → all three implemented sizes/profiles in current Play.
- **Difficulty selection menu** → persistent random Play/Next selection with a
  two-repeat limit and first-Play tutorial.
- **Board/input/loop/tutorial/difficulty work pending** → merged milestones.
- **Subtle inline-only completion** → current ordinary-board timed celebration
  plus return to the same static completed board.
- **Monetization design only** → SDK-free architecture/services are implemented,
  tested and disabled; real integration/activation remains future work.
- **Pre-playable remaining-effort forecast** → retired; new work is estimated from
  current tracked scope.

This reconciliation preserves the approved $500 ceiling, per-purchase approval,
iOS-first delivery, deferred Android gate and all production-safety controls. It
does not authorize a purchase, enrollment, upload, invitation, production merge,
store submission, live monetization or release.
