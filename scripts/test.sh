#!/usr/bin/env bash
set -euo pipefail
UNITY_EDITOR="${UNITY_EDITOR:-}"
if [[ -z "$UNITY_EDITOR" ]]; then
  echo "UNITY_EDITOR is not set. Run EditMode tests from Unity Test Runner, or set UNITY_EDITOR to the Unity executable." >&2
  exit 2
fi
"$UNITY_EDITOR" -batchmode -nographics -projectPath "$(pwd)/UnityProject" -runTests -testPlatform EditMode -testResults "$(pwd)/test-results.xml" -quit
