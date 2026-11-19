# Compatibility-control inventory — reviewed November 5, 2026

This inventory covers the first high-impact pass through the shipping Chummer.Mac application, not every capability in the vendor toolkit. Historical Chummer/ is excluded. Compilation alone cannot establish that a control works. Native driver coverage below is source-reviewed. At4351319, both platforms pass full/native bulk/sensor17/focus66; WindowsPDF also passes. The e942000 correction passes both-platform native suites and92-image review perOS; its four focus captures show the intended Gear view. The subsequent293158a native-reader gate requires its own actual platform results.

| Reachable surface | Implementation and evidence | Remaining coverage |
|---|---|---|
| Native Mac menu commands and Preferences | DesktopMenus projects shared handlers; now checks actual host enablement at creation and invocation. Native checks exercise modal disable/re-enable, stale exporter clicks, item disablement, Preferences and available Quit. | Human menu-bar/keyboard interaction alongside exporter-bridge checks. |
| PACKS/detail prompts, batch progress and Quit | Native owner-graph checks on both platforms cover cancellation, nested prompts, cleanup, stale-model/rules conflicts and source-file protection. | Additional uncommon rule prompts as reports arrive. |
| Character preview/printing | frmViewer uses real async native browser printing/PDF paths. Windows native preview/PDF CI verifies output, alongside shared output checks. | Physical printers and drivers need user/platform testing. |
| Sourcebook PDF reader | Native WebBrowser uses WKWebView/WebView2 and the bundled local PDF.js reader; server and output checks cover selected-file/path/range boundaries. Bundle/update safeguards are documented separately. | Repeat interactive page/search/close checks on intentional reader upgrades. |
| Character toolbar merging | ToolStripManager.Merge.cs implements append, insert, replace, remove, nested match and revert. `NativeCharacterSwitchChecks` runs five creation/career switch cycles, clipboard/menu/toolbar activation, inactive close and final Close Character cleanup, retaining the source snapshot; prior7d both-platform bulk runs pass. `MenuOrderChecks` separately covers simulated maximized controls and normal menu ordering. | Broader real OS maximized-window interaction remains distinct from the simulated menu-order cases; expand it when window ownership/rendering changes. |
| Searchable lists, trees, numeric controls, dropdowns | Existing rendering/input smoke checks cover cumulative typing, edits/repainting, scaling, popup coordinates and nested gear. | Expand actual OS interaction coverage for screens implicated by reports. |

## Supplemental caller acceptance

Sensor transactions use their own180-second/min17 report gate for actual creation/career purchase, funds rejection, sale and expense undo. Focus commands use a separate180-second/min66 gate in both themes, with four selected-gear captures. They supplement the original300-second/min1069 native bulk suite. Both-platform7d supplemental results exposed an accepted-sale dirty-state defect after15passing assertions; d486 contains the explicit completion fix. Both17/66 process gates pass on Mac and Windows at4351319, but direct review rejects its focus captures. The e942000 selected-tab correction passes both-platform92-image review. The separately bounded actual sourcebook-reader gate still needs its own source-specific platform proof; process success alone does not establish visual acceptance.

## Unused or deliberately unsupported APIs

Application source has no calls to the toolkit's PrintDialog, PageSetupDialog or synchronous WebBrowser.InvokeScript stubs. Source safeguards now reject new calls; use the established async native browser surfaces instead. TreeNode.FromHandle, Menu.GetContextMenu and ToolStripManager.IsShortcutDefined also have no application call sites in this audit. Their presence is vendor debt, not evidence of a current user-facing failure. Do not implement every upstream API speculatively.

## Priorities after this pass

1. Preserve the existing repeated character-switch/toolbar regressions and continue reachable API review when affected screens change. Do not treat the earlier request to add those tests as still unimplemented.
2. Act on PDF.js update-check findings with a reviewed, tested asset replacement.
3. If Mac rendering startup fails again, use the newly recorded numeric render-timer code and startup stages to distinguish initialization failure from later UI hangs. The cause is not established; a subsequent local native check passed. Avoid an unproven graphics fallback.
4. Continue staged extraction of UI-coupled rules and nullable checking as affected code changes; retain existing character compatibility checks.

## November 19 desktop working-tree inventory

Scope is the shared Mac/Windows desktop source in `Chummer.Mac/code`, including local work in progress. Counts are source declarations. The historical Windows project, mobile/PWA and vendor internals are outside this pass. Conditional controls may appear only for certain character types or rules. Platform acceptance needs evidence for the source being evaluated.

| Surface | Where the remaining compatibility controls live |
|---|---|
| Character creation | All 24 spinner fields on [frmCreate.Designer.cs](../Chummer.Mac/code/frmCreate.Designer.cs) now have native presentation. The latest seven cover installed Cyberware rating, installed Armor/gear rating, Martial Arts rating, Complex Form rating, Mystic Adept Magic allocation, Signal and Response. The 13 trees also have native presentation. |
| Career | [frmCareer.Designer.cs](../Chummer.Mac/code/frmCareer.Designer.cs) declares seven spinners and 17 tree/list controls. Spinners cover Street Cred, Notoriety, Public Awareness, Mystic Adept Magic allocation, Signal, Response and Complex Form rating. Tree/list surfaces include inventory, magic, improvements, Karma/Nuyen expenses and the calendar. |
| Preferences | All 63 spinner fields now have native presentation, including the 62 designer fields and the autosave interval created by [frmOptions.cs](../Chummer.Mac/code/frmOptions.cs). The sourcebook tree also has native presentation. |
| Specialist dialogs and shared rows | The other 19 designer spinners are in Contact, Skill, Skill Group, Power and Spirit rows, plus Advanced Lifestyle, Critter Power, Metatype, Dice Roller, Create Spell and Create Improvement. Creation uses native viewports for skill/group and contact rows, with compatibility controllers underneath. Shared row declarations can still serve career or conditional workflows. |
| Other pickers and prompts | The Add Spell, Add Program, Add Critter Power, Advanced Lifestyle, PACKS and Print Multiple tree surfaces are native. Plain Forms list boxes remain in several dialogs, including Metatype, Mentor Spirit and Add Adept Power. Search `frmSelect*.cs` for Forms subclasses and their callers. |
| Shell and shared layout | [frmMain.Designer.cs](../Chummer.Mac/code/frmMain.Designer.cs) owns the compatibility workspace menus and toolbar. The Creation and Career designers still own the tab shells and most tab layout. See the earlier inventory for menu projection, browser/printing and modal ownership. |

The scan found 112 designer spinner fields and one additional runtime-created Preferences spinner. `NativeInlineNumericBinding.source` is a reference to an existing spinner. The designer scan found 38 TreeView/ListView/DataGridView fields. Neither count measures runtime instances or overall modernization completion. The [native catalog windows](CATALOG_PICKERS.md) and the current Gear, Cyberware, Quality, Vehicle, Vehicle Modification and Basic Lifestyle picker adapters have their own acceptance boundaries.

### Preferences and Creation spinner batch

The new [compact Avalonia spinner host](../Chummer.Mac/code/NativeCompactSpinner.cs) uses the existing [numeric binding](../Chummer.Mac/code/NativeInlineNumericBinding.cs) and retains the detached Forms controllers for character rules and Preferences persistence. The 63 Preferences and seven Creation replacements passed 150 focused Mac assertions via `--check-compact-spinners FIXTURE REPORT`. The checks cover spinner geometry and values across tabs, House Rules scroll clipping, autosave interval stepping, and installed Cyberware and Armor edits through a character snapshot round trip. The combined Mac native suite passed 1,483 checks. The headless suite passed all 399 round trips and both Foundry export targets. Windows cross-compilation passed, with Windows runtime acceptance still pending.

The remaining spinner work is on the Career editor and in specialist dialogs and shared rows.

### Tree and list migration checklist

Check a field off after the replacement passes the applicable Mac and Windows behavior and visual checks.

All 38 desktop surfaces now use [native Avalonia trees](../Chummer.Mac/code/NativeEditorTree.cs) or [native Career lists](../Chummer.Mac/code/NativeCareerList.cs). The Forms fields below are retained as detached command and row-identity adapters. The existing editor handlers still own persistence, accounting, confirmation and inventory rules.

The focused drivers accept a synthetic fixture file and a JSON report path. The same checks run in the combined native bulk suite.

| Driver | Field count | Behavior exercised |
|---|---:|---|
| `--check-quality-trees` | 2 | Layout, selection and context-menu routing, expansion, removal, disposal, and light/dark captures |
| `--check-character-trees` | 14 | Editor-tab visibility, row projection, selection, stale-row removal, and checkbox presentation |
| `--check-inventory-trees` | 11 | Reorder and cancellation through the existing move commands, native pointer replay for Gear reparenting, and rejected mounted-gear drops |
| `--check-career-lists` | 3 | List order, selection, Karma/Nuyen header sorting, and row removal |
| `--check-dialog-trees` | 8 | Dialog row projection, selection, enabled state, checkbox propagation, and closing without character changes |

All five focused Mac suites passed, covering every listed field. The combined Mac native suite passed 1,335 checks. The headless suite passed all 399 character file and memory round trips and both Foundry export targets. Windows cross-compilation passed. The checklist stays open for Windows runtime acceptance and full light/dark review of the batch.

#### [frmCreate.Designer.cs](../Chummer.Mac/code/frmCreate.Designer.cs)

- [ ] `treQualities`
- [ ] `treCyberware`
- [ ] `treWeapons`
- [ ] `treArmor`
- [ ] `treSpells`
- [ ] `treLifestyles`
- [ ] `treGear`
- [ ] `treVehicles`
- [ ] `treMartialArts`
- [ ] `treFoci`
- [ ] `treMetamagic`
- [ ] `treCritterPowers`
- [ ] `treComplexForms`

#### [frmCareer.Designer.cs](../Chummer.Mac/code/frmCareer.Designer.cs)

- [ ] `treCyberware`
- [ ] `treWeapons`
- [ ] `treArmor`
- [ ] `treSpells`
- [ ] `treLifestyles`
- [ ] `treGear`
- [ ] `treVehicles`
- [ ] `treMartialArts`
- [ ] `treFoci`
- [ ] `treMetamagic`
- [ ] `lstKarma`
- [ ] `lstNuyen`
- [ ] `treCritterPowers`
- [ ] `treComplexForms`
- [ ] `treQualities`
- [ ] `lstCalendar`
- [ ] `treImprovements`

#### [frmOptions.Designer.cs](../Chummer.Mac/code/frmOptions.Designer.cs)

- [ ] `treSourcebook`

#### [frmPrintMultiple.Designer.cs](../Chummer.Mac/code/frmPrintMultiple.Designer.cs)

- [ ] `treCharacters`

#### [frmSelectAdvancedLifestyle.Designer.cs](../Chummer.Mac/code/frmSelectAdvancedLifestyle.Designer.cs)

- [ ] `trePositiveQualities`
- [ ] `treNegativeQualities`

#### [frmSelectCritterPower.Designer.cs](../Chummer.Mac/code/frmSelectCritterPower.Designer.cs)

- [ ] `trePowers`

#### [frmSelectPACKSKit.Designer.cs](../Chummer.Mac/code/frmSelectPACKSKit.Designer.cs)

- [ ] `treContents`

#### [frmSelectProgram.Designer.cs](../Chummer.Mac/code/frmSelectProgram.Designer.cs)

- [ ] `trePrograms`

#### [frmSelectSpell.Designer.cs](../Chummer.Mac/code/frmSelectSpell.Designer.cs)

- [ ] `treSpells`
