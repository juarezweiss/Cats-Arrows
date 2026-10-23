#!/usr/bin/env bash
# #66: Google test ads, no store/account/device/production actions.
set -euo pipefail
source "$(dirname "${BASH_SOURCE[0]}")/unity.sh"
output="${1:-}"
[[ "$output" == /* && ! -e "$output" ]] || { echo 'Usage: build-admob-review.sh /absolute/fresh-export' >&2; exit 2; }
[[ -z "$(git -C "$UNITY_REPO_ROOT" status --porcelain --untracked-files=all -- UnityProject scripts)" ]] || {
  echo 'Commit UnityProject/scripts inputs before a clean review export.' >&2; exit 2;
}
python3 "$UNITY_REPO_ROOT/scripts/check-admob-sdk.py"
source_sha="$(git -C "$UNITY_REPO_ROOT" rev-parse HEAD)"
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
unity_prepare_run admob-export
PAWS_IOS_EXPORT="$output" PAWS_ADMOB_SOURCE_SHA="$source_sha" "$UNITY_EXECUTABLE" -batchmode -nographics -quit \
  -projectPath "$UNITY_PROJECT_PATH" -buildTarget iOS \
  -executeMethod CatsAndArrows.Advertising.Review.Editor.AdMobReviewBuild.Build -logFile "$UNITY_LOG_FILE"
[[ -s "$output/paws-admob-review.json" && -s "$output/Unity-iPhone.xcodeproj/project.pbxproj" ]] || {
  echo 'Incomplete AdMob review export.' >&2; exit 1;
}
echo "Unsigned Google-test-ad development export ready: $output"
