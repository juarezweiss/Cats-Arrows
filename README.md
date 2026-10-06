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

## First milestone
Build a deterministic vertical slice: generate a valid Easy board, render it in portrait, support tap and drag marking, detect victory immediately, and prove the puzzle is solvable without hints.
