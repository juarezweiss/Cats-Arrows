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
threads or implement a puzzle queue. Later gameplay work should prepare a small
number of validated puzzles ahead of New Game and use a short transition for an
empty queue. Ads, if separately approved, may overlap preparation but cannot be a
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
MVP persists only local preferences and lightweight progress/statistics. The puzzle seed is sufficient to resume/recreate an active board once save/resume is implemented.

## Services
Analytics, ads, IAP, remote config, and attribution must sit behind interfaces/adapters. Core gameplay cannot require a vendor SDK to compile or run.
