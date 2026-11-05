# Compatibility-control inventory — reviewed October 30, 2026

This inventory covers the first high-impact pass through the shipping Chummer.Mac application, not every capability in the vendor toolkit. Historical Chummer/ is excluded. Compilation alone cannot establish that a control works. Native driver coverage below is source-reviewed; the previous7d bulk suite passed on both platforms. The currentd486 revision still requires its own native17/66 reports and92-image acceptance, so that earlier result is not a blanket current-source pass.

| Reachable surface | Implementation and evidence | Remaining coverage |
|---|---|---|
| Native Mac menu commands and Preferences | DesktopMenus projects shared handlers; now checks actual host enablement at creation and invocation. Native checks exercise modal disable/re-enable, stale exporter clicks, item disablement, Preferences and available Quit. | Human menu-bar/keyboard interaction alongside exporter-bridge checks. |
| PACKS/detail prompts, batch progress and Quit | Native owner-graph checks on both platforms cover cancellation, nested prompts, cleanup, stale-model/rules conflicts and source-file protection. | Additional uncommon rule prompts as reports arrive. |
| Character preview/printing | frmViewer uses real async native browser printing/PDF paths. Windows native preview/PDF CI verifies output, alongside shared output checks. | Physical printers and drivers need user/platform testing. |
| Sourcebook PDF reader | Native WebBrowser uses WKWebView/WebView2 and the bundled local PDF.js reader; server and output checks cover selected-file/path/range boundaries. Bundle/update safeguards are documented separately. | Repeat interactive page/search/close checks on intentional reader upgrades. |
| Character toolbar merging | ToolStripManager.Merge.cs implements append, insert, replace, remove, nested match and revert. `NativeCharacterSwitchChecks` runs five creation/career switch cycles, clipboard/menu/toolbar activation, inactive close and final Close Character cleanup, retaining the source snapshot; prior7d both-platform bulk runs pass. `MenuOrderChecks` separately covers simulated maximized controls and normal menu ordering. | Broader real OS maximized-window interaction remains distinct from the simulated menu-order cases; expand it when window ownership/rendering changes. |
| Searchable lists, trees, numeric controls, dropdowns | Existing rendering/input smoke checks cover cumulative typing, edits/repainting, scaling, popup coordinates and nested gear. | Expand actual OS interaction coverage for screens implicated by reports. |

## Supplemental caller acceptance

Sensor transactions use their own180-second/min17 report gate for actual creation/career purchase, funds rejection, sale and expense undo. Focus commands use a separate180-second/min66 gate in both themes, with four selected-gear captures. They supplement the original300-second/min1069 native bulk suite. Both-platform7d supplemental results exposed an accepted-sale dirty-state defect after15passing assertions; d486 contains the explicit completion fix. The new17/66 results remain pending; driver existence or a compiled adapter is not interaction acceptance.

## Unused or deliberately unsupported APIs

Application source has no calls to the toolkit's PrintDialog, PageSetupDialog or synchronous WebBrowser.InvokeScript stubs. Source safeguards now reject new calls; use the established async native browser surfaces instead. TreeNode.FromHandle, Menu.GetContextMenu and ToolStripManager.IsShortcutDefined also have no application call sites in this audit. Their presence is vendor debt, not evidence of a current user-facing failure. Do not implement every upstream API speculatively.

## Priorities after this pass

1. Preserve the existing repeated character-switch/toolbar regressions and continue reachable API review when affected screens change. Do not treat the earlier request to add those tests as still unimplemented.
2. Act on PDF.js update-check findings with a reviewed, tested asset replacement.
3. If Mac rendering startup fails again, use the newly recorded numeric render-timer code and startup stages to distinguish initialization failure from later UI hangs. The cause is not established; a subsequent local native check passed. Avoid an unproven graphics fallback.
4. Continue staged extraction of UI-coupled rules and nullable checking as affected code changes; retain existing character compatibility checks.
