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

# Build and run the native harness as a real executable. Passing the harness to the
# `swift` interpreter treats it as a non-primary library file, silently skips its
# `@main` entry point, and reports success without executing a single assertion.
# Compiling with `-parse-as-library` honors `@main`, so the assertions actually run.
HARNESS_DIR=$(mktemp -d)
trap 'rm -rf "$HARNESS_DIR"' EXIT
HARNESS_BIN="$HARNESS_DIR/afm-native-harness"

xcrun swiftc \
  -parse-as-library \
  -warnings-as-errors \
  -sdk "$MACOS_SDK" \
  -target arm64-apple-macosx26.0 \
  -strict-concurrency=complete \
  "$REPO_ROOT/Native~/Sources/AppleFoundationModelsCore/AppleFoundationModelsModels.swift" \
  "$REPO_ROOT/Native~/Sources/AppleFoundationModelsCore/AppleFoundationModelsErrorMapper.swift" \
  "$REPO_ROOT/Native~/Sources/AppleFoundationModelsCore/AppleFoundationModelsCore.swift" \
  "$REPO_ROOT/Tests/Native/AppleFoundationModelsNativeHarness.swift" \
  -o "$HARNESS_BIN"

"$HARNESS_BIN"
