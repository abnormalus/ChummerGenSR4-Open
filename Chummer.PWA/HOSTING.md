# GitHub Pages hosting

The PWA is static and self-contained. No ChatGPT/Sites runtime, account, APIs or
hosting configuration is required. Printing libraries, fonts and blank form are bundled.
Private character snapshots remain in user-owned private repositories, never Pages.

## Channels

- `/nightly/`: `master`, on relevant pushes and daily at 10:20 UTC.
- `/stable/`: reserved until release acceptance. Publish manually through the
  **PWA nightly and stable** workflow, choosing `stable` and a reviewed `pwa-vX.Y.Z` tag.
  A development branch cannot be selected for stable publication.
- Root: channel links and migration guidance. No root service worker.

The workflow must exist on `master` for scheduling/manual dispatch. Its build job
checks out the selected source, runs browser checks and packages only `Chummer.PWA/dist`.
The publish job preserves the other channel in `pwa-pages`, then deploys the combined
artifact with GitHub Pages' OIDC workflow. Configure repository Settings → Pages →
Source → GitHub Actions. Stable is never automatically promoted from nightly.

IndexedDB names and service-worker caches are scoped by application path, preventing
nightly from altering stable working copies or clearing stable's offline cache.
Both channels keep the same HTTPS origin, so this is isolation against accidental
mixing, not a security boundary against malicious same-origin code.

## Migration

Browser storage cannot transfer across origins. Export each working copy from the
old preview and import it at the new URL, or explicitly download its latest private
repository snapshot. Unsynced local edits need `.chum` export first. GitHub connections
use memory-only tokens and must be reconnected. Do not clear the old browser storage
until the transferred characters have been verified.

## Rollback

Revert the relevant PWA change on `master` to restore a previous Nightly, or
run stable with a prior reviewed release tag. The other channel is preserved. Uploaded
candidate artifacts retain source SHA metadata in `build-info.json`. Neither deployment
nor rollback changes locally stored character XML.
