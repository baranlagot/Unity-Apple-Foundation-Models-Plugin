#!/usr/bin/env bash
set -euo pipefail

if [[ $# -lt 1 ]]; then
  echo "Usage: $0 <export-path> [xcodebuild-log-path]" >&2
  exit 1
fi

EXPORT_PATH=$1
LOG_PATH=${2:-"$EXPORT_PATH/xcodebuild.log"}
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

mkdir -p "$(dirname "$LOG_PATH")"
set -o pipefail
xcodebuild \
  -project "$EXPORT_PATH/Unity-iPhone.xcodeproj" \
  -scheme UnityFramework \
  -sdk iphoneos \
  -destination "generic/platform=iOS" \
  CODE_SIGNING_ALLOWED=NO \
  CODE_SIGNING_REQUIRED=NO \
  build 2>&1 | tee "$LOG_PATH"
