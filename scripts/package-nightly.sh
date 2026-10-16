#!/usr/bin/env bash
set -euo pipefail
chummer_root="$(cd "$(dirname "$0")/.." && pwd)"
chummer_output="${1:?Usage: package-nightly.sh EMPTY_OUTPUT_DIRECTORY}"
chummer_dotnet="${CHUMMER_DOTNET:-dotnet}"
mkdir -p "$chummer_output"
chummer_output="$(cd "$chummer_output" && pwd)"
chummer_assets="$chummer_output/assets"
mkdir -p "$chummer_assets"
cd "$chummer_root"
python3 scripts/build_identity.py --output "$chummer_output/build-identity.json"
python3 - "$chummer_output/build-identity.json" <<'PYIDENTITY'
import json,sys
identity=json.load(open(sys.argv[1]))
if identity['dirty'] or identity['channel'] != 'Nightly':
    raise SystemExit('Nightly packaging requires a clean checkout; build-mac.sh supports development builds.')
PYIDENTITY
chummer_version="$(python3 -c 'import json,sys;print(json.load(open(sys.argv[1]))["informationalVersion"])' "$chummer_output/build-identity.json")"
chummer_file_version="$(python3 -c 'import json,sys;print(json.load(open(sys.argv[1]))["fileVersion"])' "$chummer_output/build-identity.json")"
bash scripts/build-mac.sh "$chummer_output/mac"
"$chummer_dotnet" "$chummer_output/mac/publish/ChummerMac.dll" --smoke-test "$chummer_output/checks" Chummer.Mac/assets/saves
"$chummer_dotnet" publish Chummer.Mac/Chummer.Mac.csproj -c Release -r win-x64 --self-contained true \
    -p:UsedAvaloniaProducts= "-p:ChummerReleaseVersion=$chummer_version" "-p:ChummerFileVersion=$chummer_file_version" -o "$chummer_output/windows"
cp "$chummer_output/build-identity.json" "$chummer_output/windows/build-identity.json"
python3 scripts/write-build-revision.py "$chummer_output/windows/source-revision.txt"
python3 packaging/windows/build-msi.py "$chummer_output/windows" "$chummer_assets/ChummerSR4-Windows-x64.msi"
python3 packaging/windows/verify-msi.py "$chummer_output/windows" "$chummer_assets/ChummerSR4-Windows-x64.msi"
# The WiX source contains builder paths; it is useful locally but is not a release asset.
mv "$chummer_assets/ChummerSR4-Windows-x64.wxs" "$chummer_output/installer.wxs"
mkdir -p "$chummer_output/mac-archive/ChummerSR4"
cp -R "$chummer_output/mac/ChummerSR4.app" "$chummer_output/mac-archive/ChummerSR4/"
cp packaging/mac/Nightly-README.txt "$chummer_output/mac-archive/ChummerSR4/README.txt"
/usr/bin/ditto -c -k --sequesterRsrc --keepParent "$chummer_output/mac-archive/ChummerSR4" "$chummer_assets/ChummerSR4-macOS-arm64.zip"
python3 - "$chummer_assets" "$chummer_output/build-identity.json" <<'PY'
import hashlib, pathlib, subprocess, sys, json
root = pathlib.Path(sys.argv[1])
commit = subprocess.check_output(['git', 'rev-parse', 'HEAD'], text=True).strip()
identity=json.load(open(sys.argv[2]));assert identity['sourceCommit']==commit
(root / 'BUILD.txt').write_text('Chummer SR4 ' + identity['informationalVersion'] + '\nChannel: ' + identity['channel'] + '\nSource commit: ' + commit + '\nMac: Apple Silicon, macOS 15+\nWindows: x64, Windows 10/11; PDF reader requires WebView2\n')
files = sorted(file for file in root.iterdir() if file.is_file())
with (root / 'SHA256SUMS.txt').open('w') as checksums:
    for file in files:
        checksums.write(hashlib.sha256(file.read_bytes()).hexdigest() + '  ' + file.name + '\n')
PY
printf 'Nightly assets are ready in %s\n' "$chummer_assets"
