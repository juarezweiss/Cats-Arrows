# Architecture

## Principle
The puzzle is a deterministic domain model first and a Unity UI second.

## Layers
### Core domain (`Assets/CatsAndArrows/Core`)
Pure C# value types and rules:
- coordinates;
- directions;
- cell kinds/states;
- immutable puzzle definition;
- validation;
- solver contracts;
- deterministic random abstraction.

No MonoBehaviour dependencies.

### Gameplay (`Assets/CatsAndArrows/Gameplay`)
Runtime session state:
- player marks;
- tap state transitions;
- drag gesture intent;
- win checking;
- hint requests;
- completed-board lock.

### Generation/Solving
Create candidate puzzles, validate truth of clues, prove uniqueness, run human-style deduction, and calculate difficulty metrics.

Issue #3 separates immutable visible `PuzzleClues` from the authored solution in
`PuzzleDefinition`. The human solver and exhaustive verifier receive only the
clues. A solvability assessment combines their evidence and checks the intended
answer/profile separately. Structured deduction records support replay and
localized explanations without coupling reasoning to UI or language.

Issue #4 implements an Easy-only seeded generator in the pure C# domain. Each call
owns its random stream and work counters. It samples feasible quotas and layouts,
adds truthful arrows with bounded solver-guided trials, and returns a board only
after complete admission. Returned puzzle truth and generation receipts are
immutable. Reproduction records the original seed, generator/profile/ruleset
versions, effective options, and canonical logical-board fingerprint.

Generation is independent of Unity objects, presentation, ads, and wall-clock
timing. Its API supports future background preparation; Issue #4 does not start
threads or implement a puzzle queue. Later gameplay work should measure the complete New Game path and choose the
smallest preparation/scheduling approach justified by visible waits. A puzzle
queue is not a prerequisite for the first board presentation. Ads, if separately approved, may overlap preparation but cannot be a
dependency for generating or receiving a puzzle.

### Localization
Pure C# locale resources and a presentation service live under Core/Localization.
English, Brazilian Portuguese, and Spanish are the initial locales. New locale
resources extend the catalog; missing keys fall back to English. Language
selection, device-culture detection, and formatting are presentation concerns.
Solver rule IDs are stable message keys, while clue references and conclusions
remain typed data. Future views must use resources for player-facing text and
accommodate translation length and fonts; puzzle logic never branches by locale.

### UI (`Assets/CatsAndArrows/UI`)
Unity presentation only:
- menu;
- board layout;
- row/column clue views;
- cell views;
- pointer/touch interaction adapter;
- inline completion presentation;
- accessibility presentation.

## State ownership
`PuzzleDefinition` owns immutable puzzle truth. `GameSession` owns mutable player state. Views render state and send intents; they do not modify solution truth.

Issue #5 exposes player marks through a live read-only view and protected cells
through a copied read-only collection. Callers cannot bypass the session's
transition or completion checks by mutating those collections. Protected input
is copied, deduplicated, and checked: outside-board coordinates and solution cats
are rejected. Arrow coordinates are omitted from the protected set because arrows
already have their own non-interactive kind. This does not automatically derive
protected cells; that remains a solver/presentation integration concern.

Completion is checked during construction as well as after marking. A generic
zero-cat puzzle therefore starts completed and read-only. This edge-case contract
does not change the Easy generation profile. Clear changes only player marks and
retains puzzle truth and protected eligibility; completed sessions reject Clear,
tap, and drag intents.

## Randomness
Use explicit seeds. Avoid `UnityEngine.Random` inside core generation. A seed must reproduce the same logical board for a fixed generator version.

## Persistence
The approved MVP persists local preferences. Board save/resume and statistics
remain deferred until feedback justifies them. The puzzle seed is sufficient to resume/recreate an active board once save/resume is implemented.

## Services
Analytics, ads, IAP, remote config, and attribution must sit behind interfaces/adapters. Core gameplay cannot require a vendor SDK to compile or run.

## Issue #6 board presentation

The UI assembly references Core and Gameplay. `BoardPresentation` creates a
read-only display snapshot of visible row/column totals, arrows, protected cells
and player marks; it does not expose solution positions. `BoardLayout` provides
one shared cell pitch for row clues, column clues and tiles, while `SafeAreaLayout`
converts pixel safe areas into the panel's top-left coordinate system.

`PortraitBoardView` binds a session and refreshes its presentation without owning
puzzle truth or mutating marks. Symbols use vector geometry rather than platform
font arrow/emoji glyphs. UI-local resources extend the existing three-language
catalog. The explicit board review scene generates a real seeded Easy puzzle;
review fixtures demonstrate symbol states separately from that generated board.
This issue adds no tap/drag mutation, Hint controls or completion modal. Those
remain Issues #7/#8. Native layout captures are presentation evidence, not proof
of working phone gestures or screen-reader integration.


## Issue #7 pointer adapter

`BoardGestureController` recognizes gestures in board-local panel units and sends
only `Tap`/`ApplyDrag` intents to `GameSession`. `PortraitBoardView` uses one UI
Toolkit Pointer event path for mouse and touch, with capture on the stable board
container. Replacing cell visuals during Refresh therefore does not replace the
capture owner. No independent mouse-click or legacy touch polling mutates cells.

An eligible press remains pending until release. An 8-panel-unit movement distance
starts a drag only from Empty, with the dominant component locking horizontal or
vertical (ties horizontal). X/Cat still support tap cycling; deliberate movement
from them cancels the pending tap. Swept intervals bridge missed movement samples
and internal gutters; each coordinate is processed once. Arrows, protected cells,
existing marks and completed sessions remain guarded by the session.

Leaving the playable grid cancels the gesture, retaining marks already applied.
Capture loss, focus/pause, detach, geometry changes, rebind and generation failure
also cancel. Later Clear controls must cancel before clearing; New Game uses Bind.
The explicit review scene offers opt-in mounted-panel synthetic input checks;
these complement rather than replace physical-device verification.


## Issue #8 Easy game shell

`EasyGameLoop` creates only admitted generated sessions and advances the requested
seed for every attempt. Normal boot selects a fresh initial seed; diagnostic
arguments can fix it for reproduction. `ClueProtection` derives the narrow
original-total-one cardinal-arrow aid from visible clues and verifies NotCat
assignments with the human solver. No answer-based automatic elimination exists.

`GameSession.ApplyHint` caches a complete clue-only proof and rescans for the
first mismatching interactive conclusion. It applies one Cat/X directly within
the session, returns the proof and correction flag, and immediately checks
completion. Player marks never become solver premises. Clear does not alter
clues/protection or require a persistent hint cursor.

`GameShellView` manages Menu/Play/Failure and blocking Rules/Hint overlays and inline completion.
It sends gameplay intents and listens for board state changes. The compact play
layout replaces the persistent legend with a 2×2 control group; scrollable help
explains the symbols. Language preference alone uses PlayerPrefs. A live session
survives Menu and focus changes; board persistence remains deferred. A final Hint
retains its structured proof for optional review, without opening an overlay. Diagnostic mounted flow and
render timing remain opt-in review paths, not analytics or gameplay dependencies.

## Issue #19 calm board polish

Completion remains domain-owned and immediate. Views apply a subtle completed
palette and inline localized status while keeping the grid visible. Final-hint
proof is retained by the shell only for optional explanation review and resets
with New Game. Tile-grid centering reserves symmetric clue gutters while pointer
mapping continues to use shared BoardLayout coordinates. PlayerSettings locks
mobile orientation to upright portrait; no solver/generator changes are involved.

## Issue #23 immediate hints

The default shell applies the domain's HintResult directly and briefly highlights
its target without opening a modal or retaining a presentation explanation.
The existing useInlineHints configuration selects immediate presentation; false
retains the prior modal/Last hint flow for rollback. The domain still owns every
assignment and completion check; proof data stays available within the domain
without adding player-facing explanation controls.

## Issue #26 tutorial mode

A versioned immutable TutorialFixture defines the approved fixed puzzle. A small
pure-C# TutorialController owns only lesson progression and allowed targets; its
GameSession owns marks and victory. Ordinary EasyGameLoop generation remains
independent. The shell chooses an active tutorial or ordinary session explicitly.

Both tap and swept drag intents pass through the same target gate before session
mutation. Tutorial progression reconciles after gesture end/cancellation, with
input cancelled before changing targets. This prevents one swipe affecting the
next lesson. A local seen preference is separate from language and board data;
manual replays create fresh sessions and counters. Tutorial instructions are
locale resources; no persistence/backend or reusable tutorial framework is added.

The refined tutorial projects read-only Empty/X/Cat samples into the view without
mutating GameSession or BoardPresentation. Guided completion creates a fresh
GameSession for the same fixture; the shell rebinds input and re-enables the view
for independent play. Only a real independent cat-set win completes onboarding.
