# Testing Strategy

## Local Unity validation

Bootstrap tooling is tracked by GitHub Issue #1. Use a local Codex task opened on
this checkout to run the Unity editor installed on your machine. Running the Codex
desktop app does not move an existing cloud task onto your Mac.

If fetching a branch fails with `could not read Username for 'https://github.com'`,
authenticate Git on the Mac; the cloud task's Git proxy authentication is not
automatically shared with a local checkout. With GitHub CLI installed, run these
commands yourself in Mac Terminal and complete sign-in through the browser:

```bash
gh auth login --hostname github.com --git-protocol https --web
gh auth setup-git --hostname github.com
```

Then local Codex can fetch/check out the issue branch (or use `gh pr checkout 9`
for this bootstrap PR). Check the working tree before switching and preserve local
changes. Alternatively, use GitHub Desktop's signed-in UI to fetch and switch to
the branch before reopening local Codex. Never paste credentials into chat or copy
cloud tokens into the local Git configuration.

Repository JSON validation accepts plain UTF-8 and Unity-generated UTF-8 BOMs
without rewriting package manifests or assembly definitions. Malformed JSON still
fails validation.

Prerequisites:
- Install and activate the pinned Unity 6.3 LTS editor through Unity Hub. The
  scripts read the exact patch from `UnityProject/ProjectSettings/ProjectVersion.txt`.
- Put Python 3.11+ on PATH for `scripts/validate.sh` (which uses `tomllib`) and result
  checking. Unity Hub does not install this prerequisite; Mac system Python may be
  absent or older. Check with `python3 --version`.
- Close the GUI Editor for this project before batch runs. Unity cannot open the
  same project in two editor processes concurrently.

Run from the repository root, or pass an absolute path to a script from elsewhere:

```bash
bash scripts/unity.sh
bash scripts/validate.sh
bash scripts/compile.sh
bash scripts/test.sh
```

Automatic discovery on macOS uses:
`/Applications/Unity/Hub/Editor/<pinned-version>/Unity.app/Contents/MacOS/Unity`.
Linux Hub installs use `$HOME/Unity/Hub/Editor/<pinned-version>/Editor/Unity`.
The scripts never fall back to a different installed patch.

For a custom Hub editor directory:

```bash
UNITY_HUB_EDITOR_ROOT="/Volumes/Development/Unity Editors" bash scripts/test.sh
```

For another installation layout, explicitly select the editor executable:

```bash
UNITY_EDITOR="/Applications/Unity/Hub/Editor/6000.3.25f1/Unity.app/Contents/MacOS/Unity" bash scripts/test.sh
```

An invalid explicit override fails rather than silently falling back. An explicit
override can select a different editor; inspect its version first and keep any
intentional project-version upgrade in separate work.

Compilation imports the project in batch mode and retains `Editor.log`. EditMode
runs omit `-quit` so the Test Framework controls completion. Each run creates a
fresh directory under ignored `.unity-results/`; test runs retain `Editor.log` and
`test-results.xml`. A stale report cannot satisfy a subsequent run. Editor failures
retain their exit status, and missing/malformed/zero-test, failed, skipped, or
inconclusive NUnit results make the test command fail. Compilation alone does not
prove the tests ran.

The bootstrap currently has eight original EditMode tests. `ExtraCatPreventsVictory`
has a known fixture-ordering defect tracked for triage under #5: it completes the
solution before adding the extra cat. Do not weaken assertions or delay immediate
victory while configuring the local runner. Report the actual native results.

The runner contracts themselves can be checked without Unity:

```bash
python3 -m unittest discover -s scripts/tests -v
```

These tests use a process fixture to exercise discovery, quoting, working-directory
independence, fresh results, and failure handling. They do not validate Unity import
or replace the C# EditMode suite.

References: [Unity editor arguments](https://docs.unity3d.com/6000.3/Documentation/Manual/EditorCommandLineArguments.html)
and [Test Framework arguments](https://docs.unity3d.com/6000.3/Documentation/Manual/test-framework/reference-command-line.html).

## Pyramid
1. Pure domain/EditMode tests: highest volume.
2. Unity integration/PlayMode tests: board/session/UI wiring.
3. Device smoke tests: iOS + Android touch, safe-area, performance, lifecycle.

## Mandatory puzzle tests
- arrow cell can never be solution cat;
- row/column totals equal solution;
- maximum cat counts by profile;
- zero-row/zero-column cap;
- arrow truth for every generated arrow;
- generated puzzle unique;
- human solver completes without guessing/hints;
- same seed + generator version => same logical puzzle;
- invalid candidates rejected.

## Mandatory session tests
- tap: Empty -> X -> Cat -> Empty;
- drag affects Empty only;
- drag never modifies arrow/protected/completed cells;
- drag processes each cell once;
- exact solution triggers win immediately;
- extra player cat prevents win;
- Empty and X are both acceptable outside solution;
- Clear resets player marks only;
- New Game replaces puzzle but preserves selected difficulty;
- completed board is immutable.

## Device matrix for MVP
- recent iPhone simulator/device with notch/Dynamic Island safe area;
- older/smaller iPhone viewport;
- mainstream Android phone;
- at least one tall/narrow Android aspect ratio.
