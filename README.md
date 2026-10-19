# Paws

Paws is a cozy mobile logic puzzle about finding every hidden paw. Read the row
and column totals, follow directional arrows, and mark the board until the exact
solution is revealed—no guessing required.

## How to play

- Each row and column total gives the exact number of hidden paws in that line.
- An arrow means at least one hidden paw lies along its forward horizontal,
  vertical, or diagonal path. Arrow cells cannot contain paws or be marked.
- Tap a playable cell to cycle `Empty → X → Paw → Empty`. **Paw** is the board
  symbol for a suspected hidden paw; **X** marks a cell that cannot contain one.
- Drag horizontally or vertically to add X marks to eligible empty cells.
- Hint applies one valid clue-derived Paw or X deduction. Clear removes player
  marks so the puzzle can be reconsidered from the original clues.
- The board is complete as soon as the placed Paw symbols exactly match the
  hidden-paw solution. Completed boards are read-only; continue with Next board.

The current game includes:

- **Easy — 5×5:** compact boards and a fixed first-play tutorial.
- **Medium — 6×6:** larger boards with denser totals and clue combinations.
- **Hard — 7×7:** the largest boards, with no starting hatches; players mark all
  deductions themselves.

Ordinary Play and Next board select among all three difficulties while limiting
long repeat streaks. Every generated puzzle is seeded, reproducible, uniquely
solvable, and validated for a complete logical path without hints.

## Project status

Paws is a playable iOS-first pre-alpha. It supports portrait play in English,
Brazilian Portuguese, and Spanish. Android work and wider distribution are
deferred while the iOS tester build is prepared.

For the detailed rules and guarantees, see [Game Design](docs/GAME_DESIGN.md) and
[Puzzle Solvability](docs/PUZZLE_SOLVABILITY.md).

## Technology

- Unity 6.3 LTS; the exact editor patch is pinned in
  [ProjectVersion.txt](UnityProject/ProjectSettings/ProjectVersion.txt)
- C# with deterministic puzzle-domain logic separated from Unity presentation
- GitHub Issues and pull requests for traceability
- Codex orchestration through [AGENTS.md](AGENTS.md), the repo-local development
  skill, [CONTEXT_MAP.md](docs/CONTEXT_MAP.md), and specialist agents when useful

## Development workflow

Work starts from a [GitHub Issue](https://github.com/juarezweiss/Paws/issues)
and a short-lived issue branch. Codex loads only the task-relevant repository
context, makes focused changes, runs the required checks, and prepares a pull
request into `develop`. The `main` branch remains human-controlled release
history; production merges and releases require explicit owner approval.

## Get started

1. Install the Unity editor version pinned in `ProjectVersion.txt` through Unity Hub.
2. Open [UnityProject](UnityProject/) in Unity.
3. Read [AGENTS.md](AGENTS.md), then use
   [CONTEXT_MAP.md](docs/CONTEXT_MAP.md) for task-specific context.
4. Select or create an Issue before implementation.
5. Validate from the repository root:

```bash
bash scripts/unity.sh
bash scripts/validate.sh
bash scripts/compile.sh
bash scripts/test.sh
```

Local Unity setup, result requirements, and test artifacts are documented in
[Testing](docs/TESTING.md#local-unity-validation).
