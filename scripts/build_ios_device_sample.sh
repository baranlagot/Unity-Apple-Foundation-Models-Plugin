#!/usr/bin/env bash
set -euo pipefail

# Builds the all-capabilities Device Validation sample scene into an iOS Xcode project for
# on-device testing. Signing team and the target device are configured in Xcode afterwards.
#
# Usage: build_ios_device_sample.sh <unity-editor-path> [export-path]

if [[ $# -lt 1 ]]; then
  echo "Usage: $0 <unity-editor-path> [export-path]" >&2
  exit 1
fi

UNITY_EDITOR_PATH=$1
REPO_ROOT=$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)
EXPORT_PATH=${2:-"$REPO_ROOT/build/ios-device-validation"}
PROJECT_PATH="$REPO_ROOT/TestProject~"

"$UNITY_EDITOR_PATH" \
  -batchmode \
  -nographics \
  -projectPath "$PROJECT_PATH" \
  -executeMethod Baran.AppleFoundationModels.Editor.AppleFoundationModelsValidationCommands.ExportIOSDeviceValidationApp \
  -afmExportPath "$EXPORT_PATH" \
  -quit

echo "Exported Device Validation iOS project to: $EXPORT_PATH"
