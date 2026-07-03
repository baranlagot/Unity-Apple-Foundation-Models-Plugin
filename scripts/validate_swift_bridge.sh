#!/usr/bin/env bash
set -euo pipefail

REPO_ROOT=$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)
IOS_SDK=$(xcrun --sdk iphoneos --show-sdk-path)
MACOS_SDK=$(xcrun --sdk macosx --show-sdk-path)

swiftc -typecheck \
  -warnings-as-errors \
  -sdk "$IOS_SDK" \
  -target arm64-apple-ios26.0 \
  -strict-concurrency=complete \
  "$REPO_ROOT/Native~/Sources/AppleFoundationModelsCore/AppleFoundationModelsModels.swift" \
  "$REPO_ROOT/Native~/Sources/AppleFoundationModelsCore/AppleFoundationModelsErrorMapper.swift" \
  "$REPO_ROOT/Native~/Sources/AppleFoundationModelsCore/AppleFoundationModelsCore.swift" \
  "$REPO_ROOT/Plugins/iOS/AppleFoundationModelsBridge.swift"

xcrun swift \
  -sdk "$MACOS_SDK" \
  -target arm64-apple-macosx26.0 \
  "$REPO_ROOT/Native~/Sources/AppleFoundationModelsCore/AppleFoundationModelsModels.swift" \
  "$REPO_ROOT/Native~/Sources/AppleFoundationModelsCore/AppleFoundationModelsErrorMapper.swift" \
  "$REPO_ROOT/Native~/Sources/AppleFoundationModelsCore/AppleFoundationModelsCore.swift" \
  "$REPO_ROOT/Tests/Native/AppleFoundationModelsNativeHarness.swift"
