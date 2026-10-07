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

## Randomness
Use explicit seeds. Avoid `UnityEngine.Random` inside core generation. A seed must reproduce the same logical board for a fixed generator version.

## Persistence
MVP persists only local preferences and lightweight progress/statistics. The puzzle seed is sufficient to resume/recreate an active board once save/resume is implemented.

## Services
Analytics, ads, IAP, remote config, and attribution must sit behind interfaces/adapters. Core gameplay cannot require a vendor SDK to compile or run.
