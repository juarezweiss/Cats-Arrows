#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "${BASH_SOURCE[0]}")/unity.sh"
if ! command -v python3 >/dev/null 2>&1; then
  echo "Python 3 is required to validate Unity's NUnit results." >&2
  exit 2
fi
unity_prepare_run editmode
UNITY_RESULTS_FILE="$UNITY_RUN_DIR/test-results.xml"

# The Test Framework exits the editor when finished; -quit can interrupt tests.
if "$UNITY_EXECUTABLE" -batchmode -nographics -projectPath "$UNITY_PROJECT_PATH" \
  -runTests -testPlatform EditMode -testResults "$UNITY_RESULTS_FILE" \
  -logFile "$UNITY_LOG_FILE"; then
  UNITY_STATUS=0
else
  UNITY_STATUS=$?
fi

if python3 "$UNITY_REPO_ROOT/scripts/check-unity-results.py" "$UNITY_RESULTS_FILE"; then
  RESULTS_STATUS=0
else
  RESULTS_STATUS=$?
fi
if [[ "$UNITY_STATUS" -ne 0 ]]; then
  echo "Unity test process failed (exit $UNITY_STATUS). Log: $UNITY_LOG_FILE" >&2
  exit "$UNITY_STATUS"
fi
exit "$RESULTS_STATUS"
