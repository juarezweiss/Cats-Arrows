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

**Pending. No physical redemption or purchase acceptance is claimed.** Build
preparation and automated contracts are separate evidence. Record the exact source
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
current scene/settings/branding/package inputs are retained. No signing, install,
launch, purchase, code redemption, App Store change or upload followed.

[Exact source, check results and artifact provenance](evidence/issue65/validation.json)
record the candidate and ignored local build paths. Owner-operated Xcode StoreKit
Test and Apple Sandbox code redemption remain pending until an explicit human
result is recorded; automated verified-evidence fakes cannot satisfy them.
