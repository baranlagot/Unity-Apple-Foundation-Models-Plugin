#!/usr/bin/env bash
set -euo pipefail

# Builds the capability-showcase sample into an iOS Xcode project for on-device or
# Simulator testing. Signing team and the target device/simulator are chosen in Xcode.
#
# Usage: build_ios_device_sample.sh <unity-editor-path> [device|simulator] [export-path]
#
# The Simulator build uses the host Mac's Apple Intelligence, so it can run real inference
# on an Apple Silicon Mac with Apple Intelligence enabled (otherwise it shows unavailable).

if [[ $# -lt 1 ]]; then
  echo "Usage: $0 <unity-editor-path> [device|simulator] [export-path]" >&2
  exit 1
fi

UNITY_EDITOR_PATH=$1
SDK_MODE=${2:-device}
case "$SDK_MODE" in
  device|simulator) ;;
  *) echo "Second argument must be 'device' or 'simulator'." >&2; exit 1 ;;
esac

REPO_ROOT=$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)
# The test project references this repo as a local package (file:../..), so the repo root
# is scanned by Unity as package content. Export to a "~"-suffixed folder, which Unity
# ignores, so the generated Xcode project (with its own managed assemblies) is never
# imported back into the project and cannot clash with UnityEngine types.
EXPORT_PATH=${3:-"$REPO_ROOT/Builds~/ios-showcase-$SDK_MODE"}
PROJECT_PATH="$REPO_ROOT/TestProject~"
export AFM_IOS_SDK="$SDK_MODE"

# Bring the packaged Capability Showcase sample into the test project so it compiles.
SAMPLE_DEST="$PROJECT_PATH/Assets/Samples/CapabilityShowcase"
rm -rf "$SAMPLE_DEST"
mkdir -p "$SAMPLE_DEST"
cp "$REPO_ROOT/Samples~/CapabilityShowcase/CapabilityShowcaseExample.cs" "$SAMPLE_DEST/"

# -buildTarget iOS makes iOS the active target at startup so the Editor assembly compiles
# with UNITY_IOS defined. The native postprocessor that copies the Swift core and
# weak-links FoundationModels lives under #if UNITY_IOS, so without this it is excluded and
# the exported project would omit the native integration.
"$UNITY_EDITOR_PATH" \
  -batchmode \
  -nographics \
  -buildTarget iOS \
  -projectPath "$PROJECT_PATH" \
  -executeMethod Baran.AppleFoundationModels.Editor.AppleFoundationModelsValidationCommands.ExportIOSCapabilityShowcaseApp \
  -afmExportPath "$EXPORT_PATH" \
  -quit

echo "Exported capability-showcase iOS ($SDK_MODE) project to: $EXPORT_PATH"
