# Issue #21 — iOS tester trial preparation

## Goal and current status

Prepare the current Paws game for a small 5–8 person iOS trial. Android physical
validation is deferred in #28 and does not block this iOS-first path.

Source baseline `develop` at `5b9e06b` includes Paws branding and paw semantics;
Easy 5×5, Medium 6×6 and Hard 7×7 generation; the fixed Easy first-play tutorial;
persistent random-difficulty Play/Next selection; tap/drag, immediate Hint,
arrow-path hold, idle Hint attention and the ordinary-board completion sequence.
SDK-free monetization services are integrated but normal composition remains
disabled and ad-free. No provider or store SDK is installed.

The latest behavior-changing baseline passed pinned Unity 6000.3.25f1 compilation
and 473/473 EditMode tests in PR #56. Earlier owner iPhone 17 checks passed the
three board sizes, touch flow, lifecycle smoke and all three languages. On
2026-10-19 the owner also reported the current Paws physical-iPhone appearance
approved, closing PR #54's branding appearance gate. Smaller phones and measured
Medium/Hard tap-to-ready latency remain unverified. See
[TESTING.md](TESTING.md) for evidence and scope limits. No clean distribution
archive or TestFlight trial is claimed.

The owner confirmed no paid Apple Developer Program membership. Local installed
phone builds can continue, but TestFlight requires enrollment. Every purchase or
subscription and each upload/invitation/distribution action requires separate
explicit owner approval. None is authorized here. No credentials belong in Git
or chat.

## Smallest required build work

1. Finalize durable app identity and numbering. Tracked settings now use the Paws
   product name and all iOS icon slots use the approved paw artwork. The repository
   still has no explicit permanent application identifier and retains version
   `1.0` / build `0`; choose and register the bundle identifier plus a `0.x.y`
   version/build only with owner approval. Do not rename compatibility-sensitive
   `CatsAndArrows` project identifiers as part of distribution setup.
2. Use one explicit normal-game iOS build entry point. Exclude benchmark autorun,
   mounted review checks, synthetic input/capture modes and development-only
   monetization mocks from the tester player.
3. Confirm the supported device family. Prefer iPhone-only for the first trial
   unless the owner approves tested iPad support; keep upright portrait and the
   existing English, Brazilian Portuguese and Spanish resources.
4. Build from an approved commit and record source revision, version/build,
   Unity/Xcode versions, artifact checksum and exact validation results. Keep
   signing local and preserve the signed artifact outside Git. Remove only
   reproducible intermediates when storage cleanup is needed.
5. Validate the clean trial app on the owner's phone after build-setting changes:
   first-launch fixed Easy tutorial and manual replay/Skip; ordinary Play/Next
   choosing Easy/Medium/Hard with no more than two consecutive successful starts;
   failure/retry retaining its pending profile; tap/drag, protected cells,
   immediate Hint, arrow hold, idle attention, Clear/Menu, celebration/static
   return and completed-board lock. Verify all three locales, portrait,
   background/foreground and saved language/first-Play/difficulty history.
   Process termination need not restore an unfinished board or tutorial step.
   Reuse the approved Paws appearance result honestly, but do not treat it as a
   substitute for task-specific interaction/lifecycle checks of the clean build.

## Distribution preparation

After separate enrollment approval, confirm active membership and access,
register the approved app identity, create the App Store Connect record and
configure local distribution signing. Prepare beta description, feedback/contact
address, What to Test, reviewer contact information and export-compliance answers.
The owner must supply public-facing contact details; do not invent them or invite
anyone without instruction.

Current source includes Unity UI/test/screen-capture modules and the pure-C#
monetization coordinator, storage and service interfaces. Default composition
injects no enabled monetization coordinator; there are no real ad, analytics,
purchasing, backend or attribution SDKs. No-charge mock ads/store behavior is
Editor/development-review-only and must not enter the trial build. Local
preferences retain language, first-Play/tutorial and difficulty history; unfinished
board progress is not saved. Final declarations still require inspection of the
actual archive, capabilities, privacy manifests and enabled configuration.

Review/capture code must be excluded as described above. Prepare an accurate short
privacy statement about offline play, local preferences and voluntary feedback,
then verify the final archive and hosting/contact details before publication.

Apple references checked on 2026-10-09:

- [Program resources, including TestFlight](https://developer.apple.com/help/account/membership/programs-overview)
- [Personal-device testing and enrollment](https://developer.apple.com/help/account/basics/about-your-developer-account)
- [TestFlight overview](https://developer.apple.com/help/app-store-connect/test-a-beta-version/testflight-overview/)

The trial questionnaire and visual references are in
[TESTER_QUESTIONNAIRE.md](TESTER_QUESTIONNAIRE.md). GitHub feedback collection and
triage live in [Issue #22](https://github.com/juarezweiss/Paws/issues/22); keep
build/distribution readiness in #21.

## Trial instructions draft

Begin with the fixed Easy tutorial. After it, `Play` and `Next board` choose among
Easy, Medium and Hard automatically; report the displayed difficulty with feedback.
Replay Tutorial if needed and try Hint when stuck. Test tapping, horizontal/vertical
drag, holding an arrow, Clear, Menu and another board. Finish a board and inspect
both the celebration and static completed board. Try a preferred language and
briefly background/reopen the app.

There are no timers or penalties. Language, first-Play and difficulty-history
preferences persist across restarts; unfinished boards and tutorial steps do not.
Report confusing clues, missed/extra marks, hard-to-read symbols, crashes or pauses.
Include device/OS, language, app build and screenshot or seed when available. Do
not collect personal information beyond what testers voluntarily send. No new
feedback system, telemetry service or in-game account is needed.

| Tester alias | Device/OS | Language/build | Displayed difficulty | Finished first board? | Needed explanation? | Wanted another board? | Concrete issue |
|---|---|---|---|---|---|---|---|
| | | | | | | | |

One unversioned session is already recorded in #22; it is qualitative evidence,
not a completed trial or final visual/difficulty decision. Continue toward the
small sample only while additional sessions are useful. Create focused follow-up
Issues for repeated progress blockers or owner-selected improvements; do not
delay the trial for final art, live monetization, analytics, backend or Android.

## Approval boundaries

Local preparation requires no additional cash. Membership price, eligibility and
renewal terms must be checked and approved separately before enrollment. No
purchase, invitation, upload, App Store submission, production merge, release or
tag is authorized. Once a concrete clean build and distribution details are
reviewable, request approval for the exact next external action.
