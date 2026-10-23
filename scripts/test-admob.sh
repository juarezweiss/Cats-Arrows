#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "${BASH_SOURCE[0]}")/unity.sh"
unity_prepare_run admob-focused
results="$UNITY_RUN_DIR/test-results.xml"
status=0
"$UNITY_EXECUTABLE" -batchmode -nographics -projectPath "$UNITY_PROJECT_PATH" \
  -runTests -testPlatform EditMode \
  -testFilter 'CatsAndArrows.Advertising.Tests;CatsAndArrows.Tests.MonetizationTests;CatsAndArrows.Tests.MonetizationStateStoreTests;CatsAndArrows.Tests.MonetizationShellTests;CatsAndArrows.Tests.AdPrivacyShellTests;CatsAndArrows.Advertising.BuildTests' \
  -testResults "$results" -logFile "$UNITY_LOG_FILE" || status=$?
python3 "$UNITY_REPO_ROOT/scripts/check-unity-results.py" "$results"
exit "$status"
