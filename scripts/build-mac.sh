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
"$chummer_dotnet" publish "$chummer_root/Chummer.Mac/Chummer.Mac.csproj" \
    -c Release -r osx-arm64 --self-contained true \
    -p:PublishSingleFile=false -p:PublishTrimmed=false -p:UsedAvaloniaProducts= \
    -o "$chummer_output/publish"
mkdir -p "$chummer_app/Contents/MacOS" "$chummer_app/Contents/Resources"
cp -R "$chummer_output/publish/." "$chummer_app/Contents/MacOS/"
cp "$chummer_root/packaging/mac/Info.plist" "$chummer_app/Contents/Info.plist"
chmod +x "$chummer_app/Contents/MacOS/ChummerMac"
if [[ "$(uname -s)" == "Darwin" ]]; then
    /usr/bin/codesign --force --deep --sign - \
        --entitlements "$chummer_root/packaging/mac/entitlements.plist" "$chummer_app"
    /usr/bin/codesign --verify --deep --strict "$chummer_app"
fi
echo "Built $chummer_app"
