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
