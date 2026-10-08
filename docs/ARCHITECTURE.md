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
- victory modal;
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
