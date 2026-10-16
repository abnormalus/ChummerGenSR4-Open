#!/usr/bin/env bash
set -euo pipefail
chummer_root="$(cd "$(dirname "$0")/.." && pwd)"
chummer_dotnet="${CHUMMER_DOTNET:-dotnet}"
chummer_output="${1:-$chummer_root/artifacts/mac}"
mkdir -p "$chummer_output"
chummer_output="$(cd "$chummer_output" && pwd)"
chummer_app="$chummer_output/ChummerSR4.app"
if [[ -e "$chummer_app" ]]; then
    echo "Output already exists: $chummer_app. Choose an empty output folder." >&2
    exit 1
fi
python3 "$chummer_root/scripts/build_identity.py" --output "$chummer_output/build-identity.json"
chummer_version="$(python3 -c 'import json,sys;print(json.load(open(sys.argv[1]))["informationalVersion"])' "$chummer_output/build-identity.json")"
chummer_file_version="$(python3 -c 'import json,sys;print(json.load(open(sys.argv[1]))["fileVersion"])' "$chummer_output/build-identity.json")"
"$chummer_dotnet" publish "$chummer_root/Chummer.Mac/Chummer.Mac.csproj" \
    -c Release -r osx-arm64 --self-contained true \
    -p:PublishSingleFile=false -p:PublishTrimmed=false -p:UsedAvaloniaProducts= \
    "-p:ChummerReleaseVersion=$chummer_version" "-p:ChummerFileVersion=$chummer_file_version" \
    -o "$chummer_output/publish"
cp "$chummer_output/build-identity.json" "$chummer_output/publish/build-identity.json"
python3 "$chummer_root/scripts/write-build-revision.py" "$chummer_output/publish/source-revision.txt"
mkdir -p "$chummer_app/Contents/MacOS" "$chummer_app/Contents/Resources"
cp -R "$chummer_output/publish/." "$chummer_app/Contents/MacOS/"
cp "$chummer_root/packaging/mac/Info.plist" "$chummer_app/Contents/Info.plist"
python3 - "$chummer_app/Contents/Info.plist" "$chummer_output/build-identity.json" <<'PYMETA'
import json,plistlib,pathlib,sys
path=pathlib.Path(sys.argv[1]);identity=json.load(open(sys.argv[2]))
data=plistlib.loads(path.read_bytes());data['CFBundleShortVersionString']=identity['version'];data['CFBundleVersion']=str(identity['buildNumber'])
path.write_bytes(plistlib.dumps(data))
PYMETA
cp "$chummer_root/packaging/mac/Chummer.icns" "$chummer_app/Contents/Resources/Chummer.icns"
chmod +x "$chummer_app/Contents/MacOS/ChummerMac"
"$chummer_dotnet" "$chummer_output/publish/ChummerMac.dll" --version-json > "$chummer_output/reported-identity.json"
python3 - "$chummer_output/build-identity.json" "$chummer_output/reported-identity.json" "$chummer_app/Contents/Info.plist" <<'PYVERIFY'
import json,plistlib,sys
expected=json.load(open(sys.argv[1]));actual=json.load(open(sys.argv[2]));plist=plistlib.load(open(sys.argv[3],'rb'))
assert actual['version']==expected['informationalVersion'] and actual['sourceCommit']==expected['sourceCommit']
assert actual['fileVersion']==expected['fileVersion'] and actual['channel']==expected['channel']
assert plist['CFBundleShortVersionString']==expected['version'] and plist['CFBundleVersion']==str(expected['buildNumber'])
PYVERIFY

if [[ "$(uname -s)" == "Darwin" ]]; then
    /usr/bin/codesign --force --deep --sign - \
        --entitlements "$chummer_root/packaging/mac/entitlements.plist" "$chummer_app"
    /usr/bin/codesign --verify --deep --strict "$chummer_app"
fi
echo "Built $chummer_app"
