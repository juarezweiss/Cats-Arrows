#!/usr/bin/env bash
# Issue #64: unsigned development preparation only. No store/account/device actions.
set -euo pipefail
source "$(dirname "${BASH_SOURCE[0]}")/unity.sh"
output="${1:-}"
[[ "$output" == /* && ! -e "$output" ]] || { echo 'Usage: build-apple-iap-review.sh /absolute/fresh-export' >&2; exit 2; }
[[ "${PAWS_APPLE_PRODUCT_ID:-}" == 'com.jweiss.paws.removeads' ]] || {
  echo 'Inject owner-approved PAWS_APPLE_PRODUCT_ID=com.jweiss.paws.removeads.' >&2; exit 2;
}
[[ -z "$(git -C "$UNITY_REPO_ROOT" status --porcelain --untracked-files=all -- UnityProject scripts)" ]] || {
  echo 'Commit UnityProject/scripts inputs before a clean review export.' >&2; exit 2;
}
source_sha="$(git -C "$UNITY_REPO_ROOT" rev-parse HEAD)"
unity_prepare_run apple-iap-export
PAWS_IOS_EXPORT="$output" PAWS_IAP_SOURCE_SHA="$source_sha" "$UNITY_EXECUTABLE" -batchmode -nographics -quit \
  -projectPath "$UNITY_PROJECT_PATH" -buildTarget iOS \
  -executeMethod CatsAndArrows.Purchasing.Apple.Review.Editor.AppleIapReviewBuild.Build -logFile "$UNITY_LOG_FILE"
[[ -s "$output/paws-iap-review.json" && -s "$output/Unity-iPhone.xcodeproj/project.pbxproj" ]] || {
  echo 'Incomplete Apple IAP review export.' >&2; exit 1;
}
echo "Unsigned development export ready: $output"
