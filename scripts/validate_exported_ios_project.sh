#!/usr/bin/env bash
set -euo pipefail

if [[ $# -lt 1 ]]; then
  echo "Usage: $0 <unity-editor-path> [export-path]" >&2
  exit 1
fi

UNITY_EDITOR_PATH=$1
REPO_ROOT=$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)
EXPORT_PATH=${2:-"${TMPDIR%/}/unity-apple-foundation-models/iOSValidation"}
PROJECT_PATH="$REPO_ROOT/TestProject~"

# -buildTarget iOS makes iOS the active target so the #if UNITY_IOS postprocessor (which
# adds the native Swift core and links the frameworks) is compiled in and runs.
"$UNITY_EDITOR_PATH" \
  -batchmode \
  -nographics \
  -buildTarget iOS \
  -projectPath "$PROJECT_PATH" \
  -executeMethod Baran.AppleFoundationModels.Editor.AppleFoundationModelsValidationCommands.ExportIOSValidationProject \
  -afmExportPath "$EXPORT_PATH" \
  -quit

"$REPO_ROOT/scripts/assert_exported_ios_project.sh" "$EXPORT_PATH"
