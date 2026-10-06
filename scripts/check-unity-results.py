#!/usr/bin/env python3
"""Reject incomplete or unsuccessful NUnit 3 results from Unity's Test Framework."""

import sys
import xml.etree.ElementTree as ET
from collections import Counter
from pathlib import Path


def check_results(path):
    root = ET.parse(path).getroot()
    if root.tag.rsplit("}", 1)[-1] != "test-run":
        raise ValueError("expected NUnit 3 test-run XML")
    cases = [node for node in root.iter() if node.tag.rsplit("}", 1)[-1] == "test-case"]
    outcomes = Counter(node.get("result") for node in cases)
    fields = {"passed": "Passed", "failed": "Failed", "skipped": "Skipped", "inconclusive": "Inconclusive"}
    if set(outcomes) - set(fields.values()):
        raise ValueError("unknown or missing test-case outcome")
    counts = {name: int(root.attrib[name]) for name in ("total", *fields)}
    if any(value < 0 for value in counts.values()):
        raise ValueError("negative result count")
    if counts["total"] != len(cases) or sum(counts[name] for name in fields) != len(cases):
        raise ValueError("reported total does not match the test cases")
    if any(counts[name] != outcomes[result] for name, result in fields.items()):
        raise ValueError("reported outcomes do not match the test cases")
    print("EditMode results: " + ", ".join(f"{name}={count}" for name, count in counts.items()))
    print(f"Report: {path}")
    for case in cases:
        if case.get("result") != "Passed":
            print(f"{case.get('result')}: {case.get('fullname') or case.get('name') or '(unnamed test)'}")
    if not counts["total"] or not counts["passed"]:
        raise ValueError("no tests completed successfully")
    if counts["failed"] or counts["inconclusive"] or root.get("result") != "Passed":
        raise ValueError("Unity test run did not pass")
    if counts["skipped"]:
        raise ValueError("test run is incomplete: some tests were skipped")


if __name__ == "__main__":
    try:
        if len(sys.argv) != 2:
            raise ValueError("usage: check-unity-results.py <test-results.xml>")
        check_results(Path(sys.argv[1]))
    except (OSError, ET.ParseError, KeyError, ValueError) as error:
        print(f"Unity results validation failed: {error}", file=sys.stderr)
        sys.exit(1)
