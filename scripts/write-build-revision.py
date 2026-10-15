#!/usr/bin/env python3
"""Only a clean checkout can identify itself as an installed source revision."""
import pathlib
import subprocess
import sys

ROOT = pathlib.Path(__file__).resolve().parents[1]


def revision(root=ROOT):
    commit = subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=root, text=True).strip()
    if subprocess.check_output(["git", "status", "--porcelain"], cwd=root, text=True).strip():
        return "development"
    return commit


if __name__ == "__main__":
    pathlib.Path(sys.argv[1]).write_text(revision() + "\n", encoding="utf-8")
