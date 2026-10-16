# ADR 0002: Provider-agnostic monetization services

Status: Owner-approved architecture; SDK-free implementation in #30; activation deferred
Date: 2026-10-14
Task: [#29](https://github.com/juarezweiss/Cats-Arrows/issues/29)

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
Enabled with unavailable adapters follows failure rules, not free unpaid hints.

Full contracts/state/flows: [MONETIZATION.md](../MONETIZATION.md).

## Consequences and alternatives

- Fake-driven deterministic tests need no native provider; mounted tests cover
  input/lifecycle/localized UI. Future hint-preparation/session-ID seams are needed;
  #30 implements those seams and SDK-free service contracts. Cached-evidence
  verification is a purchase-port responsibility rather than a separate public
  interface in the first implementation.
- Rewarded gating is a future policy change from current free hints. Unpaid
  offline hints become unavailable. Solvability and #21 free trial remain intact.
- Client-only cache is not fraud-proof; reinstall requires restore. No counter
  cloud sync or persistent boards. Kill before commit/reward notification can lose
  unconfirmed state; favor honoring durably confirmed rewards.
- Replacing an IAP adapter migrates/reverifies the same product; changing ad
  providers never resets ownership.
- Reject SDKs/PlayerPrefs ownership flags in GameSession, cadence based on starts
  or reset at restart, victory ads and accumulated offline ad debt. Defer server
  accounts, remote config, analytics SDKs and board save/resume.
