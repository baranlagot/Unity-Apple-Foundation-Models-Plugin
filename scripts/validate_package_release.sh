#!/usr/bin/env bash
set -euo pipefail

EXPECTED_TAG=${1:-}
REPO_ROOT=$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)
PACKAGE_VERSION=$(
  /usr/bin/python3 - <<'PY' "$REPO_ROOT/package.json"
import json, sys
with open(sys.argv[1], 'r', encoding='utf-8') as handle:
    print(json.load(handle)["version"])
PY
)
METADATA_VERSION=$(
  /usr/bin/python3 - <<'PY' "$REPO_ROOT/Runtime/AppleFoundationModelsReleaseMetadata.cs"
import re, sys
text = open(sys.argv[1], 'r', encoding='utf-8').read()
match = re.search(r'PackageVersion = "([^"]+)"', text)
print(match.group(1) if match else "")
PY
)
CHANGELOG_VERSION=$(
  /usr/bin/python3 - <<'PY' "$REPO_ROOT/CHANGELOG.md"
import re, sys
text = open(sys.argv[1], 'r', encoding='utf-8').read()
match = re.search(r'^## \[(\d+\.\d+\.\d+)\]', text, re.MULTILINE)
print(match.group(1) if match else "")
PY
)

if [[ -z "$CHANGELOG_VERSION" ]]; then
  echo "CHANGELOG.md does not contain a released semantic version heading." >&2
  exit 1
fi

if [[ "$PACKAGE_VERSION" != "$METADATA_VERSION" ]]; then
  echo "package.json version ($PACKAGE_VERSION) does not match runtime metadata ($METADATA_VERSION)." >&2
  exit 1
fi

if [[ "$PACKAGE_VERSION" != "$CHANGELOG_VERSION" ]]; then
  echo "package.json version ($PACKAGE_VERSION) does not match CHANGELOG.md ($CHANGELOG_VERSION)." >&2
  exit 1
fi

if [[ -n "$EXPECTED_TAG" ]]; then
  NORMALIZED_TAG=${EXPECTED_TAG#v}
  if [[ "$PACKAGE_VERSION" != "$NORMALIZED_TAG" ]]; then
    echo "package.json version ($PACKAGE_VERSION) does not match expected tag ($EXPECTED_TAG)." >&2
    exit 1
  fi
fi
