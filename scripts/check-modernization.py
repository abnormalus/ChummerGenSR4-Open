#!/usr/bin/env python3
"""Small, explicit guardrails, not a claim that the legacy code is fully modernized."""
import json
import pathlib
import re
import sys
import xml.etree.ElementTree as ET

ROOT = pathlib.Path(__file__).resolve().parents[1]
EXCEPTIONS = json.loads((ROOT / "scripts/modernization-exceptions.json").read_text())
PATTERNS = {
    "obsolete networking": r"\b(?:new\s+(?:System\.Net\.)?(?:WebClient|HttpWebRequest)|WebRequest\s*\.\s*Create)\b",
    "obsolete random provider": r"\bnew\s+(?:System\.Security\.Cryptography\.)?RNGCryptoServiceProvider\b",
    "unsafe object deserialization": r"\bnew\s+(?:System\.Runtime\.Serialization\.Formatters\.Binary\.)?BinaryFormatter\b",
    "unsupported delegate async invocation": r"\.BeginInvoke\s*\(\s*null\s*,\s*null\s*\)",
    "unsupported print dialog": r"\bnew\s+(?:Majorsilence\.Forms\.)?PrintDialog\s*\(",
    "manual message pumping": r"\bApplication\s*\.\s*DoEvents\s*\(",
}
# Retained to recover image byte arrays from legacy resources without deserializing objects.
EXPERIMENTAL_READER = "vendor/Majorsilence.Forms/Drawing/NrbfResourceReader.cs"


def code_only(text):
    # Skip comments and literals; compiler warnings remain the primary obsolete-API check.
    return re.sub(r'//[^\n]*|/\*[\s\S]*?\*/|@"(?:""|[^"])*"|"(?:\\.|[^"\\])*"|\'(?:\\.|[^\'\\])*\'',
                  lambda match: "\n" * match.group().count("\n") or " ", text)


def source_errors(path, text):
    errors = []
    if path.startswith("Chummer.Mac/code/"):
        code = code_only(text)
        for name, pattern in PATTERNS.items():
            matches = len(re.findall(pattern, code))
            allowed = EXCEPTIONS.get(name, {}).get(path, {}).get("count", 0)
            if matches > allowed:
                errors.append(f"{path}: {name} ({matches} occurrences, {allowed} documented exceptions)")
    for match in re.finditer(r"^\s*#pragma warning disable([^\n]*)", text, re.M):
        diagnostics = re.sub(r"//.*", "", match[1]).strip()
        if not diagnostics:
            errors.append(f"{path}: blanket warning suppression")
        for diagnostic in re.findall(r"\b(?:CS061[28]|SYSLIB\d+)\b", diagnostics):
            if not (path == EXPERIMENTAL_READER and diagnostic == "SYSLIB5005"):
                errors.append(f"{path}: unexplained suppression {diagnostic}")
    return errors


def check(root=ROOT):
    errors = []
    for folder in ("Chummer.Mac/code", "vendor"):
        for path in (root / folder).rglob("*.cs"):
            if {"obj", "bin"}.intersection(path.parts):
                continue
            errors.extend(source_errors(path.relative_to(root).as_posix(), path.read_text(encoding="utf-8-sig")))
    for path in [root / "Directory.Build.props", *root.joinpath("Chummer.Mac").glob("*.csproj"), *root.joinpath("vendor").rglob("*.csproj")]:
        project = ET.parse(path).getroot()
        for node in project.iter():
            if node.tag in ("NoWarn", "WarningsNotAsErrors") and re.search(r"CS061[28]|SYSLIB", node.text or ""):
                errors.append(f"{path}: obsolete API diagnostics must not be suppressed globally")
            if node.tag in ("IsTrimmable", "IsAotCompatible", "PublishTrimmed", "PublishAot") and node.text == "true":
                errors.append(f"{path}: desktop trimming/AOT requires a separately validated migration")
            if node.tag == "EnableUnsafeBinaryFormatterSerialization" and node.text == "true":
                errors.append(f"{path}: unsafe deserialization enabled")
            if node.tag in ("PackageReference", "PackageVersion") and (node.get("Version") and re.search(r"[*\[\](,]", node.get("Version"))):
                errors.append(f"{path}: floating/ranged dependency version")
        if path.suffix == ".csproj" and not path.with_name("packages.lock.json").exists():
            errors.append(f"{path}: dependency lock file is missing")
    app = ET.parse(root / "Chummer.Mac/Chummer.Mac.csproj").getroot()
    if not any(n.text == "true" for n in app.iter("TreatWarningsAsErrors")):
        errors.append("application compiler warnings must remain errors")
    if errors:
        raise ValueError("\n".join(errors))
    print("Modernization source safeguards passed.")


if __name__ == "__main__":
    try:
        check()
    except (ValueError, ET.ParseError) as error:
        print(error, file=sys.stderr)
        sys.exit(1)
