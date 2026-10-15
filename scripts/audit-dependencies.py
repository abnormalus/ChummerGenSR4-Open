#!/usr/bin/env python3
"""Query current NuGet metadata and fail if either findings or unavailable data are returned."""
import json
import os
import pathlib
import subprocess
import sys

ROOT = pathlib.Path(__file__).resolve().parents[1]
PROJECTS = ("Chummer.Mac/Chummer.Mac.csproj", "vendor/Majorsilence.Forms/Majorsilence.Forms.csproj",
            "vendor/Majorsilence.Forms.Avalonia/Majorsilence.Forms.Avalonia.csproj")


def validate(report, project):
    if report.get("version") != 1 or not report.get("sources"):
        raise ValueError("Missing dependency metadata or unsupported report format")
    if any(log.get("level", "").lower() in ("error", "warning") for log in report.get("logs", [])):
        raise ValueError("NuGet reported incomplete/unavailable dependency metadata")
    projects = report.get("projects", [])
    if not any(p.get("path", "").replace("\\", "/").endswith(project) for p in projects):
        raise ValueError("Expected project is missing from the dependency report")
    findings = []
    for entry in projects:
        for framework in entry.get("frameworks", []):
            for kind in ("topLevelPackages", "transitivePackages"):
                for package in framework.get(kind, []):
                    findings.append(f"{package['id']} {package.get('resolvedVersion', '')}")
    if findings:
        raise ValueError("Packages require review: " + ", ".join(findings))


def run(output):
    output.mkdir(parents=True, exist_ok=True)
    for project in PROJECTS:
        for category in ("deprecated", "vulnerable"):
            result = subprocess.run([os.environ.get("CHUMMER_DOTNET", "dotnet"), "list", project, "package",
                "--" + category, "--include-transitive", "--format", "json", "--no-restore"],
                cwd=ROOT, capture_output=True, text=True)
            target = output / (pathlib.Path(project).parent.name + "-" + category + ".json")
            target.write_text(result.stdout, encoding="utf-8")
            if result.stderr:
                target.with_suffix(".stderr.txt").write_text(result.stderr, encoding="utf-8")
            if result.returncode:
                raise ValueError(f"Dependency query failed for {project}; inspect {target}")
            # Do not mistake a command-line warning or unavailable feed for a clean report.
            if result.stderr.strip():
                raise ValueError(f"Dependency query reported diagnostics; inspect {target}")
            validate(json.loads(result.stdout), project)
    print("No deprecated packages or known vulnerabilities reported for direct/transitive dependencies.")


if __name__ == "__main__":
    try:
        run(pathlib.Path(sys.argv[1] if len(sys.argv) > 1 else "artifacts/dependency-audit"))
    except (ValueError, OSError) as error:
        print(error, file=sys.stderr)
        sys.exit(1)
