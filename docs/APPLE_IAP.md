# Apple Remove All Ads adapter — issue #64

## Current release decision — 2026-10-24

Paws 1.0 preparation in #67 enables this real adapter alongside Offer Codes and
AdMob/UMP. Only #82's pending purchase button becoming available again after restart
is accepted as non-blocking; #82 remains open for post-release correction. No
Apple/Unity root cause or universally harmless repeat request is established.
All verified-entitlement, paid-grant, persistence/Restore, no duplicate owned
charge, finalization and no-false-grant requirements remain release gates.
[Current criteria and environment-specific human checklist](RELEASE_RC_1_0.md)
supersede the historical blanket blocker/production-disabled statements below;
immutable receipts and previous failed/inconclusive observations are unchanged.

The production verifier accepts verified Apple production and Sandbox current
entitlements for App Review/TestFlight. Local Xcode evidence remains restricted to
the validation/review namespace. Neither PendingOrder, unfinished/history alone
nor a decoded JWS establishes ownership. SKU and pinned SDKs remain unchanged.

Authority: [#64](https://github.com/juarezweiss/Paws/issues/64), [#29](https://github.com/juarezweiss/Paws/issues/29),
[#30](https://github.com/juarezweiss/Paws/issues/30), [ADR 0002](decisions/0002-provider-agnostic-monetization.md).
Dependency #63 is integrated in develop (`10695b018c71bad7bb07b6346b58f99f77d754ee`).
The owner approved permanent non-consumable ID **`com.jweiss.paws.removeads`** on
2026-10-20 in the implementation chat. On 2026-10-21 the owner reported the product
saved in App Store Connect as **Prepare for Submission**, with price/localization
configured and Offer Codes enabled. These are owner-reported account facts;
submission and production activation remain unauthorized.

## Owner-authorized synchronized installation — 2026-10-22

The owner requested installing the synchronized build before finalizing #79/#64.
The fresh app from **`275b20a27a122da1a7f04a5c6446b0933cb72221`** was signed
with the existing valid local development profile and installed in place on
**iPhone 17 / iOS 27.0.1**. Strict app/framework signature checks, profile device
membership/expiry, unchanged unsigned-source hash, matching binary UUIDs and
installed **Paws / com.jweiss.paws / 0.1.0 (1)** identity passed. No old app was
rebuilt or copied into the new export. [Sanitized install receipt](evidence/issue64/develop-sync-device-install.json).

This supersedes the earlier unsigned/not-installed preparation status below.
No app data/history reset, launch, physical interaction or purchase test followed.
The profile expires **2026-10-27 17:36:44 UTC**; this is local development signing.
Production composition remains disabled; #82 remains the hard Paws 1.0 release
gate. Existing exact-input compile/test/native-build/QA evidence is reused for
owner-authorized foundation integration; no speculative debugging or scope growth.

## Latest develop synchronization — 2026-10-22

Active #64/PR #79 now includes authoritative develop
`a4251605878b5d4d1fd169962344ddfd58bf5d8c` (including #80/#81 branding).
Merge commit: `2603b2561b07d362e1708cfbfa918bb736422fa5`.
Review/test build source: **`275b20a27a122da1a7f04a5c6446b0933cb72221`**.
All non-#64 app/game/UI/assets/scenes/ProjectSettings/existing tests match that
develop tree exactly; only intentional IAP package changes remain in shared
Unity files. Older #64-only menu-test fixture edits were dropped in favor of
develop. Root `AGENTS.md` now requires latest develop for future monetization work.

The production icon and MenuPaw source bytes match #80. All ten fresh exported
icon PNGs match #80's validated output bytes, all four Xcode configurations use
AppIcon, and the current MenuPaw resource is packed. The review generator reads
the current production panel and shared `GameShellView`; no old review asset,
scene or ProjectSettings was copied forward. Previous signed builds and receipts
remain historical evidence and must not be used as this synchronized candidate.

Pinned compile passed; focused Apple/build/branding/menu tests **68/68**, full
EditMode **562/562** and Python contracts **51/51** passed, with successful exits.
The fresh unsigned development review artifact and exact hashes are recorded in
[synchronization evidence](evidence/issue64/develop-sync.json). No device install,
launch, purchase, account or transaction operation followed. Production remains
disabled; #82 stays the Paws 1.0 release blocker. No main change or PR merge.

## Current scope — foundation complete; release blocker #82

Owner-directed scope split on 2026-10-22: [#64](https://github.com/juarezweiss/Paws/issues/64)
now covers the completed IAP foundation in [PR #79](https://github.com/juarezweiss/Paws/pull/79).
The unresolved deferred lifecycle/physical acceptance is tracked by
[#82 — Fix Ask to Buy / deferred IAP lifecycle](https://github.com/juarezweiss/Paws/issues/82):
**HARD BLOCKER for Paws 1.0 App Store release; not a blocker for continued
development on `develop`.** Foundation closure/integration remains subject to
normal checks and owner instructions; neither is performed by this scope update.
Production composition remains disabled. [Release checklist](RELEASE.md#paws-10-app-store-hard-release-gate--82).

The verified Sandbox Pending/unowned → relaunch/unowned/buy-enabled failure is
still unresolved. Earlier Pending → Owned remains environment/approval-unverified;
the native approval/current-entitlement sequence is inconclusive and SDK replay
finalization remains a hazard. The rejected 5.4.2 comparison stands. Direct
StoreKit 2 behind `IPurchaseService` is an option requiring separate approval,
not an implementation authorized here. No new purchase, reset or fix follows.

All sections below retain dated pre-split findings. Their historical “#64 blocked”
/ “PR #79 draft” scope decisions are superseded by this transfer to #82; the failed
observations, uncertainty, source receipts and mandatory Sandbox gates are not.
Immutable evidence files remain unchanged. No passing live lifecycle result is
implied by accepting the foundation.

<a id="current-owner-gate--bounded-investigation-2026-10-22"></a>
## Retained pre-split investigation — 2026-10-22

### Bounded IAP 5.4.2 comparison — rejected at inspection

The owner authorized one 5.4.2 comparison with a stop on unacceptable SDK/data
behavior. Official package inspection found a blocking change: the Apple purchase
service unconditionally injects a new Insights emitter. On Unity 6000.3 it sends
HTTPS purchase events without Analytics/Auth packages or UGS initialization.
Payload construction includes IDFV and can include Apple receipt/JWS/transaction
fields. `DISABLE_RUNTIME_IAP_ANALYTICS` does not guard this new transport; consent
fields do not stop sending. No supported opt-out was found in the inspected
5.4.2 source/docs. This conflicts with #64's no developer-data/backend boundary.
No runtime collection or traffic was exercised to establish this static finding.

5.4.2 requires Core 1.18.0; no new package names are required. Remote Catalog and
D2C features are present, but the fallback lookup requires an Auth access token
and project ID; absent those it fails before HTTP. Apple finish timing/failure
mapping improve, while the unpaid `.pending`/Deferred path and journal replay
auto-finalization remain. No supported restart correction was identified; **no
physical result or fix is inferred**.

**Stopped before project upgrade, compile, focused tests or build preparation.**
Pins remain IAP 5.3.1/Core 1.14.0; runtime, contracts and ownership semantics are
unchanged. Repository/whitespace checks pass. No owner purchase is requested.
[Package hashes, source references, limitations and skipped checks](evidence/issue64/iap-5.4.2-comparison.json)
preserve the comparison. #64 remains blocked / PR #79 draft. Direct StoreKit 2
behind the existing `IPurchaseService` is a possible separately scoped architecture
option; this comparison does not authorize implementing it. Future purchase gates
below remain mandatory; no reset/reinstall or further IAP debugging follows.

### Bounded desktop follow-up — no supported correction established

One deterministic diagnostic used the real adapter/coordinator, real file store
and fresh-instance startup recovery. Pending survived empty inventory even when
the controlled private marker was absent. An explicitly injected same-product
UserCancelled failure, converted by the actual IAP 5.3.1
`PurchaseFailureDescription.ConvertToFailedOrder`, then cleared the persisted
coordinator flag and enabled buying while remaining unowned. Empty failure
transaction IDs are normal SDK behavior; earlier fakes often retained `123`.

The SDK audit found no automatic terminal failure after `.pending`: it emits
Deferred and returns. No evidence places the injected cancellation in the failed
phone sequence. Thus the replay shows a possible terminal-resolution route,
**not the physical cause**; normal startup did not reproduce the unlock. Blindly
rejecting empty IDs would reject normal SDK failures. No speculative guard or
terminal-resolution policy change is justified by this replay alone.

Pinned desktop diagnostic **1/1 passed**, exit 0; exact method and source hashes
are retained in [desktop evidence](evidence/issue64/desktop-pending-investigation.json).
The temporary compiled test was restored; runtime/scripts/active test sources
are unchanged. No broad suite, device action, purchase or reset followed.
**Stop this bounded investigation; #64 remains blocked.** Missing evidence is the
actual normalized callback sequence and pending-marker/coordinator commit/reload
outcomes in the failing environment. Existing evidence, histories and both visible
Sandbox gates below are preserved; no further physical test is authorized.

### Latest owner evidence correction — real Family notification

The owner reports that members of their **real Apple Family** received an actual
Messages Ask to Buy request:

> “? asked to buy the In-App Purchase ‘Remove All Ads’ for the app ‘Paws Puzzle Game’ for R$ 29,90 from the App Store.”

The originating attempt, event time and store environment have not been correlated.
Treat earlier physical purchase/Ask to Buy attempts lacking explicit environment
checks as **environment-unverified and potentially production-account attempts**,
even when a test scheme had been selected. Do not infer that Sandbox caused the
Family notification, or infer an approval, completed purchase or charge. The quoted
price/title are reported notification text, not new approved pricing/localization.
The earlier Pending-to-Owned observation was reported without owner approval in
Xcode; that does not establish the real Family request's approval state. Account,
environment, approval and transaction correlation remain unresolved. Do not promote
that earlier observation to a proven false grant in verified Sandbox or production.

The later `a686433` attempt with the fresh tester and an Apple sheet explicitly
identifying **Sandbox** remains separate verified-sandbox evidence. Native journal
entries explicitly recording `environment: xcode` retain that measured scope;
they do not certify the environment of other physical attempts. Earlier broad
environment labels in review history must be read subject to this correction.
[Owner observation and evidence scope](evidence/issue64/owner-family-ask-to-buy-observation.json).

**No further purchase attempt is authorized now.** Before any future owner-operated
purchase test, both must be visibly verified: the intended **Sandbox Apple Account
under Settings > Developer**, and **Environment: Sandbox on the purchase sheet
itself**. If either is missing, do not initiate or approve anything. A scheme name,
development signature, simulation flag, account assumption or app status is
insufficient. Keep earlier potentially production-account attempts separate from
the explicitly verified later Sandbox attempt. This is an evidence/analysis
correction only; no product/account, purchase, approval or runtime changes follow.

**#64 remains blocked.** The one bounded native-only run returned Pending, then
reported zero current entitlements before approval, including after owner restart.
The request did appear as verified in unfinished transactions and updates; those
streams do not establish current ownership. After owner approval, Xcode displayed
Purchased, but native current entitlements remained empty, including after one
restart. No approval update arrived; unfinished went from one to zero on restart
despite this probe never calling finish. No first verified current entitlement was
observed. This run is **inconclusive**, not evidence of either requested decision.
See the [sanitized trace](evidence/issue64/native-ask-to-buy.jsonl) and
[analysis](evidence/issue64/native-ask-to-buy-analysis.json). Stop local experiments;
do not repeat reset/reinstall cycles or weaken production verification.

Static SDK review found a separate finalization hazard: IAP 5.3.1's
`ProcessLoggedPurchase` automatically finishes updates whose transaction ID exists
in its journal, bypassing the adapter's durable grant/finalization gate. That
journal has no product/environment namespace; the local fixture reused ID 0 from
an earlier successful/refunded purchase. This is a plausible integration cause,
not a causally proven explanation. Preserve the SDK journal. Its role in the
earlier environment-unverified Pending-to-Owned observation is unresolved; no
connection to the real Family notification is established.

Sandbox source `bf32a15` reached the phone; the owner reports Restore leaves
**All ads removed**, and cannot confirm whether the tester already owns the
product. That UI also preserves cached access on an unavailable restore, so this
does not establish synchronized sandbox ownership or an unowned baseline. Keep
all history intact; a known-unused Sandbox tester is required for initiation.

A separate adapter bug was reproduced deterministically: a paid update after
Restore's success callback could run `Verify(false)`, settle Restore early and
discard its synchronization flags before inventory arrived. The fix retains
Restore authority until both callbacks arrive; later paid updates supersede an
in-flight read with `Verify(true)`. Regressions cover premature completion and
stale synchronized absence versus newer verified ownership. This is not yet a
causal explanation of either physical failure or a resolution of SDK auto-finish.
Source `a686433` includes this fix: provider contracts **47/47**, full EditMode
**558/558**, pinned compilation, clean export and signed development artifact
checks pass; independent QA has no blocking findings. The exact replacement is
installed/running through **Paws Sandbox Restore Fix**, Configuration **None**,
debugger off. [Fixed sandbox receipt](evidence/issue64/sandbox-restore-fix-preparation.json)
retains its signed app/symbol provenance separately from the earlier candidate.
The owner created and signed into a fresh Sandbox tester. Restore returned the
price without Owned; the owner confirmed Apple's sheet identified **Sandbox**.
One confirmed attempt showed **Pending without Owned**. After owner force-quit
and Home Screen reopen, access stayed unowned but **Remove Ads was available for
a second purchase**. Pending-lock persistence fails. This single attempt observed
no false ownership, but sandbox acceptance fails; its cause is unproven.
**Stop physical experiments:** no second purchase, history reset or reinstall.
The native diagnostic, SDK replay hazard and sandbox restart failure prevent #64
from safely unblocking. Production verification remains unchanged.

The [native reproducer](../tools/apple-iap-probe/README.md) contains no Unity IAP or
Paws ownership/cache code. One owner-operated native purchase must return Pending;
record currentEntitlements, unfinished and updates, one pre-approval restart,
then the first verified entitlement after owner approval. The diagnostic records
only timestamps/flags/opaque hashes in its own journal. A nonclean baseline or
nonpending result stops this one run. The [analyzer](../scripts/analyze-apple-iap-probe.py)
refuses incomplete evidence. Native correct/Paws Owned means an app bug to fix;
native verified current ownership before approval means a StoreKit Test anomaly
with reproducible evidence. Production verification must remain unchanged.

Sandbox initiation is prepared through the actual Paws adapter with explicit,
default-off `PAWS_APPLE_SIMULATE_ASK_TO_BUY=1`. It requires SandboxReview, the
review compile define and a development build. The SDK flag is assigned even
when false, so a cached SDK service cannot retain an earlier simulation opt-in.
The saved review scene/receipt record the option. Owner license refresh plus
stopping a stale editor licensing process restored package registration. Earlier
source `bf32a15` and its **556/556** gate are retained in the
[earlier preparation receipt](evidence/issue64/sandbox-ask-to-buy-preparation.json).
The fixed `a686433` candidate, **558/558** gate and bounded sandbox results above
supersede that preparation. Artifact readiness does not establish acceptance.
Reproduce from committed inputs with passing compile/test gates:

```bash
PAWS_APPLE_PRODUCT_ID=com.jweiss.paws.removeads PAWS_APPLE_SIMULATE_ASK_TO_BUY=1 \
  bash scripts/build-apple-iap-review.sh "$PWD/Builds/Issue64-AppleSandbox-AskToBuy"
```

Use StoreKit Configuration **None** and the owner's Sandbox Apple Account. Reconcile
Xcode cached review ownership through successful authoritative sandbox Restore
first. Pending initiation must never grant RemoveAllAds; no approval/decline
resolution in sandbox is inferred. No credential handling or Codex account/product actions,
upload, submission, main changes or production activation are authorized.

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

Unpaid deferred requests are not reliably enumerable as pending approval through
`Transaction.unfinished`; the bounded local run even surfaced a verified unfinished
entry before approval. Neither unfinished nor updates alone grants ownership.
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
  COMPILER_INDEX_STORE_ENABLE=NO DEBUG_INFORMATION_FORMAT=dwarf-with-dsym archive
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

## Signed owner-review preparation — 2026-10-21

The retained Unity export from source **`46bee5e`** is the review source, independent
of later develop changes. Local Xcode **27.1 (27A9275)** recompiles that export;
the original Xcode 27.0 unsigned archive/dSYMs remain immutable. Existing local
development provisioning covers **iPhone 17 / iOS 27.0.1** and expires
**2026-10-27 17:36:44 UTC**. Signing inputs remain in ignored local files/Keychain.
The [preparation receipt](evidence/issue64/device-preparation.json) records actual
artifact/signature checks separately from owner acceptance.
The signed build was installed and launched through **Paws StoreKit Test** in
Xcode; its running process was verified. No physical interaction was automated.

The ignored Xcode project has two Release development run schemes:

- **Paws StoreKit Test** selects `Paws-RemoveAds-OwnerReview.storekit`, synced
  read-only from the owner's existing App Store Connect product. The product is
  the approved non-consumable; price/localization were downloaded, not invented.
- **Paws Apple Sandbox** selects **None** for StoreKit Configuration. Sandbox
  account creation/sign-in and physical purchase/restore remain owner actions.

Both environments use the same review state/Keychain namespace. Switching schemes
does **not** clear ownership from Xcode testing. Before accepting sandbox purchase
evidence, explicitly Restore with successful authoritative sandbox reconciliation;
an unknown/failed result or cached Owned state is insufficient. Uninstall alone
does not guarantee a clean Keychain. Do not reset review evidence without owner
direction. This applies equally when switching back to Xcode testing.

Give the owner one action at a time using the scenario list above; record each
explicit pass/fail with its environment and exact build. Preparation, launch and
product sync do not establish purchase/restore, physical or runtime data behavior
acceptance. No App Store Connect changes, upload, submission or production
activation are part of this handoff.

## Owner startup failure and correction — 2026-10-21

The owner rejected the `46bee5e` review app's startup: a black screen on all three
attempts, including a debugger-free launch. Xcode also reported a stalled LLDB RPC
server; disabling attachment did not resolve the UI failure. The original
preparation receipt now records **failed owner startup**, not acceptance.

The original generator lost the UIDocument's panel reference across preview/scene
replacement and component Reset hooks. Replaying that sequence against the new
saved-scene regression reproduces an empty panel reference. The generator now
reloads the production panel after scene/component creation, persists its native
backing field and reopens the saved scene to require a panel, runtime theme and
camera before exporting. The review bootstrap fails visibly in logs for missing
panel/theme and emits bounded startup phase markers without store payloads.
Production scene, provider/coordinator/persistence and Settings contracts are
unchanged. Replacement-build physical and purchase/restore acceptance remain
pending the owner's explicit report.

2026-10-22: corrected source **`9fa818b`** has a fresh export at
`Builds/Issue64-AppleIapReview-SceneFix/`, a verified signed development app/dSYMs
at `Builds/Issue64iPhone/SceneFix/`, and a passing **unsigned** Release archive at
`Builds/Issue64-AppleIapReview-SceneFix.xcarchive/`. Xcode **27.1 (27A9275)** checks
cover identity, ARM64, iOS 16.3 minimum, matching dSYMs, native IAP symbols,
framework/privacy inventory and actual panel/theme inclusion. The signed copy
remains unchanged by unsigned archiving. The [corrected preparation receipt](evidence/issue64/device-preparation-scene-fix.json)
records both artifacts and the passing automated gates.

2026-10-22: the corrected signed build was installed/launched through Xcode
**Paws StoreKit Test**, using Run Without Building with Debug executable disabled.
The cached signed artifact matches the retained candidate hash; strict signature,
valid profile/device coverage, enabled Developer Mode/DDI services, installed
identity **0.1.0 (1)** and a running Paws process were verified on **iPhone 17 /
iOS 27.0.1**. The owner explicitly reported **“Paws menu appears”**. This passes
startup/menu visibility only. Settings/price is the next owner check;
purchase/restore, remaining physical scenarios and runtime-data acceptance remain
pending. The separate Apple Sandbox scheme still has StoreKit Configuration None.

## Archive gate status — 2026-10-20

Final clean unsigned development export: source **`46bee5e`**, Unity 6000.3.25f1 /
Xcode 27.0, retained at `Builds/Issue64-AppleIapReview/` (ignored). The
[review receipt](evidence/issue64/apple-iap-review-receipt.json) records the package
lock, resolved identity and actual frameworks/capabilities/privacy manifest, plus
the final automated test evidence. It includes the recovered-Pending correction.

After freeing disk space, the owner resumed the gate on 2026-10-20. The **unsigned
Release archive passed** using that unchanged export, retained at
`Builds/Issue64-AppleIapReview.xcarchive/` with ARM64 dSYMs. Binary checks confirm
`com.jweiss.paws`, **0.1.0 (1)**, iPhone portrait, minimum iOS **16.3**, matching
binary/dSYM UUIDs, unsigned executables, native IAP entry points and the embedded
framework/privacy inventory. The receipt records the archive checksum and checks;
the successful log is `.unity-results/issue64-archive-resume.aoWOQj/xcodebuild.log`.
Earlier disk-exhaustion logs remain under `.unity-results/issue64-archive/`.

This is a development review archive with production monetization disabled.
StoreKit Test/sandbox, runtime network/data observation and physical
purchase/restore acceptance remain owner actions. Keep the PR draft until these
validation gates are resolved; no signing, installation, purchase or upload was
performed for this archive gate.

2026-10-22 owner Settings check: price is visible and Restore Purchases is visible,
the owner observed `$4.99` (the correct USD price) but expected BRL. The initial
configuration selected `USA` / `en_US`, consistent with that observation. Changed
only the local StoreKit test storefront/locale to `BRA` / `pt_BR` in Xcode, saved
and relaunched through the same scheme. Owner reports **“R$ 4,99”**, passing BRL formatting. Product
price/localizations and App Store Connect remain unchanged. The adapter forwards
Apple's `localizedPriceString` unchanged to Settings. Local StoreKit formatting
validation does not establish the Brazilian App Store Connect price; verify that
separately in Apple sandbox.

2026-10-22 owner cancellation check in **Xcode StoreKit Test / BRA / pt_BR**:
after cancelling the purchase sheet, the price remains visible and no Owned
status appears. Cancellation scenario passes; simulated purchase is the next
owner step. Other purchase/restore and sandbox acceptance remain pending.

2026-10-22 owner simulated purchase in **Xcode StoreKit Test / BRA / pt_BR**:
**“Owned appears”** after confirming the test purchase. Purchase-to-Owned UI passes;
this does not establish restore, persistence after restart/offline, revocation or
interrupted finalization. Restore in the same test environment is next.

2026-10-22 owner follow-up in **Xcode StoreKit Test / BRA / pt_BR**: Restore
finishes with Owned retained and no purchase sheet (existing-owned restore only).
After Xcode stop/relaunch through the same test scheme, **“Owned remains”** without
repurchase or restore. Restart persistence passes. Offline ownership, clean-state
restore and the other scenario gates remain open.

2026-10-22 offline restart attempt: owner enabled Airplane Mode and disabled Wi-Fi
with USB connected. Xcode Run Without Building was rejected **before app start**
by iOS developer-certificate trust/security checks (`IDELaunchCoreDevice 0`,
`CoreDeviceError 10002`). This is a blocked offline-start test, not an entitlement
failure/pass. Requested networking restored for the same-build retry; no signing
identity, profile, credential or trust setting was changed. Offline ownership
acceptance remains pending.

Online retry launches the same signed build successfully without signing/trust
changes. The next owner check removes phone connectivity while the app is
running; it cannot establish offline cold-start acceptance.

2026-10-22 owner offline running-app check: with Paws already running, enabled
Airplane Mode and disabled Wi-Fi, then returned to Settings without closing Paws.
**“Owned remains while offline”** passes retained ownership during that offline
background/return scenario. Offline cold-start and clean-state restore remain
separate pending gates; local StoreKit behavior does not validate Apple sandbox.

2026-10-22 owner revocation check in **Xcode StoreKit Test / BRA / pt_BR**:
owner refunded the selected local Remove All Ads transaction in Xcode and reports
**“Owned disappears; price returns”** in Paws Settings. Running-app revocation/UI
passes. Restore after revocation is next; this does not establish cold-start
revocation, clean-state restore, or Apple sandbox acceptance.

2026-10-22 owner Restore after revocation: **“Price remains; no Owned status”**.
Restore does not resurrect the refunded local entitlement. Enabled Ask to Buy
only in the ignored StoreKit configuration; owner deferred-request check pending.

Ask to Buy handoff detail: Apple’s StoreKit 2 example confirms the initial test
payment sheet before the Ask Permission dialog appears; select Ask there, then
check Pending without Owned. A regular initial payment sheet alone is not a
deferred-flow failure. See [Apple’s StoreKit 2 demonstration](https://developer.apple.com/videos/play/wwdc2021/10114/?time=938).
Owner reported the initial regular sheet; actual deferred result remains pending.

2026-10-22 owner Ask to Buy request: after confirming the initial simulated payment
sheet and choosing Ask, **“Pending appears; no Owned status”**. Deferred/unowned UI
passes. Stopped Paws (device process list empty), then launched through the same
StoreKit Test scheme and verified a fresh process. Owner pending persistence and
duplicate-purchase suppression checks remain pending before approval.

### Blocked owner gate: deferred request becomes Owned after restart — 2026-10-22

After a verified fresh-process restart with no approval, owner reports **“Owned
appears”**. Owner and desktop transaction-manager inspection both show the newest
request still **Pending Approval**; the prior transaction remains Refunded. This
**fails** deferred restart acceptance; do not proceed to sandbox or treat #64 as
complete. Existing earlier scenario passes do not override this failure.

Owner explicitly approved copying only `Documents/apple-iap-review/state.bin` to
ignored local diagnostics. Its checksum-valid normalized state is Owned, pending
false, evidence present, no unfinished journal; last-finished reference matches
the pending transaction. That reference does not prove which native ownership
candidate was accepted. No raw store/Keychain payload is copied or committed.
Automatic approval review rejected directory/single-file copies before the owner
gave explicit permission; only the approved single file was subsequently read.

A native-only, review-gated diagnostic override is being prepared in the ignored
export to log verification source, opaque transaction hash, environment and flags.
Tracked provider logic and the retained signed candidate remain unchanged. No
approval, decline, reset or other test-transaction mutation has been performed.

#### Native isolation result

The diagnostic native verifier reads the pending transaction itself as a verified,
valid, unrevoked Xcode `currentEntitlements` candidate and latest transaction. Its
opaque reference differs from the older refunded transaction. Closing/reopening
the manager still reports **Pending Approval**.

An independently signed **StoreKit-only native probe**, using the same bundle,
phone and byte-identical test configuration, reproduces the same verified current
and latest transaction. It includes no UnityFramework/Unity IAP and reads/writes
no Keychain or app state; it performs no purchase, sync, finish or reset. Its
initial shell attempt failed before the probe because the SDK requires UIScene;
the corrected scene-based probe launches and completes the read-only snapshot.
This isolates a StoreKit Test API/session-versus-manager discrepancy. It does not
establish correct pending behavior or prove an Apple sandbox/production defect.

Independent QA recommends keeping this environment gate blocked and leaving
entitlement policy unchanged; do not override genuine verified ownership merely
because an earlier deferred marker exists. Diagnostic binaries/source hashes are
recorded in the corrected receipt; local sources/products are retained under
ignored Builds/`.unity-results`. The generated native source and exact original
signed Paws candidate/dSYMs have been restored, and Paws launched through the
original StoreKit Test scheme. No transaction approval/decline/reset was performed.
Requested owner restart/reconnect with networking on for the next isolation step.

2026-10-22 post-reboot isolation: owner restarted/reconnected/unlocked the online
iPhone; Developer Mode/DDI readiness passes. The native-only probe runs in a new
post-reboot process and still returns the same pending request from verified
`currentEntitlements` and `latest`, Xcode environment, unrevoked. No SDK/cache/state
access or transaction mutation occurs. Restored and relaunched the exact retained
Paws candidate through StoreKit Test; refreshed manager status awaits owner report.
The validation blocker remains open.

Post-reboot manager confirmation: owner and desktop inspection still show the
newest transaction Pending Approval and the earlier transaction Refunded. The
contradiction survives reboot. Proposed next isolation is one clean local retest:
owner-approved deletion of only those two Xcode StoreKit Test records, followed
by owner Restore to reconcile the empty test inventory before a new Ask to Buy
request. No deletion/reset has occurred; preserve this evidence and obtain owner
direction before discarding local test history. No direct Keychain/app-data reset
or App Store Connect/sandbox/production transaction change is proposed.

2026-10-22 owner explicitly approved the local test-history reset. Deleted exactly
Xcode records #0 Refunded and #1 Pending Approval; verified **No Transactions**.
No direct app-data/Keychain reset or App Store Connect/sandbox/production action.
Owner then tapped Restore and reports **“Price shown; no Owned status”**, passing
reconciliation with empty local inventory. The bounded clean retest will request
Ask to Buy again and use owner force-quit/icon reopen, avoiding a new Xcode app
installation, to distinguish normal restart from Run Without Building behavior.
The original discrepancy remains recorded and unresolved.
