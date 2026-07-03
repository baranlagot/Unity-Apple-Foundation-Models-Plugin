#!/usr/bin/env bash
set -euo pipefail

if [[ $# -lt 1 ]]; then
  echo "Usage: $0 <results-file-or-directory>" >&2
  exit 1
fi

RESULTS_PATH=$1

/usr/bin/python3 - <<'PY' "$RESULTS_PATH"
import os
import sys
import xml.etree.ElementTree as ET


def local_name(tag: str) -> str:
    return tag.rsplit("}", 1)[-1]


def parse_candidate(path: str):
    if not os.path.isfile(path):
        return None
    if os.path.getsize(path) == 0:
        raise SystemExit(f"Unity test results file is empty: {path}")

    try:
        tree = ET.parse(path)
    except ET.ParseError as exc:
        raise SystemExit(f"Failed to parse Unity test results XML {path}: {exc}") from exc

    root = tree.getroot()
    if local_name(root.tag) != "test-run":
        return None

    result = (root.attrib.get("result") or "").strip()
    total = (root.attrib.get("total") or "0").strip()
    passed = (root.attrib.get("passed") or "0").strip()
    failed = (root.attrib.get("failed") or "0").strip()
    inconclusive = (root.attrib.get("inconclusive") or "0").strip()
    skipped = (root.attrib.get("skipped") or "0").strip()

    try:
        total_count = int(total)
    except ValueError as exc:
        raise SystemExit(f"Unity test results contain a non-integer total in {path}: {total}") from exc

    if total_count <= 0:
        raise SystemExit(f"Unity test results reported zero executed tests: {path}")

    if result != "Passed":
        raise SystemExit(
            "Unity test results did not pass: "
            f"path={path}, result={result or '<missing>'}, total={total}, "
            f"passed={passed}, failed={failed}, inconclusive={inconclusive}, skipped={skipped}"
        )

    print(
        "Unity test results passed: "
        f"path={path}, total={total}, passed={passed}, failed={failed}, "
        f"inconclusive={inconclusive}, skipped={skipped}"
    )
    return path


results_path = os.path.abspath(sys.argv[1])

if not os.path.exists(results_path):
    raise SystemExit(f"Unity test results path does not exist: {results_path}")

candidates = []
if os.path.isdir(results_path):
    for root_dir, _, filenames in os.walk(results_path):
        for filename in filenames:
            if filename.lower().endswith(".xml"):
                candidates.append(os.path.join(root_dir, filename))
else:
    candidates.append(results_path)

matches = []
for candidate in sorted(candidates):
    parsed = parse_candidate(candidate)
    if parsed is not None:
        matches.append(parsed)

if not matches:
    raise SystemExit(f"No Unity NUnit <test-run> XML file found under: {results_path}")

if len(matches) > 1:
    joined = ", ".join(matches)
    raise SystemExit(f"Expected exactly one Unity NUnit results XML, found {len(matches)}: {joined}")
PY
