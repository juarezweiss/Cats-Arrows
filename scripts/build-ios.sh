#!/usr/bin/env bash
# #63/#67: immutable local export/archive preparation; never uploads or activates ads.
set -euo pipefail
source "$(dirname "${BASH_SOURCE[0]}")/unity.sh"
mode="${1:-}"
case "$mode" in
  export)
    output="${2:-}"
    [[ "$output" == /* && ! -e "$output" ]] || { echo 'Usage: build-ios.sh export /absolute/fresh-export' >&2; exit 2; }
    profile="${PAWS_MONETIZATION_PROFILE:-production}"
    case "$profile" in
      production)
        [[ "${PAWS_MONETIZATION_CONFIG:-}" == /* && -f "${PAWS_MONETIZATION_CONFIG:-}" ]] || {
          echo 'Production export requires PAWS_MONETIZATION_CONFIG naming an absolute local JSON configuration.' >&2; exit 2;
        } ;;
      validation|disabled) ;;
      *) echo 'PAWS_MONETIZATION_PROFILE must be production, validation or disabled.' >&2; exit 2 ;;
    esac
    # Unity may create editor defaults. Only remove files absent at invocation;
    # retain existing owner configuration and any nonempty Services Core file.
    generated_defaults=("")
    for relative in ProjectSettings/SceneTemplateSettings.json ProjectSettings/Packages/com.unity.services.core/Settings.json; do
      [[ -e "$UNITY_PROJECT_PATH/$relative" ]] || generated_defaults+=("$UNITY_PROJECT_PATH/$relative")
    done
    cleanup_defaults() {
      for path in "${generated_defaults[@]}"; do
        [[ -n "$path" ]] || continue
        if [[ "$path" == */com.unity.services.core/Settings.json && -s "$path" ]]; then continue; fi
        rm -f -- "$path"
      done
    }
    trap cleanup_defaults EXIT
    unity_prepare_run ios-export
    PAWS_IOS_EXPORT="$output" PAWS_MONETIZATION_PROFILE="$profile" "$UNITY_EXECUTABLE" -batchmode -nographics -quit \
      -projectPath "$UNITY_PROJECT_PATH" -buildTarget iOS \
      -executeMethod CatsAndArrows.UI.Editor.ProductionIOSBuild.Build -logFile "$UNITY_LOG_FILE"
    cleanup_defaults
    python3 "$UNITY_REPO_ROOT/scripts/ios-release.py" --export "$output" --output "$UNITY_RUN_DIR/export-receipt.json"
    ;;
  archive-unsigned|archive-signed)
    exported="${2:-}"
    archive="${3:-}"
    [[ "$exported" == /* && -d "$exported" && "$archive" == /* && ! -e "$archive" ]] || {
      echo 'Usage: build-ios.sh archive-unsigned|archive-signed /absolute/export /absolute/fresh.xcarchive' >&2; exit 2;
    }
    signing=(CODE_SIGNING_ALLOWED=NO CODE_SIGNING_REQUIRED=NO)
    if [[ "$mode" == archive-signed ]]; then
      # Explicit values from the local shell/Keychain, never account enrollment or profile download.
      for variable in PAWS_IOS_TEAM_ID PAWS_IOS_SIGNING_IDENTITY PAWS_IOS_PROFILE; do
        [[ -n "${!variable:-}" ]] || { echo "Missing local signing input: $variable" >&2; exit 2; }
      done
      signing=(CODE_SIGNING_ALLOWED=YES CODE_SIGN_STYLE=Manual
        "DEVELOPMENT_TEAM=$PAWS_IOS_TEAM_ID" "CODE_SIGN_IDENTITY=$PAWS_IOS_SIGNING_IDENTITY"
        "PROVISIONING_PROFILE_SPECIFIER_APP=$PAWS_IOS_PROFILE")
    fi
    # Validate the unmodified Unity export before allocating archive intermediates.
    unity_prepare_run ios-archive
    python3 "$UNITY_REPO_ROOT/scripts/ios-release.py" --export "$exported" --output "$UNITY_RUN_DIR/prearchive-receipt.json"
    xcodebuild -project "$exported/Unity-iPhone.xcodeproj" -scheme Unity-iPhone \
      -configuration Release -destination 'generic/platform=iOS' \
      -archivePath "$archive" -derivedDataPath "$UNITY_RUN_DIR/DerivedData" \
      "${signing[@]}" archive > "$UNITY_RUN_DIR/xcodebuild.log" 2>&1 || {
        echo "Archive failed; inspect $UNITY_RUN_DIR/xcodebuild.log" >&2; exit 1;
      }
    python3 "$UNITY_REPO_ROOT/scripts/ios-release.py" --export "$exported" --archive "$archive" \
      --output "$UNITY_RUN_DIR/release-receipt.json"
    echo "Archive validated. DerivedData may be removed after preserving the archive: $UNITY_RUN_DIR/DerivedData"
    ;;
  *) echo 'Usage: build-ios.sh export|archive-unsigned|archive-signed ...' >&2; exit 2 ;;
esac
