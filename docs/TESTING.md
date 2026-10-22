# Testing Strategy

## Current IAP scope and Paws 1.0 release gate

Owner-directed split (2026-10-22): [#64](https://github.com/juarezweiss/Paws/issues/64)
is completed IAP foundation scope; [PR #79](https://github.com/juarezweiss/Paws/pull/79)
can proceed through normal integration review with production monetization disabled.
[#82](https://github.com/juarezweiss/Paws/issues/82) owns the unresolved Ask to Buy
lifecycle and physical acceptance: **HARD BLOCKER for Paws 1.0 App Store release;
not a blocker for continued development on `develop`.** Enforce the
[release checklist](RELEASE.md#paws-10-app-store-hard-release-gate--82) independently
of #64 closure/#79 integration. No merge/closure or physical test occurs in this update.

Prior compile/provider/full-suite results below support the unchanged foundation;
they do not establish correct physical Pending/restart, approval, entitlement or
finalization. Preserve verified Sandbox versus environment-unverified evidence.
The following pre-split snapshots retain historical “#64 blocked” status text;
that scope decision now belongs to #82, with no failed finding erased. The 5.4.2
rejection remains in force. This documentation-only split requires repository
validation/whitespace checks; no new Unity run or purchase attempt is claimed.

## Issue #64 retained foundation validation and pre-split history

Dependency #63 is integrated in current develop; owner approved product mapping
`com.jweiss.paws.removeads` on 2026-10-20. Implementation and owner handoff:
[APPLE_IAP.md](APPLE_IAP.md). Production composition remains disabled.

Bounded IAP 5.4.2 comparison (2026-10-22): **stopped at package inspection**.
The new unconditional Apple Insights transport conflicts with #64's no
developer-data/backend boundary; no supported opt-out was found for Unity
6000.3.25f1. Official IAP/Core tarball SHA1 values match the registry. Repository
and whitespace checks pass. No project upgrade, compile, focused/restart/full
suite, build or physical attempt followed. Pins remain 5.3.1/Core 1.14.0; no
claim of a 5.4.2 restart pass/fail. [Exact inspection evidence and source hashes](evidence/issue64/iap-5.4.2-comparison.json).

Bounded desktop follow-up (2026-10-22): **1/1 diagnostic passed**, exit 0
(`issue64-desktop-sdk-pending-replay.f6kQUF`). Real adapter/coordinator/file-store
recovery retained Pending with empty inventory and a controlled absent private
marker. Explicitly injecting a same-product UserCancelled failure through the
real IAP 5.3.1 failure conversion then reproduced an unowned/buyable state. No
automatic SDK post-deferred failure or occurrence of that callback on the phone
was established. This is a controlled diagnostic, **not a root-cause finding or
fix**. [Method, hashes and limits](evidence/issue64/desktop-pending-investigation.json)
are retained; temporary compiled test changes were removed. No implementation
change remains, so no broad merge gate was repeated. #64 remains blocked pending
callback/persistence provenance; no further physical attempt is authorized.

**Latest owner evidence correction (2026-10-22):** real Apple Family members
received a Messages Ask to Buy request naming Remove All Ads / Paws Puzzle Game /
R$ 29,90. Its originating attempt/environment is uncorrelated; earlier physical
Ask to Buy attempts without explicit environment checks are potentially
production-account attempts, not established Sandbox tests. Do not infer that
Sandbox caused this notification. The later fresh-tester attempt whose sheet
explicitly identified Sandbox remains separate evidence; native `environment:
xcode` observations apply only to their recorded native run. See
[the observation and scope](evidence/issue64/owner-family-ask-to-buy-observation.json).
No further purchase attempt now. Before any future owner-operated purchase test,
require both the intended Sandbox Apple Account visibly present under Settings >
Developer and **Environment: Sandbox** visibly present on the purchase sheet. If
either is missing, do not initiate or approve anything. Earlier environment labels
below do not override this correction; no earlier unverified pass is promoted to
verified Sandbox evidence.

Latest follow-up source **`a686433`** fixes a reproduced Restore race: a paid
update after sync success could prematurely settle an unsynchronized read; an
update during synchronized verification must supersede stale absence with a
new synchronized read. Both failing reproductions are retained locally
(`issue64-restore-race-red.suDuBo`, `issue64-restore-update-red.z8KdLc`). Final
provider contracts **47/47** pass (`issue64-restore-final-contracts.4HkmOH`),
pinned compile passes (`compile.iPOQmp`), full EditMode **558/558** passes with
successful exit and zero failed/skipped/inconclusive (`editmode.o5A1f5`), and
repository/whitespace checks pass. Independent QA reports no blocking findings.
Clean export (`apple-iap-export.o6DmaX`) and signed development artifact checks
pass; the [fixed sandbox candidate](evidence/issue64/sandbox-restore-fix-preparation.json)
is installed/running with no local StoreKit configuration or debugger. No app
data, Keychain or transaction history was reset. The owner's earlier Restore
report (**All ads removed**, prior tester ownership unknown) does not establish
an unowned baseline. With a fresh tester signed in, owner Restore returned the
price without Owned and Apple's sheet identified Sandbox. One confirmed request
showed Pending/unowned. After owner force-quit/Home Screen reopen it stayed
unowned, but Remove Ads became available for another purchase: **pending-lock
restart fails**. No second purchase or history reset follows; physical experiments
stop. The native diagnostic remains inconclusive, and the earlier environment-unverified Pending-to-Owned observation,
SDK replay hazard and this restart failure keep #64 blocked. The Restore fix is
not claimed as the physical failures' cause.

2026-10-22 bounded follow-up: the independent native-only Ask to Buy trace is
**inconclusive**: Pending returned and current entitlements stayed empty before
approval, but remained empty after approval/restart too. Verified unfinished and
updates before approval are not ownership. [Trace/analysis](APPLE_IAP.md#current-owner-gate--bounded-investigation-2026-10-22)
and the prior Paws early-grant failure remain blockers; no reset/reinstall cycles
or verification relaxation follow. Sandbox initiation is still pending.
The development-only sandbox simulation preparation passes focused EditMode
**59/59**, exit 0 (`issue64-ask-final-focused.H3zCru`). Current Python contracts
**51/51**, native policy **11 assertions**, iOS Swift typecheck, repository and
whitespace checks pass. Initial compilation failed with zero registered packages
(`compile.XiesJY`); after owner license refresh, a stale licensing client blocked
the next attempt (`compile.T5lcfL`, stopped with exit 143). Stopping that stale
process restored pinned compilation (`compile.9WLAoT`). Source `bf32a15` passes
full EditMode **556/556**, successful exit and zero failed/skipped/inconclusive
(`editmode.RuRc4L`), clean sandbox export (`apple-iap-export.FL16oX`) and signed
development build. Signature/profile, identity, ARM64/dSYMs, IAP ABI, saved UI
assets and privacy inventory checks pass. [Receipt](evidence/issue64/sandbox-ask-to-buy-preparation.json)
records the exact retained signed candidate and sandbox-only debugger-free launch.
Owner reports Sandbox tester signed in; authoritative Restore and Ask to Buy
initiation/restart remain pending. Earlier results below apply to their source.

Unity **6000.3.25f1** final compilation passed (`compile.IYRpa3`). Focused provider/build
tests **40/40 passed** (`issue64-final-focused.v0gOQX`), including private-marker
write failure with successfully persisted coordinator Pending state. The final full EditMode gate
**537/537 passed**, zero failed/skipped/inconclusive and successful editor exit
(`editmode.v6sp26`). The preceding full gate also passed 536/536 (`editmode.ijaG8a`). An earlier full run had one existing portrait Settings test
failure (Back target position); that test passed in isolation and in the full
rerun without a UI/source change. This transient is recorded, not suppressed.
All **41** Python runner/inspector contracts (including five new review-export
contracts), repository validation and whitespace
checks passed. Native desktop policy regressions passed **11 assertions**, and
Swift typechecking passed against the iOS 16.3 simulator SDK target. Independent
QA reports no blocking findings after finalization/refund/refresh corrections.

The provider tests use actual IAP 5 SDK models through the adapter with controlled
store/native-evidence seams. They cover verified versus unverified success,
cancel/failure/deferred/restart, duplicate/late updates, restore callback ordering,
offline cache/reinstall/account change/revocation, store/private-state write
failures, multiple unfinished transactions, crash after native finish before
journal clear, timeouts and focus refresh during a native buy. Native regressions
execute the actual same-transaction refund reconciliation and production versus
sandbox/Xcode environment policy. Review build tests protect the separate scene,
development-only define and approved product mapping. Existing #30 coordinator,
storage and UI coverage remains in the full gate.

These automated checks do not establish live StoreKit/sandbox behavior. Owner
App Store Connect actions and physical purchase/restore/lifecycle acceptance remain
pending; use the checklist and provenance fields in [APPLE_IAP.md](APPLE_IAP.md).
No physical-device tests, account/product creation, signing, purchase or upload
were automated.

The final clean unsigned iOS development export succeeded from `46bee5e`
(`apple-iap-export.9OzXMM`) with pinned Unity/Xcode 27.0. Its normal shell Settings
composition includes the recovered-Pending correction; production remains disabled.
[Review receipt](evidence/issue64/apple-iap-review-receipt.json) records actual
package lock, resolved identity, framework linkage, capabilities and privacy
manifest. After earlier disk exhaustion and an owner-directed pause, the owner
resumed the archive gate on 2026-10-20. The **unsigned Release archive passed**
(`issue64-archive-resume.aoWOQj/xcodebuild.log`) from the unchanged final export.
Artifact checks verify ARM64 binaries and matching dSYM UUIDs, approved identity
**com.jweiss.paws / 0.1.0 (1)**, iPhone portrait, minimum iOS **16.3**, unsigned
executables, native IAP ABI symbols and embedded framework/privacy inventory.
The receipt records the archive checksum and checks. Export and archive remain
under ignored `Builds/Issue64-AppleIapReview/` and
`Builds/Issue64-AppleIapReview.xcarchive/`. No runtime network observation is
claimed. Owner StoreKit Test/sandbox and physical acceptance remain pending.

2026-10-21: the owner reported the approved App Store Connect product saved in
Prepare for Submission with price/localization configured and Offer Codes enabled.
The retained source `46bee5e` export compiled and signed with current Xcode
**27.1 (27A9275)** using existing development provisioning, then launched through
the **Paws StoreKit Test** scheme on **iPhone 17 / iOS 27.0.1**. Strict signatures,
profile/device/expiry, ARM64/dSYM UUIDs, minimum iOS, native IAP symbols and privacy
inventory passed; the original Xcode 27.0 unsigned archive checksum is unchanged.
The local StoreKit file was synced read-only from the existing App Store Connect
product; a separate **Paws Apple Sandbox** scheme has StoreKit Configuration None.
[Sanitized receipt](evidence/issue64/device-preparation.json) records provenance.
These are technical preparation checks only: no purchase, restore, physical
interaction or runtime data-behavior acceptance is claimed. No Unity/source
changes or repeat of the already passing EditMode gates was needed. The owner
receives one review step at a time; both environments remain pending explicit
human results. Review cache must be reconciled when switching environments, as
documented in [APPLE_IAP.md](APPLE_IAP.md).

Owner startup review failed for source `46bee5e`: black screen on three attempts,
including a debugger-free launch. Replaying the original scene generator against
the new regression reproduces a saved UIDocument with an empty panel reference
(`issue64-legacy-scene.owrd9M`, intentionally failing reproduction). The corrected
generator reloads/persists the panel after component Reset hooks and verifies the
saved scene's panel/theme/camera before export. Focused scene checks **6/6** pass;
final combined scene/portrait checks **7/7** pass (`issue64-ui-scene-focused.IrnhBL`).
Pinned compilation passes (`compile.U8qfQ2`); final full EditMode **539/539** pass,
zero failed/skipped/inconclusive and successful exit (`editmode.TqTHXs`); Python
contracts **41/41**, repository/whitespace checks and independent QA pass.

Two earlier full reruns each hit the previously recorded portrait Back target
failure (`editmode.hbdmzp`, `editmode.MJBywm`); its isolated check passed. The fixture
mixed synthetic phone insets with GeometryChanged callbacks reading the Editor's
real safe area. The fixture now consistently reapplies its synthetic input on
geometry changes and asserts it stays unchanged through Settings/selector
navigation, retaining every original target-size/bounds assertion. Scene tests
also drain destroyed runtime panels during teardown. Production safe-area/UI
behavior is unchanged. Replacement physical/store acceptance remains pending.

2026-10-22: clean corrected export **`9fa818b`** succeeded (`apple-iap-export.LIgZpH`);
the actual export contains both BoardReviewPanel and UnityDefaultRuntimeTheme,
absent from the rejected build. Xcode **27.1 (27A9275)** signed development build
and unsigned Release archive pass, including binary/dSYM, identity/minimum iOS,
native ABI, UI asset and privacy inventory checks. [Corrected receipt](evidence/issue64/device-preparation-scene-fix.json)
records hashes and provenance. The signed app/dSYMs and archive remain under
ignored Builds. After reconnect, this exact signed candidate was installed/launched
through **Paws StoreKit Test**, debugger disabled. Signature/profile validity,
artifact hash, current device readiness, installed **0.1.0 (1)** identity and a
running process pass. The owner explicitly reported **“Paws menu appears”** on
**iPhone 17 / iOS 27.0.1**: startup/menu visibility passes only. Settings/price,
purchase/restore, remaining physical scenarios and Apple sandbox are pending.

## Issue #63 production iOS configuration

2026-10-20: pinned Unity **6000.3.25f1** compilation passed (`compile.O8favA`);
final EditMode **495/495 passed**, zero failed/skipped/inconclusive and successful
editor exit (`editmode.H8QhAh`). The 15 new release tests check actual approved
PlayerSettings, unsigned configuration, sole production scene/bootstrap/panel,
non-development build options, inherited diagnostic define rejection, canonical
positive build numbers and fresh absolute outputs. The scene test uses an
isolated preview scene. Existing 480 gameplay/UI/persistence/domain tests pass.
Repository validation, whitespace and all **36** Python contracts pass, including
25 iOS inspector/shell tests. Independent release QA reports no blocking findings.

Source `b1dee6a6e641e7f026336b319a458dfca4c8483e`: one clean production iOS
export and one **unsigned** Release archive passed with Xcode **27.0 (27A266a)**.
The actual artifact has `com.jweiss.paws`, version/build **0.1.0 (1)**,
iPhone-only family and minimum iOS **16.3**. Inspector checks validate Unity's
unmodified identity/provisioning bindings, production settings, excluded
review/benchmark/mock types (191 generated IL2CPP sources/headers), capabilities,
framework linkage and privacy-manifest inventory. No monetization adapter is
composed. [Full receipt](evidence/issue63/ios-release-receipt.json) and
[reproduction/physical checklist](RELEASE.md) preserve provenance and scope.
The archive and dSYMs remain in ignored `Builds/Issue63-Paws-0.1.0-1.xcarchive/`;
small export metadata/logs/receipts are retained, while task-only export and
DerivedData were removed after the local installation below.

The unsigned archive result does not establish distribution signing or human
physical-iPhone acceptance. Subsequent owner-authorized local preparation signed
a copy of its existing Release app with the matching development profile and
installed/launched **0.1.0 (1)** on **iPhone 17 / iOS 27.0.1**. Strict signatures,
profile/device/expiry checks and running-process verification passed; no rebuild
or IPA was needed. This development signature expires 2026-10-27;
[device-preparation receipt](evidence/issue63/device-preparation.json) records
separate provenance. No physical interaction tests were automated.
The owner reported **“all passed” on 2026-10-20**, completing #63's physical
review checklist on **iPhone 17 / iOS 27.0.1**, **0.1.0 (1)**, source `b1dee6a`.
Scope: Paws name/icon, portrait/safe areas, EN/pt-BR/es; first Play tutorial,
replay/Skip and Play/Next across difficulties; tap/drag, protected cells, arrow
hold, Hint/Clear/Menu; celebration/static return and completion lock;
background/reopen/restart and saved language/preferences; no ads/purchase prompts.
The report is the owner's human acceptance, separate from technical install/launch
checks. It does not establish other-device coverage, measured performance or
App Store distribution signing/approval. Account actions, signing credentials,
purchases and distribution remain owner-controlled.

## Issue #76 Home and Settings navigation

Supersedes #72’s visible Tutorial/flags/purchase menu layout. Coverage includes
Home → Settings → language selector/Back, native option names and persisted
immediate locale refresh, manual tutorial replay, live-board Play resume and
first-Play behavior. Mounted tests use en/pt-BR/es at 320×568, 390×844 and 430×932
with simulated safe insets and check minimum 48-unit targets and scrollable rows.
Purchase/restore coverage uses existing SDK-free fakes for disabled, unavailable,
localized price, pending, owned and status refresh; no provider is integrated.
The complete EditMode suite also checks tap cycle, drag, protected cells,
completion locks and Clear/Next board behavior.

2026-10-20: pinned Unity **6000.3.25f1** compilation passed (`compile.paERlQ`);
focused EditMode **72/72 passed**, zero failed/skipped/inconclusive and successful
editor exit (`issue76-focused.uWao6L`). Final required EditMode **497/497 passed**,
zero failed/skipped/inconclusive and successful editor exit (`editmode.vPgLpb`).
Repository validation, whitespace and all
**36** Python runner contracts pass. Independent QA reports no blocking findings
following code and native screenshot review.

Owner-approved Play refinement (2026-10-20): button width matches the rendered
Paws text, height is 52 logical units and its localized label is bold. Pinned
compilation passed (`compile.Ys3uSL`), focused **72/72** passed
(`issue76-focused.2MC6W5`), and required full EditMode **497/497** passed
(`editmode.WY5AA9`), all with successful editor exits and no failed/skipped/
inconclusive tests. Existing mounted checks additionally verify text-width match,
centering, height and bold weight in all nine portrait/locale combinations.
Independent QA reports no blocking findings. Home screenshots below were refreshed
using the updated native review player (`issue76-review-build.YsRYyy`); its
`--game-loop-check=1` smoke passed again, including actual mounted Play clicks.

The macOS development review player built successfully
(`issue76-review-build.9tLUA1`). Native `--game-loop-check=1`,
`--tutorial-check=1` and SDK-free `--monetization-mock=1 --monetization-check=1`
smokes passed at EN 390×844 with simulated 59/34 safe insets. The navigation smoke
includes Clear/Hint, retained-session Play resume, completion locks and 20 fresh
boards; tutorial smoke uses simulated touch and manual Settings replay. Mock
checks exercise rewarded assistance and owned/unowned Settings without real
stores, ads or charges. Logs/player remain under ignored `.unity-results/issue76/`.

| Screen | Compact EN · 320×568 | Standard pt-BR · 390×844 | Tall ES · 430×932 |
| --- | --- | --- | --- |
| Home | [Capture](evidence/issue76/home-en320.png) | [Capture](evidence/issue76/home-pt390.png) | [Capture](evidence/issue76/home-es430.png) |
| Settings | [Capture](evidence/issue76/settings-en320.png) | [Capture](evidence/issue76/settings-pt390.png) | [Capture](evidence/issue76/settings-es430.png) |
| Language | [Capture](evidence/issue76/language-en320.png) | [Capture](evidence/issue76/language-pt390.png) | [Capture](evidence/issue76/language-es430.png) |

Enabled SDK-free Settings evidence: [localized mock price/unowned](evidence/issue76/settings-mock-unowned-en390.png),
[verified mock ownership](evidence/issue76/settings-mock-owned-en390.png).
Reproduce using `BoardReviewBuild.Build` with
`CATS_ARROWS_MONETIZATION_REVIEW=1` for mock-only diagnostics; the ordinary app
composes no provider. Run the resulting windowed macOS player with
`--board-locale=<locale> --board-width=<width> --board-height=<height>
--board-safe-top=<pixels> --board-safe-bottom=<pixels>` and exactly one of
`--menu-capture=<absolute-PNG-path>`, `--settings-capture=<absolute-PNG-path>` or
`--language-capture=<absolute-PNG-path>`. Captures isolate first-Play/history and
do not start a board. Compact captures use 20/0 insets, standard/tall use 59/34.
Owner-authorized local iPhone preparation completed on 2026-10-20 for source
`f15860d`: Release/arm64 export/archive, signatures/provisioning, in-place install,
plain launch and running process passed on iPhone 17 / iOS 27.0.1, Paws 0.1.0 (1).
[Device receipt](evidence/issue76/device-preparation.json) and
[release record](RELEASE.md#76-approved-menu--local-iphone-preparation-2026-10-20)
retain separate technical evidence. No physical interaction was automated.

Owner physical iPhone review passed on 2026-10-20. The owner reported
“iphone review finished, pass” for the supplied #76/#77 menu/settings checklist
on iPhone 17 / iOS 27.0.1, Paws 0.1.0 (1), installed source `f15860d`.
Scope: approved compact bold Play/gear comfort, Settings/Back, native-language
selection with immediate localization/persistence, Tutorial replay and Home Play
resume. This is the owner's human report, separate from automated technical
readiness and desktop evidence. It closes the pending physical menu review for
this source/device; broader-device coverage, measured performance and App Store
distribution acceptance are not inferred.

## Issue #72 initial menu (historical; superseded by #76)

2026-10-20: pinned Unity **6000.3.25f1** compilation passed
(`compile.mCNjsj`); final EditMode **480/480 passed**, zero failed/skipped/
inconclusive, successful editor exit (`editmode.didpG3`). Repository validation
and whitespace checks passed. `MenuPresentationTests` mounts all three locales
at 320×568, 390×844 and 430×932, checking safe bounds, minimum touch targets,
side-by-side actions, flag order/selection, removed copy and unchanged live-board
navigation. The full suite includes tap/drag, fixed cells, completion locks,
clear/new-board, tutorial and locale persistence regressions. Independent QA
reported no blocking findings after code and screenshot review.

The macOS review player passed the existing `--game-loop-check=1` navigation/
ordinary-play smoke at EN 390×844 with 59/34 simulated safe insets (Play,
Menu/resume, clear/hint, completion locks and next-board routing). The updated
`--hint-idle-check=1` smoke also passed, including fresh idle timing after pause.
Native menu
captures use 20/0 insets for compact EN and 59/34 for standard PT and tall ES:

| Compact EN · 320×568 | Standard pt-BR · 390×844 | Tall ES · 430×932 |
| --- | --- | --- |
| [Menu](evidence/issue72/menu-en320.png) | [Menu](evidence/issue72/menu-pt390.png) | [Menu](evidence/issue72/menu-es430.png) |

Reproduce captures with `BoardReviewBuild.Build`, then the local player arguments
`--board-locale=<locale> --board-width=<width> --board-height=<height>
--board-safe-top=<pixels> --board-safe-bottom=<pixels>
--menu-capture=<absolute-PNG-path>`. This opt-in path captures the initial menu
without starting gameplay and uses isolated first-Play/difficulty history stores.
Logs and the player remain under ignored `.unity-results/issue72/`.

Physical iPhone review is pending. Human checklist: confirm paw/title balance and
safe spacing, comfortable Play/Tutorial/flag targets, selected flag visibility,
all three localized labels, Tutorial/Play routes and live-board
Menu/resume. Desktop checks do not establish physical appearance or comfort.

## Physical iPhone tests: human review required

Owner policy for all future runs: physical iPhone interaction, appearance,
lifecycle and comfort tests must be performed by a human. Do not automate these
tests through Device Hub, screen mirroring, scripted touch, in-app diagnostic
flows or another remote-input tool. If a device check cannot be completed by
the agent without such automation, hand it to the owner/human reviewer.

When authorized, agents may prepare the build, verify signing/profile/device
compatibility, install, launch and check that the process runs. Then provide a
short checklist for the changed behavior and request the human's results.
Record the source/build, device/OS, reviewer, reported pass/fail, coverage and
remaining gaps. Missing human feedback means **physical review pending**;
build/install/launch success, captures and automated results cannot close it.
Keep desktop/simulated input tests and Unity tests as separate evidence.

Historical agent-driven physical-device exercises remain factual records but
do not satisfy this human review gate. Do not repeat their automation workflow.

## Issue #53 Paws physical appearance

PR #54 prepared source `a17ed5b` as an arm64 Release build, verified signing and
device compatibility, and installed/launched it on iPhone 17 / iOS 27.0.1. The
owner subsequently reported on 2026-10-19 that the physical-iPhone appearance was
reviewed and approved. This closes the pending appearance scope for the Paws app
name/icon, paw readability and localized visual fit.

The report does not add a new tap/drag, lifecycle, performance, comfort,
smaller-phone or distribution-archive result. Those remain separate task-specific
checks. Automated and desktop validation for the rebrand remains recorded in PR
#54; the later full behavior-changing baseline passed 473/473 EditMode tests in
PR #56.

## Issue #44 timed completion

2026-10-16: pinned Unity **6000.3.25f1** compilation passed (`compile.d5Lj3z`);
complete EditMode **466/466 passed**, zero failed/skipped/inconclusive and
successful editor exit (`editmode.P5v8B4`). Repository and whitespace checks pass.
`CompletionCelebrationTests` covers exact three-second confetti cutoff,
250 ms sequential fades, four-second message fade-out, disabled early actions,
duplicate wins, cancellation, time jumps and invalid/backwards clocks.
`CompletionShellTests` covers final-Hint victory, same-board static return,
Hint/Clear/tap/drag locks, unchanged history, duplicate notification, Menu/locale,
focus/pause, detach, same/different-session rebind, stale ticks, both tutorial
stages and lowercase localized resources. Existing interaction, tutorial,
difficulty and domain suites pass. Independent QA found no blocking findings.

The opt-in macOS review player `--completion-check=1` passed mounted mouse/touch
and final-Hint flows in EN **320×568**, pt-BR **390×844** and ES **430×932**.
Checks include panel picking for all three completion actions, static completed
marks/lock, unchanged grid bounds, safe-area button bounds, existing controls,
Next selection/anti-streak and Menu return without replay. Physical-pixel snapping
is allowed in world-bound measurements; button minimum style height remains 48
panel units. Simulated top/bottom insets were 20/0, 59/34 and 59/34 pixels.
Logs and all nine captures are under ignored `.unity-results/issue44/`.

| Portrait / locale | Celebration | Actions |
| --- | --- | --- |
| Compact EN | [message/confetti](evidence/issue44/celebration-en320.png) | [actions](evidence/issue44/actions-en320.png) |
| Standard pt-BR | [message/confetti](evidence/issue44/celebration-pt390.png) | [actions](evidence/issue44/actions-pt390.png) |
| Tall ES | [message/confetti](evidence/issue44/celebration-es430.png) | [actions](evidence/issue44/actions-es430.png) |

Reproduce with the existing `BoardReviewBuild.Build` local review build, then run
its executable with `--completion-check=1 --game-seed=14 --board-locale=<locale>
--board-width=<width> --board-height=<height> --board-safe-top=<pixels>
--board-safe-bottom=<pixels> --game-captures=<absolute-directory>`.
The diagnostic uses an injected clock and isolated difficulty/first-Play stores;
normal play uses realtime. Desktop captures do not establish device touch comfort
or lifecycle behavior; physical-device evidence follows.

Historical agent-driven Device Hub exercise (2026-10-16): source
`9a1ead34102321fa5224fbafe5fe336b96c00da5` exported with pinned Unity
6000.3.25f1 and built arm64 Release with Xcode 27.0. Strict signature/profile
checks, install and launch passed on **iPhone 17 Juca, iOS 27.0.1**. Apple's
Device Hub drove the physical screen/input with ordinary realtime and no review
launch arguments. The automated exercise passed tap cycling, horizontal drag, arrow/protected
cells, Clear, final-Hint and tap victories, EN/pt-BR/es celebration text, all three
completion actions, identical locked static return, Menu/locale no replay and
Next routing. Ordinary starts included Hard/Hard/Easy, respecting the anti-streak.
Backgrounding during a tap celebration resumed the **same process** with the same
static finished board and no replay. Tutorial entry/Continue/Skip also passed.
English was restored and a fresh ordinary board was left running.

[Physical celebration](evidence/issue44/iphone-celebration-en.png),
[physical actions](evidence/issue44/iphone-actions-en.png),
[same-board foreground return](evidence/issue44/iphone-resume-static-pt.png).
These are historical Device Hub captures of a physical device. This automated
exercise does **not** satisfy the owner's human physical iPhone review gate.
The owner explicitly approved merging and closing #44/#52 on 2026-10-16;
no detailed human pass/fail report was supplied, so no such result is claimed.
It is not calibrated timing/frame-rate
measurement or an owner-finger comfort report;
the complete guided/independent lesson remains covered by the automated suite.
No source code changed for this smoke run.

Storage: initial free space was 1.68 GiB. Apple's supported
`simctl runtime dyld_shared_cache remove --all` cleared rebuildable simulator
caches while preserving runtimes and simulator data. One device build used no
archive/IPA and no separate dSYM. The signed app, source receipt/hashes, small
export metadata and evidence remain in ignored `Builds/Issue44iPhone/` and
`.unity-results/issue44-iphone/`; temporary iOS export, task DerivedData and Bee
cache were removed. The prior signed #48 app/symbols remain intact. Free space
after cleanup was **8.35 GiB**.

## Issue #29 monetization design test plan

2026-10-16 integration of PR #31 with current `develop` preserves #43 first-play,
difficulty/history and queued-command safeguards and #47/#48 holds/idle styling.
The existing idle clock, six-second boundary and immediate reset are retained
around asynchronous rewarded-Hint delivery. New mounted-panel tests cover reward
and no-reward closes, busy suppression, a fresh post-close interval, focus/pause
suppression and explicit recovery of an earned reward. Fourth-win progression
tests now cover Easy, Medium and Hard with isolated difficulty fixtures.
Pinned compilation passed (`compile.2lkgP6`); complete EditMode **450/450 passed**,
zero failed/skipped/inconclusive and successful editor exit (`editmode.tlafB7`).
Independent integration QA found no blockers. Default runtime still constructs no
monetization coordinator/providers; successful mocks require the existing isolated
Editor/development opt-in. Real-provider/device/store validation remains deferred.
The merged development player passed mounted mock flows at EN 320×568, pt-BR
390×844 and ES 430×932. Default ad-free idle checks passed mouse/touch at EN
320×568 with **zero** geometry delta; the ordinary pt-BR 390×844 game loop passed
20 Next requests, and guided/independent tutorial checks passed EN 390×844.
Logs/captures are under ignored `.unity-results/integration31`. The temporary
desktop player and generated build cache are disposable; the owner-tested #48
signed iPhone artifact remains tied to its original source commit.

Implementation #30 adds `MonetizationTests`, `MonetizationStateStoreTests`,
`MonetizationShellTests` and `HintPreparationTests`. These exercise the pure state
machine, actual disk restart/checksum/backup/future-schema/revision/lock behavior,
reward storage faults, entitlement/pending/finalization and localized UI/input
regressions with controlled SDK-free adapters. Native execution evidence is recorded
with the implementation PR; the broader future real-provider matrix below remains
an integration gate, not a claim that any live ad/store integration was tested.

SDK-free implementation validation (2026-10-14): pinned Unity 6000.3.25f1 compile
passed; final EditMode report `editmode.ObOY2x` passed 319/319 with zero failed,
skipped or inconclusive and successful editor exit. A development macOS review
player passed mounted checks in EN 320×568, pt-BR 390×844 and ES 430×932, including
button text/bounds, reward delivery and safe input return, mock purchase and direct
owned hints. Fifteen captures/logs are retained under ignored `.unity-results/`;
three representative images are in `docs/evidence/issue30/`. QA found no blockers.
No physical-phone, real-provider sandbox, charge or store checks were performed.

The matrix below remains the contract/integration target; it is not a claim of
real-provider execution.
Use pure-C# policy/coordinator tests with manually controlled ad/store callbacks,
fake clock, revisioned memory store/fault injection and deterministic operation IDs.
Recreate coordinators from serialized state to test restart, not just object reuse.

| Area | Required cases |
| --- | --- |
| Cadence | Completions 1–3 none, 4/8/12 due; starts/abandon/Clear/generation failure none; same seed/new instance counts; duplicate win and final-hint win each count once |
| Boundaries | No ad at victory/Menu/launch/resume; explicit fresh-board transition only; tutorial guided/independent and already-complete fixtures excluded; fourth win persists before restart |
| Opportunity handling | Persist-before-present; crash before/after consumption; no-fill/offline/restricted/failed skip without debt; no queued catch-up; final rewarded-hint win retains fourth-completion opportunity |
| Storage | Atomic envelope and expected-revision conflicts, migration, damaged primary/last-good recovery, missing and future-schema preservation, write failures during count/reward/purchase; no mandatory ad loop |
| Hints | Prepare without mutation; no eligible fact means no ad; reward vs close-only; duplicate taps/reward/closed callbacks in either order; board/revision mismatch; no stale auto-apply; one recovered grant; crash around apply/consume; earn-write and consume-write failures retain in-memory deduplication and unlock only after native closure; retry latest snapshot, not consumed grant; completed/tutorial locks |
| Ownership | Disabled direct/free; Owned bypasses both placements and survives restart/offline; grant consumes due milestone; revocation creates no catch-up; unknown bootstrap cannot charge restored owner an ad |
| Purchase/restore | Pending/cancel/verification/network failure cannot grant/revoke; pending survives restart and incomplete queries, including failed pending-state write; no duplicate purchase before pending refresh; verified success/AlreadyOwned; persist before success/finalization; replay unfinished store work; duplicate transaction updates; same-store fresh-install restore; authoritative revocation; adapter migration preserves ownership |
| Presentation races | Purchase during queued/presented ad, timeout vs native overlay, pause/resume, focus loss, menu/session replacement, input cancellation and localization changes; no mutation beneath ad or duplicate hint delivery |
| Adapter portability | Identical contract suite for fakes and future provider sandboxes; late callbacks, unsupported/restricted inventory, platform evidence, finalization replay and store-account changes |

Mounted/UI checks retain tap cycle, axis drag, protected cells, completed-board lock,
Clear/New Game behavior, menu/replay/tutorial flows and representative compact/
standard/tall portrait layouts in en/pt-BR/es. Check Hint (ads)/Hint, earned status,
Restore/owned/pending/error labels and touch targets without color-only information.
Physical iPhone lifecycle/ad/store sandbox checks are future integration gates;
Android follows its own deferred platform task. No live charge or production SDK
is needed for design validation. Documentation changes use scripts/validate.sh
and git diff --check; native compile/EditMode runs apply when code is implemented.

## Issue #48 idle Hint attention

The idle controller uses an injected clock: tests advance time directly, including
5.999/6-second boundaries, reset cycles and active-press suppression. Shell tests
exercise eligibility and lifecycle resets independently of successful mutations;
the existing Hint, tap/drag, arrow hold, tutorial and completion suites remain
regression gates. Timer advancement must never apply a Hint or mutate marks.

The opt-in mounted player diagnostic `--hint-idle-check=1` advances the same fake
clock and dispatches mouse/touch events through the board and Hint button. It
checks non-marking arrow/protected presses, long presses, cancellation, the active
arrow outline, Hint/Clear, Menu/modal/locale replacement and final-Hint completion.
Captures compare unchanged button/grid bounds and localized labels with stable
inverted Hint colors. Run compact, standard and tall portrait
layouts in en/pt-BR/es. Native desktop checks do not establish physical-phone
appearance; iPhone smoke remains a pre-merge gate.

2026-10-16: pinned Unity **6000.3.25f1** compilation passed
(`compile.iexUD0`); final EditMode **375/375 passed**, zero failures, skips or
inconclusive results, with successful editor exit (`editmode.Hrc8QT`). This
includes nine pure clock cases and seven headless `UIDocument` shell tests,
covering fractional reset times, no-result Hint, all difficulties, modal/Menu,
focus/pause, disabled/suspended play, rebind/detach, failure/retry, completion and
both tutorial stages. No graphics errors are suppressed and no tests are skipped.

Final macOS review build passed (`issue48-review-build.jzXvUI`). Mounted idle
checks passed mouse/touch in **Hard EN 320×568**, **Easy pt-BR 390×844** and
**Medium ES 430×932**. Checks include secondary-pointer/mouse duplication,
hover, swept drag, no-op presses, a visible #47 arrow hold and stable Hint/Clear
bounds, Hint content center, label and grid. Maximum measured geometry difference
was **0.0000153 panel units** (float rounding; assertion tolerance 0.01).

Existing mounted input and arrow-hold checks passed for mouse/touch; fixed guided
and independent tutorial checks and the ordinary game loop (20 Next requests)
also passed. Independent interaction QA found no blocking findings. Logs are
under ignored `.unity-results/issue48/`.

The captures below predate the inverted-color refinement and show the original
border/font treatment on generated ordinary boards:
[Hard compact](evidence/issue48/hint-idle-en320.png),
[Easy Portuguese](evidence/issue48/hint-idle-pt390.png),
[Medium Spanish tall](evidence/issue48/hint-idle-es430.png).
These earlier captures were followed by the physical-iPhone review below.

Styling refinement: attention swaps the normal Hint text/background colors with a
native 200 ms fade-in; resets set transition duration to zero and restore normal
colors immediately. Existing shell assertions cover both colors, unchanged
border/font/padding, transition properties and duration, including a reset during
fade-in. Timer, eligibility and interaction tests are unchanged.
Pinned Unity 6000.3.25f1 focused EditMode validation passed **26/26**, zero
failed/skipped/inconclusive, with successful editor exit (`hint-style48.CFa20C`):
`HintAttentionShellTests`, `HintIdleAttentionTests` and `InlineHintTests`.
Repository validation and whitespace checks passed. The styling refinement was
not rebuilt for desktop captures.

Physical iPhone review (2026-10-16): source `0cc755f54dc9c3171df13f21b1a8707875eb7aaf`
exported with Unity 6000.3.25f1 and built with Xcode 27.0. Strict signature and
device-profile checks, installation, launch and running-process checks passed on
iPhone 17 Juca. Owner reported **pass** for the six-second color inversion and
smooth fade without movement, immediate resets via Hint/board taps/holds/Clear,
and suppression on Menu/tutorial/completion/backgrounding. This is a single-device
owner smoke result, without calibrated timing or comfort measurements. Signed app,
symbols and receipt are retained under ignored `Builds/Issue48Inverted`; native
logs are under `.unity-results/issue48-inverted-iphone`.
Pre-merge validation of the final styling revision: pinned compile passed
(`compile.XBvefC`); complete EditMode **375/375 passed**, zero failed/skipped/
inconclusive and successful editor exit (`editmode.VeLLUS`).

## Issue #47 arrow-path hold

Threshold-only follow-up (2026-10-16): activation is now **350 ms**. Focused
`ArrowHoldControllerTests` passed **17/17**, zero failed/skipped/inconclusive,
with successful editor exit (`arrowhold350.wyaLQZ`). The boundary test checks
349 ms versus 350 ms; hold duration, release/cancel and movement remain unchanged.
QA and repository checks passed. No player/device build or full suite was rerun;
the 1.0-second implementation evidence below is historical, and the installed
iPhone build still uses that earlier threshold.

Pinned Unity 6000.3.25f1 compilation passed (`compile.jdXwdO`); native EditMode
`editmode.muSyXZ` passed **359/359**, with successful editor exit and zero
failed/skipped/inconclusive. Clock-injected controller cases cover the one-second
boundary, early release, cancellation, repeated presses, one-pointer ownership,
jitter/movement, completion, invalid coordinates/time and non-arrow eligibility.
All eight directions on 5×5/6×6/7×7 use independently calculated expected rays;
mixed rays include arrows, protection, X and Cat without mutating marks. Two
Hint regressions retain Clear's outline removal in both inline and legacy modes.
Existing input, fixed tutorial, generator/goldens and completion-lock tests pass.

Final native macOS mounted hold checks passed for synthetic mouse and touch in
EN 320×568, pt-BR 390×844 and ES 430×932. They cover full diagonal outlines,
one-second eligibility, release, capture-out/pointer cancellation, movement,
suspension/rebind/completion, unchanged marks and Hint outline coexistence/expiry.
The harness advances a fake hold clock and synchronizes with the panel scheduler;
pending capture changes are flushed through normal pointer post-dispatch. A
bounded scheduler timeout aborts rather than reporting a pass.

Existing mounted input checks passed for mouse/touch; ordinary game flows passed
in all three locales (20 Next board requests each), and the EN 390×844 guided and
independent tutorial flow passed. Independent interaction QA found no blockers.
Repository validation and whitespace checks passed. Logs and full captures are
under ignored `.unity-results/issue47/verified/`; final player build log is
`.unity-results/issue47/review-build-dispatch.log`.

The [compact outline](evidence/issue47/arrow-path-en320.png) and
[Spanish tall capture](evidence/issue47/arrow-path-es430.png) use a deliberate
7×7 input fixture containing arrows/protection/player marks, not a generated Hard
puzzle. They demonstrate presentation and pointer behavior, not generator rules.
Physical iPhone smoke of the new hold gesture remains a pre-merge validation
step; desktop checks do not establish touch comfort. No #48 idle-Hint behavior,
#44 completion effect or PR #31 integration is included.

## Issue #43 persistent Play selection

Pinned Unity 6000.3.25f1 compilation passed (`compile.SLmw3I`); native EditMode
`editmode.1BTfgW` passed **340/340**, with successful editor exit and zero
failed/skipped/inconclusive. Deterministic policy tests cover every eligible-mode
boundary, long sequences with history reconstruction, a frozen cross-runtime
seeded sequence, malformed/unsupported records, pending retries, duplicate starts
and failed persistence without publishing a session. Preference tests restore
pre-existing owner values and cover first Play across restarts, fresh manual
Tutorial, legacy returning-user migration and history unaffected by tutorial/Menu.
Existing generator goldens, fixed tutorial, input and completed-board tests pass.

Native macOS mounted flows passed EN 320×568, pt-BR 390×844 and ES 430×932,
including localized Play/Next board, absence of difficulty choices, mixed-profile
starts, 20 Next board requests per flow, tap/drag, protected cells, Hint/Clear/Menu
and completed-board locking. A separate EN 390×844 mounted tutorial flow passed
first-Play routing, guided/independent lessons and subsequent ordinary Play.
The [compact menu](evidence/issue43/menu-en320.png) uses the ordinary-flow
diagnostic with Tutorial disabled; production retains Tutorial, verified by shell
tests and the separate tutorial flow. The [Hard capture](evidence/issue43/hard-en320.png)
shows the unchanged compact board with Next board.

Independent persistence/interaction QA found no blockers. Repository validation
passed. Logs and full captures remain under ignored `.unity-results/issue43/`.
These are desktop portrait simulations; no physical-device smoke test was run
for #43. Completion effects (#44) and PR #31 remain excluded.

## Issue #37 Hard hatch regression

Pinned Unity 6000.3.25f1 compile passed (`compile.Uv7Gv7`); native EditMode
`editmode.imRrEn` passed **303/303**, successful editor exit and zero
failed/skipped/inconclusive. Hard policy tests require no solver proof and return
no initial hatches even on boards with cardinal one-cat NotCat consequences.
Generated-session/UI snapshots verify former hatch cells start Empty, accept
manual X via tap/drag, reset on Clear, and stay unhatched across Menu, language
and optional Hint. Arrow protection and completion locks retain existing tests.
The fixed Easy tutorial's exact hatch set remains unchanged after Hard selection.

Medium's 131 full receipts (seeds 0–127, -1, int.MinValue, int.MaxValue) match
the original revision exactly, including board/solution hashes, versions and work
counters. Existing Easy goldens, signed Hard v3 replay, 128-seed/profile independent
human/uniqueness proofs, independent pruning reference and tight-budget/cancellation
coverage pass. Hard's int.MaxValue golden intentionally changes because pruning
now scores actual arrows alone; Medium's frozen goldens remain unchanged.
Core focused checks passed 34/34. QA found no blockers. Logs/corpus comparison
are under ignored `.unity-results/issue37/`. This is not new physical-device or
perceived-difficulty evidence; the #35 phone numbers below refer to Hard v2.

Mounted native macOS flows passed Hard EN 320×568, Hard ES 390×844 and Medium
pt-BR 390×844: actual menu/button and board pointer events, tap cycle, protected
arrows, Hint/Clear/Menu, 20 New Games per flow, centered grids and completed locks.
The [compact Hard capture](evidence/issue37/hard-en-320x568.png) shows normal
interactive cells instead of derived hatches. Logs and all 18 captures remain
under `.unity-results/issue37/`; portrait simulation does not prove phone comfort.

Physical iPhone 17, 2026-10-15, playable develop `2131cc9`: the owner confirmed all
requested hatch-fix checks passed. Multiple Hard boards had no initial hatches;
former one-cat-arrow consequence cells accepted manual X by tap/drag. Hint,
Clear, Menu/back, repeated New Game and completed-board lock passed; brief
Easy/Medium hatch regression checks also passed. The owner reported “hard feels
better.” Build/export, native signing verification, install and launch passed;
the existing 303/303 test evidence applies to identical merged code. This is
qualitative evidence on one device, not measured touch-to-ready latency,
calibrated difficulty or smaller-device coverage. Signed app/symbols and receipt
are under ignored `Builds/Issue37Playtest/`, with logs/duplicate receipt under
`.unity-results/issue37-playtest*`. See [#37](https://github.com/juarezweiss/Paws/issues/37).

## Issue #35 clue-pruning pilot

Pinned Unity 6000.3.25f1 compile passed (`compile.KyrwU0`); native EditMode
`editmode.ejNSDX` passed **294/294**, with successful editor exit and zero
failed/skipped/inconclusive. This includes unchanged Easy goldens/tutorial/shell
regressions, new signed Medium/Hard v2 receipts, paired 128-seed/profile
occupancy/proof checks, independent two-order reference selection, and zero/tiny
phase/shared-budget retention of only previously admitted boards. Default cases
complete both orders; all proof work is accounted for in phase and shared limits.

The fresh seeds 1000–1999 accepted 1,000/1,000 per tuned profile and matched every
research-selected fingerprint. Each result was independently re-assessed and
work-checked. Combined arrow/hatch occupancy averaged 20.9% Medium and 26.7% Hard;
arrow p10/median/p90 was 3/5/9 and 7/10/13. Median waves to the last cat, counting
the UI's existing hatches as already known facts, were 5 and 7. No tuned request
exhausted pruning in this corpus. Peak shared human/verifier work was
16,475/113 and 49,351/419, below the unchanged aggregate ceilings. Raw reports
and the measurement-only wave harness are under ignored `.unity-results/issue35/`.
These are corpus observations, not universal yield or perceived-difficulty proof.

The opt-in native macOS/Mono benchmark completed 1,000/1,000 successful samples
for each Easy/Medium/Hard profile with schema-v2 clue/version/work fields populated.
Warm Generate p95 was 11.14/28.36/66.63 ms respectively. These are desktop
generation-only measurements, not phone/New Game input latency. Reports and
build logs remain under `.unity-results/issue35/benchmark-*`; new per-board clue
metrics are calculated outside the timed Generate/allocation scope.

Physical iPhone 17 / iOS 27.0.1, source `e9b19ca`, Xcode 27.0 Release/IL2CPP:
three fresh Hard benchmark launches each accepted seeds 0–999 (3,000 successes,
1,000 distinct seeds). Warm p95 was 35.44/37.24/37.30 ms; pooled p95 36.95 ms,
maximum 53.52 ms. First invocation was 67.37–69.05 ms, excluding app startup.
Every fingerprint matched the native macOS Hard report; reported proof/pruning
work stayed within all ceilings with no pruning exhaustion. Full reports,
logs and checksums are retained under `.unity-results/issue35/`, and the signed
diagnostic/symbols under `Builds/Issue35Pruning/`. The prior playable review app
was restored after measurement. These are generation-only measurements on one
device, not New Game touch-to-ready latency, Medium phone timing or perceived
challenge/playtest evidence for the tuned boards.

The earlier #32 phone smoke evidence below covers the original generator/profile.
It does not establish the tuned pilot's player comfort or subjective challenge.

## Issue #32 difficulty coverage

Pinned Unity 6000.3.25f1 compile passed; EditMode `editmode.dIeNZn` passed 285/285
with successful editor exit, zero failed/skipped/inconclusive. Difficulty tests
cover 128 fixed seeds per profile with independent human/uniqueness reassessment,
truthful arrows, quotas/zeros/required 3, frozen signed-seed receipts, explicit
budgets/cancellation/invalid profiles and concurrent/interleaved replay. Easy
goldens and the existing tutorial tests pass; new shell regressions check the
unchanged tutorial fingerprint after Medium/Hard selection, localized choices,
actual/live-vs-next labels, retry, protected input and completed-board invariants.
Separate pure-C# seed 0–999 corpora accepted 1,000/1,000 for each new profile.

Mounted macOS checks passed Easy/Medium/Hard EN 320×568, Medium pt-BR 390×844,
Hard ES 430×932: actual menu pointer selection, Hint/Clear/Menu/NewGame, protected
cells, pointer tap cycle/completion/readonly state, centered grid and 20 new-game
transitions per run. Thirty captures and logs are retained in ignored
`.unity-results/issue32-captures/`; representative images are in
`docs/evidence/issue32/`. These are desktop portrait simulations, not physical
phone comfort/latency evidence. QA found no blocking source defects. No new solver
rules or difficulty-score thresholds were used.

Physical iPhone 17 / iOS 27.0.1, 2026-10-14: the owner confirmed all requested
Medium 6×6 and Hard 7×7 smoke checks passed: edge/rapid/held taps and the tap cycle,
horizontal/vertical drags, Hint/Clear/Menu/back, repeated New Game, hints-to-win
and completed-board lock, background/reopen and portrait lock. No missed/extra
marks, cramped targets, unreadable clues, visible pauses or layout problems were
reported. The owner also confirmed all three languages (English, pt-BR and
Spanish) passed. This is qualitative acceptance on one device; smaller physical
phones and measured Medium/Hard p95 latency are not established by this run.

The Release/IL2CPP arm64 review build used source d3452ba (gameplay identical to
develop 2ca1e14), Unity 6000.3.25f1 and Xcode 27.0 (27A266a). Export, final Xcode
build, signature verification, install and launch passed. Logs/receipt remain in
ignored `.unity-results/issue32-ios-export.log`, `issue32-ios-xcode-final.log`,
`issue32-iphone-install-final.json`, `issue32-iphone-flow-final.log` and
`issue32-device-build.json`. Signed app/symbols remain in
`Builds/Issue32Difficulty/Playable/` and `Symbols/`; generated Xcode intermediates
were removed after preservation. Managed launch arguments did not enable the
diagnostic flow, so no fresh automated phone timing result is claimed.

## Local Unity validation

Bootstrap tooling is tracked by GitHub Issue #1. Use a local Codex task opened on
this checkout to run the Unity editor installed on your machine. Running the Codex
desktop app does not move an existing cloud task onto your Mac.

If fetching a branch fails with `could not read Username for 'https://github.com'`,
authenticate Git on the Mac; the cloud task's Git proxy authentication is not
automatically shared with a local checkout. With GitHub CLI installed, run these
commands yourself in Mac Terminal and complete sign-in through the browser:

```bash
gh auth login --hostname github.com --git-protocol https --web
gh auth setup-git --hostname github.com
```

Then local Codex can fetch/check out the issue branch (or use `gh pr checkout 9`
for this bootstrap PR). Check the working tree before switching and preserve local
changes. Alternatively, use GitHub Desktop's signed-in UI to fetch and switch to
the branch before reopening local Codex. Never paste credentials into chat or copy
cloud tokens into the local Git configuration.

Repository JSON validation accepts plain UTF-8 and Unity-generated UTF-8 BOMs
without rewriting package manifests or assembly definitions. Malformed JSON still
fails validation.

Prerequisites:
- Install and activate the pinned Unity 6.3 LTS editor through Unity Hub. The
  scripts read the exact patch from `UnityProject/ProjectSettings/ProjectVersion.txt`.
- Put Python 3.11+ on PATH for `scripts/validate.sh` (which uses `tomllib`) and result
  checking. Unity Hub does not install this prerequisite; Mac system Python may be
  absent or older. Check with `python3 --version`.
- Close the GUI Editor for this project before batch runs. Unity cannot open the
  same project in two editor processes concurrently.

Run from the repository root, or pass an absolute path to a script from elsewhere:

```bash
bash scripts/unity.sh
bash scripts/validate.sh
bash scripts/compile.sh
bash scripts/test.sh
```

Automatic discovery on macOS uses:
`/Applications/Unity/Hub/Editor/<pinned-version>/Unity.app/Contents/MacOS/Unity`.
Linux Hub installs use `$HOME/Unity/Hub/Editor/<pinned-version>/Editor/Unity`.
The scripts never fall back to a different installed patch.

For a custom Hub editor directory:

```bash
UNITY_HUB_EDITOR_ROOT="/Volumes/Development/Unity Editors" bash scripts/test.sh
```

For another installation layout, explicitly select the editor executable:

```bash
UNITY_EDITOR="/Applications/Unity/Hub/Editor/6000.3.25f1/Unity.app/Contents/MacOS/Unity" bash scripts/test.sh
```

An invalid explicit override fails rather than silently falling back. An explicit
override can select a different editor; inspect its version first and keep any
intentional project-version upgrade in separate work.

Compilation imports the project in batch mode and retains `Editor.log`. EditMode
runs omit `-quit` so the Test Framework controls completion. Each run creates a
fresh directory under ignored `.unity-results/`; test runs retain `Editor.log` and
`test-results.xml`. A stale report cannot satisfy a subsequent run. Editor failures
retain their exit status, and missing/malformed/zero-test, failed, skipped, or
inconclusive NUnit results make the test command fail. Compilation alone does not
prove the tests ran.

The bootstrap has eight original EditMode tests. Issue #5 corrects the ordering in
`ExtraPawPreventsVictory`: the extra paw is placed before completing the solution.
The test checks that each mark was applied, that the extra paw prevents victory,
and that removing it immediately wins. Gameplay keeps immediate victory and the
completed-board lock; the correction changes only the fixture.

## Issue #55 internal Paw terminology compatibility

Gameplay/domain APIs, tests, localization keys, and current source-of-truth docs
use Paw terminology. Renamed enum members retain their previous numeric values:
`CellKnowledge.Paw = 1`, `CellKnowledge.NotPaw = 2`, `PlayerMark.Paw = 2`, and
`BoardCellKind.Paw = 2`; tutorial step values are also explicit and unchanged.
Board progress and solver state are not persisted. Existing preference keys,
generator/ruleset versions, fingerprint prefixes and canonical coordinate input
remain unchanged, so prior reproduction receipts keep identical hashes. The
`generation-benchmark-v2` JSON fields `cats` and `stepsToLastCat` are retained as
legacy schema names.

The runner contracts themselves can be checked without Unity:

```bash
python3 -m unittest discover -s scripts/tests -v
```

These tests use a process fixture to exercise discovery, quoting, working-directory
independence, fresh results, and failure handling. They do not validate Unity import
or replace the C# EditMode suite.

References: [Unity editor arguments](https://docs.unity3d.com/6000.3/Documentation/Manual/EditorCommandLineArguments.html)
and [Test Framework arguments](https://docs.unity3d.com/6000.3/Documentation/Manual/test-framework/reference-command-line.html).

## Issue #3 solver and localization checks

`SolverTests` includes a profile-valid 5x5 target, individual deduction rules,
trace replay, and an independent exhaustive oracle over 1,377 small-board clue
variants. The oracle enumerates placements without using production geometry or
deduction code, and checks each recorded conclusion against surviving completions.
Fixtures cover cardinal traversal through arrow cells, shared/satisfied arrows,
invalid inputs, contradictory clues, deterministic ordering, immutable results,
and work/node budget boundaries.

A separate constructed 7x7 fixture proves that uniqueness does not imply human
completion: the verifier proves one solution while the approved human rule set
stalls, and admission rejects it. That fixture uses an explicit test-only 7x7
profile permitting four cats per line and no zero lines; it does not relax
Easy/Medium/Hard generation policy.

Malformed sizes, clue lengths, coordinates, or directions are `InvalidInput`.
Well-formed but infeasible constraints, including totals exceeding non-arrow
capacity, are `Contradiction`/`NoSolution`. Finding one witness before the search
budget expires is `Incomplete`, never `Unique`.

`LocalizationTests` checks complete key and placeholder coverage for `en`,
`pt-BR`, and `es`, English fallback, device and explicit selection, accented text,
locale formatting, resource immutability, and extension with another locale.
Actual solver traces and verifier results are checked across the three cultures.
Future UI work must separately verify saved preferences, font coverage, and long
translations at representative portrait aspect ratios; there is no board/settings
UI to exercise yet.

## Issue #5 session regression coverage

`GameSessionTests` covers the tap cycle, extra-paw victory regression, mixed
Empty/X non-solution marks at victory, completed-state tap/drag/Clear rejection,
and Clear preserving puzzle identity and protected eligibility. Mixed drags
include Empty, X, Paw, arrow, protected, duplicate, and outside-board coordinates.
All four board edges, null/empty drag, null puzzle, copied protected input,
duplicate/arrow normalization, and invalid protected coordinates/paws are tested.

Collection mutation attempts before and after victory must throw, while the live
read-only marks view must continue to reflect legitimate session transitions.
A generic zero-paw fixture starts completed; this does not alter generated Easy
policy. Gesture axis locking and release-event suppression belong to Issue #7
and are not claimed as tested by these pure session tests.

Native macOS Unity `6000.3.25f1`, 2026-10-07, Issue #5 branch based on `develop`:
repository validation and compilation passed; **59/59 EditMode tests passed**,
including 25 session cases, with zero failures/skips/inconclusive results.
Logs: `.unity-results/compile.m1cUrF/Editor.log` and
`.unity-results/editmode.dhZFAw/test-results.xml`. That historical run excluded the then-unmerged Issue #4 generator tests;
their separate results are recorded below.

After updating PR #13 from `develop` on 2026-10-08, native compilation and the
combined suite passed: **81/81**, zero failed/skipped/inconclusive. Logs:
`.unity-results/compile.iB1O8C/Editor.log` and
`.unity-results/editmode.s34z2D/test-results.xml`. The merge conflict was limited
to this documentation; both session and generator test records were retained.

## Issue #4 generation checks and cost report

`GenerationTests` locks SplitMix64 golden vectors, signed seed mapping, bounded
rejection sampling, independently calculated SHA-256 fingerprints, and accepted
board/solution fingerprints for seeds `0`, `-1`, `int.MinValue`, and
`int.MaxValue`. It checks reproduction from recorded options across cultures,
instances, interleaved requests, and concurrent requests on one generator.
Separate positive and zero work limits exercise layout, random, human, and
verification exhaustion. Pre-cancellation returns no board and consumes no work.
Returned puzzle and diagnostics collections reject mutation.

The fixed `0..999` Easy corpus independently checks cat positions, row/column
counts, full-ray arrow truth, clue ceilings, and complete admission for every
success. Timing encloses only `Generate`, after an unmeasured warm-up; independent
post-generation checks are excluded. Timing is informational, never a correctness
assertion. The known corpus is a regression set, not a guarantee that every
possible seed succeeds.

Native macOS Unity `6000.3.25f1`, 2026-10-07: repository validation and compilation
passed; all **61 EditMode tests passed**, with zero failed/skipped/inconclusive.
Final corpus results:

| Measurement | Median | 95th percentile | Maximum |
| --- | ---: | ---: | ---: |
| Attempts | 1 | 1 | 2 |
| Layout nodes | 6 | 6 | 12 |
| Human constraint evaluations | 1,634 | 3,309 | 5,770 |
| Verifier nodes | 1 | 1 | 7 |
| Random draws | 156 | 268 | 503 |
| Generation time (ms) | 4.306 | 7.973 | 13.835 |

All 1,000 requests succeeded: 1,000 distinct logical boards and 971 distinct cat
layouts, excluding seed metadata. One layout reached the arrow ceiling and its
next attempt succeeded. Boards had 4–10 cats (mean 6.962) and 2–10 arrows (mean
5.053). The earlier warmed run had median 4.343 ms, p95 8.121 ms, and maximum
17.531 ms; these are observed editor timings, not mobile or cold-start guarantees.

The runtime allocation counter did not advance on a known allocation probe, so
allocation bytes are **unavailable**, not zero. Device profiling remains required
for allocations, New Game latency, frame pacing, and eventual background queue
integration. Measure on representative iOS/Android hardware before deciding on
loading animation duration or background scheduling policy.

## Pyramid

1. Pure domain/EditMode tests: highest volume.
2. Unity integration/PlayMode tests: board/session/UI wiring.
3. Device smoke tests: iOS + Android touch, safe-area, performance, lifecycle.

## Mandatory puzzle tests
- arrow cell can never be solution paw;
- row/column totals equal solution;
- maximum paw counts by profile;
- zero-row/zero-column cap;
- arrow truth for every generated arrow;
- generated puzzle unique;
- human solver completes without guessing/hints;
- same seed + generator version => same logical puzzle;
- invalid candidates rejected.

## Mandatory session tests
- tap: Empty -> X -> Paw -> Empty;
- drag affects Empty only;
- drag never modifies arrow/protected/completed cells;
- drag processes each cell once;
- exact solution triggers win immediately;
- extra player paw prevents win;
- Empty and X are both acceptable outside solution;
- Clear resets player marks only;
- New Game replaces puzzle but preserves selected difficulty;
- completed board is immutable.

## Device matrix for MVP
- recent iPhone simulator/device with notch/Dynamic Island safe area;
- older/smaller iPhone viewport;
- mainstream Android phone;
- at least one tall/narrow Android aspect ratio.

## Issue #14 diagonal regression checks

The v2 suite uses independent signed-displacement ray predicates for all eight
directions and exhaustive small-board placements, rather than production ray
traversal. Focused cases cover exact diagonal geometry, outward edge rays, cats
in the quadrant but off the ray, traversal through arrows, shared/multiple cats,
unsatisfied singletons, satisfied arrows and cardinal-only line exclusion. All
four diagonal directions have explanations and help in en/pt-BR/es.

The generator corpus retains seeds 0..999 and the same numeric work bounds.
Generator board fingerprints are intentionally refreshed for v2; PRNG and extreme
seed solution fingerprints remain unchanged. Native results follow; phone timing is recorded in `GENERATION_BENCHMARK.md`; historical v1 measurements above remain
associated with their original revisions.

Native macOS Unity 6000.3.25f1, 2026-10-08: final compilation passed
(`compile.gPQJd5`); **84/84 EditMode tests passed**, zero failed/skipped/inconclusive
(`editmode.V14TG4/test-results.xml`). Repository validation also passed.

All 1,000 v2 corpus requests succeeded on their first attempt: 1,000 distinct
logical boards, 971 solution layouts, and 917 boards containing diagonals,
covering all four orientations. Human evaluations median/p95/max were
1,343/2,907/5,113; verifier nodes 1/1/11; random draws 214/374/445. Arrow count
median/p95/max was 4/7/10 (mean 4.34), compared with the historical v1 mean 5.053.
Numeric work bounds were not increased. Editor generation timing was
3.811/7.391/11.993 ms median/p95/max in this run, with no allocation counter;
concurrent Xcode work makes these informational editor figures unsuitable for a
controlled performance comparison. Physical-player measurements are the timing
gate. This fixed corpus does not prove all possible seeds succeed.

The first measurement intentionally exposed four changed board fingerprint
expectations. One new multiple-cat fixture was uniquely solvable but human-stalled;
it was replaced with a line-forced multiple-cat fixture so the test measures
at-least-one semantics without asserting unsupported reasoning. The final gate
above includes these corrections and localized diagonal/validator cases.

## Combined session and diagonal integration gate

Before merging PR #15 into `develop` on 2026-10-08, the branch incorporated
PR #13 session changes. Native Unity 6000.3.25f1 compilation passed and the
combined **104/104 EditMode tests passed**, with zero failed/skipped/inconclusive.
Repository validation and whitespace checks passed. Logs:
`.unity-results/compile.mgYXTV/Editor.log` and
`.unity-results/editmode.bpkWOt/test-results.xml`. No conflict or additional
game behavior change was introduced by this integration.


## Issue #6 local portrait presentation gate

Native macOS Unity 6000.3.25f1 compilation passed (`compile.ICdHwH`). The final
**144/144 EditMode tests passed**, zero failed/skipped/inconclusive
(`editmode.FnW9mJ/test-results.xml`). This includes visible snapshot isolation,
eight arrow directions, protected/completed states, safe-area/clue geometry,
pre-panel startup, localized header transitions and stale-board failure clearing.
Repository validation and whitespace checks passed.

A local macOS player rendered six portrait/symbol captures across English,
Brazilian Portuguese and Spanish, including 320×568 compact layouts. The final
visual review found no blocking layout issue. See `BOARD_PRESENTATION_REVIEW.md`
for evidence and reproduction. Simulated safe insets and desktop rendering do
not establish physical-phone touch usability or owner color-vision comfort.
No input adapter or controls are delivered in this display-only issue.


## Issue #7 local input gate

Native Unity 6000.3.25f1 compiled the gesture adapter (`compile.6tFSn5`). Final
**184/184 EditMode tests passed**, zero failed/skipped/inconclusive
(`editmode.ZMRYTJ/test-results.xml`). Tests cover tap/hold/release, duplicate and
secondary pointers, axis locking, sparse movement, mixed/protected/completed
states, cancellation, exact boundaries/gaps and representative portrait layouts.

The local macOS player passed mounted synthetic mouse and touch dispatch checks,
including pointer capture surviving two Refresh calls (`issue7-mounted-input.log`).
The owner played the mouse board to completion and reported correct behavior.
A subsequent completed-board mouse check left state unchanged. Xcode Release
build/signing passed and the app was installed/launched on the iPhone 17;
the owner confirmed requested physical iPhone checks felt correct (tap cycle,
horizontal/vertical drag, release, multiple fingers, background/reopen). Android
device verification remains pending. See `TOUCH_INPUT_REVIEW.md` for steps and limitations.


## Issue #8 local complete-loop gate

Native Unity 6000.3.25f1 compilation passed (`compile.DLTpZy`). **210/210 EditMode
tests passed**, zero failed/skipped/inconclusive (`editmode.PG7Ojs/test-results.xml`).
Coverage includes clue-only one-fact hints/corrections, Clear/rescan, narrow
protection, seeded progression/failure, a 20-seed hints-to-win corpus, resources,
preferences, live-menu resume, modal command guards and completed action locks.

Mounted shell flows passed in all three initial locales. A final mounted pass
verifies panel picking before synthetic button events (`issue8-panel-picking.log`).
UX reviewed 18 historical menu/gameplay captures without blockers.
QA found no local code/test/render blockers. iOS Release build/sign/install
passed. Owner iPhone playtest passed the full flow, three languages, saved locale,
completed-board locks and background/reopen. Native iPhone 17 Release diagnostic
passed 20 New Game requests: median 65.994 ms, p95 66.966 ms, max 72.543 ms;
first start separately 59.497 ms. Timing includes queued feedback and rendered
readiness after synthetic button action, excluding touch hardware latency.
See `GAME_LOOP_REVIEW.md` for measurements, reproduction and limitations.


## Issue #19 calm portrait board gate

Native Unity compile passed (`compile.PllPPK`) and **219/219 EditMode tests
passed**, zero failed/skipped/inconclusive (`editmode.ZwmUpM`). Three native
macOS flows pass centering, inline completion from final hints and mounted board
pointer taps, action locks and New Game reset. QA/UX reviewed 21 locale/portrait
captures without blockers. Exported iOS Info.plist permits upright portrait only.
Owner iPhone review on 2026-10-09 confirmed centered grid, darker cells, working
orientation lock and improved completion. See `BOARD_POLISH_REVIEW.md`.

## Issue #23 immediate hints

InlineHintTests cover immediate one-fact application, all three locales,
correction of conflicting marks, consecutive hints, reset/menu/locale behavior
and completed-board controls without explanation UI. Existing GameShellTests
exercise the legacy presentation for rollback. Native GameLoopReviewChecks
exercise both configurations with mounted pointer events and fixed-grid checks.
See INLINE_HINT_REVIEW.md for fresh results and owner review status.

## Issue #26 tutorial checks

Deterministic tests cover exact fixture quotas, true arrows/protection, current
human-solver completion and independent exhaustive uniqueness; complete lesson
replay; tap-cycle sequence; off-target input; zero-line overlap; two-cat lessons;
no early victory; final completion lock; fresh replay; first-play/Skip/Menu/locale
routing and ordinary-game regression. Native mounted checks must exercise actual
pointer events through tutorial gates, including swipes across fixed cells and
existing cats, gesture cancellation/second fingers, Skip and replay. Render the
three locales at compact/standard/tall portrait sizes and verify text fit,
stationary grid and non-color highlights. Owner phone walkthrough remains the
acceptance gate for touch comfort.

Owner refinements add separate sample-state rendering/mark-isolation tests,
protected-only and arrow-only highlights, welcome Continue emphasis, a clean
same-fixture session swap, and independent play with wrong-cat recovery before
completion. Native checks must prove the first independent pointer tap works,
not merely test the replacement session directly.
