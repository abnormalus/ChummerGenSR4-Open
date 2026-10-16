# Publishing Nightly

Download current previews from [Nightly](https://github.com/abnormalus/ChummerGenSR4-Open/releases/tag/Nightly).

Updates to `master` automatically run **Nightly desktop builds**. It checks the shared application on Mac and Windows, packages the Mac Apple Silicon ZIP and Windows x64 MSI, verifies installer contents and the Windows compatibility manifest, then updates the Nightly prerelease and its download assets. Failed checks leave the previous release available. Pull requests run validation without publishing.

To rebuild manually, go to **GitHub → Actions → Nightly desktop builds → Run workflow**, select `master`, and run it. No local signing keys, upload commands, or personal tokens are required: the publish job uses GitHub's repository-scoped workflow token. If Actions are disabled in the fork, enable them once from its Actions tab.

Assets have stable names: `ChummerSR4-macOS-arm64.zip`, `ChummerSR4-Windows-x64.msi`, `BUILD.txt`, and `SHA256SUMS.txt`. The moving `Nightly` tag points to the source commit. Checksums and build metadata make it easy to identify a tester's version.

## Local fallback

On an Apple Silicon Mac with .NET 10, Python 3, and Homebrew `msitools` installed, run:

```sh
bash scripts/package-nightly.sh /absolute/path/to/new-output
```

This builds both platforms, runs character checks, packages both downloads, verifies every MSI file, and generates checksums. It uses the same scripts as GitHub Actions. With an authenticated GitHub CLI, publish those assets with:

```sh
GH_REPO=abnormalus/ChummerGenSR4-Open SOURCE_SHA="$(git rev-parse HEAD)" bash scripts/publish-nightly.sh /absolute/path/to/new-output/assets
```

Windows installation is per user, under `%LOCALAPPDATA%\Programs\ChummerSR4`, with a Start menu shortcut. .NET is included; PDF viewing requires WebView2. Same-version previews replace each other. User settings and character files are outside the installation tree.

Mac builds are ad-hoc signed development previews, not Apple-notarized releases. Developer ID signing and notarization can be added later with repository secrets. Do not label Nightly as a stable release until native usability and installer checks are complete on both platforms.

## Archived builds

Before replacing Nightly, the publisher preserves its downloads in one **Archive — YYYY-MM-DD** prerelease per America/Chicago calendar day. Downloads from each build share a UTC timestamp and identity prefix, so names cannot collide even when daylight saving repeats a local time. The release body lists local build times, source commits and download links in a table.

Every original asset is preserved byte-for-byte under its prefixed filename, including BUILD.txt and its original checksum file. ARCHIVE.json retains original names, source commits, original release notes and SHA-256 hashes. SHA256SUMS-ARCHIVE.txt validates the renamed downloads directly. Original checksum files still refer to the original filenames; use the daily checksum file for the grouped assets.

Each copied build and its uploaded metadata are downloaded again and hash-verified before publication proceeds. A failure leaves Nightly and the original archive release available. Retries reuse verified build assets and reject conflicting content. Daily archives append builds rather than overwrite them. Archive tags stay fixed; actual application source revisions are recorded separately for each build.

The **Consolidate daily download archives** workflow safely migrates the old timestamped archive releases. It runs when the archival scripts change or can be started manually from Actions. It removes each old release heading only after its daily copy and metadata are verified; original Git tags remain. It never removes Nightly. Consolidation and Nightly publication share a release-assets concurrency lock to prevent concurrent metadata edits.

The Nightly Assets section contains only the current packages and metadata. Superseded filenames are removed from Nightly only after verified archival and successful replacement uploads. GitHub does not provide nested release categories; daily releases keep the main Releases page compact while preserving individual builds.

## Local retention

Keep one verified Stable package set and one verified Nightly package set locally. Use temporary directories for intermediate builds and remove their generated bundles/publish folders after validation, provided they are not running. Do not delete logs, screenshots, character exports, source files or installed/running applications as part of build cleanup. Replace a retained channel only after verifying the replacement checksums. Older releases remain in GitHub daily archives; local per-iteration bundles are unnecessary.
