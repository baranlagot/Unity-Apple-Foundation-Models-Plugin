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

"$UNITY_EDITOR_PATH" \
  -batchmode \
  -nographics \
  -projectPath "$PROJECT_PATH" \
  -executeMethod Baran.AppleFoundationModels.Editor.AppleFoundationModelsValidationCommands.ExportIOSValidationProject \
  -afmExportPath "$EXPORT_PATH" \
  -quit

PROJECT_FILE="$EXPORT_PATH/Unity-iPhone.xcodeproj/project.pbxproj"
if [[ ! -f "$PROJECT_FILE" ]]; then
  echo "Expected Xcode project at $PROJECT_FILE" >&2
  exit 1
fi

if [[ $(grep -Ec 'AppleFoundationModelsCore.swift in Sources \*/ = \{isa = PBXBuildFile;' "$PROJECT_FILE") -ne 1 ]]; then
  echo "Expected AppleFoundationModelsCore.swift to be added to UnityFramework sources exactly once." >&2
  exit 1
fi

if [[ $(grep -Ec 'AppleFoundationModelsModels.swift in Sources \*/ = \{isa = PBXBuildFile;' "$PROJECT_FILE") -ne 1 ]]; then
  echo "Expected AppleFoundationModelsModels.swift to be added to UnityFramework sources exactly once." >&2
  exit 1
fi

if [[ $(grep -Ec 'AppleFoundationModelsErrorMapper.swift in Sources \*/ = \{isa = PBXBuildFile;' "$PROJECT_FILE") -ne 1 ]]; then
  echo "Expected AppleFoundationModelsErrorMapper.swift to be added to UnityFramework sources exactly once." >&2
  exit 1
fi

if ! grep -q "FoundationModels.framework in Frameworks" "$PROJECT_FILE"; then
  echo "Expected FoundationModels.framework to be linked in the generated project." >&2
  exit 1
fi

if ! grep -q "FoundationModels.framework in Frameworks.*ATTRIBUTES = (Weak, );" "$PROJECT_FILE"; then
  echo "Expected FoundationModels.framework to be weak-linked." >&2
  exit 1
fi

xcodebuild \
  -project "$EXPORT_PATH/Unity-iPhone.xcodeproj" \
  -scheme UnityFramework \
  -sdk iphoneos \
  -destination "generic/platform=iOS" \
  CODE_SIGNING_ALLOWED=NO \
  CODE_SIGNING_REQUIRED=NO \
  build
