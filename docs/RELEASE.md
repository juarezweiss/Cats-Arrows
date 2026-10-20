# Environments, Releases, and Rollback

## Branch/environment model
- Feature branches: isolated development.
- `develop`: integration branch; produces internal/dev artifacts.
- release candidate tag/branch: staging/TestFlight/internal testing when introduced.
- `main`: production-ready history only; human-approved merge required.

Environments should differ through build profiles/configuration, not divergent code copies.

## Production control
Agents may prepare lower-environment artifacts only when explicitly instructed. Production actions require explicit human instruction in the current task.

## Versioning
Owner-approved for #63 on 2026-10-20: initial product version **0.1.0**, iOS
build **1**. Use semantic `0.x.y` product versions and a separate globally
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

`PawsBootstrap` starts the ordinary game with saved preferences, real safe area,
normal tutorial/lifecycle and `monetization: null`. No providers initialize;
Hints remain free and purchase UI hidden. This is the binary disabled/rollback
configuration. Future enabled releases require separate approved real adapters
and tests; reverting to this composition retains existing monetization storage
and verified ownership. No remote kill switch is claimed.

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
bash scripts/build-ios.sh export "$PWD/Builds/Paws-0.1.0-1-export"
bash scripts/build-ios.sh archive-unsigned "$PWD/Builds/Paws-0.1.0-1-export" "$PWD/Builds/Paws-0.1.0-1.xcarchive"
```

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

Preserved locally (ignored): `Builds/Issue63-export/`,
`Builds/Issue63-Paws-0.1.0-1.xcarchive/` including app/framework dSYMs,
`.unity-results/ios-export.EPWSZe/` and `.unity-results/ios-archive.6LQRxp/`.
Only this run's reproducible DerivedData was removed after archive preservation.
Repository checks, 36 Python contracts, pinned compilation and **495/495**
EditMode tests passed; independent QA reports no blocking findings. See
[TESTING.md](TESTING.md). **Signing/distribution readiness and human physical
iPhone acceptance remain pending.** No credentials, account/registration,
purchase, install, upload, submission or release actions were performed for #63.
