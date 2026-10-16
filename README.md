# Cats & Arrows

A cozy mobile logic puzzle about deducing the exact locations of hidden cats from row/column totals and directional arrow clues.

## Status
Playable pre-alpha on iOS. The next milestone is a small 5–8 person tester trial:
[build preparation #21](https://github.com/juarezweiss/Cats-Arrows/issues/21) and
[feedback #22](https://github.com/juarezweiss/Cats-Arrows/issues/22).
Android work is deferred. Local iPhone builds are available; a TestFlight build
and distribution setup are still pending.

## Playable features
- Play chooses Easy 5×5, Medium 6×6 or Hard 7×7, with at most two consecutive
  boards of one difficulty even across restarts. Hard starts without
  hatches so players make and mark their own deductions.
- First Play opens the fixed Easy tutorial; subsequent Plays choose random boards.
  Tap cycling (`Empty → X → Cat → Empty`), horizontal/vertical
  drag marking, immediate one-fact Hint, Clear, Menu and Next board.
- Exact-cat victory and read-only completed boards; portrait layout in English,
  Brazilian Portuguese and Spanish.
- Deterministic seeded generation, with independent uniqueness verification and
  a complete logical derivation without guessing or hints for every admitted board.

See [game rules](docs/GAME_DESIGN.md) and the
[puzzle solvability contract](docs/PUZZLE_SOLVABILITY.md) for the full guarantees.

## Validation
As of 2026-10-16, Unity 6000.3.25f1 compilation and **340/340 EditMode tests**
passed for [Play selection #43](https://github.com/juarezweiss/Cats-Arrows/issues/43).
Native desktop portrait checks cover English, Brazilian Portuguese and Spanish;
physical-device validation of this flow is pending. See [Play test evidence](docs/TESTING.md#issue-43-persistent-play-selection).

The owner previously passed the Hard hatch-fix smoke checks on iPhone 17,
including manual marking, controls, completion locks and unchanged Easy/Medium
behavior, and reported that Hard feels better. This is qualitative evidence on
one device. See [hatch-fix evidence](docs/TESTING.md#issue-37-hard-hatch-regression).

## Technology
- Unity 6.3 LTS
- C#
- iOS first; Android deferred
- GitHub Issues/PRs for task traceability
- Codex `AGENTS.md`, repo-local skill, and project-scoped subagents

## Branches
- `main`: human-approved production/release history only
- `develop`: integration/lower-environment branch
- feature/fix branches: short-lived work branches

## Start here
1. Install the exact Unity editor pinned in [ProjectVersion.txt](UnityProject/ProjectSettings/ProjectVersion.txt) through Unity Hub.
2. Open `UnityProject/`.
3. Read [AGENTS.md](AGENTS.md) and use [CONTEXT_MAP.md](docs/CONTEXT_MAP.md) to load the context relevant to your task.
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

## Next milestone
Prepare a clean, versioned iOS tester build with confirmed app identity and review
modes excluded, then collect independent playtest feedback. See
[iOS trial preparation](docs/IOS_TRIAL_PREPARATION.md) and the
[tester questionnaire](docs/TESTER_QUESTIONNAIRE.md). Enrollment, spending and
distribution require separate owner approval.
