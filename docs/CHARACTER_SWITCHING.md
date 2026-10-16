# Character switching and toolbar ownership

The main window owns document activation. Every `MdiChildActivate` synchronizes the selected tab, even for an editor that already has a tab. Tab clicks select their editor in turn; selecting an already-active editor is a no-op in the compatibility framework, so this does not introduce an activation loop or duplicate tabs.

`frmMain.RefreshCharacterToolbar` is the common toolbar boundary for creation and career editors. It reverts the prior merge on the explicit main-window strip and merges the actual active editor's strip. Load and native Activated callbacks delegate to the same boundary. Hosted MDI switches do not consistently produce a separate native Activated event; relying only on that event left commands belonging to the previous character visible and clickable.

`ReleaseCharacterToolbar` releases commands only when the closing editor owns them. Closing an inactive editor must not revert the active editor's merge. Both methods use the explicit parent strip instead of the global `toolStrip` name lookup. Existing replacement-workflow skip flags remain in place. No character serialization or purchase rules change.

Activation also re-evaluates the existing copy/paste policy: clipboard contents are shared across documents, while the selected paste destination belongs to each editor. Both menu and toolbar receive the refreshed availability before merging commands.

The existing Save/Save As/Close menu actions resolve the current active editor when invoked, and their availability follows document activation. Closing the final editor restores the main toolbar's hidden placeholders and disables document menu commands. An inactive or disposed editor must never retain live toolbar actions.

## Verification

`CharacterTabChecks` covers canceled/active/inactive/final closes, programmatic activation, repeated tab selection and header reuse in the headless smoke suite. The programmatic activation assertion reproduced the previous header mismatch before the fix.

`NativeCharacterSwitchChecks` uses real creation and career editors on the native dispatcher. It checks five repeated switches, exact Save-button ownership, menu availability, inactive close, the shared Close Character command and an unchanged source character snapshot. The combined native CI report requires at least 102 checks on Mac and Windows. Generic MDI forms alone cannot verify editor toolbar handlers.

## Remaining work

The stale Paste regression is covered for both incompatible and compatible clipboard changes while another character is active. Continue auditing undo and remaining edit commands and modal transitions. Armor is now the second native catalog picker; see [CATALOG_PICKERS.md](CATALOG_PICKERS.md). The shell and editor toolbars still use the compatibility layer; this change centralizes their lifecycle rather than replacing the entire shell. Shared framework commands and native layouts remain the preferred direction when that section is migrated.

## Character menu integration

The same activation boundary now reverts and merges the active editor's Edit and Special menu roots. The editor File root is temporarily excluded because the main window already owns Save/Save As/Close/Print routing; it is restored to the source immediately after merging. Reversion returns moved commands to their original editor before another editor is attached or the active editor is disposed. Inactive closes leave the current commands intact. Child strips stay hidden after the merge.

DesktopMenus.Refresh explicitly republishes the native projection after shared menu changes. A weak owner registration avoids retaining closed windows. Native click guards reject commands detached from the current tree, including queued clicks from a previously active character. This restores creation-only Add PACKS Kit in the Mac Special menu without exposing it in career mode.

## BP discrepancy investigation

The two supplied character saves were inspected read-only and opened from temporary copies. Both reopened with 1 BP remaining under the test profile, 0 BP charged for knowledge skills and 3 BP charged for the bonded Force-3 sustaining focus. Their knowledge changes do not explain a 3 BP increase. This does not establish the cause of the user's observed 4 BP; profile settings or an intermediate UI state still require reproduction. Do not change point rules based on this evidence alone. BuildPointPersistenceChecks exercises free-knowledge changes and focus costs across serialization and editor reopening using synthetic data; no personal character files are committed.

Creation also exposes a PACKS tab under Street Gear. Add PACKS Kit opens the existing kit browser and safe import workflow; Create PACKS Kit opens the existing selection/export dialog. Buttons delegate to the same menu commands to avoid maintaining a second import path. Career import remains unsupported.
