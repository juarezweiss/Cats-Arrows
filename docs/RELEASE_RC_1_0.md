# Paws 1.0 monetized candidate — #67

Authority: owner's 2026-10-24 decision; [#67](https://github.com/juarezweiss/Paws/issues/67),
[#82](https://github.com/juarezweiss/Paws/issues/82), [release control](RELEASE.md).
Base: fetched develop `3a44192697b8f1c416cc49afabeb839c5a0c3dc7`.
The release plan is **IAP and AdMob enabled**; disabled mode is rollback only.

## Classification

| Classification | Requirement / evidence |
| --- | --- |
| Accepted known issue | Unresolved Ask to Buy may re-enable Remove Ads after restart. #82 remains open for post-release work. Cause is unproven; repeat requests are not declared universally harmless. |
| Release blocker | Paid purchase fails to grant/restore verified ownership; already-owned duplicate charging; entitlement lost through finalization; fabricated ownership; unreliable Restore; actual security/compliance problem. |
| Required owner evidence | Normal priced-product Apple Sandbox purchase, restart/Restore, no duplicate owned purchase, and physical integrated ad/consent/lifecycle checks on exact candidate. Missing evidence is pending, not a demonstrated defect or a pass. |
| Post-release work | #82 callback/persistence investigation and focused correction; improvements beyond purchase safety/compliance that do not prevent a functional initial release. |

## Composition and integrity

Preserve `com.jweiss.paws.removeads`, verified current-entitlement authority,
private Keychain evidence, coordinator atomic persistence, finalization journal,
Restore and revocation behavior. Keep Unity 6000.3.25f1, IAP 5.3.1, Core 1.14.0,
Google Unity 11.6.0/native 13.11.0 and UMP 3.1.0 pins. No speculative SDK upgrade.

The production profile must contain real Apple IAP/Offer Code and Google providers,
external live AdMob identifiers and approved audience/privacy treatment. It must
contain no mock ledger/ownership, review/benchmark bootstrap, test ad identifiers,
debug geography or Ask to Buy simulation. The integrated validation profile uses
the same adapters with official Google test ads and real Apple verification;
Xcode simulation is permitted only there. Both profiles use non-development
IL2CPP/ARM64 Release builds. A validation archive is **not** a distribution archive.
The disabled rollback profile must initialize no providers and preserve entitlement
and cadence storage. No remote kill switch exists.

UI reference lock: retain the existing #76 Settings list and #66 localized privacy
captures (outlined rows, existing typography/colors, scrolling, visible selection
and focus). The age choice follows the existing language selector with four equal
options and an available Back action; no new visual system or bitmap assets.
The initial choice is optional; its default makes no adult assumption. Privacy
and support open only on explicit Settings intent, outside native presentation.

Before distribution, freeze committed inputs and inspect the actual final archive:
source/config digest, bundle/version/build, release flags/defines, package/native
pins, symbols/dSYMs, signatures/entitlements/capabilities, SDK resources/privacy
manifests and absence of review/mock/test configuration. Build 1 is local validation
only; the owner allocates a globally increasing distribution number before export.

## Minimal physical iPhone checklist

Human operation only under AGENTS.md; record SHA, config/environment, artifact
checksum, version/build, device/iOS, reviewer and explicit PASS/FAIL/NOT TESTED per
item in #67. No raw receipts/JWS, credentials or personal account identifiers.
Keep app data, SDK journal and purchase histories intact; do not reset or reinstall
the owner's app to manufacture a clean baseline.

1. **Apple Sandbox, normal paid-product flow:** use the integrated test-ad candidate,
   StoreKit Configuration **None**, Ask to Buy simulation off and a known-unowned
   Sandbox tester supplied by the owner. Visibly verify the intended Sandbox account
   under Settings > Developer and **Sandbox on the purchase sheet** before confirming.
   Cancel once: no ownership. Complete one normal priced-product Sandbox transaction
   (no real money): verified Owned, both ads suppressed immediately, direct Hints.
   Restart, temporarily offline restart if signing permits, then online Restore:
   ownership retained. Remove Ads cannot initiate another charge while Owned.
   Check recovery on a separate clean tester installation only if separately prepared
   and authorized; do not erase existing history. Report a failure for focused repair.
2. **Xcode StoreKit Test, simulated Ask to Buy:** select a local `.storekit` file
   for the same SKU and enable Ask to Buy in Xcode's local test controls. Pending
   must remain unowned. Restart may show the accepted available-button behavior;
   do not initiate a repeat request. Approve in Xcode: verified current entitlement
   must grant, restart and Restore must retain it. In a separate local simulated
   request, decline/cancel: never Owned. Record **simulation**, not Apple Sandbox
   approval. Sandbox does not provide Xcode transaction-manager approval/decline.
3. **Finalization and recovery:** on the integrated candidate, interrupt a completed
   test purchase around return/relaunch and Restore; entitlement must recover with
   no new charge. Local StoreKit Test can exercise interrupted/unfinished, refund
   and offline cases; label its scope. A signed unfinished/update payload alone
   must not grant without current entitlement. Existing SDK replay hazard remains
   unproven physically; automated recovery contracts are separate evidence.
4. **Offer Codes:** cancel native sheet safely; redeem an owner-supplied Sandbox
   code for the same product if needed, then restart/Restore and check suppression.
   Prior #65 Sandbox grant is historical evidence, not a current-candidate pass.
   Record untested external/pre-first-launch/invalid/expired/duplicate/delayed cases;
   do not distribute production codes or change console settings during this test.
5. **Google test ads / UMP:** verify test labels and registered debug device/geography.
   First verify unanswered/under-18/prefer-not choices start no Google flow and
   leave gameplay/free Hints/IAP/Restore usable. Choose 18+ to exercise ads.
   Required consent and Privacy Options work; no request before `CanRequestAds`.
   Downgrading age blocks new Paws ad requests and retains any native guard until
   real closure. Previously started SDK/native work may finish; the SDK has no
   claimed global uninitialize/data-deletion switch. A restricted cold launch
   never constructs the Google bridge.
   Wins 1–3: no progression ad; fourth victory: no ad; deliberate next board: at
   most one. Offline/no-fill/restricted: continue, no debt, eligible free Hint.
   Reward completion: exactly one Hint. **Presented ad closed before reward: no
   Hint, no late Hint and safe input return** remains unvalidated from #66. If the
   creative has no early close, record NOT TESTED; never infer a pass or force-close
   native UI to substitute for voluntary closure.
6. **Lifecycle / owned / game:** background and return during purchase, form and
   ad; no input under native overlay, safe recovery afterward. Verified ownership
   bypasses both placements, including queued opportunities. Check tap cycle,
   drag/protected cells/completed lock, Clear/new game and EN/pt-BR/es at available
   compact/standard/tall portrait sizes. Desktop viewport tests remain distinct.

Apple documents [Sandbox product/server testing](https://developer.apple.com/documentation/storekit/testing-in-app-purchases-with-sandbox)
and [local Ask to Buy testing](https://developer.apple.com/documentation/storekit/testing-ask-to-buy-in-xcode)
as distinct environments. Google's [UMP instructions](https://developers.google.com/admob/ios/privacy)
require launch consent updates and request gating. Nonpersonalized ads still use
identifiers for purposes such as frequency capping/reporting; see the inspected
[SDK data inventory](ADMOB.md#data-collection-networking-and-opt-outs).

## Privacy and submission prerequisites

The owner approved the production policy on 2026-10-24: **NPA only, no ATT prompt
or cross-app tracking, publisher first-party ID off, SDK crash reports off,
G-rated ad content, no additional analytics/attribution/telemetry SDKs**. Audience
is general, not child-directed, not Apple's Kids Category; adults and teenagers
may play. Do not infer that all users are adults or classify all users as children.
Under-age handling must be explicit before Google requests; UMP does not determine
a player's age or automatically forward its TFUA flag to the ad SDK. The
conservative candidate implementation offers local bands under 13, 13–17, 18+
and prefer not to answer. Only a declared adult starts Google/UMP; under-18,
unanswered and prefer-not users get no Google requests and free eligible Hints.
IAP/Restore stay available to all; local age choice is revisable in Settings.
No birth date or age band is sent to a developer server. This is the conservative
implementation of the owner's protection requirement, not a claim that law
universally requires an 18-year threshold. NPA/G content alone does not satisfy
age protections. [Google age treatment](https://developers.google.com/admob/unity/targeting)
and [UMP under-age handling](https://developers.google.com/admob/unity/privacy/gdpr)
inform this boundary; this is not a blanket legal-compliance certification.

Local resource audit found only Google's sample app identifier in retained #66
`Info.plist` files, including `Builds/Issue66/f3539da-registered-eea-export/Info.plist`
and `Builds/Issue66/f3539da-registered-eea-device/Paws.app/Info.plist`. Those are
historical test artifacts, not production inputs. No root `.env*`, configured
production ad input environment variable, owner-local live-ID JSON or confirmed
public Paws privacy/support URL was found in the repository and inspected ignored
configuration. This is a local audit, not proof those resources do not exist in
the owner's consoles or elsewhere. Existing #66 registered device hashes stay
private in local/ignored configuration; no hash/credential is published here.

The owner must supply external app/interstitial/rewarded IDs and manually confirm
the AdMob iOS app, correct format ad units and account/app serving readiness;
published EEA/UK/Swiss and applicable US-state UMP messages with approved copy,
privacy URL and correct audience treatment; and actual public support/privacy URLs.
No console state or published message can be certified from a local plist.
The approved policy is not zero data collection.
Do not publish a no-data-collected claim. Use exact GMA/UMP/Unity manifests and
inspected SDK behavior for factual App Privacy and required-reason declarations.
Runtime privacy/network acceptance and final legal/console declarations remain
owner review; static URL strings are not observed network traffic.

Owner also confirms public support/privacy URLs, age rating/export compliance,
agreements and the existing SKU's price/territories/localizations, IAP review
screenshot/notes and inclusion with the first submitted version. Offer Code
production distribution remains separate. No enrollment, console change or spend
is authorized by this preparation.

## External build configuration

Pass an absolute owner-local JSON path through `PAWS_MONETIZATION_CONFIG`.
Keep live identifiers and device registration outside Git. Required production
fields are `adMobAppId`, `interstitialAdUnitId`, `rewardedAdUnitId`,
`privacyPolicyUrl`, `supportUrl` and `audiencePolicy: "local-adult-only-v1"`.
The build sets `profile` explicitly. IDs must be real iOS AdMob values belonging
to the same publisher; placements must be distinct. Both URLs must use HTTPS.
Missing/invalid inputs fail export; syntactically valid values do not prove
console publication, ownership, serving readiness or reachable privacy content.

Validation accepts an optional local configuration with an actual owner AdMob
app ID for its published UMP messages, `consentDebugGeography` (Disabled, EEA,
RegulatedUSState or Other) and `umpTestDeviceIds` (registered local hashes).
Its placements are always overwritten with official Google test units.
Without this configuration, the sample app ID/actual geography applies; forced
region/required-message acceptance cannot be inferred. Production rejects debug
geography, device lists and Google sample identifiers. Receipts bind the actual
serialized resource/configuration digest and redact device hashes.

## Path to submission

1. Finish automated/CI/QA and exact-source native archive evidence; fix any actual
   entitlement, finalization, compliance or release-integrity failure.
2. Supply external production configuration and complete the minimal human tests
   above. Record explicit results and any untested scope in #67. #82 remains open.
3. Allocate the distribution build number, freeze final source/config and produce
   a fresh enabled production archive; inspect again without launching live ads.
   Owner reviews privacy/metadata/IAP attachments and signs off GO.
4. Obtain separate explicit owner authorization for develop integration, main merge,
   distribution signing/upload, submission and release as applicable. No automatic
   merge, tag, TestFlight/App Store upload, live traffic or account activation.

Current verdict and exact test receipts belong in `docs/evidence/issue67/` and
the final #67 preparation report. Missing configuration/human evidence means
NO-GO for distribution until completed; it does not change the monetized scope.
