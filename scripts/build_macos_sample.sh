#!/usr/bin/env bash
set -euo pipefail

# Builds the capability-showcase sample into a native macOS .app that uses the real
# on-device model (Foundation Models, Vision, Image Playground) on an Apple Intelligence
# Mac. Because macOS builds do not go through an Xcode project like iOS, the native Swift
# bridges are compiled into a dylib and injected into the app bundle here, then the bundle
# is re-signed ad-hoc for local running.
#
# Usage: build_macos_sample.sh <unity-editor-path> [export-app-path]

if [[ $# -lt 1 ]]; then
  echo "Usage: $0 <unity-editor-path> [export-app-path]" >&2
  exit 1
fi

UNITY_EDITOR_PATH=$1
REPO_ROOT=$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)
EXPORT_PATH=${2:-"$REPO_ROOT/Builds~/mac-showcase/AFMShowcase.app"}
PROJECT_PATH="$REPO_ROOT/TestProject~"

# Bring the packaged Capability Showcase sample into the test project so it compiles.
SAMPLE_DEST="$PROJECT_PATH/Assets/Samples/CapabilityShowcase"
rm -rf "$SAMPLE_DEST"
mkdir -p "$SAMPLE_DEST"
cp "$REPO_ROOT/Samples~/CapabilityShowcase/CapabilityShowcaseExample.cs" "$SAMPLE_DEST/"

# 1. Build the macOS .app. StandaloneOSX players select the native provider.
"$UNITY_EDITOR_PATH" \
  -batchmode \
  -nographics \
  -projectPath "$PROJECT_PATH" \
  -executeMethod Baran.AppleFoundationModels.Editor.AppleFoundationModelsValidationCommands.ExportMacShowcaseApp \
  -afmExportPath "$EXPORT_PATH" \
  -quit

# 2. Compile the native bridges into a dylib and inject it into the app bundle.
PLUGINS_DIR="$EXPORT_PATH/Contents/PlugIns"
mkdir -p "$PLUGINS_DIR"
DYLIB="$PLUGINS_DIR/AppleFoundationModelsMac.dylib"
MACOS_SDK=$(xcrun --sdk macosx --show-sdk-path)

swiftc -emit-library -o "$DYLIB" \
  -sdk "$MACOS_SDK" \
  -target arm64-apple-macosx26.0 \
  -strict-concurrency=complete \
  "$REPO_ROOT/Native~/Sources/AppleFoundationModelsCore/AppleFoundationModelsModels.swift" \
  "$REPO_ROOT/Native~/Sources/AppleFoundationModelsCore/AppleFoundationModelsErrorMapper.swift" \
  "$REPO_ROOT/Native~/Sources/AppleFoundationModelsCore/AppleFoundationModelsCore.swift" \
  "$REPO_ROOT/Plugins/iOS/AppleFoundationModelsBridge.swift" \
  "$REPO_ROOT/Plugins/iOS/AppleVisionBridge.swift" \
  "$REPO_ROOT/Plugins/iOS/AppleImagePlaygroundBridge.swift" \
  -framework FoundationModels \
  -framework Vision \
  -framework ImagePlayground

# 3. Re-sign so the injected dylib matches the app signature (ad-hoc, for local running).
codesign --force --sign - "$DYLIB"
codesign --force --deep --sign - "$EXPORT_PATH"

echo "Built macOS showcase app: $EXPORT_PATH"
echo "Injected native library:  $DYLIB"
