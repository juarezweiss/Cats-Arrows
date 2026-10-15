# Issue #21 — iOS tester trial preparation

## Goal and current status

Prepare the current playable game for a small 5–8 person iOS trial, beginning with
Easy. Android development and physical checks (#7) are deferred by owner on
2026-10-09. Baseline develop commit 2ca1e14 includes merged PR #33 (#32): Easy 5×5,
Medium 6×6 and Hard 7×7 selection, with the fixed Easy tutorial unchanged. Native
Unity 6000.3.25f1 compile, 285/285 EditMode tests, repository checks and five mounted
desktop portrait flows passed. On 2026-10-14, the owner passed Medium 6×6 and Hard
7×7 smoke tests on iPhone 17 / iOS 27.0.1: touch comfort/readability, tap/drag, controls,
completion locks, repeated New Game, background/reopen and portrait lock, with
no visible pauses reported. All three languages (English, pt-BR and Spanish)
also passed. Smaller physical phones and measured Medium/Hard latency remain
unverified. See [TESTING.md](TESTING.md) for build provenance and
limits. PR #31 monetization is unmerged and excluded. This document is a
preparation plan, not a claim that a distribution archive exists.

The owner confirmed no paid Apple Developer Program membership. Local installed
phone builds can continue, but TestFlight requires enrollment. Every purchase or
subscription and the actual distribution/upload must receive separate explicit
owner approval; none is authorized here. No credentials belong in Git/chat.

## Smallest required build work

1. Confirm permanent app identity. Proposed display name: Cats & Arrows; proposed
   bundle ID: com.juarezweiss.catsarrows; initial version: 0.1.0, build 1. These are
   proposals, not registered identifiers. Current tracked settings still say
   DefaultCompany/UnityProject, version 1.0/build 0, and no explicit identifier.
2. Add a simple provisional app icon; current iOS icon slots are empty. Final art
   is unnecessary for the trial, but a correctly sized icon is needed.
3. Use one normal game scene/bootstrap and explicit iOS build entry point, with
   diagnostic capture, synthetic input and benchmark autorun unavailable in the
   trial player. Keep existing gameplay unchanged. The benchmark assembly already
   excludes normal players unless its diagnostic define is enabled.
4. Prefer iPhone-only for this first trial, pending owner confirmation. Current
   review app includes both iPhone and iPad; iPad is not yet tested. Keep portrait
   lock and the three languages. Avoid adding unsupported platform promises.
5. Build from the approved commit and record source revision, version/build,
   Unity/Xcode versions, artifact checksum and exact validation results. Keep
   signing local and preserve the signed artifact outside Git. Prune reproducible
   build intermediates after preservation to maintain storage headroom.
6. Validate the actual trial app on the owner’s phone: first-launch fixed Easy
   tutorial and replay/Skip; Easy/Medium/Hard menu selection; Start, seeded New
   Game and failure/retry retaining the selected mode. Check tap/drag, protected
   cells, immediate Hint, Clear/Menu, inline victory and readonly boards across
   all three sizes. Check narrow-screen Hard touch comfort, symbol readability
   and generation pauses; three languages, saved locale/tutorial-seen preference,
   portrait and background/reopen. Menu/back must preserve the live board when
   choosing the next difficulty. App restart defaults to Easy and does not
   recover unfinished boards. Run native tests/validation when implementation or
   build settings change. Reuse historical evidence honestly.

## Distribution preparation

After separate enrollment approval, confirm active membership and access, register
approved app identity, create the App Store Connect record and configure local
distribution signing. Prepare beta description, feedback/contact address, What
to Test, reviewer contact information and export-compliance answers. The owner
must provide public-facing contact details; do not invent them or invite anyone
without instructions. Inspect final SDKs, capabilities and privacy manifests
before declarations; no extra SDK or analytics is required for this trial.

Baseline dependency inspection finds only Unity UI/test/screen-capture modules,
no ad/analytics/purchasing/backend SDK, and local language and tutorial-seen
PlayerPrefs. Review/capture code still needs exclusion from the trial player as
described above. This is source inspection, not a final no-data-collected
declaration. Prepare an accurate short privacy statement about local preferences,
offline gameplay and voluntary feedback, then verify the final archive and
hosting/contact details before publishing.

Apple references checked on 2026-10-09:
- [Program resources, including TestFlight](https://developer.apple.com/help/account/membership/programs-overview)
- [Personal-device testing and enrollment](https://developer.apple.com/help/account/basics/about-your-developer-account)
- [TestFlight overview](https://developer.apple.com/help/app-store-connect/test-a-beta-version/testflight-overview/)

The trial questionnaire and contrasting visual references are in
[TESTER_QUESTIONNAIRE.md](TESTER_QUESTIONNAIRE.md). GitHub feedback collection
and triage live in [Issue #22](https://github.com/juarezweiss/Cats-Arrows/issues/22);
keep build/distribution preparation in #21.

## Trial instructions draft

Begin with the fixed Easy tutorial, then play a few Easy puzzles at your own pace.
Use How to Play if needed, and try a Hint when stuck. After the first Easy board,
optionally try Medium and Hard; report the chosen mode with feedback, especially
whether the smaller Hard cells are comfortable to tap. Test tapping,
horizontal/vertical drag, Clear, Menu and New Game. Finish one board and take a
moment to inspect it. Try your preferred language and briefly background/reopen
the app. There are no timers or penalties. Language and tutorial-seen preferences
are saved; unfinished boards and difficulty choice are not restored after app
termination in this MVP.

Report confusing clues, missed/extra marks, hard-to-read symbols, crashes or pauses.
Include device/OS, language, app build and a screenshot or seed if available. Do
not collect personal information beyond what testers voluntarily send. No new
feedback system, telemetry service or in-game account is needed.

| Tester alias | Device/OS | Language/build | Difficulty | Finished first board? | Needed explanation? | Wanted another board? | Concrete issue |
|---|---|---|---|---|---|---|---|
| | | | | | | | |

Observe approximately 5–8 testers and fix actual progress blockers. A clear
majority should finish their first Easy puzzle and want to play again, following
the master plan’s trial signal. Do not delay this for difficulty scoring, backend,
monetization, final art or Android work.

## Approval boundaries

Local preparation costs zero additional cash. Membership price and renewal terms
must be checked and approved separately before enrollment. No purchase, invitation,
upload, App Store submission, production merge, release or tag is authorized.
Once a concrete clean build and distribution details are reviewable, seek the
specific approval needed for the next external action.
