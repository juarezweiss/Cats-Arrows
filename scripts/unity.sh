#!/usr/bin/env bash
# Shared discovery for local Unity validation; compatible with macOS Bash 3.2.
set -euo pipefail

UNITY_REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
UNITY_PROJECT_PATH="$UNITY_REPO_ROOT/UnityProject"

unity_find_editor() {
  UNITY_PROJECT_VERSION="$(sed -n 's/^m_EditorVersion: *//p' "$UNITY_PROJECT_PATH/ProjectSettings/ProjectVersion.txt" | tr -d '\r')"
  if [[ -z "$UNITY_PROJECT_VERSION" ]]; then
    echo "Cannot read the pinned Unity version from ProjectVersion.txt." >&2
    return 2
  fi

  if [[ -n "${UNITY_EDITOR:-}" ]]; then
    if [[ "$UNITY_EDITOR" != /* || ! -f "$UNITY_EDITOR" || ! -x "$UNITY_EDITOR" ]]; then
      echo "UNITY_EDITOR must be an absolute path to an executable Unity editor." >&2
      return 2
    fi
    UNITY_EXECUTABLE="$UNITY_EDITOR"
    return
  fi

  case "$(uname -s)" in
    Darwin)
      UNITY_INSTALL_ROOT="${UNITY_HUB_EDITOR_ROOT:-/Applications/Unity/Hub/Editor}"
      UNITY_EXECUTABLE="$UNITY_INSTALL_ROOT/$UNITY_PROJECT_VERSION/Unity.app/Contents/MacOS/Unity"
      ;;
    Linux)
      UNITY_INSTALL_ROOT="${UNITY_HUB_EDITOR_ROOT:-$HOME/Unity/Hub/Editor}"
      UNITY_EXECUTABLE="$UNITY_INSTALL_ROOT/$UNITY_PROJECT_VERSION/Editor/Unity"
      ;;
    *)
      echo "Automatic discovery supports macOS/Linux. Set UNITY_EDITOR to an executable path." >&2
      return 2
      ;;
  esac

  if [[ "$UNITY_EXECUTABLE" != /* ]]; then
    echo "UNITY_HUB_EDITOR_ROOT must be an absolute path." >&2
    return 2
  fi
  if [[ ! -f "$UNITY_EXECUTABLE" || ! -x "$UNITY_EXECUTABLE" ]]; then
    echo "Unity $UNITY_PROJECT_VERSION was not found at: $UNITY_EXECUTABLE" >&2
    echo "Install the pinned editor in Unity Hub, or set UNITY_EDITOR to its executable." >&2
    return 2
  fi
}

unity_prepare_run() {
  unity_find_editor
  mkdir -p "$UNITY_REPO_ROOT/.unity-results"
  UNITY_RUN_DIR="$(mktemp -d "$UNITY_REPO_ROOT/.unity-results/$1.XXXXXX")"
  UNITY_LOG_FILE="$UNITY_RUN_DIR/Editor.log"
  printf 'Unity executable: %s\nProject version: %s\nRun artifacts: %s\n' \
    "$UNITY_EXECUTABLE" "$UNITY_PROJECT_VERSION" "$UNITY_RUN_DIR"
}

if [[ "${BASH_SOURCE[0]}" == "$0" ]]; then
  unity_find_editor
  printf '%s\n' "$UNITY_EXECUTABLE"
fi
