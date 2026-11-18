#!/usr/bin/env python3
"""Select desktop validations using recent, real master-branch test results."""
import datetime as dt
import json
import os
import re
import subprocess
import urllib.parse
import urllib.request

PLATFORMS = ("macos-latest", "windows-latest")
REQUIRED_PASSES = 5
MAX_AGE = dt.timedelta(days=7)
TRUSTED_EVENTS = {"push", "schedule", "workflow_dispatch"}
INDEPENDENT_PATHS = {"README.md", "README-MAC.md", "CONTRIBUTING.md",
                     ".github/workflows/ios-dossier.yml", ".github/workflows/pwa-pages.yml"}
INDEPENDENT_DIRECTORIES = ("Chummer.PWA/", "Chummer.iOS/", "docs/")
SHA = re.compile(r"[0-9a-f]{40}")


def desktop_inputs_changed(paths):
    return any(path not in INDEPENDENT_PATHS and not path.startswith(INDEPENDENT_DIRECTORIES)
               for path in paths)


def changed_since(commit):
    # Reuse only from an ancestor whose desktop inputs are identical. Treat both
    # the old and new paths of a rename as changes.
    if subprocess.run(["git", "merge-base", "--is-ancestor", commit, "HEAD"],
                      stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL).returncode:
        return True
    paths = subprocess.check_output(
        ["git", "diff", "--no-renames", "--name-only", "-z", commit, "HEAD"]
    ).decode("utf-8", errors="surrogateescape").rstrip("\0").split("\0")
    return desktop_inputs_changed(path for path in paths if path)


def plan(runs, load_jobs, changed, now):
    history = {platform: [] for platform in PLATFORMS}
    for run in runs:
        if run["event"] not in TRUSTED_EVENTS or run["head_branch"] != "master":
            continue
        commit = run["head_sha"]
        if not SHA.fullmatch(commit):
            raise ValueError("Invalid source revision in run history.")
        jobs = {job["name"]: job for job in load_jobs(run["id"])}
        for platform, results in history.items():
            if len(results) == REQUIRED_PASSES:
                continue
            job = jobs.get(f"package-acceptance ({platform})")
            # A skipped lane contributed no test result.
            if job is not None and job["conclusion"] != "skipped":
                results.append((commit, job))
        if all(len(results) == REQUIRED_PASSES for results in history.values()):
            break

    decisions = {}
    for platform, results in history.items():
        if len(results) < REQUIRED_PASSES or any(job["conclusion"] != "success" for _, job in results):
            decisions[platform] = "Fewer than five consecutive passing desktop validations."
            continue
        commit, latest = results[0]
        completed = dt.datetime.fromisoformat(latest["completed_at"].replace("Z", "+00:00"))
        if not dt.timedelta() <= now - completed < MAX_AGE:
            decisions[platform] = "Last passing desktop validation is at least seven days old."
        elif changed(commit):
            decisions[platform] = "Desktop inputs differ from the last passing validation."
        else:
            decisions[platform] = None
    return decisions


def github_get(path, query):
    repo = os.environ["GITHUB_REPOSITORY"]
    base = os.environ["GITHUB_API_URL"].rstrip("/")
    token = os.environ["GH_TOKEN"]
    request = urllib.request.Request(
        f"{base}/repos/{repo}/actions/{path}?{urllib.parse.urlencode(query)}",
        headers={"Authorization": f"Bearer {token}", "Accept": "application/vnd.github+json",
                 "X-GitHub-Api-Version": "2022-11-28"},
    )
    with urllib.request.urlopen(request, timeout=5) as response:
        return json.load(response)


def main():
    event = os.environ["GITHUB_EVENT_NAME"]
    if event in {"schedule", "workflow_dispatch"}:
        decisions = dict.fromkeys(PLATFORMS, "Scheduled or manually requested full validation.")
    else:
        try:
            runs = github_get("workflows/desktop-build.yml/runs",
                              {"branch": "master", "status": "completed", "per_page": 40})["workflow_runs"]
            decisions = plan(
                runs, lambda run: github_get(f"runs/{int(run)}/jobs", {"per_page": 100})["jobs"],
                changed_since, dt.datetime.now(dt.timezone.utc),
            )
        except (OSError, ValueError, KeyError, TypeError, subprocess.SubprocessError) as error:
            print(f"Could not verify desktop validation history ({type(error).__name__}); running all checks.")
            decisions = dict.fromkeys(PLATFORMS, "Validation history could not be verified.")

    selected = [platform for platform, reason in decisions.items() if reason is not None]
    notes = ["### Desktop validation plan", ""]
    for platform, reason in decisions.items():
        notes.append(f"- **{platform}** — {reason or 'Reused five recent passes. Desktop inputs are unchanged.'}")
    print("\n".join(notes))
    with open(os.environ["GITHUB_OUTPUT"], "a", encoding="utf-8") as output:
        print(f"platforms={json.dumps(selected, separators=(',', ':'))}", file=output)
        print(f"run_desktop={'true' if selected else 'false'}", file=output)
    if summary := os.environ.get("GITHUB_STEP_SUMMARY"):
        with open(summary, "a", encoding="utf-8") as output:
            print("\n".join(notes), file=output)


if __name__ == "__main__":
    main()
