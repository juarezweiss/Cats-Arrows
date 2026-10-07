# Cats & Arrows

A cozy mobile logic puzzle about deducing the exact locations of hidden cats from row/column totals and directional arrow clues.

## Status
Pre-alpha foundation. The previous browser prototype is reference material only; this repository rebuilds the game in Unity with deterministic puzzle logic, solver-backed generation, mobile-first input, automated tests, and controlled releases.

## Technology
- Unity 6.3 LTS
- C#
- iOS and Android first
- GitHub Issues/PRs for task traceability
- Codex `AGENTS.md`, repo-local skill, and project-scoped subagents

## Branches
- `main`: human-approved production/release history only
- `develop`: integration/lower-environment branch
- feature/fix branches: short-lived work branches

## Start here
1. Install a current Unity 6.3 LTS editor through Unity Hub.
2. Open `UnityProject/`.
3. Read `AGENTS.md` and `docs/GAME_DESIGN.md`.
4. Work from a GitHub Issue.
5. Run `scripts/validate.sh` before opening a PR.

## Local Codex and Unity validation

Open this repository as a **local task** in the Codex app to give Codex access to
the editor on your Mac. A cloud task remains attached to its cloud machine.
Read [local validation instructions](docs/TESTING.md#local-unity-validation) for
editor discovery, prerequisites, and artifacts. From the repository root:

```bash
bash scripts/unity.sh     # Print the discovered editor executable.
bash scripts/validate.sh # Check repository contracts (Python 3.11+).
bash scripts/compile.sh  # Import and compile with the local editor.
bash scripts/test.sh     # Run and validate EditMode results.
```

The scripts find the exact version in `ProjectVersion.txt` in Unity Hub's default
installation directory. No manual `UNITY_EDITOR` setting is needed for a standard
Mac installation. Activate Unity Personal through Hub and close this project's
GUI Editor before running batch commands.

## First milestone
Build a deterministic vertical slice: generate a valid Easy board, render it in portrait, support tap and drag marking, detect victory immediately, and prove the puzzle is solvable without hints.
