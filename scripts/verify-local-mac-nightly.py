#!/usr/bin/env python3
"""Bind a locally accepted macOS ZIP and its checks to a clean Nightly source."""
import argparse
import hashlib
import json
import pathlib
import re
import zipfile

GATES = {"bulk": 1069, "sensor-transactions": 17, "focus-command": 66, "pdf-reader": 10}
APP = "ChummerSR4/ChummerSR4.app/Contents/MacOS/"


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("zip", type=pathlib.Path)
    parser.add_argument("evidence", type=pathlib.Path)
    parser.add_argument("identity", type=pathlib.Path)
    mode = parser.add_mutually_exclusive_group(required=True)
    mode.add_argument("--local-results", type=pathlib.Path)
    mode.add_argument("--expected-sha256")
    args = parser.parse_args()

    identity = json.loads(args.identity.read_text())
    if identity["dirty"] or identity["channel"] != "Nightly" or not re.fullmatch("[0-9a-f]{40}", identity["sourceCommit"]):
        raise SystemExit("Expected a clean Nightly identity.")
    digest = hashlib.sha256()
    with args.zip.open("rb") as archive_file:
        for block in iter(lambda: archive_file.read(1024 * 1024), b""):
            digest.update(block)
    sha256 = digest.hexdigest()
    with zipfile.ZipFile(args.zip) as archive:
        embedded = json.loads(archive.read(APP + "build-identity.json"))
    if embedded != identity:
        raise SystemExit("Mac ZIP source identity does not match the release checkout.")

    expected = {"source": identity["sourceCommit"], "macSha256": sha256, "checks": {}}
    if args.local_results:
        results = args.local_results
        if "SMOKE TESTS PASSED" not in (results / "headless.log").read_text():
            raise SystemExit("Local Mac smoke suite did not pass.")
        for gate, minimum in GATES.items():
            report = json.loads((results / f"{gate}.json").read_text())
            count = len(report["checks"])
            if report["passed"] is not True or count < minimum:
                raise SystemExit(f"Local Mac {gate} checks did not pass.")
            expected["checks"][gate] = count
        args.evidence.write_text(json.dumps(expected, indent=2) + "\n")
        print(f"Verified Mac candidate SHA-256 {sha256}")
    else:
        evidence = json.loads(args.evidence.read_text())
        checks = evidence.get("checks", {})
        if sha256 != args.expected_sha256 or evidence.get("source") != expected["source"] or evidence.get("macSha256") != sha256:
            raise SystemExit("Mac candidate SHA-256 or source differs from its local evidence.")
        if any(type(checks.get(gate)) is not int or checks[gate] < limit for gate, limit in GATES.items()):
            raise SystemExit("Mac candidate evidence lacks a required native check.")
        print("Mac candidate matches its local acceptance evidence.")


if __name__ == "__main__":
    main()
