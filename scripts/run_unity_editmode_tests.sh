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
RESULTS_FILE="$RESULTS_DIR/editmode-${TARGET_MODE}.xml"
mkdir -p "$RESULTS_DIR"

case "$TARGET_MODE" in
  default)
    EXPLICIT_BUILD_TARGET=${AFM_DEFAULT_EDITMODE_BUILD_TARGET:-StandaloneOSX}
    ;;
  ios)
    EXPLICIT_BUILD_TARGET=iOS
    ;;
  *)
    echo "Unsupported target mode: $TARGET_MODE" >&2
    exit 1
    ;;
esac

ARGS=(
  -batchmode
  -nographics
  -projectPath "$PROJECT_PATH"
  -runTests
  -testPlatform EditMode
  -buildTarget "$EXPLICIT_BUILD_TARGET"
  -testResults "$RESULTS_FILE"
)

"$UNITY_EDITOR_PATH" "${ARGS[@]}"
"$REPO_ROOT/scripts/assert_unity_test_results.sh" "$RESULTS_FILE"
