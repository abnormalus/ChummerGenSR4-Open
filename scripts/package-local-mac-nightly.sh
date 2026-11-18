#!/usr/bin/env bash
set -euo pipefail
root="$(cd "$(dirname "$0")/.." && pwd -P)"
output="${1:?Usage: package-local-mac-nightly.sh NEW_OUTPUT_DIRECTORY}"
if [[ "$(uname -s)-$(uname -m)" != Darwin-arm64 ]]; then
    echo "Run local Mac packaging on an Apple Silicon Mac." >&2
    exit 1
fi
mkdir "$output"
output="$(cd "$output" && pwd -P)"
cd "$root"
python3 scripts/build_identity.py --output "$output/identity.json"
python3 - "$output/identity.json" <<'PY'
import json, sys
identity = json.load(open(sys.argv[1]))
if identity["dirty"] or identity["channel"] != "Nightly":
    raise SystemExit("Use a clean checkout of the exact master commit you want to publish.")
PY
bash scripts/build-mac.sh "$output/mac"
mkdir -p "$output/stage/ChummerSR4" "$output/results"
cp -R "$output/mac/ChummerSR4.app" "$output/stage/ChummerSR4/"
cp packaging/mac/Nightly-README.txt "$output/stage/ChummerSR4/README.txt"
zip="$output/ChummerSR4-macOS-arm64.zip"
/usr/bin/ditto -c -k --sequesterRsrc --keepParent "$output/stage/ChummerSR4" "$zip"
/usr/bin/ditto -x -k "$zip" "$output/extracted"
app="$output/extracted/ChummerSR4/ChummerSR4.app"
/usr/bin/codesign --verify --deep --strict "$app"
exe="$app/Contents/MacOS/ChummerMac"
results="$output/results"
python3 scripts/run-bounded.py --label headless --log-file "$results/headless.log" --timeout 600 -- \
    "$exe" --smoke-test "$results/characters" "$root/Chummer.Mac/assets/saves"
for gate in bulk sensor-transactions focus-command pdf-reader; do
    /usr/bin/caffeinate -diu python3 scripts/run-bounded.py --label "$gate" --log-file "$results/$gate.log" --timeout 300 -- \
        "$exe" "--check-$gate" "$results/characters/creation.chum" "$results/$gate.json"
done
python3 scripts/verify-local-mac-nightly.py "$zip" "$output/mac-acceptance.json" "$output/identity.json" --local-results "$results"
