# Character switching and toolbar ownership

The main window owns document activation. Every `MdiChildActivate` synchronizes the selected tab, even for an editor that already has a tab. Tab clicks select their editor in turn; selecting an already-active editor is a no-op in the compatibility framework, so this does not introduce an activation loop or duplicate tabs.

`frmMain.RefreshCharacterToolbar` is the common toolbar boundary for creation and career editors. It reverts the prior merge on the explicit main-window strip and merges the actual active editor's strip. Load and native Activated callbacks delegate to the same boundary. Hosted MDI switches do not consistently produce a separate native Activated event; relying only on that event left commands belonging to the previous character visible and clickable.

`ReleaseCharacterToolbar` releases commands only when the closing editor owns them. Closing an inactive editor must not revert the active editor's merge. Both methods use the explicit parent strip instead of the global `toolStrip` name lookup. Existing replacement-workflow skip flags remain in place. No character serialization or purchase rules change.

Activation also re-evaluates the existing copy/paste policy: clipboard contents are shared across documents, while the selected paste destination belongs to each editor. Both menu and toolbar receive the refreshed availability before merging commands.

The existing Save/Save As/Close menu actions resolve the current active editor when invoked, and their availability follows document activation. Closing the final editor restores the main toolbar's hidden placeholders and disables document menu commands. An inactive or disposed editor must never retain live toolbar actions.

## Verification

`CharacterTabChecks` covers canceled/active/inactive/final closes, programmatic activation, repeated tab selection and header reuse in the headless smoke suite. The programmatic activation assertion reproduced the previous header mismatch before the fix.

`NativeCharacterSwitchChecks` uses real creation and career editors on the native dispatcher. It checks five repeated switches, exact Save-button ownership, menu availability, inactive close, the shared Close Character command and an unchanged source character snapshot. The combined native CI report requires at least 105 checks on Mac and Windows. Generic MDI forms alone cannot verify editor toolbar handlers.

## Remaining work

The stale Paste regression is covered for both incompatible and compatible clipboard changes while another character is active. Continue auditing undo and remaining edit commands and modal transitions. Armor is now the second native catalog picker; see [CATALOG_PICKERS.md](CATALOG_PICKERS.md). The shell and editor toolbars still use the compatibility layer; this change centralizes their lifecycle rather than replacing the entire shell. Shared framework commands and native layouts remain the preferred direction when that section is migrated.

## Character menu integration

The same activation boundary now reverts and merges the active editor's Edit and Special menu roots. The editor File root is temporarily excluded because the main window already owns Save/Save As/Close/Print routing; it is restored to the source immediately after merging. Reversion returns moved commands to their original editor before another editor is attached or the active editor is disposed. Inactive closes leave the current commands intact. Child strips stay hidden after the merge.

DesktopMenus.Refresh explicitly republishes the native projection after shared menu changes. A weak owner registration avoids retaining closed windows. Native click guards reject commands detached from the current tree, including queued clicks from a previously active character. This restores creation-only Add PACKS Kit in the Mac Special menu without exposing it in career mode.

## BP discrepancy investigation

The two supplied character saves were inspected read-only and opened from temporary copies. Both reopened with 1 BP remaining under the test profile, 0 BP charged for knowledge skills and 3 BP charged for the bonded Force-3 sustaining focus. Their knowledge changes do not explain a 3 BP increase. This does not establish the cause of the user's observed 4 BP; profile settings or an intermediate UI state still require reproduction. Do not change point rules based on this evidence alone. BuildPointPersistenceChecks exercises free-knowledge changes and focus costs across serialization and editor reopening using synthetic data; no personal character files are committed.

Creation also exposes a PACKS tab under Street Gear. Add PACKS Kit opens the existing kit browser and safe import workflow; Create PACKS Kit opens the existing selection/export dialog. Buttons delegate to the same menu commands to avoid maintaining a second import path. Career import remains unsupported.

## Mac full-screen menu access

DesktopMenus keeps the shared command strip visible inside a full-screen window, while retaining the system-menu projection. This fallback uses the same items and handlers as the native menu, so character switching and modal availability retain their existing ownership. Leaving full screen hides the fallback again. NativeControlChecks verifies both transitions. It does not change the user’s macOS menu-bar preferences.

Preferences and Quit also reappear in the shared strip during Mac full screen, since the application menu can be hidden with the system bar. Returning to normal removes those duplicate placements. NativeCharacterSwitchChecks exercises the actual main-window command visibility on Mac.

## PACKS replacement and menu ownership

PACKS import replaces the creation editor after preparing a complete character snapshot. The new editor must become the active MDI child, retain the toolbar and menu ownership, and accept subsequent inventory commands. `NativePacksInteractionChecks` imports a synthetic Backpack kit, opens and cancels the real Add Armor and Add Gear buttons, verifies owner enabled state and rejects swallowed toolbar refresh errors.

Merging moves the character menu roots into the main menu. The File root is temporarily excluded because main-window File commands already route to the active character. Restoring File must account for the source collection shrinking during the merge: its former index can exceed the remaining count. Restoration clamps the insertion point, and a finally-reset guard prevents synchronous callbacks from entering a half-completed transfer. A refresh failure counter makes errors observable to native regressions as well as diagnostic logs.

The reported intermittent unresponsive inventory buttons recovered after saving, closing and reopening the character. Tests reproduce and cover the menu restoration exception and the subsequent purchase transitions, but do not establish that every blocked-input sequence is resolved. Preserve this distinction when triaging future reports.

## Interaction diagnostics

Fixed Add Armor/Add Gear command IDs are logged in creation and career editors. Diagnostic UI snapshots include main Form enabled state, native main-window enabled state, editor type/enabled/focus state and dialog count. Native dialogs record opening, opened, failed and cleanup transitions; cleanup captures state after restoring ownership. The two enabled states are deliberately separate because native modality can block input while the compatibility Form still reports enabled.

Snapshots run on the UI thread. Logs exclude titles, input text and character identities. Initial coverage does not imply that every blocked command or duration is instrumented. Use the lifecycle events and structural state to identify missing coverage for the next confirmed defect.

## File and Edit order with maximized documents

The compatibility menu contains leading/trailing MDI window controls, which the Mac system menu omits. Insert merge positions now count application commands rather than those chrome slots, retaining File, Edit, Tools, Special, Window, Help order. `MenuOrderChecks` reproduced the previous File/Edit swap with real MDI control items and verifies repeated merge/revert cycles and item identity. Platform native acceptance remains part of the next combined batch.

The local failing baseline is `/tmp/chummer-menu-order-baseline.log` (File-before-Edit assertion, exit1). The correction passes `/tmp/chummer-menu-order-fixed.log`; extended maximized/restored cycle coverage passes `/tmp/chummer-menu-transitions.log` (full headless suite without repeating the unchanged399 fixture loop). Existing native character-switch assertions now require shared File/Edit order for both editor types and File/Edit order in the Mac projection. These platform assertions have compiled but await the next combined exact-head CI.
