# AdMob / UMP development integration — #66

Authority: [#66](https://github.com/juarezweiss/Paws/issues/66),
[#29](https://github.com/juarezweiss/Paws/issues/29)/[#30](https://github.com/juarezweiss/Paws/issues/30),
[ADR 0002](decisions/0002-provider-agnostic-monetization.md), and
[#67](https://github.com/juarezweiss/Paws/issues/67).
Base: fetched `develop` **1acb87c0e6c1fae156b24baffd9419b84cc41ea6**.
The existing October 19 documentation amendment is preserved and implemented here.
[#82](https://github.com/juarezweiss/Paws/issues/82) remains an open, independent
Paws 1.0 release blocker. This task does not resolve its purchase lifecycle.

## Bounded SDK/privacy inspection — 2026-10-23

Inspected official Google Mobile Ads Unity **11.6.0**, native Google Mobile Ads
**13.11.0**, and UMP **3.1.0**, before integration. Archive checksums match Google's
release/download metadata. [Inspection, hashes and manifest contents](evidence/issue66/sdk-inspection.json)
and [exact imported file inventory](evidence/issue66/sdk-inventory.json) are retained.
`python3 scripts/check-admob-sdk.py` detects altered/missing/uninspected SDK files.

The [official release](https://github.com/googleads/googleads-mobile-unity/releases/tag/v11.6.0)
declares Unity 2022.3.0f1 or newer, encompassing pinned **6000.3.25f1**.
Its dependency XML allows GMA `~> 13.11`; this integration instead pins **13.11.0**
by exact native bytes. UMP's declared 3.1.0 meets the GMA podspec's `>= 1.1` dependency.
The native [release notes](https://developers.google.com/admob/ios/rel-notes)
raise Xcode to 26.2 starting at 13.4; local Xcode is **27.1 / 27A9275**.
This newer native requirement takes precedence over the older minimum in the
Unity quick-start. The project's iOS minimum stays **16.3**.
Version eligibility alone is not a compile, native link or device validation pass.

The official UPM package adds Unity External Dependency Manager **2.1.0** and
Android JNI **1.0.0**. The `.unitypackage` editor installer also adds EDM on editor
load. Inspection of EDM 2.1.0 found editor analytics for initialization, resolver
selection, build GUID/settings, API use and CocoaPods setup, gated by
`EditorAnalytics.enabled`. This is editor telemetry, not player Analytics or the
IAP Apple Insights behavior rejected in #82. We avoid this additional telemetry
entirely using Google's supported
[manual native dependency path](https://developers.google.com/admob/unity/quick-start).
No vendor editor installer, EDM, Android DLL/AAR/manifest, mediation adapter,
Firebase, GoogleAppMeasurement, separate Analytics/Auth or backend is installed.
Do not import the full upstream package on top of this selective import.

The checked-in SDK DLLs and native frameworks are unmodified official bytes.
Only Unity importer metadata and our export composition are custom. Native SDKs
live outside `Assets`, under `UnityProject/ThirdParty~/GoogleMobileAds`, and are
copied/linked only by the explicit AdMob development export. Runtime SDK DLLs and
the Google/review assemblies require Editor or `PAWS_ADMOB_REVIEW`. Production
composition remains `monetization: null`; the default kill switch is unchanged.
There is no live-ID configuration or production activation path in this adapter.

## Data collection, networking and opt-outs

Google's [data disclosure](https://developers.google.com/admob/ios/privacy/data-disclosure)
describes IP/coarse location, device identifiers, advertising/interactions,
performance and SDK crash/diagnostic data. The actual GMA manifest additionally
lists Analytics, third-party/developer advertising purposes, linked location/ad/
interaction/device-ID fields and device-ID tracking capability. Required-reason
APIs: UserDefaults `CA92.1`, boot time `35F9.1`, disk space `E174.1`.
UMP's actual manifest lists unlinked/nontracking coarse location, performance and
product interaction for app functionality, plus UserDefaults `CA92.1`.
The SDK manifests are included verbatim as resource bundles in review exports;
they do not replace owner-reviewed App Store disclosures.

Static inspection found Google ad configuration/content/reporting URLs including
`googleads.g.doubleclick.net`, `pagead2.googlesyndication.com`,
`imasdk.googleapis.com`, `mediation.goog` and UMP's
`fundingchoicesmessages.google.com/a/consent`. Debug/reporting and regional URL
strings also exist; see the evidence. These are binary strings, **not observed
network traffic**, an exhaustive allowlist or proof that a feature executes.
Ad creatives/forms may fetch additional server-selected content. No dynamic
network-capture or closed-source implementation guarantee is claimed.

Publisher first-party ID is [enabled by default](https://developers.google.com/admob/unity/privacy/strategies).
The binding sets `PublisherFirstPartyIdEnabled = false`, publisher personalization
Disabled, `npa=1` on both formats, and disables SDK crash reporting and mediation
initialization before GMA initialization. These are supported opt-outs, **not a
universal data-collection switch**. Google's
[NPA explanation](https://support.google.com/admob/answer/7676680?hl=en)
retains identifiers/cookies for frequency capping and reporting; test ads also
must not be described as zero collection. No consent-sync ID, user/account ID,
location, puzzle data, receipt, JWS or custom targeting is supplied by Paws.

GMA can use IDFA subject to iOS permission. UMP removed advertisingIdentifier
references in 2.2.0; UMP 3.1.0 adds optional consent syncing, which is unused here.
No ATT prompt or `NSUserTrackingUsageDescription` is added. ATT denial/no prompt
is not an opt-out of all ad measurement. Review exports contain only Google's
`cstr6suwn9.skadnetwork`; the upstream list's additional buyer entries are not
silently adopted. SKAdNetwork can attribute without IDFA. No separate attribution
SDK or ATS exception is added.

The owner explicitly approved the **development-only** policy on 2026-10-23:
no ATT prompt, nonpersonalized ads, first-party ID off, SDK crash reports off,
no EDM telemetry, Google test ads only. This allowed Phase 2 after inspection.
Production ATT/personalization, age/under-age treatment, buyer identifiers,
privacy policy and consent-message copy, factual App Privacy declarations,
AdMob/UMP console setup and any live identifiers/traffic remain owner decisions
under #66/#67. A general-audience ad content cap is not an age classification.

## Runtime and failure contract

`IAdService`, `MonetizationCoordinator`, atomic storage and existing hint/domain
logic remain authoritative. `AdMobAdService` references only an injected bridge;
only `Advertising/Google` references Google types. Native callbacks enter a
thread-safe queue drained on the application thread before provider/coordinator
Tick. Both formats use one-use loaded objects and generation/operation guards.
Only Progression→interstitial and Hint→rewarded are mapped.

Every fourth ordinary completion creates one opportunity, consumed before the
next deliberate fresh-board transition. Victory/launch/resume do not show ads;
unavailable opportunities proceed without debt. A prepared eligible unowned Hint
fails open directly when infrastructure is unavailable. An actual voluntary close
without reward grants none; a trusted reward persists/deduplicates one grant.
Both paths recheck board, session and mark revision. Stale boards never receive
automatic delivery; an unused earned grant recovers through a later explicit Hint.
Verified Remove Ads suppresses both formats and queued requests. Purchase state
is never inferred from SDK advertising/consent callbacks.

UMP update runs once per enabled launch. A required form completes before GMA
initialization or loading. Current `CanRequestAds` gates initialization, every
load and every show; privacy changes invalidate loaded ads and recheck it.
Settings exposes Privacy Options only when UMP requires it, including for owners.
Consent failure/unavailability leaves ordinary play and eligible free Hints working.
Initial ownership Unknown retains the existing restore-first behavior.

Initialization/load/update waits have 30-second deadlines and no tight retry
loop. Launch/resume, successful privacy completion and ad closure are preload
boundaries. Google has no supported programmatic fullscreen close. Cancellation,
timeout, disposal and ownership changes cannot certify native dismissal. Native
presentation guards survive until real close/failure callbacks; the bridge
certifies those callbacks as safely absent. Ambiguous native Show/form exceptions
also retain guards until terminal evidence. No timeout unlocks beneath native UI.
Google-only reward callbacks precede dismissal; post-terminal/duplicate callbacks
cannot create a new grant. Mediation would invalidate this assumption and needs
separate review.

`SetiOSAppPauseOnBackground(false)` prevents the plugin from pausing Unity solely
for its own native ad overlay: the application's presentation guard suspends input
while the dispatcher continues. Actual OS background/focus loss still cancels
automatic Hint delivery and retains earned authorization for explicit recovery.

## Validation result — 2026-10-23

Committed implementation **aefda544c15a43d77f8dfc2160437ebb387be63c**: focused
**167/167**, full EditMode **668/668**, Python **51/51**, pinned compile,
repository checks and **322-file** SDK integrity gate pass. Independent final QA:
no blocking findings. [Exact reports, hashes and CI](evidence/issue66/validation.json).

The [unsigned native iOS development build](evidence/issue66/native-build.json)
passes Google bridge/GMA/UMP symbol, SDK privacy-resource and matching ARM64 app/
framework dSYM checks. The [ordinary disabled export](evidence/issue66/disabled-export.json)
contains no Google managed/native SDK, Google review assembly or AdMob/UMP/ATT
configuration. The SDK-independent adapter assembly can remain compiled; the
production bootstrap never constructs it. No native build/launch is claimed for
that disabled export. The exporter generated an empty Services Core settings file;
removing only that empty file allowed the unchanged export's provenance check.

Nine SDK-free desktop Settings captures cover all three locales at all three
portrait sizes. Examples: [EN compact](evidence/issue66/settings-privacy-en-320x568.png),
[pt-BR standard](evidence/issue66/settings-privacy-pt-BR-390x844.png),
[ES tall](evidence/issue66/settings-privacy-es-430x932.png).
The reference lock uses the existing #76 Settings list: same typography, outlined
rows, spacing, scroll, focus and localized alert treatment. No new visual system.
Mounted tests also exercise native-form suspension/recovery using controlled fakes.
These captures and automated tests do not establish actual native ad/form behavior.

## Build and owner review

Run focused `bash scripts/test-admob.sh`, full `bash scripts/test.sh`, pinned
`bash scripts/compile.sh`, repository validation and SDK pin verification.

Prepare a clean committed-source review export:

```sh
PAWS_UMP_GEOGRAPHY=EEA bash scripts/build-admob-review.sh /absolute/fresh/export
```

Optional `PAWS_UMP_TEST_DEVICE_IDS` contains locally supplied comma-separated UMP
test hashes; do not commit personal device hashes. UMP debug geography only takes
effect on registered test devices (simulators are test devices). Supported values:
`EEA`, `RegulatedUSState`, `Other`. No consent reset is performed automatically.
The generated receipt records source SHA, exact versions, geography and device
count, not hashes. Build the exported Xcode project unsigned with local Xcode.
The development app uses `admob-review/state.bin` and a separate **mock ownership**
ledger. Its Remove Ads action is simulated and does not invoke StoreKit or spend.
Production and existing Apple review state are preserved.

Physical iPhone review requires an **explicit owner report**. The scoped report
below records 13 checked conditions as passed; early rewarded closure remains
unvalidated. Agents must not operate the phone's interaction/appearance/lifecycle
tests. Owner checklist:

1. Confirm exact reviewed SHA/build, device/OS, Google test labels, mock purchases
   and correct UMP debug device/geography; never enter live IDs or buy anything.
2. Relaunch for UMP update; test required form, denial/unavailability/offline and
   Settings Privacy Options/revocation in EEA, regulated US and Other conditions.
   Native SDK requests and consent data still exist under the approved test policy.
3. Complete boards 1–4: review victory without an ad, then deliberately start the
   next board. Test close/no-fill/offline/restart: no retry debt or launch ad.
4. On eligible Hint, test offline/unavailable→one free Hint; presented close with
   no reward→no Hint; completion→exactly one Hint. Include final-Hint victory.
5. Background/foreground during consent, load and ad; wait for safe native closure.
   Verify input recovery, stale-board suppression and explicit earned-Hint recovery.
6. Use **simulated** Remove Ads and restart/Restore to verify both formats suppress,
   direct hints work and no due opportunity catches up. Real IAP/#82 is separate.
7. Check EN/pt-BR/es compact/standard/tall Settings/board, touch/drag, protected cells,
   completed lock and clear/new-game behavior. Report pass/fail and exact scope.

Record runtime privacy/SDK network evidence before #67 production declarations.
No native request/no-request behavior or physical acceptance follows merely from
a successful unsigned build. No main merge, upload, store submission or release.
Rollback removes the development opt-in; retain entitlement/cadence state.


## Owner-authorized iPhone preparation — 2026-10-23

Source **f3539dae9db084ea4cd97159167c8d6b1a87b83c**, synchronized with latest
fetched develop, was exported with **one registered UMP test-device identifier**
and **EEA** debug geography. The owner authorized a bounded technical discovery
install/launch to obtain the identifier directly from Google's SDK log, followed
by the final registered build. The identifier remains only in local/ignored
configuration and serialized review artifacts; it is omitted from Git evidence.

The final Google-test-only app is development-signed using the existing Paws
profile and identical application/keychain identities, installed **in place** as
`com.jweiss.paws` **0.1.0 (1)** on **iPhone 17 / iOS 27.0.1**, and plain-launched.
Signature/profile, embedded registration, native linkage/privacy resources,
matching ARM64 dSYMs and the running process pass technical readiness checks.
[Signed artifact and preparation evidence](evidence/issue66/device-preparation.json).
The local development profile expires **2026-10-27 17:36 UTC**.

The app uses only `admob-review` and simulated Remove Ads. Existing Apple review
state remains separate. iOS relocated its data-container path during update;
pre-existing state directories remain and Apple state/backup/lock metadata is
unchanged across the final install and launch. No uninstall, data/consent reset,
account action, purchase, redemption, transaction-history operation or Keychain
content access occurred. This is a metadata-only preservation check, not a full
pre-discovery data backup or a revalidation of Sandbox ownership/history.

No native consent form, ad or game interaction was automated. At preparation
time, physical UMP/ad/lifecycle acceptance remained pending. The subsequent scoped
owner report is recorded below. #82 and production gates remain unchanged.
Runtime source, shared scenes, assets and ProjectSettings were not changed for this preparation; broad suites
were not repeated because compiled implementation inputs are unchanged.

## Owner physical validation — 2026-10-23

The owner reported **13 checked conditions passed**, with **one unchecked condition
not validated**, on the registered EEA candidate prepared above (`f3539da`, iPhone
17 / iOS 27.0.1, Paws 0.1.0 build 1). Candidate metadata comes from the preparation
receipt. [Exact checklist, owner note and scope](evidence/issue66/owner-validation-2026-10-23.json).

Passed scope covers UMP startup/required form/Settings Privacy Options; boards
1–3 without interstitials, fourth-board victory review and one next-New-Game test
interstitial with safe return; exactly one Hint after rewarded completion and a
free eligible Hint when offline/unavailable; no accumulated interstitial after
reconnection; reported ad/form background safety; and simulated Remove Ads/direct
owned Hints with restart/mock Restore persistence.

**Presented rewarded ad closed early → no Hint remains NOT VALIDATED.** The owner
reported that the test ads offered no early-exit option and had to finish.
This is an unexercised physical scenario, not an observed app failure or a pass.
Previously passing deterministic adapter/coordinator no-reward-close contracts
remain separate evidence and do not satisfy this physical item. No forced native
closure, new build, phone automation or creative/console change follows this note.

Other unreported #66/#67 scenarios remain unclaimed. Simulated ownership does not
validate Apple IAP or #82. PR #86 remains draft; #66 stays open, production stays
disabled, and #82 remains an independent release blocker. No merge, closure,
production or release authorization follows from this owner report.
