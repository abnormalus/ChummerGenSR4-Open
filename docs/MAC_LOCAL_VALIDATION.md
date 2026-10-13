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

## Editable prompted details

The shared free-text prompt returns the literal <placeholder> on accepted blank or whitespace-only input. Cancel still cancels. The editable restricted-item picker used for licenses follows the same rule. Qualities and gear with free-text or restricted-item details expose Edit Detail in their context menu and on double-click, in both creation and career. ID/credstick items without a prompted detail edit their existing GearName field. Numeric and fixed-choice selectors retain their existing value types.

Edits update the item's existing Extra/GearName fields and matching Text/Restricted improvement records. No character-file fields or encoding were changed. Checks cover blank acceptance, prefilled editing, quality/SIN/license/credstick edits, save/reopen persistence, and all 399 existing fixtures. Older qualities with empty saved bonus XML resolve edit eligibility through the rules definition.

A license picker that accepts a forced value during Load exposed a Form.ShowDialogAsync null result-task crash. Retaining the invocation's completion task locally fixes creation and reopening of those license details.

Native Retina validation: double-clicking Codeslinger opens the prefilled Edit Detail window; clearing its field and accepting displays Codeslinger (<placeholder>) and marks the character dirty. Right-click shows Notes and Edit Detail. The test used an isolated smoke-test character. Captured double-click dispatch and tree-node context-menu forwarding are covered at scales 1 and 2 with restored and maximized MDI children.

TreeNode.Text now invalidates the cached TreeView image when renamed. Regression checks compare the rendered frame before/after changing a detail without extra pointer events. Native validation confirms accepting DRUGS immediately updates the selected listing. Quality lists disable connector lines in both creation and career to prevent branches crossing text while glyphs are hidden. All 399 fixture checks pass.

### Searchable quality details

Codeslinger uses the supplied SR4A/Unwired action list. Choices are filtered by the character's enabled books (`SR4`, `UN`), and review-required entries are red, italic, and explicitly labelled. Selecting an item stores its plain name in the existing Extra/improvement fields. Blank input still becomes `<placeholder>`; existing/custom details remain editable.

`frmSelectText.ConfigureChoices(IEnumerable<DetailChoice>)` is the reusable picker entry point, independent of quality names and source rules. `DetailChoices.ForQuality` supplies data-backed choices from `data/quality-detail-choices.xml`; add a `choices quality="…"` group with `choice` rows (`name`, optional `source`, `book`, `review`) to enable the same picker for another quality. Sources are filtered each time the prompt opens. Qualities without a group retain free text. Allergies have not been assigned a curated choice list yet.

Regression checks cover disabled/enabled Unwired, review flags, case-insensitive search, editing after selection, restoring the full list, placeholder handling, plain-name storage, and a separate sample list using the same picker.

Chummer enables `Form.CenterModalDialogsOnRootOwner` so modal prompts, including nested dialogs and legacy screen-centered/manual dialogs, open over the main Chummer window. ScreenBounds now measures native desktop coordinates on Retina rather than framebuffer pixels. Three selector designers also explicitly use CenterParent. Position checks cover a moved root owner, nested dialogs, and Retina coordinate conversion.

Native Mac verification: typed `control`, selected its match with Down, clicked OK, and observed `Codeslinger (Control Device)` immediately in the character quality list. Review entries have readable red italic text and source labels. Test edits were made to an isolated fixture and were not saved to user characters.

### Equipment scrolling and review display

Tree views measure expanded equipment names and show a horizontal scrollbar when the text exceeds the viewport. Horizontal trackpad gestures pan labels; collapsing a long branch resets the offset, and expansion restores the bar. Node bounds move with the text so selection and expansion hit testing remain aligned. Vertical and horizontal viewport sizes account for both bars.

StatusStrip now measures AutoSize labels from the text it renders, instead of retaining designer widths. Nuyen values, including negative and large amounts, grow to fit. Fixed-size labels retain their assigned size.

Choice captions omit book names. Character Options.BookEnabled still filters sources using the character preference settings. Review-required choices remain red in creation and career quality trees; coloring derives from the saved quality/action name each time a node renders, including after reloading. Changing to an ordinary action clears the review color.

Creation and career content containers now fill their available panels so nested equipment pages cannot grow below the window. Empty child toolbars are hidden after their commands merge into the main toolbar. Full-builder checks verify the scrollbar lies within the visible character content.
