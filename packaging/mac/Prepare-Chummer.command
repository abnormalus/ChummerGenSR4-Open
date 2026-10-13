#!/bin/bash
set -euo pipefail
chummer_directory="$(cd "$(dirname "$0")" && pwd)"
chummer_app="$chummer_directory/ChummerSR4.app"
if [[ "$(uname -s)" != "Darwin" || "$(uname -m)" != "arm64" ]]; then
    echo "This preview requires an Apple Silicon Mac."
    exit 1
fi
if [[ ! -d "$chummer_app" ]]; then
    echo "Keep this script next to ChummerSR4.app."
    exit 1
fi
echo "Applying a local development signature to the Chummer preview..."
/usr/bin/codesign --force --deep --sign - \
    --entitlements "$chummer_directory/entitlements.plist" "$chummer_app"
/usr/bin/codesign --verify --deep --strict "$chummer_app"
exec /usr/bin/open "$chummer_app"
