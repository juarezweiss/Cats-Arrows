#!/usr/bin/env bash
# Issue #64: unsigned development preparation only. No store/account/device actions.
set -euo pipefail
source "$(dirname "${BASH_SOURCE[0]}")/unity.sh"
output="${1:-}"
[[ "$output" == /* && ! -e "$output" ]] || { echo 'Usage: build-apple-iap-review.sh /absolute/fresh-export' >&2; exit 2; }
[[ "${PAWS_APPLE_PRODUCT_ID:-}" == 'com.jweiss.paws.removeads' ]] || {
  echo 'Inject owner-approved PAWS_APPLE_PRODUCT_ID=com.jweiss.paws.removeads.' >&2; exit 2;
}
ask_to_buy="${PAWS_APPLE_SIMULATE_ASK_TO_BUY:-0}"
[[ "$ask_to_buy" == 0 || "$ask_to_buy" == 1 ]] || {
  echo 'PAWS_APPLE_SIMULATE_ASK_TO_BUY must be 0 or 1 (default: 0).' >&2; exit 2;
}
[[ -z "$(git -C "$UNITY_REPO_ROOT" status --porcelain --untracked-files=all -- UnityProject scripts)" ]] || {
  echo 'Commit UnityProject/scripts inputs before a clean review export.' >&2; exit 2;
}
source_sha="$(git -C "$UNITY_REPO_ROOT" rev-parse HEAD)"
# NewScene and Services Core can create editor-only defaults during the export.
# Remove only files absent on entry; preserve an existing owner's configuration.
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
unity_prepare_run apple-iap-export
PAWS_IOS_EXPORT="$output" PAWS_IAP_SOURCE_SHA="$source_sha" PAWS_APPLE_SIMULATE_ASK_TO_BUY="$ask_to_buy" "$UNITY_EXECUTABLE" -batchmode -nographics -quit \
  -projectPath "$UNITY_PROJECT_PATH" -buildTarget iOS \
  -executeMethod CatsAndArrows.Purchasing.Apple.Review.Editor.AppleIapReviewBuild.Build -logFile "$UNITY_LOG_FILE"
[[ -s "$output/paws-iap-review.json" && -s "$output/Unity-iPhone.xcodeproj/project.pbxproj" ]] || {
  echo 'Incomplete Apple IAP review export.' >&2; exit 1;
}
echo "Unsigned development export ready: $output"
