# Chummer SR4: Apple Silicon preview 1

This is a native ARM64 development preview of the supplied 0.1.501 fork, including its character creation and career workflows. It runs on modern .NET with an Avalonia-backed Forms implementation. It does not use Wine, Mono, Rosetta, or a Windows virtual machine. The original rules, XML catalogs and character serialization are retained.

## Try the application

Requirements: an Apple Silicon Mac running macOS 15 or later. The runtime is included; no .NET installation is required for the application archive.

1. Extract `ChummerSR4-AppleSilicon-Preview1.zip` to a writable folder. Keep the application and companion files together initially.
2. Run `Prepare-Chummer.command`. It applies a local, ad-hoc development signature using macOS's `codesign`, verifies it, then opens `ChummerSR4.app`. The script does not change Gatekeeper settings or remove quarantine attributes.
3. This download is not Developer ID signed or notarized. If macOS blocks it, review the application's entry in System Settings > Privacy & Security and use Open Anyway for this specific application if you choose to run the preview. Then rerun the preparation script if signing has not yet completed.
4. Use File > New Character for creation, or File > Open to load a `.chum` file. Start with a copy of an existing character while testing.

After local signing, the `.app` can be placed in Applications. Save through the normal Save/Save As controls. The Mac port writes a `.bak` beside an existing character before replacing it and writes the new file through a temporary file, avoiding truncation of the original during serialization.

Preferences, editable rule settings, custom XML data and temporary mugshots live under `~/Library/Application Support/ChummerGenSR4Mac/`. Bundled base rules remain inside the app. Put custom/override XML files in that user folder's `data` subfolder. PACKS creation writes there too. The default rule settings are copied to the writable settings folder on first launch.

## Validation completed

Validation was performed on Linux using the cross-platform offscreen UI backend. It exercises the same Chummer forms and rule code, but does not establish macOS windowing, file-picker, keyboard, webview or printer behavior.

- Release build of the full Mac project.
- English localization, embedded toolbar images and XML rule catalog loading.
- Main window and MDI character creation window rendering.
- Karma creation-method selection and Human metatype creation through the actual dialog controls.
- Creation attribute spinner updates the rule model; the actual Save toolbar button writes a character.
- Creation character identity and attributes survive saving/loading.
- Career status, Karma, Nuyen and expense ledger survive saving/loading.
- Weapons, armor, gear, cyberware, qualities and spell selection dialogs initialize and render.
- Portable career history charts render.
- Character-sheet XML transforms to HTML using the original XSLT templates and local includes.
- All 399 bundled character fixtures load, save and reload; collection counts survive and the two serialized copies compare equal. Fixtures are copied before loading to contain the legacy loader's automatic migrations.
- Self-contained `osx-arm64` publishing; the apphost and .NET runtime are ARM64 Mach-O binaries, and the graphics libraries include ARM64 slices.

The original Windows project and its source files are preserved. The Mac project is an independent migrated copy under `Chummer.Mac`.

## Still needs interactive Mac validation

The package was cross-compiled on Linux. An Apple Silicon Mac was not available in this environment. Signing, Finder launch, native dialogs, keyboard shortcuts, Retina layout, embedded HTML viewing, printing, and longer interactive workflows have not been tested here. The Forms compatibility dependency is beta and has partial/no-op API members; compilation and the listed tests do not prove complete behavioral parity across every screen.

Before treating this as a normal release, test a BP character and a Karma-built character; mundane, awakened and technomancer characters; gear purchases; quality choices; skill/attribute improvement; career rewards/spending/undo; saving/reopening; multiple documents; and sheet viewing/printing on the target Mac. The included GitHub workflow can run the portable checks on macOS and generate a locally signed ARM64 application; it has been supplied but not run against a hosted repository.

The upstream update checker points to the original fork's releases. Those releases do not distribute this Mac preview. Obtain Mac updates separately.

## Build from source

Install the .NET 10 SDK on an Apple Silicon Mac. From the source archive's root:

```bash
dotnet build Chummer.Mac/Chummer.Mac.csproj -c Release
dotnet Chummer.Mac/bin/Release/net10.0/ChummerMac.dll --smoke-test /tmp/chummer-check "$PWD/Chummer.Mac/assets/saves"
bash scripts/build-mac.sh "$PWD/artifacts/mac"
open artifacts/mac/ChummerSR4.app
```

The packaging script refuses to overwrite an existing `ChummerSR4.app`; use another output folder for a subsequent build. macOS builds receive an ad-hoc signature automatically. A Developer ID certificate/notarization can be added later for public distribution. The app uses the ARM64 .NET JIT, not Native AOT; trimming is disabled to preserve the legacy reflection and resource behavior.

Dependencies are explicitly pinned, with a NuGet lock file. The principal migration dependency is `Majorsilence.Forms` / `Majorsilence.Forms.Avalonia` 26.11.4. It preserves the existing event handlers and designer layouts while replacing the Windows Forms and GDI+ implementations. Windows-only DataVisualization was replaced by `HistoryChart`, which draws ledger data with Skia. Modern .NET XSLT includes use a resolver restricted to the local template folder.
