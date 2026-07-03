#!/usr/bin/env bash
set -euo pipefail

if [[ $# -lt 1 ]]; then
  echo "Usage: $0 <unity-editor-path> [default|ios]" >&2
  exit 1
fi

UNITY_EDITOR_PATH=$1
TARGET_MODE=${2:-default}
REPO_ROOT=$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)
PROJECT_PATH="$REPO_ROOT/TestProject~"
RESULTS_DIR=${3:-"${TMPDIR%/}/unity-apple-foundation-models/TestResults"}
mkdir -p "$RESULTS_DIR"

ARGS=(
  -batchmode
  -nographics
  -projectPath "$PROJECT_PATH"
  -runTests
  -testPlatform EditMode
  -testResults "$RESULTS_DIR/editmode-${TARGET_MODE}.xml"
  -quit
)

if [[ "$TARGET_MODE" == "ios" ]]; then
  ARGS+=(-buildTarget iOS)
fi

"$UNITY_EDITOR_PATH" "${ARGS[@]}"
