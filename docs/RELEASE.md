# Environments, Releases, and Rollback

Current authority: the owner's **2026-10-24 decision** in [#67](https://github.com/juarezweiss/Paws/issues/67).
Paws 1.0 ships **with Apple IAP, Offer Codes, AdMob and UMP**. Disabled mode is a
rollback profile, not the release plan. The narrow [#82](https://github.com/juarezweiss/Paws/issues/82)
restart-button limitation is accepted and non-blocking; purchase safety is mandatory.
See [candidate gates and minimal physical checklist](RELEASE_RC_1_0.md).
The dated #63–#66 evidence below retains its original scope; historical blanket
#82-blocker and disabled-only statements are superseded by this decision.

## Branch/environment model
- Feature branches: isolated development.
- `develop`: integration branch; produces internal/dev artifacts.
- release candidate tag/branch: staging/TestFlight/internal testing when introduced.
- `main`: production-ready history only; human-approved merge required.

Environments should differ through build profiles/configuration, not divergent code copies.

## Paws 1.0 revised release gate — #67 / #82

**Accepted known issue:** an unresolved Ask to Buy request may leave Remove Ads
available again after restarting the app. Keep #82 open for post-release resolution.
The root cause is unproven. No second purchase was initiated in the verified
Sandbox restart failure; repeat requests are not asserted to be universally harmless.

**Release blockers:** failure to grant/restore a paid verified entitlement; loss of
ownership through unsafe finalization; charging an already-owned user for the same
non-consumable; fabricated ownership from deferred/failed/cancelled/unverified
transactions; unreliable Restore recovery; actual compliance/security problems.
Only verified Apple current entitlement for `com.jweiss.paws.removeads` grants
access. Unknown/offline/partial inventory preserves verified cached access;
authoritative revocation remains effective. The accepted button behavior waives
none of these requirements.

The exact candidate must pass normal priced-product Apple Sandbox purchase,
restart/Restore/ad suppression and finalization/recovery gates. Xcode StoreKit Test
provides simulated Ask to Buy approval/decline; it is not real Apple approval
evidence. Do not demand Xcode's approval controls from Sandbox. The owner operates
the physical iPhone and reports explicit results. Sandbox confirmation requires
the intended account visible in Settings > Developer and the sheet visibly
identifying Sandbox; otherwise stop. No real-money transaction is authorized.

Release readiness is recorded in #67. No main merge, upload/submission, live ad
activation, account change or release follows from preparation. Separate explicit
owner authorization remains required at those boundaries.

## Production control
Agents may prepare lower-environment artifacts only when explicitly instructed. Production actions require explicit human instruction in the current task.

## Versioning
Owner-approved for #63 on 2026-10-20: initial product version **0.1.0**, iOS
build **1**. The 2026-10-24 Paws 1.0 preparation targets **1.0.0**, with build 1
retained only for unreleased local validation. Use semantic product versions and a separate globally
increasing positive integer iOS build number. Before each new distribution
artifact, the owner checks the highest allocated/uploaded number and commits a
larger number in `PlayerSettings.iOS.buildNumber`; never reuse numbers across
product versions. Rebuilding the same unreleased source for local validation
is not a new distribution allocation. No build script uploads or allocates
numbers through App Store Connect.

## Immutable artifacts
Release artifacts must be tied to a commit SHA/tag and retained long enough to reproduce/diagnose a release.

## Rollback
For client regressions:
1. disable remotely configurable risky behavior if available;
2. prepare a hotfix from the last known good tag or forward-fix branch;
3. never rewrite an already published tag;
4. preserve affected puzzle seeds and telemetry context for reproduction.

App stores limit instantaneous binary rollback, so risky monetization/live-service behavior should be kill-switchable independently of app binaries when those systems are later added.

## Production iOS identity — #63

Owner approved on 2026-10-20:

| Setting | Approved value |
| --- | --- |
| Public display name | Paws |
| Permanent bundle identifier | `com.jweiss.paws` |
| Device family | iPhone only |
| Minimum OS | iOS 16.3 |
| Orientation | Upright portrait |
| Initial version / build | 0.1.0 / 1 |

`UnityProject/ProjectSettings/ProjectSettings.asset` is the canonical identity,
version and platform configuration. `ProductionIOSBuild.Build` validates it and
exports only `Assets/CatsAndArrows/Scenes/Paws.unity`, with device SDK, ARM64,
IL2CPP, Release and `BuildOptions.None`. `PAWS_PRODUCTION` is supplied per build,
never persisted as a global define. Inherited defines/development/profiler/debug
settings are rejected. Review checks/bootstrap/mocks are compiled out; benchmark
code remains excluded. The receipt also checks generated IL2CPP sources.

The enabled #67 production composition starts the ordinary game with saved
preferences, real safe area and normal tutorial/lifecycle, plus the existing real
Apple IAP/Offer Code and AdMob/UMP adapters. Build-time configuration selects
production, integrated validation or disabled rollback; missing production inputs
fail export. The validation artifact uses real Apple verification and Google test
ads with no simulated ownership. It is not the final distribution profile.
Disabled rollback initializes no providers and keeps free Hints; existing storage
and verified ownership are retained. No remote kill switch is claimed.

Compatibility-sensitive `CatsAndArrows` namespaces, preference/storage keys,
product GUID, company name, generator/ruleset versions and fingerprints are
unchanged. The permanent bundle ID creates a separate iOS app sandbox from
`com.juarezweiss.catsarrows.inputreview`; review preferences do not automatically
migrate. Review iOS exports temporarily use that review identifier and restore
the canonical settings afterward. Never manually patch generated Xcode bundle
identifiers. Changing the public company/legal seller identity is an owner
account decision; `companyName` is retained for local persistence compatibility.

## Reproduce the production configuration

Close this project's GUI Editor, use the pinned editor and commit all build
inputs (`UnityProject/` and `scripts/`) first. The builder rejects modified or
untracked build inputs and records HEAD; unrelated documentation-only owner edits
do not affect compiled inputs. Source receipts require the same HEAD/configuration
at inspection. Each output path must be fresh and absolute.

```bash
bash scripts/unity.sh
bash scripts/validate.sh
bash scripts/compile.sh
bash scripts/test.sh
PAWS_MONETIZATION_PROFILE=validation bash scripts/build-ios.sh export "$PWD/Builds/Issue67-1.0.0-validation-export"
bash scripts/build-ios.sh archive-unsigned "$PWD/Builds/Issue67-1.0.0-validation-export" "$PWD/Builds/Issue67-1.0.0-validation.xcarchive"
```

The default profile is `production`; it requires an absolute owner-local
`PAWS_MONETIZATION_CONFIG` JSON file and rejects Google sample IDs/debug settings.
Do not invent production identifiers to bypass this gate. Use
`PAWS_MONETIZATION_PROFILE=disabled` explicitly for rollback validation. The
validation example above prepares actual IAP with test advertising and is not
eligible for distribution. See [candidate preparation](RELEASE_RC_1_0.md).

An unsigned archive validates compilation/configuration only. It is not an
installable, distribution-signed or App Store accepted build. For an explicitly
authorized signed preparation, use `archive-signed` with local
`PAWS_IOS_TEAM_ID`, `PAWS_IOS_SIGNING_IDENTITY` and `PAWS_IOS_PROFILE` supplied
by the owner through the shell/Keychain. Missing inputs fail clearly before
archive work. Unity signing fields remain empty; no profile downloads, account
actions or credential creation are automated. Signing inputs, profiles and
certificates never belong in Git or chat. The script overrides only signing
settings, never bundle identity, family, minimum OS or numbering.

Exports/archives stay under ignored `Builds/`; logs and receipts stay under
ignored `.unity-results/`. Receipts inventory source SHA, actual versions,
configuration, capabilities/entitlements, frameworks/native links, locked Unity
packages, privacy manifests and a deterministic archive-tree SHA-256 (paths,
types, permissions and contents; timestamps excluded). They distinguish export,
unsigned archive and signed archive, and keep physical review pending. The
receipt lives outside the inspected tree to keep its checksum stable. Preserve
archive/symbols/receipt together; remove only task-owned reproducible export and
DerivedData intermediates when no longer needed. Reproducible configuration does
not promise byte-identical Apple-signed binaries.

## #63 physical iPhone handoff

After owner-provided signing/install readiness, a human reviews the exact source,
version/build on a named device/iOS. Record reviewer, pass/fail and scope in #63:

- Confirm Paws name/icon, portrait/safe areas, readable EN/pt-BR/es and comfortable
  targets; fresh production identity opens cleanly.
- First Play tutorial, replay/Skip, ordinary Play/Next across Easy/Medium/Hard;
  tap cycle, drag, arrow/protected cells, hold, Hint, Clear/Menu.
- Completion celebration/static return and read-only finished board; no ads,
  purchases, captures or review autorun.
- Background/foreground and restart; locale/first-Play/difficulty preferences
  persist. Unfinished board/tutorial progress is not restored after termination.

Physical acceptance, Apple enrollment/App ID/App Store Connect creation, signing
credentials, purchases, uploads/submission and release remain owner-controlled.
#21's tester distribution/enrollment/privacy gates remain open.

## #63 verified build evidence — 2026-10-20

Source `b1dee6a6e641e7f026336b319a458dfca4c8483e` exported with pinned
Unity **6000.3.25f1** and archived **unsigned** with Xcode **27.0 (27A266a)**.
Actual identity is `com.jweiss.paws`, **0.1.0 (1)**, iPhone only, iOS 16.3,
upright portrait. Clean export and Release archive succeeded without identifier
patches or signing inputs. [Receipt](evidence/issue63/ios-release-receipt.json)
records archive-tree SHA-256
`e5b15df98ebe53f98479fe28ed00159aebc17539c3a260f01c358c6742910e5f`.
Documentation-only commits after this source do not alter its build inputs;
check out that recorded source commit to reproduce receipt inspection.

The inspector verified 191 generated IL2CPP source/header files with excluded
review/benchmark/mock types absent. The archive embeds only UnityFramework;
required device capabilities are arm64/metal. Its Unity privacy manifest and
native framework dependencies are inventoried in the receipt. Locked Unity
packages are listed separately from embedded native frameworks; their presence
is not evidence that Editor/test code shipped. No real monetization provider
was added or enabled. This inspection is build evidence, not final App Store
privacy/compliance approval.

Preserved locally (ignored):
`Builds/Issue63-Paws-0.1.0-1.xcarchive/` including app/framework dSYMs,
`.unity-results/ios-export.EPWSZe/` and `.unity-results/ios-archive.6LQRxp/`.
This run's reproducible DerivedData and, after local installation below, full
export were removed; small export/signing metadata remains with the signed app.
Repository checks, 36 Python contracts, pinned compilation and **495/495**
EditMode tests passed; independent QA reports no blocking findings. See
[TESTING.md](TESTING.md). Signing/distribution readiness and human physical
iPhone acceptance were pending at this unsigned-archive milestone. At that point, no
credentials, account/registration, purchase, install, upload, submission or release
actions had been performed. The later owner-authorized local installation follows.


## #63 owner-authorized local iPhone preparation — 2026-10-20

The owner completed development provisioning for `com.jweiss.paws` and requested
installation. The validated archive's existing Release `Paws.app` was copied,
embedded with the matching local development profile, then signed inside-out
(UnityFramework followed by the app) using the existing Keychain identity.
**No Unity/Xcode recompilation, second archive or IPA was needed.** The original
unsigned archive checksum remains unchanged; Mach-O UUIDs match its dSYMs.
The application identifier/version/family/minimum OS were not changed.

Strict signature verification, profile/device/certificate/expiry checks, install,
plain launch and running-process verification passed on **iPhone 17 / iOS 27.0.1**,
version **0.1.0 (1)**, source `b1dee6a`. This is a **local development signature**
(`get-task-allow=true`), expiring **2026-10-27 17:36:44 UTC**; it is separate from
App Store distribution signing/validation. [Device-preparation receipt](evidence/issue63/device-preparation.json)
records its own signed-app tree hash and scope. No physical interaction,
appearance, lifecycle or comfort test was automated. The owner's later acceptance
is recorded below.

Retained: `Builds/Issue63iPhone/Paws.app` (about 46 MiB), its small
`ExportMetadata/`, the immutable unsigned archive/dSYMs (about 406 MiB), plus
small receipts/logs. The temporary full export (about 690 MiB) and archive
DerivedData (about 1.7 GiB) have been removed. Owner signing settings remain
outside Git; the committed Unity/release configuration remains authoritative.
Use the physical handoff checklist above and record the owner's scoped result.


## #63 owner physical acceptance — 2026-10-20

The owner reported **“all passed”** after testing the handed-off **Paws 0.1.0 (1)**
on **iPhone 17 / iOS 27.0.1**, source `b1dee6a`. This closes #63's human physical
review for the supplied checklist: identity/locales/portrait/safe areas;
tutorial/replay/Skip and Play/Next across difficulties; tap/drag/protected cells,
arrow hold, Hint/Clear/Menu; celebration/static return and completion lock;
background/reopen/restart and saved language/preferences; no ads/purchase prompts.
The [device-preparation receipt](evidence/issue63/device-preparation.json) now
includes this dated owner report separately from automated technical readiness.
No new build or device automation was performed to record acceptance. The original
unsigned-archive receipt remains a point-in-time build record. Other-device and
measured-performance coverage are not inferred. This pass does not authorize a
merge, upload, submission or production release; distribution signing and owner
release decisions remain separate.


## #76 approved menu — local iPhone preparation, 2026-10-20

Owner-approved menu source `f15860d0e5ba8c4b7deb393419e73388e39a05a8` was
exported with pinned Unity 6000.3.25f1, archived as Release/arm64 and locally
signed using the existing development provisioning. Production composition,
provider/diagnostic exclusion, strict app/framework signatures, profile/device/
expiry, unchanged unsigned archive hash and binary/dSYM UUIDs passed inspection.

Paws **0.1.0 (1)** (`com.jweiss.paws`) was installed over the existing app on
**iPhone 17 / iOS 27.0.1**, with no uninstall or data reset. Plain launch and the
running process were verified. Local development signing expires
**2026-10-27 17:36:44 UTC**; this is not an App Store distribution signature.
[Sanitized preparation receipt](evidence/issue76/device-preparation.json) records
source, artifact hashes and the separately recorded owner physical review pass.

Retained locally: `Builds/Issue76iPhone-f15860d.xcarchive/` with dSYMs,
`Builds/Issue76iPhone-f15860d/Paws.app` and its small export metadata, plus ignored
technical logs/receipts. This run's export and DerivedData were removed after
success. No provider integration, upload, store submission or merge was performed.

Owner physical review passed on 2026-10-20: “iphone review finished, pass”.
The report covers the supplied #76/#77 menu/settings handoff checklist for this
installed source/device: approved Play/gear comfort, Settings/Back, native-language
selection/localization/persistence, Tutorial replay and Home Play resume.
This closes the physical menu review for source `f15860d` on iPhone 17 / iOS
27.0.1. No device automation, rebuild or installation was performed to record the
report. Other-device coverage, measured performance and App Store distribution
acceptance are not inferred; the earlier #63 report remains separate.
