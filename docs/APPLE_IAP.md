# Apple Remove All Ads adapter — issue #64

Authority: [#64](https://github.com/juarezweiss/Paws/issues/64), [#29](https://github.com/juarezweiss/Paws/issues/29),
[#30](https://github.com/juarezweiss/Paws/issues/30), [ADR 0002](decisions/0002-provider-agnostic-monetization.md).
Dependency #63 is integrated in develop (`10695b018c71bad7bb07b6346b58f99f77d754ee`).
The owner approved permanent non-consumable ID **`com.jweiss.paws.removeads`** on
2026-10-20 in the implementation chat. This approves the mapping only. Price,
storefront localizations, account/product creation and activation remain owner actions.

## Implementation and rollback

`CatsAndArrows.Purchasing.Apple` alone references Unity IAP/StoreKit. Its public
`ApplePurchaseService` implements the existing `IPurchaseService`; the injected
mapping pairs logical `RemoveAllAds` with the approved Apple ID. It never consumes
the product. Core, Gameplay, coordinator, state format and Settings contracts are
unchanged. Existing Settings Remove Ads and Restore Purchases consume it through
the coordinator. Prices come exclusively from `Product.metadata.localizedPriceString`;
missing metadata disables buying and leaves Restore available.

Pinned **Unity IAP 5.3.1**, minimum Unity 2021.3, compiles against the repository's
**6000.3.25f1** pin. Services Core is explicitly pinned to **1.14.0**, with the full
resolved dependency graph in `Packages/packages-lock.json`. The pre-5.4 package
line avoids the later developer-data framework. No Analytics, Authentication,
Attribution, Remote Catalog, transaction-verifier backend or ads package is installed
or initialized. No `UnityServices.InitializeAsync`, catalog auto-initialization,
account token or backend is used. Services Core is a package dependency, not an
initialized service; IAP's telemetry wrappers remain unwired. Unity IAP generates
an unused Android store selector in Assets/Resources on editor load; it is ignored.
The adapter explicitly selects AppleAppStore, never the Editor FakeStore.

The production `Paws` scene/bootstrap still composes no monetization coordinator.
Installing the package includes its Apple native code/StoreKit linkage in iOS
exports; package presence does not enable purchases. Rollback keeps production
composition disabled, and retains coordinator/vault state. Review composition is
a separate, explicitly built development scene and state file. No main changes,
production activation, store upload, purchase, signing credentials or registration
are part of this implementation.

## Verification and lifecycle

Unity `PendingOrder` means paid but awaiting confirmation; `DeferredOrder` means
unpaid Ask to Buy. Neither callback alone proves verified current ownership.
IAP 5.3.1 can surface unverified purchase details and silently filter unverified
inventory entries. The supplemental Swift adapter therefore checks native
`VerificationResult.verified`, exact product, non-consumable type, app bundle,
environment, revocation and current entitlement. Decoding a JWS or an editable
coordinator boolean is never sufficient. Verification callbacks return on the
main actor/application thread. Superseded reads cannot rewrite native cache.

Verified JWS/transaction payloads stay in an adapter-private, non-synchronizing
Keychain record (`AfterFirstUnlockThisDeviceOnly`); only opaque evidence and hashed
transaction references cross `IPurchaseService`. The cache records previously
native-verified ownership for offline restart; it does not reverify a JWS using
a server. Compromised-device/process fraud protection and cross-store ownership
are outside this client-only design. StoreKit may retain transaction history
independently of the app. Uninstall removes the coordinator file but may preserve
Keychain; fresh-install ownership still goes through the store refresh/restore path.

Production accepts only production transactions. Explicit `SandboxReview` accepts
Apple sandbox/Xcode transactions with separate Keychain keys. Sandbox evidence
cannot verify a production cache reference. StoreKit/account changes that produce
unknown/partial/unverified results preserve verified access. An empty local
inventory cannot revoke a cached owner; successful explicit Restore/AppStore.sync
plus verified complete absence can. A signed refund/revocation invalidates the
cache with a durable tombstone before notifying the coordinator, preventing
resurrection through an older state-file backup. No catch-up ad debt is created.

Unpaid deferred requests cannot be enumerated using `Transaction.unfinished`.
The adapter persists a separate private deferred marker before reporting Pending,
and retains it across ambiguous empty/offline refreshes. Composition must also
pass the successfully loaded/validated coordinator state's `PendingPurchase` as
`recoveredPendingPurchase`; the review bootstrap does this before constructing the
coordinator. Either durable copy preserves pending after a single write failure;
simultaneous failure of both copies followed by a crash has no invented guarantee.
It resolves only on
verified ownership or a terminal store cancellation/failure; an absent deferred
request is not inferred cancelled. A request denied while the app was absent may
therefore require later authoritative store resolution. No daily polling/backend
is invented. Transport/verification failure preserves existing access; successful
synchronized complete absence can resolve an uncertain native buy without clearing
a separately recorded unpaid deferred request. A timeout never starts a second
native purchase sheet while the first is unresolved.

The coordinator atomically journals ownership and finalization before success/finish.
Private-evidence write failure can preserve verified in-memory access but blocks
finish. Store updates and refresh replay unfinished work; only one unfinished
transaction is exposed at a time to preserve the existing single-entry journal.
Confirmation requires both a successful SDK `ConfirmedOrder` and verified absence
from native unfinished inventory. Verified completed history makes a restart after
native finish/before journal-clear idempotent. SDK `FailedOrder`, timeout or
unavailable evidence leaves finalization retryable and ownership intact. Native
history and payloads are never copied into issue/PR logs.

## Reproduce the unsigned review export

Close this project's GUI Unity editor. From the repository root:

```bash
bash scripts/unity.sh
PAWS_APPLE_PRODUCT_ID=com.jweiss.paws.removeads bash scripts/build-apple-iap-review.sh "$PWD/Builds/Issue64-AppleIapReview"
```

The fresh export preserves #63 identity/minimum iOS/device family and uses a
development review scene with the normal shell/Settings actions. No ads service
is installed. This task preserves #30 policy; the separately approved rewarded-Hint
fail-open amendment is not implemented by this purchase adapter. State is `apple-iap-review/state.bin` under the app's persistent
directory; production and mock ledgers are separate. The build leaves a
`paws-iap-review.json` receipt and removes the generated scene. Do not use the
production archive inspector to certify this intentionally enabled development
review composition. Unsigned archive preparation, without provisioning/download:

```bash
xcodebuild -project Builds/Issue64-AppleIapReview/Unity-iPhone.xcodeproj -scheme Unity-iPhone \
  -configuration Release -destination 'generic/platform=iOS' \
  -archivePath "$PWD/Builds/Issue64-AppleIapReview.xcarchive" \
  -derivedDataPath "$PWD/.unity-results/issue64-archive/DerivedData" \
  CODE_SIGNING_ALLOWED=NO CODE_SIGNING_REQUIRED=NO \
  COMPILER_INDEX_STORE_ENABLE=NO DEBUG_INFORMATION_FORMAT=dwarf archive
```

## Owner StoreKit Test and sandbox handoff

Owner-only: create/approve the App Store Connect non-consumable with the exact ID,
price/localizations and availability; satisfy agreements/enrollment/account access;
choose local signing and sandbox account/device. Never put these inputs in Git.
No price, product text, sandbox credential or purchase is generated here.

For StoreKit Test, create a local Xcode StoreKit configuration, sync the approved
App Store Connect product or enter the owner-chosen test price/localizations, and
select that configuration for the Run scheme. Do not copy the resulting account
or transaction payloads to Git. For Apple sandbox, deselect the local StoreKit
configuration and use the owner-provisioned sandbox account. StoreKit Test and
sandbox must remain distinct evidence; neither establishes production activation.

On each target, the owner reviews:

- Settings shows Apple's localized price; Restore remains present when product
  fetch is unavailable. Cancel/failure/unverified data never grants access.
- Ask to Buy stays Pending across restart; approval grants once, denial/failure
  never grants. Ordinary taps/drag/protected cells/completed lock/Clear/New Game
  remain usable after native sheets close; EN/pt-BR/es and portrait safe areas fit.
- Buy/Already Owned/Restore grants both ad removals and direct owned hints through
  the existing coordinator; repeated taps, callbacks and resume do not duplicate.
- Interrupt after verified purchase/before confirmation, and after native finish/
  before local journal clear; restart completes recovery without another charge.
- Owned restart offline preserves access. Fresh install/restore and account switch
  reconcile correctly; network/partial results do not revoke; refund/revocation
  removes access without accumulated ad debt.
- Background/foreground/relaunch during sheets and pending/finalization recover.
  Record actual network/data behavior and inspect final embedded frameworks,
  capabilities and every privacy manifest before making privacy declarations.

Record reviewer, device/iOS, source SHA, version/build, Unity/package/Xcode versions,
StoreKit Test versus sandbox, scenario pass/fail and gaps. Physical interaction and
purchase/restore acceptance stays **pending** until the owner's explicit report;
unsigned export/archive or automated tests cannot satisfy it. No real charge needed.

## Archive gate status — 2026-10-20

Final clean unsigned development export: source **`46bee5e`**, Unity 6000.3.25f1 /
Xcode 27.0, retained at `Builds/Issue64-AppleIapReview/` (ignored). The
[review receipt](evidence/issue64/apple-iap-review-receipt.json) records the package
lock, resolved identity and actual frameworks/capabilities/privacy manifest, plus
the final automated test evidence. It includes the recovered-Pending correction.

The owner explicitly selected **leave the archive gate pending** after local disk
exhaustion. Native Swift and IL2CPP compilation were reached, but static-archive
assembly/native linking exhausted disk space; a superseded retry was interrupted
for the pending-state correction. This is not a successful archive gate. Logs
and the initial export inventory are retained under ignored
`.unity-results/issue64-archive/`. Failed task-only intermediates were removed;
existing owner-tested builds were preserved. Free at least 5 GB before a later
archive attempt. StoreKit Test/sandbox and physical purchase/restore acceptance
remain owner actions. Keep the PR draft until these validation gates are resolved.
