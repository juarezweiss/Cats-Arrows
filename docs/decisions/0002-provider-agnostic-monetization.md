# ADR 0002: Provider-agnostic monetization services

Status: Owner-approved architecture; rewarded-Hint policy amended 2026-10-19;
monetized production preparation authorized 2026-10-24 in #67; live activation deferred
Date: 2026-10-14
Task: [#29](https://github.com/juarezweiss/Paws/issues/29)

Owner approval: 2026-10-14, explicit “approved” following the completed design.
Approval covers the documented architecture and policy decisions. Implementation,
SDK/store configuration, spending and production activation remain separate work.
On 2026-10-14 the owner authorized SDK-free implementation, persistence/tests and
a PR to develop, tracked in #30. Real integration and production remain deferred.

## Context

Owner requests an ad every fourth completed ordinary board, rewarded hints and a
one-time Remove All Ads entitlement covering both. Cadence and ownership survive
restarts; ownership restores. Current free MVP and production boundaries remain;
no SDK or store configuration is authorized.

## Decision

Pure-C# policy/application coordinators use injected ad, purchase/evidence, atomic
state, hint-application, clock and diagnostics ports. Core/Gameplay never depend
on Monetization or vendors. Bootstrap composes replaceable platform adapters;
only these adapters reference SDKs.

Persist a versioned envelope with completion/deduplication watermarks, handled
fourth-board milestone, verified ownership, store finalization and at most one
unused confirmed reward authorization. Tutorial wins do not count. Preserve
immediate victory/solved-board review; consume a due opportunity before attempting
an ad on deliberate next-board intent. Skip unavailable ads without debt. Rewarded
hints do not alter cadence, even when the fourth win comes from a rewarded hint.

Prepare an eligible hint before a reward. Deliver once after confirmed reward and
safe dismissal with board/request checks. Recover an unused earned authorization
on a later explicit Hint, never automatically on another board. Owned bypasses
both placements and changes `Hint (ads)` to `Hint`.

Stores are ownership authority; verified cache supports offline use. Purchase and
restore share durable idempotent grant/finalization. Pending/transport errors never
grant or revoke; verified revocation is respected. Restore stays visible. Cross-
store ownership/accounts/backends are out of scope.

Disabled configuration remains default, preserves direct free hints and hides
purchase UI without initializing providers. Rollback retains purchases/counters.
Enabled with unavailable rewarded-ad infrastructure grants an eligible unowned
Hint directly for free; a voluntarily closed/cancelled presented ad grants no Hint.

Full contracts/state/flows: [MONETIZATION.md](../MONETIZATION.md).

## Amendment: rewarded-Hint infrastructure fail-open (2026-10-19)

The owner explicitly approved this policy change on 2026-10-19. It supersedes the
original decision that an unavailable rewarded ad leaves an unpaid Hint unavailable.

For an unowned player, an eligible Hint requires confirmed rewarded completion
when an ad can actually be served. Offline, no-fill, provider unavailable,
initialization failure, restriction, timeout before presentation or equivalent
infrastructure failure instead grants the eligible Hint directly for free, without
retry, accumulated ad debt or blocking. If an ad was successfully presented and
the player voluntarily cancels or closes it before earning the reward, no Hint is
granted. Confirmed reward still authorizes exactly one Hint.

Existing hint preparation, board/session/revision revalidation, durable reward
deduplication, safe native-closure handling and progression-ad cadence remain
unchanged. A plain close without reward is not reclassified as infrastructure
failure. This amendment changes policy documentation only; provider implementation
and production activation remain separately authorized work.

## Development implementation #66 — 2026-10-23

The owner authorized the AdMob/UMP adapter and implementation of the approved
fail-open amendment in #66. After bounded SDK inspection, the owner explicitly
approved development-only nonpersonalized Google test ads, no ATT prompt,
first-party ID and SDK crash reports disabled, and no dependency-manager telemetry.
[SDK evidence, exact versions and remaining production decisions](../ADMOB.md).
The existing ports, coordinator and persistence remain authoritative. Default
production composition stays disabled; this does not authorize live traffic.

## Production preparation amendment — 2026-10-24

The owner explicitly authorizes #67 to prepare Paws 1.0 with Apple IAP/Offer Codes
and AdMob/UMP enabled, reusing the existing ports, persistence and SDK pins.
Build profiles select actual production providers, integrated real-IAP/Google-test-
ad validation, or disabled rollback. Only verified current Apple entitlement grants
ownership; production also accepts Apple's Sandbox environment for App Review and
TestFlight, while Xcode is confined to validation/review. No mock grants are used.
The one #82 restart-button limitation is accepted as non-blocking; paid entitlement,
Restore, finalization and no-false-grant requirements remain mandatory.
[Current release criteria](../RELEASE_RC_1_0.md) govern evidence and remaining owner
privacy/configuration decisions. This changes preparation scope, not authority to
activate live ads, change accounts, merge main or distribute/release.

## Consequences and alternatives

- Fake-driven deterministic tests need no native provider; mounted tests cover
  input/lifecycle/localized UI. Future hint-preparation/session-ID seams are needed;
  #30 implements those seams and SDK-free service contracts. Cached-evidence
  verification is a purchase-port responsibility rather than a separate public
  interface in the first implementation.
- Rewarded gating is a future policy change from current free hints when an ad can
  be served. Infrastructure-unavailable Hints fail open to direct free delivery;
  voluntary no-reward closure does not. Solvability and #21 free trial remain intact.
- Client-only cache is not fraud-proof; reinstall requires restore. No counter
  cloud sync or persistent boards. Kill before commit/reward notification can lose
  unconfirmed state; favor honoring durably confirmed rewards.
- Replacing an IAP adapter migrates/reverifies the same product; changing ad
  providers never resets ownership.
- Reject SDKs/PlayerPrefs ownership flags in GameSession, cadence based on starts
  or reset at restart, victory ads and accumulated offline ad debt. Defer server
  accounts, remote config, analytics SDKs and board save/resume.
