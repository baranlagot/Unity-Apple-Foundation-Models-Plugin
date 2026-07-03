#!/usr/bin/env bash
set -euo pipefail

# Ordinary invocation (no argument): verify package.json and runtime metadata agree and
# that release notes are staged under the changelog's Unreleased section. This is the
# check that runs on every push/PR while the release is still being prepared.
#
# Tag-time invocation (expected tag argument, e.g. v0.1.0): additionally require the tag
# to match the package version and the changelog to carry a dated released heading for
# that version. This is the check that gates publishing the Git tag.

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

if [[ "$PACKAGE_VERSION" != "$METADATA_VERSION" ]]; then
  echo "package.json version ($PACKAGE_VERSION) does not match runtime metadata ($METADATA_VERSION)." >&2
  exit 1
fi

if ! grep -qE '^## \[Unreleased\]' "$REPO_ROOT/CHANGELOG.md"; then
  echo "CHANGELOG.md must contain an '## [Unreleased]' section." >&2
  exit 1
fi

if [[ -z "$EXPECTED_TAG" ]]; then
  echo "Release metadata consistent (pre-release): package=$PACKAGE_VERSION, metadata=$METADATA_VERSION."
  exit 0
fi

# Tag-time validation.
NORMALIZED_TAG=${EXPECTED_TAG#v}
if [[ "$PACKAGE_VERSION" != "$NORMALIZED_TAG" ]]; then
  echo "package.json version ($PACKAGE_VERSION) does not match expected tag ($EXPECTED_TAG)." >&2
  exit 1
fi

RELEASED_HEADING=$(
  /usr/bin/python3 - <<'PY' "$REPO_ROOT/CHANGELOG.md" "$PACKAGE_VERSION"
import re, sys
text = open(sys.argv[1], 'r', encoding='utf-8').read()
version = re.escape(sys.argv[2])
match = re.search(r'^## \[' + version + r'\] - \d{4}-\d{2}-\d{2}', text, re.MULTILINE)
print("ok" if match else "")
PY
)

if [[ "$RELEASED_HEADING" != "ok" ]]; then
  echo "CHANGELOG.md must contain a dated '## [$PACKAGE_VERSION] - YYYY-MM-DD' heading before tagging $EXPECTED_TAG." >&2
  exit 1
fi

echo "Release metadata consistent for tag $EXPECTED_TAG: package=$PACKAGE_VERSION."
