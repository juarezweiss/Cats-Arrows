# Apple Offer Codes — #65

Authority: [#65](https://github.com/juarezweiss/Paws/issues/65),
[#64](https://github.com/juarezweiss/Paws/issues/64),
[ADR 0002](decisions/0002-provider-agnostic-monetization.md).
Implementation branches from fetched develop
`646acfe3f294dcb472144bd97197254a26d6cac5`, including #64/#79 and current app/assets/settings.
The configured non-consumable remains `com.jweiss.paws.removeads`.

## Integration and limits

Settings includes **Redeem Offer Code / Resgatar código de oferta / Canjear código
de oferta**. The optional `IOfferCodeRedemptionService` capability reports only
Closed, Unsupported or Unavailable. Apple-specific types stay in the purchasing
assembly. Code values never enter C#, app storage, logging or diagnostics.

`NativeAppleOfferCodeSheet` presents Apple's native
`AppStore.presentOfferCodeRedeemSheet(in:)` in Unity's active window scene on the
main actor. This API starts at iOS 16.0; this non-consumable flow requires **16.3**.
The installed iOS 27.1 SDK deprecates this compatible API at iOS 27 in favor of
`presentOfferCodeRedeemSheet(from:options:)`. The compatibility API retains the
existing deployment floor; no minimum-iOS setting changes. Unsupported runtimes
receive localized App Store/redemption-link guidance. Missing/inactive windows,
concurrent presentations and presentation errors return a dismissible localized
message. No redemption URL is constructed or opened by Paws.

The sheet's dismissal is not redemption evidence. Apple handles invalid, expired,
cancelled and already-redeemed codes. Closing the sheet starts the existing
`IPurchaseService.Refresh`; verification can finish later. The sheet prevents
repeated redemption/purchase intents while open without holding a gameplay lock
or creating a persisted pending purchase. Restore remains available after closure.

The existing enabled coordinator refreshes at construction, and the Apple review
bootstrap refreshes on focus/resume. Unity IAP's store-update subscription remains
active after sheet dismissal. App Store/URL/other-device/pre-first-launch
redemption therefore enters the same verified inventory/update path. No explicit
Restore or in-app redemption is required to discover a completed transaction.
If offline, detection waits for a successful later refresh/update; previously
verified access survives network failure. Store delivery timing is not promised.

Only the existing native verifier's exact-product, non-consumable, environment-
checked evidence grants Owned. Atomic persistence, unfinished-transaction journal,
idempotent finish retry, revocation and no-catch-up policy are unchanged. No second
ownership flag, backend, Analytics/Auth or package is added. The known #64 SDK
deferred replay/finalization risks remain tracked by **#82**, a hard Paws 1.0
release blocker; #65 does not fix or waive them.

Production still composes no monetization coordinator. The new row follows the
existing disabled Settings controls in production; it cannot initialize providers.
Rollback removes this presentation capability or retains disabled composition,
without deleting evidence, ownership or cadence state.

## UI reference lock

Direct implementation in the existing Paws Settings design system; no redesign.
Live Refero lookup was unavailable (NO_SUBSCRIPTION); bundled craft/accessibility
guidance and the existing product are the reference sources.

| Decision | Source and role |
| --- | --- |
| Row after Restore; existing scroll container, 56-point minimum and wrapping text | `GameShellView.AddSettingsRow`, #76 existing product reference |
| Existing ink/tile palette, 16-point type and 10-point radius | Existing Settings component tokens, unchanged roles |
| Named text button; EN/pt-BR/es label and matching tooltip | #65; bundled Refero craft guidance on semantic controls and touch targets |
| Native code entry and validation; local message + OK for unsupported/errors | #65 and #83 notification component |

Mounted tests check reachability, localized labels, dismissal/focus return and
preserved board state at 320×568, 390×844 and 430×932. Desktop evidence does not
establish VoiceOver or physical comfort acceptance.

## Owner-only StoreKit Test and Sandbox acceptance

### Owner results — 2026-10-23

The owner explicitly reported the following for source **`87772f2`**, with **no
purchase-history reset**. Preserve the starting **Owned** baseline in both runs.
[Exact reported results and scope assessment](evidence/issue65/owner-validation-2026-10-23.json).

| Check | Xcode StoreKit Test (simulated) | Apple Sandbox (owner-reported actual Sandbox) |
| --- | --- | --- |
| Starting ownership | Owned | Owned |
| Native redemption sheet opens | PASS | PASS |
| Cancellation returns to gameplay | PASS | PASS |
| Code redemption succeeds | NOT TESTED | PASS |
| Verified ownership appears | NOT TESTED | PASS |
| Ownership survives restart | PASS | PASS |
| Restore preserves ownership | PASS | PASS |
| Notes | None | None |

These reports pass native presentation/cancellation and retained-ownership
restart/Restore in each named environment. Sandbox code redemption and Owned UI
are passed **as reported by the owner**. Because both sessions started Owned,
neither demonstrates the minimal handoff's **unowned → Owned grant**. Do not
relabel the local NOT TESTED results or infer a new verified transaction from an
already-Owned label. No separate transaction/finalization evidence was supplied.

Fresh-grant owner coverage remains pending. Keep #65 open and PR #85 draft;
defer broad merge gates/integration preparation until this acceptance gap is
resolved. Preserve existing app/account/transaction history; no reset or new
purchase is requested/performed by this evidence update. Remaining RC scenarios
are not covered by these two reports. #82 stays open as the separate hard Paws
1.0 release blocker. Production remains disabled; main is unchanged.

### Minimal owner handoff — 2026-10-23

The exact reviewed implementation **`87772f2`** has been development-signed and
installed **in place** on iPhone 17 / iOS 27.0.1, Paws **0.1.0 (1)**. The original
unsigned app is unchanged; strict signatures, device/profile coverage and matching
ARM64 dSYMs pass. The existing local profile expires **2026-10-27 17:36:44 UTC**.
No rebuild or code change was needed. No broad automated tests were repeated.
[Preparation receipt](evidence/issue65/owner-preparation.json).

At preparation time, the app had **not been launched** by the agent. No app-state/
Keychain access, reset, uninstall, account action or transaction action was
performed. Physical acceptance was pending; the later scoped owner reports are
recorded above. #82 remains open as a separate hard Paws 1.0 release blocker;
its investigations are outside this run.

Open the ignored local project `Builds/Issue65-OfferCodes-87772f2/Unity-iPhone.xcodeproj`.
Select the connected iPhone, then use **Product > Perform Action > Run Without
Building** (Control-Command-R). Both prepared schemes use Release with Debug
executable off and resolve to the exact signed app under
`Builds/Issue65iPhone-87772f2/Build/Products/Release-iphoneos/Paws.app`.
Only ignored local launch settings were adjusted; compiled inputs are unchanged.

| Environment | Scheme and owner setup | Evidence scope |
| --- | --- | --- |
| Xcode StoreKit Test | **Paws 65 StoreKit Test** references the new local `Builds/Issue65-OfferCodes-87772f2/Paws65-FreeOffer.storekit`. The native Xcode editor created free offer **Paws65-Free-Local** for the copied approved non-consumable. Ask to Buy is off. The original configuration/history is retained. | Simulated StoreKit only; never label this an actual Sandbox code result. A new configuration does not prove a fresh/unowned baseline. |
| Apple Sandbox | **Paws 65 Sandbox**, StoreKit Configuration **None**. Owner verifies the intended Sandbox Apple Account in Settings > Developer, and **Environment: Sandbox** on any payment sheet. Owner supplies/enters a test-only Sandbox code for `com.jweiss.paws.removeads`. | Actual Sandbox redemption only after these environment checks; a cached Owned label is not a new redemption. |

Keep existing app/account/transaction history. Record the starting state first:
unowned, already Owned or Pending. If already Owned/Pending prevents a new grant,
report that baseline; do not reset state, initiate another purchase or investigate
Ask to Buy. Switching test environments shares the same review cache: use a
successful authoritative Restore to reconcile ownership in the selected
environment, and report an unavailable/failed reconciliation without inferring
fresh ownership. Account/code setup and all physical actions belong to the owner.

For each environment, prioritize this short sequence:

1. Open **Settings > Redeem Offer Code**, cancel, and confirm ordinary play works.
2. Redeem **one free offer** in the correct native test flow; confirm the prior
   unowned state changes to **All ads removed**. Dismissal alone is not success.
3. Force-quit/reopen using the same environment and confirm ownership remains.
4. Tap **Restore Purchases**; confirm ownership remains without another purchase.

Report `environment; starting state; cancellation; redemption/Owned transition;
restart; Restore` with pass/fail or blocked for each, plus any native error text
without codes/account details. The received scoped results are recorded above.
Record additional scoped evidence in #65 and PR #85. If accepted, run the required
merge gates and prepare integration into develop; otherwise retain the blocker
without expanding scope. No merge, main change, production activation or App Store
submission is authorized by this preparation.

### Xcode 27.1 scheme-editor crash correction — 2026-10-23

The owner's three Edit Scheme crashes share the StoreKit option controller's
workspace-relative path-resolution assertion. The agent-generated scheme supplied
an absolute `StoreKitConfigurationFileReference`; this was a preparation error.
It now uses **`../../Paws65-FreeOffer.storekit`**, relative to the project workspace.
The local file was created with Xcode's native unsynced StoreKit template, then
the approved product metadata was copied and **Paws65-Free-Local / Free** was
created/saved with the native offer editor. No live/store code was generated.

Both scheme editors now open: Local shows `Paws65-FreeOffer.storekit`, Sandbox
shows **None**. XML/target references validate, and read-only Release build-setting
resolution for both points to the retained signed app. Its tree hash and the
original StoreKit configuration hash are unchanged. No build, install, app launch,
account action, transaction access/reset or redemption test was performed.
[Configuration repair evidence](evidence/issue65/scheme-repair.json) supersedes
the original preparation receipt's local StoreKit reference only.

The scheme editor is not needed to run either configuration. In the existing
#65 project choose **Product > Scheme > Paws 65 StoreKit Test** (local free offer)
or **Paws 65 Sandbox** (None), select **iPhone 17 Juca**, then **Product > Perform
Action > Run Without Building** (Control-Command-R). The owner performs redemption.
At this configuration-repair stage runtime launch and StoreKit/Sandbox acceptance
were pending; later scoped owner results are recorded above. Configuration
verification itself is not a passing purchase/physical test.

### Full RC matrix

**Remaining scenarios pending.** The scoped owner reports above are the only
physical results claimed. Build preparation and automated contracts are separate
evidence. Record the exact source
SHA, app build, OS, Xcode version, locale and environment with each explicit human
pass/fail. Screenshots must redact codes, account details and raw transaction data.
Retain existing test history; a reset/deletion or new account change requires owner
direction. No automated physical iPhone input is permitted.

1. Prepare a fresh development review export from committed inputs:

   ```bash
   PAWS_APPLE_PRODUCT_ID=com.jweiss.paws.removeads \
     bash scripts/build-apple-iap-review.sh "$PWD/Builds/Issue65-OfferCodes-<source-sha>"
   ```

   Keep Ask to Buy simulation off (default). Use this new export's scene/assets/
   settings; never reuse the historical #64 exports. Signing/install preparation
   does not constitute review and must not overwrite/reset owner evidence.
2. **Xcode StoreKit Test:** owner selects a local configuration containing the
   same non-consumable and a test offer; no App Store configuration change. Test
   Settings redemption and cancellation, then use Transaction Manager's offer
   redemption to exercise external delivery. Verify Owned only after the verified
   transaction, retained access after restart and a single durable finalization.
   This simulates delivery; it does not prove a real Sandbox code is accepted.
3. **Apple Sandbox:** use StoreKit Configuration **None**. The owner creates and
   handles test-only codes for the existing product in App Store Connect. Before
   any confirmation, verify the intended Sandbox Apple Account visibly under
   Settings > Developer and **Environment: Sandbox** on any payment sheet. Stop
   if either is missing. Do not initiate a paid purchase to test redemption.
4. Owner redeems a Sandbox code using Settings. Also test external redemption
   through Sandbox Account Settings > Initiate Transaction > Offer Codes, then
   return to Paws. Test cold launch after redemption without Paws ever having run
   for that app-state/account baseline. Use another device/account state only when
   owner-prepared; a cached Owned UI alone is not fresh-retrieval evidence.
5. Check cancellation, invalid/expired code and duplicate/already-owned handling:
   native errors stay native; closing always restores ordinary play and never
   creates pending approval or clears existing verified ownership. A successful
   code yields the same lifetime Remove All Ads UI and direct Hint behavior as
   a verified purchase, including Restore after restart.
6. Check delayed/offline delivery: close/return and keep playing before the
   transaction arrives; resume online and verify eventual Owned. Verify retained
   offline ownership, authoritative revocation without resurrection on Restore,
   restart during interrupted finalization, and no accumulated progression ads.
   Test a below-16.3 runtime in a separate compatibility harness if available;
   the current app floor stays 16.3. Controlled unsupported-result UI coverage is
   automated; no unsupported device result is inferred.
7. Include these results in the monetization RC gate (#67), independently of
   the unresolved Ask to Buy release blocker (#82). Keep production disabled.

Owner controls offers, eligibility, code type/count/expiry, App Store status and
distribution. Production one-time/custom code generation requires **Ready for
Distribution** plus an **Approved** IAP; Sandbox codes are test-only. No production
code generation/download/distribution, App Store changes, upload, submission,
release, main merge or monetization activation is part of #65 implementation.

Apple references checked 2026-10-22:
[Offer Code setup and availability](https://developer.apple.com/help/app-store-connect/manage-in-app-purchases/create-offer-codes-for-in-app-purchases/),
[redemption and test flows](https://developer.apple.com/documentation/storekit/supporting-offer-codes-in-your-app),
[compatible sheet API](https://developer.apple.com/documentation/storekit/appstore/presentoffercoderedeemsheet(in:)).

## Validation evidence

Desktop mounted captures (controlled provider outcomes, not a native sheet or
physical review): [EN Settings](evidence/issue65/settings-en.png),
[pt-BR Settings](evidence/issue65/settings-pt-BR.png),
[ES Settings](evidence/issue65/settings-es.png),
[unsupported pt-BR](evidence/issue65/OfferCodeUnsupported-pt-BR.png),
[unavailable ES](evidence/issue65/OfferCodeUnavailable-es.png).

Candidate **`87772f283bf5ca14dcd477ca26b404a02faf9725`** passes pinned Unity
6000.3.25f1 compilation, focused **80/80**, full EditMode **578/578**, Python
runner contracts **51/51**, repository validation/whitespace and independent QA.
Swift typechecks against the installed iOS 27.1 SDK at deployment targets 15.0
and 16.3. Fresh development export and **unsigned** Xcode 27.1 native build pass;
the new bridge symbol is linked, ARM64 app/framework dSYMs match, iOS 16.3 and all
current scene/settings/branding/package inputs are retained. At this automated-
gate milestone, no signing, install, launch, purchase, code redemption, App Store
change or upload had followed. Later preparation and owner results are above.

[Exact source, check results and artifact provenance](evidence/issue65/validation.json)
record the candidate and ignored local build paths. The later explicit owner
reports are recorded separately above. Fresh-grant coverage remains pending;
automated verified-evidence fakes cannot replace that physical result.
