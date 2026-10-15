# Compatibility-control audit — October 15, 2026

This inventory covers the first high-impact pass through the shipping Chummer.Mac application, not every capability in the vendor toolkit. Historical Chummer/ is excluded. Compilation alone cannot establish that a control works.

| Reachable surface | Implementation and evidence | Remaining coverage |
|---|---|---|
| Native Mac menu commands and Preferences | DesktopMenus projects shared handlers; now checks actual host enablement at creation and invocation. Native checks exercise modal disable/re-enable, stale exporter clicks, item disablement, Preferences and available Quit. | Human menu-bar/keyboard interaction alongside exporter-bridge checks. |
| PACKS/detail prompts, batch progress and Quit | Native owner-graph checks on both platforms cover cancellation, nested prompts, cleanup, stale-model/rules conflicts and source-file protection. | Additional uncommon rule prompts as reports arrive. |
| Character preview/printing | frmViewer uses real async native browser printing/PDF paths. Windows native preview/PDF CI verifies output, alongside shared output checks. | Physical printers and drivers need user/platform testing. |
| Sourcebook PDF reader | Native WebBrowser uses WKWebView/WebView2 and the bundled local PDF.js reader; server and output checks cover selected-file/path/range boundaries. Bundle/update safeguards are documented separately. | Repeat interactive page/search/close checks on intentional reader upgrades. |
| Character toolbar merging | ToolStripManager.Merge.cs implements append, insert, replace, remove, nested match and revert. Creation/career use this implementation, not a no-op. | Broaden native repeated document-switch checks, including restored/maximized windows. |
| Searchable lists, trees, numeric controls, dropdowns | Existing rendering/input smoke checks cover cumulative typing, edits/repainting, scaling, popup coordinates and nested gear. | Expand actual OS interaction coverage for screens implicated by reports. |

## Unused or deliberately unsupported APIs

Application source has no calls to the toolkit's PrintDialog, PageSetupDialog or synchronous WebBrowser.InvokeScript stubs. Source safeguards now reject new calls; use the established async native browser surfaces instead. TreeNode.FromHandle, Menu.GetContextMenu and ToolStripManager.IsShortcutDefined also have no application call sites in this audit. Their presence is vendor debt, not evidence of a current user-facing failure. Do not implement every upstream API speculatively.

## Priorities after this pass

1. Add native repeated character-switch/toolbar regressions, then continue screen-by-screen reachable API review.
2. Act on PDF.js update-check findings with a reviewed, tested asset replacement.
3. If Mac rendering startup fails again, use the newly recorded numeric render-timer code and startup stages to distinguish initialization failure from later UI hangs. The cause is not established; a subsequent local native check passed. Avoid an unproven graphics fallback.
4. Continue staged extraction of UI-coupled rules and nullable checking as affected code changes; retain existing character compatibility checks.
