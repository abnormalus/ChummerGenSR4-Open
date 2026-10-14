#!/usr/bin/env python3
"""Preserve every existing Nightly download before the publisher replaces assets."""
import hashlib
import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile


def gh(*args):
    return subprocess.check_output(["gh", *args])


def api(repo, path):
    return json.loads(gh("api", f"repos/{repo}/{path}"))


def digest(path):
    value = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            value.update(chunk)
    return value.hexdigest()


def archive(repo, incoming):
    releases = [release for page in json.loads(gh("api", f"repos/{repo}/releases?per_page=100", "--paginate", "--slurp")) for release in page]
    nightly = next((r for r in releases if r["tag_name"] == "Nightly"), None)
    if not nightly or not nightly["assets"]:
        return
    assets = nightly["assets"]
    names = [a["name"] for a in assets]
    if len(set(names)) != len(names) or any(Path(n).name != n or n in (".", "..") for n in names):
        raise ValueError("Unsafe or duplicate release asset name")
    identity = hashlib.sha256(json.dumps([(a["id"], a["size"], a["updated_at"]) for a in sorted(assets, key=lambda a: a["name"])], sort_keys=True).encode()).hexdigest()[:12]
    stamp = max(a["updated_at"] for a in assets).replace("-", "").replace(":", "").replace("T", "-").replace("Z", "")
    tag = f"archive-{stamp}-{identity}"
    ref = api(repo, "git/ref/tags/Nightly")["object"]
    while ref["type"] == "tag":
        ref = api(repo, "git/tags/" + ref["sha"])["object"]
    with tempfile.TemporaryDirectory(prefix="chummer-archive-") as temporary:
        old = Path(temporary) / "old"
        old.mkdir()
        gh("release", "download", "Nightly", "--repo", repo, "--dir", str(old))
        for asset in assets:
            if not (old / asset["name"]).is_file() or (old / asset["name"]).stat().st_size != asset["size"]:
                raise ValueError("Incomplete archive download")
        if set(names) == {p.name for p in incoming.iterdir() if p.is_file()} and all(digest(old / n) == digest(incoming / n) for n in names):
            return
        existing = next((r for r in releases if r["tag_name"] == tag), None)
        notes = Path(temporary) / "notes.md"
        notes.write_text("Archived Nightly downloads, preserved before replacement.\n\nOriginal build source: `" + ref["sha"] + "`. The archive tag identifies the publishing revision; use BUILD.txt and the original release notes for the archived application source.\n\n" + (nightly.get("body") or ""))
        if existing is None:
            gh("release", "create", tag, "--repo", repo, "--target", os.environ.get("SOURCE_SHA") or api(repo, "git/ref/heads/master")["object"]["sha"], "--draft", "--prerelease", "--title", "Archive — " + stamp, "--notes-file", str(notes))
            uploaded = set()
        else:
            uploaded = {a["name"] for a in existing["assets"]}
        for name in names:
            if name not in uploaded:
                gh("release", "upload", tag, str(old / name), "--repo", repo)
        verified = Path(temporary) / "verified"
        verified.mkdir()
        gh("release", "download", tag, "--repo", repo, "--dir", str(verified))
        if any(not (verified / n).is_file() or digest(old / n) != digest(verified / n) for n in names):
            raise ValueError("Archive verification failed; Nightly is unchanged")
        gh("release", "edit", tag, "--repo", repo, "--draft=false", "--prerelease")
        print("Preserved previous downloads in " + tag)


if __name__ == "__main__":
    archive(os.environ["GH_REPO"], Path(sys.argv[1]))
