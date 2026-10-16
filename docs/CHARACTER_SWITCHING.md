# Character switching and toolbar ownership

The main window owns document activation. Every `MdiChildActivate` synchronizes the selected tab, even for an editor that already has a tab. Tab clicks select their editor in turn; selecting an already-active editor is a no-op in the compatibility framework, so this does not introduce an activation loop or duplicate tabs.

`frmMain.RefreshCharacterToolbar` is the common toolbar boundary for creation and career editors. It reverts the prior merge on the explicit main-window strip and merges the actual active editor's strip. Load and native Activated callbacks delegate to the same boundary. Hosted MDI switches do not consistently produce a separate native Activated event; relying only on that event left commands belonging to the previous character visible and clickable.

`ReleaseCharacterToolbar` releases commands only when the closing editor owns them. Closing an inactive editor must not revert the active editor's merge. Both methods use the explicit parent strip instead of the global `toolStrip` name lookup. Existing replacement-workflow skip flags remain in place. No character serialization or purchase rules change.

Activation also re-evaluates the existing copy/paste policy: clipboard contents are shared across documents, while the selected paste destination belongs to each editor. Both menu and toolbar receive the refreshed availability before merging commands.

The existing Save/Save As/Close menu actions resolve the current active editor when invoked, and their availability follows document activation. Closing the final editor restores the main toolbar's hidden placeholders and disables document menu commands. An inactive or disposed editor must never retain live toolbar actions.

## Verification

`CharacterTabChecks` covers canceled/active/inactive/final closes, programmatic activation, repeated tab selection and header reuse in the headless smoke suite. The programmatic activation assertion reproduced the previous header mismatch before the fix.

`NativeCharacterSwitchChecks` uses real creation and career editors on the native dispatcher. It checks five repeated switches, exact Save-button ownership, menu availability, inactive close, the shared Close Character command and an unchanged source character snapshot. The combined native CI report requires at least 71 checks on Mac and Windows. Generic MDI forms alone cannot verify editor toolbar handlers.

## Remaining work

The stale Paste regression is covered for both incompatible and compatible clipboard changes while another character is active. Continue auditing undo and remaining edit commands and modal transitions. Armor is now the second native catalog picker; see [CATALOG_PICKERS.md](CATALOG_PICKERS.md). The shell and editor toolbars still use the compatibility layer; this change centralizes their lifecycle rather than replacing the entire shell. Shared framework commands and native layouts remain the preferred direction when that section is migrated.
