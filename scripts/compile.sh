#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "${BASH_SOURCE[0]}")/unity.sh"
unity_prepare_run compile

if "$UNITY_EXECUTABLE" -batchmode -nographics -projectPath "$UNITY_PROJECT_PATH" \
  -logFile "$UNITY_LOG_FILE" -quit; then
  UNITY_STATUS=0
else
  UNITY_STATUS=$?
fi
if [[ "$UNITY_STATUS" -ne 0 ]]; then
  echo "Unity import/compilation failed (exit $UNITY_STATUS). Log: $UNITY_LOG_FILE" >&2
  exit "$UNITY_STATUS"
fi
if [[ ! -s "$UNITY_LOG_FILE" ]]; then
  echo "Unity exited without a nonempty compilation log: $UNITY_LOG_FILE" >&2
  exit 1
fi
if grep -Eiq 'error CS[0-9]+|Scripts have compiler errors' "$UNITY_LOG_FILE"; then
  echo "Unity reported compiler errors. Log: $UNITY_LOG_FILE" >&2
  exit 1
fi
echo "Unity import/compilation passed. Log: $UNITY_LOG_FILE"
