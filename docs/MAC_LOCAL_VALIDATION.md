# Local Mac validation — 2026-10-13

Imported the standalone Mac port from ChummerGenSR4-0.1.501 into the upstream-based fork on branch mac-local-build. Original Windows source remains unchanged. Matching Mac data assets are under Chummer.Mac/assets because the upstream checkout uses a different data layout.

- .NET SDK 10.0.401, osx-arm64: Release build succeeded, 0 errors, 7 warnings.
- Existing offscreen smoke checks passed on this Mac, including all 399 fixture round trips.
- Self-contained ARM64 app packaging and ad-hoc signature verification succeeded.
- Direct execution from the Codex sandbox aborted in HIServices _RegisterApplication during Avalonia native startup. GUI automation briefly observed a Chummer window, but subsequent window inspection failed; interactive startup is not confirmed.

Next check: open artifacts/mac/ChummerSR4.app manually in Finder outside the agent launch context and verify the window remains open. If it fails, collect that launch's crash report. Earlier MAC_VALIDATION.txt and migration reports are supplied historical results, not evidence of this local run.

## Retina correction

User confirmed normal Finder launch stays open. The main-window framebuffer metadata caused extra Retina magnification and click misalignment. Added a pinned, locally corrected Avalonia backend under vendor/; core dependency unchanged. Interactive New toolbar and Select Setting OK clicks now match their painted centers. Release build and all 399 offscreen fixture checks passed again; locally signed app is artifacts/retina-fixed/ChummerSR4.app. BP dialog spinner arrow placement still needs correction.

## Numeric spinner correction

Corrected the shared numeric control's logical arrow-strip geometry and renderer conversion. Arrow clicks passed at scales 1 and 2; all 399 fixture round trips passed. The packaged app was inspected in the native character-creation window and arrows no longer overlap the values. The current package is artifacts/spinner-fixed/ChummerSR4.app. Local library source and attribution are in vendor/LOCAL_PATCHES.md.

## Character creation mouse routing

Fixed repeated logical-coordinate conversion at the MDI handoff and the hosted form's screen origin/scale used during mouse capture. Root-window press and captured release checks pass at scales 1 and 2, in both restored and maximized child windows. All 399 fixture round trips passed again. Interactive native Retina checks confirmed Body 1 → 2 with 10 BP spent and the Add Quality button opening its dialog. Latest package: artifacts/character-controls-fixed/ChummerSR4.app.

## Native character file dialogs

Open and Save As use the Avalonia native storage provider with the visible main window as owner. File menu Save and Save As dispatch to the active character and display save icons. Suggested filenames use the alias, or chummer-yyyyMMdd-HHmmss.chum. Dialogs start in Documents/chummer until a successful open or save remembers another folder; the preference is persisted in the existing settings store. Regular Save keeps the selected filename.

The character serializer remains unchanged, including UTF-16 little-endian XML, atomic replacement, and backups. Cancellation preserves filename and dirty state. Expected file errors are displayed without discarding the character. Native Mac Open and Save As were exercised with a test character; the 399 upstream fixture round trips passed.

Window-menu clipping is corrected for dynamically added character entries. Automated pointer-move checks select each top menu label at scales 1 and 2.

User screenshots exposed a separate popup-placement issue: offsets were doubled on Retina macOS because Control.PointToScreen multiplied native desktop coordinates by framebuffer scale. Screen-coordinate conversion now measures the backend's native mapping independently of render scale; PointToClient and rectangle conversions use the same mapping. A regression backend with 2x framebuffer scale and 1x native screen coordinates verifies placement and round trips. Hosted input checks and all 399 fixtures pass.
