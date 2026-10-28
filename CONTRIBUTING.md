# Contributing to the shared desktop application

## Build the supported application

Use a serviced .NET 10 SDK. `global.json` selects the latest installed stable .NET 10 feature band from its minimum; prerelease SDKs are excluded. The shipping Mac and Windows application is `Chummer.Mac.sln`. Do not build the historical .NET Framework `Chummer` project as the current Windows application.

From a clean checkout at the repository root:

```text
dotnet --version
dotnet restore Chummer.Mac.sln --locked-mode -p:UsedAvaloniaProducts=
dotnet build Chummer.Mac.sln -c Release --no-restore --disable-build-servers -p:UsedAvaloniaProducts=
dotnet Chummer.Mac/bin/Release/net10.0/ChummerMac.dll --smoke-test artifacts/checks Chummer.Mac/assets/saves
```

Restore needs access to the configured NuGet feed and a writable package cache. Do not regenerate dependency locks merely to bypass a failed locked restore. Review deliberate dependency changes and repeat the locked restore and platform checks. Application warnings fail the build; documented vendor warning debt has a separate policy.

Long commands should use the bounded runner and separate retained stage logs described in [MAINTENANCE](docs/MAINTENANCE.md). A timeout is a failed verification attempt. Preserve its log and investigate before retrying. A local restricted-process build stalled during continuation verification; a build with normal process access, `-m:1 -p:UseSharedCompilation=false` succeeded. That observation is an environment diagnostic, not a required application workaround or proof of a root cause.

The commands above were verified from an independent clean checkout at `d4bb8bf` with SDK 10.0.401 on Mac: locked restore, the exact Release build command and the full headless suite passed, including all 399 character file/memory round trips and both Foundry export targets. Headless execution completed in 187 seconds. The build retained ten existing vendor documentation warnings and had zero errors. This validates these commands on that Mac/source tree; Windows native acceptance, packaging and release/recovery procedures are separate verification. Keep verification tied to source revisions and retain its limits.

## Find the right boundary

- `Chummer.Mac/code` contains the shared application, rules, compatibility adapters and regression drivers. Existing creation/career handlers still own many rules and accounting operations; the folder is not a fully separated rules engine.
- `Chummer.Mac/assets` contains the matching catalogs, translations, sheets, settings and sample characters. Preserve existing character fields, encodings, source/PDF associations and settings when changing data or serialization.
- `vendor/Majorsilence.Forms*` contains the compatibility toolkit. Consult `vendor/LOCAL_PATCHES.md` before changing it; add a behavior regression or record platform verification for a patch.
- `scripts` and `.github/workflows` own source safeguards, dependency auditing, verification and packaging. The desktop workflow validates both Mac and Windows. Passing local headless checks does not establish native interaction or visual correctness.

For a bounded feature example, follow the [architecture map](docs/ARCHITECTURE.md) from the ware picker through the actual creation/career caller and character serialization. Acceptance in a picker is not itself persistence. [CYBERWARE_PICKER](docs/CYBERWARE_PICKER.md) records the behavior and independent references. Broader architecture and exception review remain follow-up work.

## Change and verify a section

Follow [SECTION_MODERNIZATION](docs/SECTION_MODERNIZATION.md): record behavior and callers, preserve independent expected results, consolidate shared behavior with explicit differences, verify failure/cancellation and real platform transitions, then retire replaced paths. Explain ownership and non-obvious rules in the same change. Historical development checkpoints are evidence for their recorded source, not current acceptance status.

The headless command above includes character file/memory round trips and both Foundry export targets. Use the real native drivers and visual gates in `.github/workflows/desktop-build.yml` for affected interactions. The workflow also runs runner, modernization, archival and build-identity checks. Do not lower report/count gates or substitute a small passing subset for full required acceptance.

## Diagnose and publish

[MAINTENANCE](docs/MAINTENANCE.md) describes privacy-preserving logs and shared boundaries. [MODERNIZATION](docs/MODERNIZATION.md) records dependency/API policy; [MAINTENANCE_RESPONSIBILITIES](docs/MAINTENANCE_RESPONSIBILITIES.md) identifies automation entry points and reviewed exceptions with removal conditions. [COMPATIBILITY_CONTROLS](docs/COMPATIBILITY_CONTROLS.md) inventories toolkit capabilities. Keep exception rationale and removal conditions explicit.

[Publishing and package recovery](docs/RELEASING.md) describes the existing scripts, archive identity/checksum selection and interrupted-publication limits. Never infer release approval from green CI. Preserve character data, prior downloads and Stable; merge/release/promotion requires the applicable authorization and exact-source platform/package/download verification. Release/recovery instruction validation and CI/dependency ownership review remain explicit completion work.
