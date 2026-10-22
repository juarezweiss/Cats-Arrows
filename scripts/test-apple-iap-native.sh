#!/usr/bin/env bash
# Issue #64: desktop policy regressions and iOS SDK typecheck, no physical-device input.
set -euo pipefail
root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
mkdir -p "$root/.unity-results"
run="$(mktemp -d "$root/.unity-results/apple-native.XXXXXX")"
native="$root/UnityProject/Assets/CatsAndArrows/Purchasing/Apple/Plugins/iOS"
xcrun swiftc "$native/PawsAppleEvidencePolicy.swift" "$root/scripts/tests/apple_iap_policy.swift" -o "$run/policy-tests"
"$run/policy-tests"
xcrun --sdk iphonesimulator swiftc -typecheck -target arm64-apple-ios16.3-simulator \
  -sdk "$(xcrun --sdk iphonesimulator --show-sdk-path)" \
  "$native/PawsAppleEvidencePolicy.swift" "$native/PawsAppleEvidence.swift"
echo 'Apple native iOS SDK typecheck: passed'
