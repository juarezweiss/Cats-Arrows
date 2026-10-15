# Cats & Arrows — Master Project Plan

**Version:** 1.0 — approved master plan  
**Prepared:** 2026-10-08  
**Status:** Approved by the owner on 2026-10-08  
**Owner:** Project owner; Codex coordinates implementation and review  
**Currency:** USD  
**Pre-revenue cash ceiling:** $500  
**Owner availability:** approximately 1–2 hours daily, or 7–14 hours weekly

## 1. Purpose and product description

Build a small, calm mobile deduction game that people can play and enjoy early.
Players find hidden cat heads in a grid using exact row/column totals and arrows
that guarantee at least one cat in their pointing direction. They mark cells,
request an optional logical hint, finish a board, and start another seeded puzzle.

The experience is portrait-first, minimalist, readable, and relaxed: no timers,
lives, penalties for an incorrect hypothesis, or mandatory advertising. The
audience is people who enjoy short, thoughtful logic puzzles. The initial business
hypothesis is that a satisfying core loop can attract repeat players; revenue
potential is unproven and is not assumed in this budget.

**Delivery priority:** a complete Easy game, a small player trial, and an initial
iOS release. Feedback drives subsequent features. Fix concrete problems that
prevent play or understanding; do not delay testing for exhaustive hardening,
speculative optimization, or supporting systems an offline game does not need.

## 2. Source material and decision precedence

This plan consolidates:

- [Original AI conversation](../original_material.md): corrected arrow semantics,
  cat limits, tap/drag behavior, non-interactive cells, unlimited hints, immediate
  victory, finished-board review, and color-vision concerns.
- This chat: Unity architecture, seeded procedural generation, solver/uniqueness
  guarantees, three initial languages, device timing evidence, human-controlled
  releases, and the latest minimalist MVP direction and $500 ceiling.
- Repository [product](../docs/PRODUCT.md), [rules](../docs/GAME_DESIGN.md),
  [solvability](../docs/PUZZLE_SOLVABILITY.md), [UX](../docs/UX.md),
  [architecture](../docs/ARCHITECTURE.md), [release](../docs/RELEASE.md), and
  [benchmark](../docs/GENERATION_BENCHMARK.md) documents; GitHub Issues and PRs.
- The approved MVP-direction commit `675204f` on `feat/6-portrait-board-ui`.
  Those changes are not yet integrated into `develop`.

The latest explicit owner decisions override older scope descriptions. In
particular, Easy/Medium/Hard was the original ambition; **Easy-only is the first
MVP**, with other difficulties later. The owner's latest revision includes
cardinal and diagonal arrows in the initial MVP; older cardinal-only documents
must be aligned before implementation. The reference app
[Arrows – Puzzle Escape](https://apps.apple.com/us/app/arrows-puzzle-escape/id6748397500)
and the owner's Zen Bound reference inform mood, not a change to the puzzle rules.

The original transcript is historical reference, not evidence that its former
browser implementation worked. Its attached screenshots/code are not reproduced
in full in that file. Current native tests and device results establish delivery
status. This approved plan governs scope, priorities, budget, and gates;
detailed rule/technical documents and Issues remain supporting specifications.
`AGENTS.md` production boundaries remain in effect.

## 3. Current baseline

| Area | Status at plan approval | Evidence / remaining work |
| --- | --- | --- |
| Unity/repository foundation | Complete, integrated | Issue #1 closed; PR #9 merged into `develop` |
| Puzzle model and validator | Complete, integrated | Issue #2 closed after PR #12 immutability fix |
| Human solver, uniqueness verifier, localization foundation | Complete, integrated | Issue #3 closed; PR #10 merged |
| Seeded Easy generator | Complete, integrated | PR #12 merged; Issue #4 remains open as a tracking item |
| Session state rules | Implemented; final fixes await integration | Draft PR #13 / Issue #5; native branch tests 59/59 passed |
| Board, native gestures, controls and completion UI | Not implemented | Issues #6, #7, #8 |
| Playable game for external users | Not delivered | The installed iPhone app is a diagnostic benchmark, not gameplay |

PR #12's tested code passed native Unity compilation and **61/61 EditMode tests**.
PR #13's separate branch passed **59/59**; these are different branch suites, not
cumulative totals. All work is tied to issue/PR history. No production release
has been approved by this plan.

On iPhone 17, three release IL2CPP runs completed 3,000 generation requests
(three repetitions of 1,000 seeds): median **6.35 ms**, p95 **12.14 ms**, maximum
**23.73 ms**; first calls **29.6–33.6 ms**. This removes the immediate concern
about a substantial Easy-generation wait on that phone. It does not establish
older-device performance, allocations, full New Game latency, or larger-board
cost. Full methods and limitations are in the benchmark report.

## 4. First-MVP scope

### Included

1. **Easy 5×5 procedural boards:** explicit seeds and generator versions;
   no dependency on manually authored levels. Maximum two cats per row/column
   and at most one zero-cat row and one zero-cat column. Only validated, uniquely
   solvable, human-deducible boards enter gameplay.
2. **Readable board:** totals centered beside rows and above columns;
   arrows in all eight directions inside non-interactive cells. A diagonal arrow
   means at least one cat on its exact 45-degree ray to the board edge, excluding
   its own cell and continuing through other arrows. It does not mean the whole
   quadrant, and it adds no diagonal count clues. Cat and arrow cells never
   overlap. Empty, X, Cat, Arrow, and Protected have distinct shapes/treatments,
   with adequate contrast and safe-area support.
3. **Reliable interaction:** tap `Empty → X → Cat → Empty`; deliberate horizontal
   or vertical drag changes only eligible Empty cells to X. It ignores existing
   X/Cat, arrow/protected cells, and completed boards. No extra tap on drag release.
4. **Narrow automatic protection rule:** when a line contains exactly one cat
   and a cardinal arrow along that line guarantees it lies in the pointed direction,
   opposite-side normal cells are non-interactive. Derive this from visible clues,
   never by revealing the hidden answer. Do not apply this row/column rule to
   diagonal arrows. Diagonals still participate in valid ordinary deductions and
   hints; broader automatic elimination is deferred.
5. **Complete loop:** simple Menu with an Easy start action, brief rules help,
   unlimited Hint, Clear, New Game, immediate victory, and Back to the Game.
   Completed boards remain visible and read-only; Hint/Clear are disabled while
   Menu/New Game remain available. Unsupported difficulties need no placeholder UI.
6. **Localization:** English, Brazilian Portuguese, Spanish; device-language
   detection, a simple language selector, English fallback, and saved preference.
   New languages extend resources without changing puzzle logic.
7. **Initial iOS distribution:** owner playtest, small TestFlight trial, then an
   explicitly approved public release. Android is a follow-up release, requiring
   its own physical-device checks.

Use simple replaceable icons/art and restrained feedback. Haptics, audio, and
additional animation are optional polish, not prerequisites for the first trial.

### Deferred

Medium/Hard and final difficulty calibration; daily puzzles;
accounts/cloud sync; multiplayer/leaderboards; a backend; anti-cheat or elaborate
security systems; advanced settings; tutorial frameworks; extensive art/themes;
analytics/vendor SDKs; ads/IAP; paid acquisition; and persistent board save/resume.

**Approved lifecycle assumption:** ordinary background/foreground switching
retains the in-memory board and cannot create duplicate actions or crashes. After
the app is terminated, an unfinished board need not resume in the initial MVP.
This limitation is disclosed to testers. Add lightweight persistence if lost
progress becomes a recurring obstacle to calm play; it is not hidden extra scope.

## 5. Acceptance criteria and delivery gates

| Gate | Required evidence |
| --- | --- |
| Puzzle correctness | Every delivered board passes invariant, complete human-solver, and exhaustive uniqueness admission with all eight directions. Diagonal rays have exact geometry, cross other arrows, and do not use cardinal-only exclusion. Seed/version/options reproduce its fingerprint. A deterministic seed corpus demonstrates actual diagonal-containing generated boards. Failure/exhaustion supplies no unverified board and presents a retry path. |
| Board presentation (#6) | The owner can read aligned totals and all five cell kinds without relying on color or zoom. Portrait safe areas work on iPhone 17 and a narrow representative editor layout. Layout math supports future sizes without implementing other difficulties now. |
| Input (#7) | Real iPhone taps transition once, including rapid taps; drags lock axis, process cells once, preserve ineligible marks, and do not tap on release. Focused tests cover these normal interactions and the historical regressions. |
| Controls and victory (#8) | Start → play → optional Hint/Clear → immediate win → finished-board review → New Game/Menu works. Extra cats prevent victory; other cells may be Empty or X. Clear preserves clue truth/protection; New Game retains Easy and requests a new seed. |
| Hint | Unlimited, optional, and explainable; never uses an incorrect player hypothesis as proof. Proposed behavior: apply one guaranteed conclusion derived from visible clues, correcting a conflicting player mark with an explanation. No random hidden-answer reveal or ad requirement. |
| Language/accessibility | All player-facing text uses the three resources, including four diagonal direction names and a brief ray explanation. Translated controls/explanations fit the portrait layout, accents render, and language preference survives restart. Owner checks symbol contrast, all eight orientations, and color-vision readability. |
| First playable on phone | At least one complete seeded game, retry path, and background/foreground transition work without crashes or broken input. Measure actual tap-to-ready New Game timing, not just isolated generation. |
| Small beta | A versioned build tied to a commit is delivered to approximately 5–8 testers. Core flow works, known progress-blocking defects are fixed, and feedback is recorded. Analytics infrastructure is not required. |
| Initial public iOS MVP | Beta gate met; actual SDK/data behavior checked for accurate privacy declarations; store screenshots, description, support/privacy information and release notes prepared; release build excludes the benchmark; owner explicitly approves production merge and submission. |

**Proposed responsiveness target:** p95 New Game tap-to-ready at or below 250 ms
over at least 20 ordinary requests on the iPhone 17, with first-game loading
recorded separately. This is an approved planning target, not a measured result or promise for
all devices/seeds. Show feedback during any wait and keep touch/rendering usable.
If generation visibly disrupts play, use the smallest suitable background or
advance-preparation approach; a large queue framework is not required. Animations
and ads are not substitutes for measuring the actual wait.

Test the player paths and concrete regressions. Broader device coverage or
speculative failure modes are not additional beta gates. Android testing gates
Android release; instrumentation and monetization do not gate an iOS trial.

## 6. Milestones, effort and forecast

Remaining effort estimates are planning allowances, not fixed-price quotes or
guarantees. They cover AI-assisted implementation, integration, owner review,
targeted testing, and release preparation; they do not count historical work.
Use approximately 10 owner hours/week as the central planning case and recalibrate
after the first board/input iteration.

| Milestone | Deliverable / dependency | Remaining effort allowance |
| --- | --- | ---: |
| A — Establish approved baseline | Approve this plan; review/integrate PR #13 when explicitly authorized; align MVP docs and Issue #4 status | 1–2 h |
| A2 — Diagonal puzzle support | Focused tracked task before #6; eight-direction geometry, safe deductions, generator, localization, native tests and phone remeasurement | 8–16 h |
| B — Native board | Issue #6; actual generated board and session rendered in portrait | 8–14 h |
| C — Native gestures | Issue #7; depends on B, verified on phone | 8–14 h |
| D — Complete Easy loop | Issue #8; simple localized Hint/controls/victory, new-seed generation and narrow protection rule | 12–20 h |
| E — Owner-ready build | Language selector/help/preferences, layout checks, actual New Game timing and focused lifecycle check | 6–10 h |
| F — Small player trial | TestFlight preparation, tester observations, one focused feedback/fix cycle | 8–14 h |
| G — Initial iOS release | Store materials, accurate declarations, final smoke test and owner approval | 4–8 h |
| **Base remaining effort** | | **55–98 h** |
| **Planning contingency (approximately 20%)** | Concrete integration/rework, not optional feature expansion | **11–20 h** |
| **Working total** | Rounded planning range | **65–120 h** |

At 7 h/week this is approximately 10–18 working weeks; at 10 h/week, 7–12;
at 14 h/week, 5–9. Test recruitment, feedback, enrollment and store review add
elapsed time and may overlap development. A central **7–14-week release window
after approval** is a provisional forecast, not a committed launch date.
The first playable phone build is the earlier checkpoint, roughly 3–6 weeks at
the central pace. Do not wait for final art or release preparation to play it.

A milestone finishes when its acceptance evidence is available, not when a target
date arrives. When blocked, proceed with independent work. Use one short progress
review each week: completed flow, next playable improvement, blockers, actual
effort/spend, and any scope change needing a decision.

## 7. Cash budget and spending rules

**Hard ceiling: $500 in incremental project cash spending before revenue.**
This is a cap, not a spending target. Recommended lean expectation is approximately
**$100–250** before the first iOS release if existing devices/tools are sufficient
and most art/translations are created or reviewed without hiring. No contractor
quotes have been obtained.

| Envelope | Maximum allocation | Basis |
| --- | ---: | --- |
| Apple developer membership | $100 | Published fee $99/year; check whether already paid before purchasing |
| Optional art/audio/store assets | $75 | Planning reserve; prefer existing/simple assets |
| Optional native-language review | $50 | Planning reserve; seek feedback first |
| Device access or storage | $75 | Borrow hardware where possible; actual storage shortage already occurred |
| Incremental AI/tools/static hosting | $75 | No unbudgeted upgrade or usage charge; use current tools first |
| Later Google Play registration | $25 | Optional Android reserve, not an iOS prerequisite |
| Contingency/early maintenance | $100 | Taxes, unexpected small expenses, or necessary fixes |
| **Total reserved ceiling** | **$500** | Unused allocations remain unspent |

Verified vendor prices as of drafting:
[Apple Developer Program](https://developer.apple.com/programs/enroll/) is
$99 per membership year; [Google Play registration](https://support.google.com/googleplay/android-developer/answer/6112435)
is $25 once. [Unity Personal](https://unity.com/products/unity-personal) is free
subject to its eligibility criteria, including its $200,000 revenue/funding
threshold. Fees may be billed locally; taxes/renewals consume the same cap.

Budget assumptions: the Mac and iPhone are already owned; owner labor is unpaid;
existing shared subscriptions/hardware are sunk costs, while new subscriptions,
upgrades, incremental usage, assets, contractors, hosting and storage bought for
this project count against the cap. If the owner wants existing recurring
subscriptions charged to the project too, request a budget revision before incurring charges.
No existing membership/subscription expenditure has been confirmed as paid in
the project's ledger. Existing signing certificates do not prove paid enrollment.

Keep a simple actual/committed/remaining ledger. **No purchases or subscriptions
are authorized. Ask for and receive explicit owner approval before every purchase
or subscription, including any paid renewal, upgrade, or incremental usage charge.
Plan approval and budget allocations never constitute spending approval.**
This plan does not purchase or subscribe to anything. Reallocate envelopes within
the cap only with owner agreement. If required spending would exceed $500, reduce
scope, find a free alternative, or request a revised cap. Do not count hoped-for
revenue as available money. Unpaid effort is not represented as zero economic
cost; outsourcing would need a separate agreed rate and quote.

## 8. Risks and proportionate responses

Ratings are qualitative planning judgments, not measured probabilities.

| Risk | Likelihood / impact | Response / owner |
| --- | --- | --- |
| Correct puzzles are confusing, repetitive or not enjoyable | Medium / High | Observe 5–8 players early; adjust explanation/clue presentation before adding difficulty or features. Product owner + game design |
| Touch duplicates events or unintentionally turns taps into drags | High / High | Reproduce the original phone regressions with one pointer path; focused real-device gesture tests. Engineering + QA |
| Hint treats incorrect marks as logical facts | Medium / High | Use clue-derived guaranteed conclusions; explain corrections; test one conflicting-mark case. Puzzle/gameplay engineering |
| Full UI or older hardware makes generation feel slow | Medium / Medium | Current iPhone evidence is encouraging; measure tap-to-ready in gameplay and address only observed stalls. Android check before its release. Engineering |
| Color-vision needs or longer translations make the board unreadable | Medium / High | Shape-based states, owner contrast check, and all three languages on narrow portrait layout. UX + owner |
| Scope expands before anyone can play | High / High | Easy-only gate; defer optional systems; approve changes with time/cash impact. Owner + coordinator |
| Signing, enrollment, tester access or store review delays release | Medium / Medium | Verify entitlement early; prepare listing/review build; treat review duration as external. Owner + release engineering |
| Disk pressure blocks builds | High / Medium | Maintain practical build headroom; prune reproducible intermediates while preserving source, current artifacts and reports. Engineering + owner |
| Users lose an unfinished board after termination | Medium / Medium | Explicit MVP limitation; observe whether it harms play; add a small save/resume feature only if justified. Product owner |
| Tools, devices or contractors consume the cap before revenue | Medium / High | Track actual/committed costs; require purchase approval; prefer borrowed devices and simple assets. Owner |

## 9. Playtest and product learning

Recruit approximately 5–8 people who enjoy casual logic games, including feedback
on each initial language where feasible. Ask them to start without a developer
walkthrough, read the brief rules, play a few boards, use Hint if desired, and
describe confusing or frustrating moments. Observe arrow interpretation, whether
they think every cell must become X, accidental inputs, Hint usefulness, and
willingness to voluntarily start another puzzle.

**Proposed trial decision signal:** a clear majority can finish their first Easy
board with the built-in help/Hint and start another without intervention. Repeated
misunderstanding or input problems trigger a focused fix and short retest. This
small group supplies qualitative direction, not statistically reliable retention
or revenue forecasts. No elaborate dashboard or analytics SDK is needed.

Public release readiness is technical and experiential; a specific install count,
rating, retention percentage or revenue target is not assumed. After initial
feedback, decide whether to add difficulty, lightweight persistence, or one
monetization experiment. Do not expand all three simultaneously.

## 10. Revenue and later phases

The first validation MVP is free and has no monetization SDK, so no immediate
revenue is expected. Before committing more spending, the owner decides whether
feedback justifies a small revenue experiment within the remaining cap. Preferred
options to evaluate are optional supporter purchases or cosmetics; ads require
a separate decision and appropriate measurement/declarations.

Basic hints remain unlimited and generation never depends on an ad. Avoid
interrupting active deduction, forcing ads before meaningful play, or tuning
puzzles to make paid assistance necessary. Monetization and paid acquisition
require their own scope, budget, metrics and explicit approval. The $500 cap
continues to apply until revenue actually exists or the owner changes it.

## 11. Ownership, workflow and change control

- **Owner:** approves the master plan, product tradeoffs, each individual purchase
  or subscription before any charge, release
  readiness, production merges and store submission; supplies playtest feedback.
- **Codex coordinator:** implements tracked work, preserves local changes,
  coordinates bounded specialist help, runs relevant checks, and prepares
  reviewable PRs/builds. Specialist reviews are used where they help concrete
  puzzle, interaction, UX or release decisions, not as a ceremony for every edit.
- **GitHub Issues:** authoritative work items. A focused diagonal-domain task
  precedes the #6 → #7 → #8 feature sequence. Update criteria to reflect the
  owner's explicit diagonal-MVP decision before implementation.
- **Branches:** one concern per branch/PR where practical. `develop` is integration;
  merge there only when explicitly asked and checks pass. `main`, release tags,
  production configuration and submissions remain human-controlled.
- **Validation:** existing repository checks plus native compilation and relevant
  tests. UI work includes focused phone smoke tests. Retain version/seed evidence;
  no credentials or signing material enter Git.
- **Change control:** record a short decision, effect on acceptance/time/cash, and
  owner approval. Defer non-blocking suggestions to a follow-up list. If a milestone
  exceeds its estimate materially, reforecast or reduce scope rather than hiding
  work in the contingency.

## 12. Approval and establishment as the master plan

The owner approved this plan and the focused diagonal-support task on 2026-10-08.
Implementation follows tracked Issues and the acceptance gates below. This approval
does not authorize any purchase/subscription, pending PR merge, or production release.

Approval established version 1.0 with the agreed scope, $500 ceiling, owner
availability, milestone forecast, acceptance gates and agreed Hint/lifecycle
choices. Align the Product/Roadmap/Current Sprint documents with it, retain
the original material as reference, and start the next tracked UI task. Technical
specifications continue to detail implementation under this scope.

Approved planning assumptions:

1. Easy-only, iOS-first MVP and the deferred feature list.
2. Eight-direction arrows, cardinal-only one-cat/arrow protection,
   basic clue-derived Hint, and deferred
   persistent board resume.
3. $500 incremental-cash definition, envelopes, and existing-subscription treatment.
4. The revised 65–120-hour effort forecast and 250 ms New Game target.

**Owner decision:** Approved, including the focused diagonal-support task  
**Approval date / approved version:** 2026-10-08 / 1.0  
**Next review:** After the first diagonal-support validation, then weekly

## 13. Diagonal-arrow development adjustment (Issue #14)

The owner has explicitly included diagonal arrows in the first MVP. This section
records the approved engineering work in Issue #14. Complete the core
change before relying on it for board UI and Hint integration.

1. Extend directions with up-left, up-right, down-left and down-right, preserving
   existing direction identifiers. Give both solution validation and clue solving
   consistent bounded ray traversal; invalid directions must fail safely.
2. Keep arrow constraints as “at least one,” with existing row/column totals.
   The uniqueness search can retain its general constraint approach. Audit and
   restrict the human solver's row/column exclusion to cardinal arrows: a diagonal
   is not contained in one row or column. Keep singleton-ray deductions for all
   directions; admit a generated board only if the documented human rules finish.
3. Expand truthful generator candidates from four to eight directions, retain
   deterministic ordering and work limits, and ensure diagonal candidates actually
   appear in accepted boards. Do not force a diagonal into every board merely to
   demonstrate support. Review observed variety and difficulty before adding quotas.
4. Version the changed generator/rules contract as appropriate, update deterministic
   reference vectors, and preserve historical benchmark provenance. Old seed/version
   receipts must not silently claim the new algorithm produces the old board.
5. Add focused independent test geometry for all four diagonal orientations,
   corners/edges, empty rays, shared cats, traversal through arrows, ambiguous and
   impossible clues, and a regression against unsound diagonal line exclusion.
6. Add four localized direction names and readable icons/help. Drag marking remains
   horizontal/vertical; arrow orientation does not change the gesture rules.
7. Rerun native validation and the same seed corpus, comparing success, human
   completion, variety and cost. Remeasure on the phone after the core change;
   current cardinal-only timings are the historical baseline, not diagonal evidence.

This adds an estimated 8–16 hours before the UI milestones, reflected in the
revised forecast above. It is a planning allowance, not a measured estimate.
Implementation is approved and tracked as Issue #14. No paid tools or purchases are necessary to
perform this assessment.

### Implementation checkpoint, 2026-10-08

Issue #14 is implemented locally on `feat/14-diagonal-arrows`, pending integration.
Native compilation and 84/84 tests pass. The unchanged-bounds 1,000-seed corpus
accepted all boards, including 917 with diagonals and all four orientations.
UI arrow rendering and live Hint controls remain in the subsequent UI tasks.
Fresh physical benchmark: 3,000/3,000 requests succeeded, median 7.11 ms, p95
13.34 ms, maximum 21.68 ms on iPhone 17; see the benchmark document. PR #13 remains a
separate pending session change; this task does not merge it.

## Approved presentation amendment — 2026-10-09 (Issue #19)

The owner’s phone playtest requested darker fixed tiles, centering the tile grid
rather than the grid-plus-clues group, and an upright portrait lock. Completion
now keeps the read-only board visible with a subtle grey treatment and the
localized “All cats found!” phrase beneath Easy. This supersedes earlier references
to an automatic victory modal/Back to the Game completion action; menu resume is
retained. Final-hint explanation is available on request. Exact-cat completion,
Hint/Clear locks, Menu/New Game, scope and purchase approval rules are unchanged.

## Delivery sequencing confirmation — 2026-10-09 (#21)

The owner explicitly chose to continue iOS delivery and return to Android later.
Android development/physical acceptance (#7) stays deferred, with no unsupported
claim of Android readiness. The next active milestone is small iOS tester trial
preparation. Owner reports no paid Apple Developer membership yet; enrollment and
every related purchase remain subject to separate explicit approval.

## Approved difficulty extension — 2026-10-14 (#32)

The owner explicitly authorized Medium/Hard generation and an initial-menu choice,
superseding their earlier deferral for this tracked implementation. Easy and the
fixed Easy tutorial remain unchanged. Medium is 6×6 and permits zero/three-cat
lines; Hard is 7×7, has no zero-cat rows/columns and always has a row or column with
exactly three cats. Existing Medium max3/Hard max4 caps and Medium's at-most-one
zero row/column remain; no extra difficulty rules or solver families are approved.
All difficulties retain seeded determinism, independent uniqueness and complete
human-style derivation. Scope includes focused tests/docs and PR to develop only;
no merge, spending, production release or unrelated integration is authorized.
