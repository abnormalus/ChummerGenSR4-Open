#!/usr/bin/env bash
set -euo pipefail
chummer_assets="${1:?Usage: publish-nightly.sh ASSET_DIRECTORY}"
chummer_repo="${GH_REPO:?Set GH_REPO to the target fork}"
chummer_sha="${SOURCE_SHA:-$(git rev-parse HEAD)}"
# Avoid replacing a newer master build with an obsolete queued run.
chummer_head="$(gh api "repos/$chummer_repo/git/ref/heads/master" --jq .object.sha)"
if [[ "$chummer_sha" != "$chummer_head" ]]; then
    echo "Master advanced; leaving Nightly for the newer build."
    exit 0
fi
(cd "$chummer_assets" && sha256sum --check SHA256SUMS.txt)
chummer_notes="$(mktemp)"
trap 'rm -f "$chummer_notes"' EXIT
chummer_label="$(head -n 1 "$chummer_assets/BUILD.txt")"
cat > "$chummer_notes" <<EOF_NOTES
**$chummer_label**

Development builds from commit [$chummer_sha](https://github.com/$chummer_repo/commit/$chummer_sha).

- **Mac:** download ChummerSR4-macOS-arm64.zip. Apple Silicon, macOS 15+. Locally signed preview; Apple notarization is not configured.
- **Windows:** download ChummerSR4-Windows-x64.msi. Per-user installation and Start menu shortcut; .NET is bundled. The PDF reader requires Microsoft Edge WebView2.
- Both packages use the same source, pass character compatibility checks, and include the Windows native PDF-window manifest fix.
- BUILD.txt records the source commit; SHA256SUMS.txt verifies every download.

Nightly updates automatically when master changes and checks pass. To rebuild manually, open **Actions → Nightly desktop builds → Run workflow** on master. See [publishing instructions](https://github.com/$chummer_repo/blob/master/docs/RELEASING.md).
EOF_NOTES
if ! gh release view Nightly --repo "$chummer_repo" >/dev/null 2>&1; then
    gh release create Nightly --repo "$chummer_repo" --target "$chummer_sha" --draft --prerelease --title Nightly --notes-file "$chummer_notes"
fi
python3 "$(dirname "$0")/archive-nightly.py" "$chummer_assets"
gh release upload Nightly "$chummer_assets"/* --repo "$chummer_repo" --clobber
# Only the moving nightly tag is updated; versioned release tags stay immutable.
gh api --method PATCH "repos/$chummer_repo/git/refs/tags/Nightly" -f sha="$chummer_sha" -F force=true >/dev/null
gh release edit Nightly --repo "$chummer_repo" --draft=false --prerelease --title Nightly --notes-file "$chummer_notes"

# Remove superseded legacy download names only after their archive is verified and new uploads succeeded.
while IFS= read -r chummer_old_asset; do
    if [[ ! -f "$chummer_assets/$chummer_old_asset" ]]; then
        gh release delete-asset Nightly "$chummer_old_asset" --repo "$chummer_repo" --yes
    fi
done < <(gh api "repos/$chummer_repo/releases/tags/Nightly" --jq '.assets[].name')
