# Issue #64: bounded native Ask to Buy reproducer

The single owner-authorized run on 2026-10-22 is stopped **inconclusive**. Its
[sanitized journal](../../docs/evidence/issue64/native-ask-to-buy.jsonl) records
Pending, zero current entitlements before approval, verified unfinished/updates
after restart, then zero current entitlements after approval and one restart.
Xcode showed Purchased, but no first verified current entitlement was observed.
This is not a passing native sequence or proof of a preapproval current-entitlement
anomaly. Do not run this recipe again as a reset/reinstall loop. The exact executed
source is preserved; its postapproval startup `reason` says
`restart-before-approval`, so use recorded `phase` and ordered approval events to
interpret that snapshot.

This is a diagnostic app, independent of Unity IAP and Paws ownership/persistence.
It does not grant content, read Keychain/Paws saves, call `AppStore.sync`, finish a
transaction, approve/decline a request, or clear test history. Only an owner tap
calls native `Product.purchase()`. A diagnostic phase/journal survives one restart;
that phase is instrumentation, never evidence of entitlement.

Use one clean local StoreKit Test run. Preserve the prior failure first. Do not
cycle reinstalls/resets. If the baseline is not clean, product fetch fails, or the
native result is not Pending, stop and record an incomplete diagnostic.

## Prepare

Create an iOS UIKit application project in Xcode, replace its generated Swift
entry points with `Probe.swift`, and use iOS 16.3 or later. Use the existing local
development signing chosen by the owner; never put signing inputs in Git. Set a
scene-based lifecycle: UIApplicationSceneManifest / UISceneConfigurations /
UIWindowSceneSessionRoleApplication / UISceneConfigurationName = Probe and
UISceneDelegateClassName = `$(PRODUCT_MODULE_NAME).ProbeSceneDelegate`.

Supply these Info.plist values:

- `PawsDiagnosticProductId`: owner-approved non-consumable product ID.
- `PawsDiagnosticRunId`: a fresh UUID for this single run.

Select the owner's local `.storekit` configuration in the Run scheme. Enable Ask
to Buy and payment dialogs. Copy price/localization from the approved store
configuration; do not invent them. Disable debugger attachment if needed. Keep
App Store Connect actions, uploads and production settings outside this workflow.
The bounded local run rejects observed non-Xcode or unverified baseline entries.

## One run, owner-operated

1. With the prior evidence preserved, obtain owner direction for exactly one local
   test-history cleanup. Verify Xcode's Paws transaction table is empty. This does
   not clear Paws/Keychain state; the diagnostic never consumes those stores.
2. Install/launch the native probe once. Require a zero-current, zero-unfinished,
   zero-unverified baseline. Confirm the selected Xcode configuration in the receipt.
3. Owner taps **Start one test purchase**, confirms the local test sheet and chooses
   **Ask**. Require the native `purchase-result` event to be `pending`.
4. Owner taps **Record before-approval checkpoint**. Capture current entitlements,
   unfinished transactions and any updates. Do not approve yet.
5. Owner force-quits and reopens from the Home Screen once, without Xcode
   reinstalling it. Tap the before-approval checkpoint again. Inspect the journal
   before authorizing approval. Match opaque hashes with the pending manager item.
6. Owner approves that one request in Xcode. Record the manager result and its order
   relative to the completed before-approval checkpoint. Back in the probe, tap
   **Record after-approval checkpoint**. Identify the first verified current
   entitlement and the updates/unfinished events. Do not finish in this probe.
7. Export only this run's generated JSONL with owner permission when required.
   Analyze using `python3 scripts/analyze-apple-iap-probe.py /path/to/run.jsonl`.
   Preserve device/OS/Xcode, source/config/binary hashes and owner approval ordering.

The journal is `Documents/issue64-native-diagnostic/<run UUID>.jsonl`. It contains
only timestamps, counters, diagnostic phase, verification/environment/revocation
flags and deterministic opaque transaction hashes. No JWS, receipts, account,
payment details, provider error payloads or credentials are recorded.

The analyzer refuses missing/unclean/nonpending evidence and unexpected fields.
It does not infer owner approval from a transaction update or prove Paws/sandbox
acceptance. An entitlement before the completed owner checkpoint establishes a
native StoreKit Test contradiction; an entitlement only after owner approval is
the expected native sequence. Preserve the prior Paws failure until the cause is
resolved. Never weaken production verification to match local test behavior.

## Sandbox follow-up

Use the Paws review export with the explicit, development-only
`PAWS_APPLE_SIMULATE_ASK_TO_BUY=1` option and **StoreKit Configuration None**.
The owner configures/signs into their Sandbox Apple Account on the device.
Reconcile cached Xcode review ownership through successful authoritative sandbox
Restore before testing. Then the owner initiates once through Settings Remove Ads:
native deferred initiation must show Pending, never Owned, and disable a second
purchase across resume/restart. Sandbox supports initiation; approval/decline
resolution is a separate Xcode Test capability. No account/product creation,
credentials, upload, submission or production activation is authorized here.

References: [Apple Ask to Buy testing](https://developer.apple.com/documentation/storekit/testing-ask-to-buy-in-xcode),
[Apple sandbox simulation option](https://developer.apple.com/documentation/storekit/product/purchaseoption/simulatesasktobuyinsandbox(_:)).
