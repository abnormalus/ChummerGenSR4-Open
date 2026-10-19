# Native numeric input rollout

Use maintained Avalonia NumericUpDown controls for migrated inputs. NativeIntegerInput supplies whole-number parsing, unit stepping, logical sizing, formatting and framework range clipping. Keep decimal precision for currency, multipliers and fractional quantities; never round a decimal domain just to reuse the integer factory. The model owns rules, limits and calculations; the control owns typing, buttons, keyboard routing and layout.

## User priority: follow the character-creation journey

The user explicitly prioritizes creation-facing spinners by when they appear in normal use. This supersedes choosing the smallest remaining popup or continuing the critter-power candidate merely because its baseline is ready. Preserve completed local work for combined acceptance; defer further specialist picker expansion.

1. Opening setup: BP/Karma limits are already native. Review metatype selection as part of setup, but its conditional Force spinner is not the next common interaction for ordinary metahuman creation.
2. Main creation screen: migrate the Attributes section (Body, Agility, Reaction, Strength, Charisma, Intuition, Logic, Willpower, Edge, conditional Magic/Resonance) and starting-money BP/Karma allocation first. These are the next implementation priority.
3. Skills and skill groups, then contact Connection/Loyalty: follow the creation tabs and preserve shared career behavior where those controls are reused.
4. Shopping: prioritize the common Gear and Cyberware purchase/rating/quantity controls, then their inline creation details, lifestyle months and vehicle quantities/ratings. Existing native catalog work remains part of acceptance, not a reason to reorder the user's journey.
5. Specialist magic, technomancer and critter controls (including metatype Force when applicable), custom spell/improvement editors and advanced lifestyle. Preferences and career-only numeric controls follow the creation-facing work unless a confirmed stability defect takes priority.

The first Attributes migration must replace a coherent section with standard controls, not add an untested global spinner overlay. Current `frmCreate` rules inspect the legacy panel's NumericUpDown identities, values, enabled state and maximums (`CanImproveAttribute`, primary BP/Karma cost calculation and refresh). Characterization must cover BP half-budget/one-max rules, Karma, Ignore Rules, metatype limits, Magic/Resonance enablement and opening/reloading without dirty changes. Keep the rules model/caller-owned; map native draft changes through a verified boundary rather than copying the rules into view code. Starting-money allocation stores decimal `NuyenBP` even though its current designer spinner has no fractional display; preserve the domain and existing conversion/caps.

## Current inventory and release state

The rollout baseline was 145 legacy NumericUpDown field declarations across all access modifiers. The earlier 130 count omitted 15 internal fields. This counts declarations, not user-visible defects or a percentage of overall modernization.

| Checkpoint | Remaining legacy fields |
|---|---:|
| Quantity/shared-number/sale prompts through 0.2.135 |141|
| Starting money, dice hits, calendar and build limits in 0.2.136 |134|
| Expense, natural weapon and vehicle selection in 0.2.137 |127|
| Local shared vehicle/weapon modification picker |125|
| Local basic lifestyle picker |123|
| Local nexus builder |117|

Nightly 0.2.138 contains the released127-field checkpoint plus save recovery. The117-field state is **local and not platform-accepted or published**. The largest remaining groups are preferences 62, creation 24 and career 7; other dialogs and controls account for the remaining24. Prioritize bounded migrations without changing their domain limits or saved representation.

A global overlay of every legacy spinner would add focus, clipping, scrolling and lifetime integration across unmigrated screens. Prefer whole native sections and shared components. A bridge is an option only when its actual transitions are tested.

## Shared catalog machinery

CatalogPickerWindow provides standard lists, source/PDF links, literal search, purchase options, owner-theme handling and safe empty/error states. CatalogSelection preserves canonical name/category, accepted rating and purchase intent. CatalogPickerModel supports overridable display/search policies while retaining the existing default behavior for other catalogs.

IRatedCatalogPicker models clamp ratings before publishing a new range/value pair. During publication they ignore transient feedback from the control coercing its old value against the new range. Do not replace ItemsSource for an ordinary detail update: it can clear the selected row. Basic lifestyle adds a complete-input predicate to acceptance; other models retain their previous predicate.

Armor modification selection is already native. ArmorModPickerModel retains Rating/Armor Cost expressions, FixedValues capacity, Standard/PerRating/Zero display styles, markup and free preview. Its availability test still uses marked-up cost even when the displayed price is free. General and parent-allowed categories all respect enabled sources; duplicate names such as Transparent Ruthenium Polymer Coating retain name/category identity. Character fields and encoding are unchanged.

Catalog and PDF error labels use Fluent SystemControlErrorTextForegroundBrush. The old fixed Firebrick/black baseline failed at 3.14:1; verified native transitions measured17.72:1 dark and 6.18:1 light after correction. Explicit legacy review red/italic styling is unchanged. This does not claim every legacy color is migrated. See [NATIVE_DIALOG_THEME.md](NATIVE_DIALOG_THEME.md).

The pinned Avalonia 12.1.1 behavior is documented by [NumericUpDown](https://github.com/AvaloniaUI/Avalonia/blob/12.1.1/src/Avalonia.Controls/NumericUpDown/NumericUpDown.cs) and [ButtonSpinner](https://github.com/AvaloniaUI/Avalonia/blob/12.1.1/src/Avalonia.Controls/ButtonSpinner.cs). Keyboard tests focus the template TextBox and put the caret at the line boundary: TextBox can consume arrows for caret movement before an unhandled arrow reaches ButtonSpinner. Do not add parallel stepping logic to work around framework routing.

## Released prompts and contracts

### Quantity, shared number and sale

Career Gear Add/Remove Quantity uses0..caller maximum, markup−99..1000 and Add-only free purchase. Explicit acceptance/disposal preserves canonical Gear, capacity, unit-cost rounding, restrictions, expense and undo. Native failing baselines reproduced removal of existing improvements on an unaffordable increase and recording old stock instead of the purchased increment. Both corrections passed platform checks in 0.2.132. Create Stack's same increment defect was reproduced separately and corrected in 0.2.134; old expense records are not rewritten.

The shared number prompt preserves bounds, initial draft, description, title and AllowCancel. Default bounds remain 0..10000000. Required variable-price choices block ordinary dismissal but unwind on Quit through INativeDialogCancellation, returning Cancel rather than an accepted zero. Creators signal failed required selection with the existing empty-GUID convention; callers must reject it. The local vehicle-mod caller audit below is a separate unproven boundary. Shared-number platform checks passed in 0.2.134.

Sale retains default 50 and range 0..9999, including values above100. All 25 career callers require OK and dispose the prompt. Actual Gear sales at 0/50/150 and Armor cancellation preserve the existing value calculations and rounding. Platform checks passed in 0.2.135.

### Starting money, dice, calendar and build limits

StartingMoneyRoll keeps bounds Dice..Dice×6 and the existing decimal `(roll + extra) × multiplier` calculation outside layout. Mac/Windows baselines reproduced canceled finalization mutating money/lifestyle. Explicit accepted prompts now gate carryover Karma, fallback Street lifestyle, starting money and skill-group changes; creation/Karma confirmations require Yes. This shipped in 0.2.136. Destination cancellation and failed-write recovery subsequently shipped in 0.2.137/0.2.138; their complete current ordering is in [SAVE_FINALIZATION.md](SAVE_FINALIZATION.md).

Dice-hit selection reuses the integer prompt with0..Dice, accepted/canceled results and the existing Text/Title/Description boundary. Its WIL-test caller disposes the prompt and runs conversion only after acceptance. It shipped in 0.2.136.

Calendar start retains Year 2000..9000 and Chummer's52-week4/4/5 mapping: only months3/6/9/12 have week5. January starts with its canonical four-week limit immediately. All 52 CalendarWeek mappings are characterized. Career start/shift callers require OK/disposal; native checks cover cancel/start/shift/year rollover while retaining note/GUID/relative spacing. It shipped in 0.2.136.

Build limits retain BP0..5000, availability0..100, profile defaults with400 BP/750 Karma fallbacks and Nuyen BP caps 50/100. Existing-character edits lock the build method. A reproduced baseline showed acceptance clearing enabled Ignore Rules; preload now preserves it. Accepted creation-menu edits refresh and mark dirty; cancellation does neither. This shipped in 0.2.136 and does not explain or claim to fix the unreproduced Gneiss BP report.

### Expense, natural weapon and vehicle selection

Expense retains integer amount1..9999999, configured zero/negative entry bounds, Nuyen percentage 0..1000 and midpoint-to-even rounding. Karma ignores the percentage. Automatic expenses lock financial fields while date/reason stay editable. Standard DatePicker/TimePicker composition preserves DateTime kind, hidden seconds/ticks and date-only hidden time. Bounds remain 1753-01-01 midnight through 9998-12-31 midnight; a later time on the maximum day is invalid. Cleared required amount/visible Nuyen percentage blocks acceptance and clears preview; locked values, hidden Karma percentage and explicit zero remain valid. Six career callers require OK/disposal. Actual gain/spend/edit/undo/cancel/insufficient-funds and time cases passed.

Natural weapon selection retains damage/AP modifiers−10..10, reach−3..3, damage base STR/2 or1..20 and Physical/Stun suffixes. Canonical skills stay separate from translated labels. An accepted selection prepares one unattached zero-cost melee weapon with the existing Natural Weapon source/page notation. Both callers require OK/disposal; cancellation creates no inventory.

Vehicle selection retains used discount20..50/default 20, ceiling of the discounted catalog price **before** markup, subsequent midpoint-to-even markup rounding and availability+4 with raw R/F suffix retained. Saved UsedCost stays separate from the charged marked-up price. Existing Vehicle.Create, inventory, restrictions, capacity, funds, ledger and undo stay caller-owned. All 483 catalog prices are characterized; actual paid/free/canceled/undo/insufficient-funds and persistence cases passed.

These three migrations and shared catalog error contrast shipped in 0.2.137 via PR 29. Exact head 12db2fc189a568b755f61ab2a6b8bb22367f8a99 passed CI 39135828265 on both platforms: all 399 fixtures/full smoke, minimum 412 native checks, dependency audit and Windows preview/PDF. Sixteen exact new synthetic screenshots across both platforms were hash-verified and visually reviewed. Original task outputs/pr29-verification preserve the evidence. Their former intermediate pending statements are obsolete.

## Local batch: shared vehicle/weapon modification picker

This window serves both catalogs and all six carried/vehicle-mounted/vehicle-mod creation/career callers. All now require OK/disposal. Fixed modifications retain disabled rating0; rated items use1..catalog maximum; tires use quantity1..20. Context is captured at opening. Preserve Vehicle Cost, Weapon Cost, Total Cost, Body, Speed, Accel(Running), Accel, Rating and ModMultiplier expressions, integer conversion before multiplier/markup and conversion again afterward. Body≤0 is treated as2 by the existing caller-facing setter.

Weapon aliases retain canonical identities and WeaponType eligibility. Electronics retain their improvement requirement and response/signal limits (current+2 or modular10). AllowedCategories remains an exposed but previously unused API; it does not acquire a speculative filter. Literal translated/canonical search, source/PDF links, Add More, free and separate variable-price handling remain intact. Variable-price preview/test retains its existing handling; the caller owns the mandatory price and final charge.

Verified local evidence:

- All 420 definitions:418 fixed-price entries plus2 variable entries;6688 fixed-price comparisons across rating endpoints, markup−99/0/25/1000 and free/paid states, including explicit midpoint cases.
- All 416 ordinary definitions prepare canonical details individually at maximum rating. Twelve unset/ordinary/modular electronics contexts match the released eligibility identities.
- `/tmp/chummer-mod-native-smoke.log` passed all 399 fixtures, both Foundry contracts and full headless checks (75851 exit0). Later native-only coverage additions did not change production behavior.

Fifty compiled native assertions cover the new modal, input/range/source transitions, all six caller paths, paid/free/markup/quantity, persistence, undo cancellation/acceptance and insufficient funds. Four exact synthetic modification PNGs are whitelisted per platform. **These native checks have not passed yet.**

A pre-fix regression selects the shielded compartment in each real vehicle-mod caller and cancels the mandatory number prompt through CancelForShutdown. It requires no exception, the complete original snapshot and identical owned modification references. VehicleMod.Create signals that cancellation with an empty GUID; source inspection suggests callers continue insertion. This remains unproven at runtime and production guards remain unchanged pending the native baseline. Accepted Generic Mod price1234, description and round trip are also covered by compiled checks. Do not report this cancellation concern as fixed.

## Local batch: basic lifestyle

The standard catalog window uses percentage 0..900/default 100 and roommates 0..10/default 0. BasicLifestyleSelection applies only after OK to the caller's unattached target. The source object is read-only; four basic add/edit callers retain insertion/replacement, GUID and months (career additions start at zero months). Advanced lifestyle branches are unchanged. Preserve edited or supplied residence names, notes and purchased status, base cost separate from percentage/roommates, starting-money dice/multiplier, canonical saved page and alternate-page PDF display.

The original first enabled XML choice (normally Street) is selected even though the list is sorted. Empty numeric fields disable both acceptance actions and clear price; explicit zero is valid. Missing/disabled/unknown edit identities reject replacement rather than silently choosing another catalog entry. This does not create custom catalog definitions or bypass source filtering.

The pre-migration legacy form passed six assertions/120 price cases across all eight definitions. Thirteen current model assertions retain those cases and cover immutable drafts, names, incomplete/zero values and identity/source rejection. `/tmp/chummer-basic-lifestyle-smoke.log` passed all 399/both Foundry/full smoke (24320 exit0). Final default/name refinements passed full headless checks in `/tmp/chummer-basic-lifestyle-final.log` (14240 exit0). The final build and strengthened native clean/dirty assertions compile with zero warnings/errors.

Twenty-two native assertions cover inputs, two exact synthetic owner-theme images, modal transitions and all four actual add/edit/cancel/persistence paths, including GUID/month/name preservation. Cancellation must leave a clean editor; accepted edits must mark it dirty. **Native behavior and any dirty-state defect remain unproven.**

## Local batch: nexus builder

Six integer fields retain Processor 10..50/default 10; System/Response/Firewall 1..10/default 1; Signal 1..10/default 3; Persona 3..50/default 3. System raises Persona's minimum to 3×System; lowering it preserves an already higher value. An empty System retains the last valid Persona bound while blocking acceptance. Every incomplete draft clears stale price and prevents construction.

NexusConfiguration owns calculation and Gear construction. Only OK creates one parent/five-module tree. All four carried/vehicle creation/career callers require OK/disposal. Preserve their charging, restriction multipliers and AddGear/AddVehicleGear undo. Career carried selection still hides Free while charging normally; career vehicle selection exposes Free. Free stores zero parent cost and retains module availability.

The representation remains localized Nexus/Processor naming, parent category Nexus/source UN/page 50/availability0, and five zero-cost Nexus Module children ordered Response, Signal, System, Firewall, Persona. Ratings stay in their existing names; no structured save fields are added. The legacy Response>6 component cost is zero because its branch multiplies a zero accumulator. Baselines characterize that behavior; they do not establish a rulebook price. Verify Unwired p50 separately before a price correction.

Seven legacy assertions/120 tier-boundary cases passed before extraction; eleven current model assertions retain them plus incomplete/clipping/free boundaries. `/tmp/chummer-nexus-native-smoke.log` passed all 399 fixtures, both Foundry targets and full headless checks (43628 exit0). Final module-persistence assertions compile with zero warnings/errors in `/tmp/chummer-nexus-persistence-checks-build.log` (28028 exit0). Thirty-two compiled native assertions cover controls, two synthetic theme images and all four real purchase/cancel/funds/free/undo/persistence paths. **Native acceptance remains pending.**

## Required acceptance before publishing this batch

The combined local workflow requires at least519 native checks on each platform (Mac has seven additional projection assertions), all 399 fixtures/full smoke, both Foundry contracts, dependency audit and Windows preview/PDF. Confirm the exact tested source/integration tree, inspect any failures and resolve the required-price/dirty-state baselines from runtime evidence. Inspect and hash-verify the eight new exact synthetic PNGs per platform: four modification, two lifestyle and two nexus images. Never broaden uploads to arbitrary character or diagnostic files.

Direct startup and a normally built/signed temporary macOS app both stopped before any window with render-timer−6661. Terminal diagnostics are preserved under `/tmp/chummer-mod-app-checks/native-bulk-user/logs/diagnostics.jsonl`; no native pass is inferred, no graphics fallback was added, and this does not correlate the user's earlier wake crash. The stopped temporary app/publish copies were removed; logs/metadata and installed/running applications were preserved. Repeating the unchanged startup attempt is not verification.

Source safeguards, exact .chum schema/UTF16, original files, settings and source/PDF associations remain required. Finish the coherent batch under the user's 4–6 hour publication cadence, then verify Nightly source/version/assets/archive/retained checksums while leaving Stable unchanged. See [SECTION_MODERNIZATION.md](SECTION_MODERNIZATION.md).

## Retained lifestyle fields

Review reproduced a local replacement defect: an accepted basic lifestyle edit dropped existing notes and purchased status. `/tmp/chummer-lifestyle-retained-fields-baseline.log` fails the retained-field invariant (54163 exit1). The immutable selection now captures both values from the source or supplied new target and applies them only after acceptance. This does not expose new controls, change price formulas or alter the saved schema. Native add/edit/cancel/persistence assertions now also require both fields to survive. This is a correction to the unpublished local draft; no claim about a released fix is made.

The retained-field correction passes the full headless suite in `/tmp/chummer-lifestyle-retained-fields-fixed.log` (57830 exit0), with zero warnings/errors in its build. The existing399-fixture result remains the preceding nexus integration check; final combined-head platform CI must repeat compatibility before publication. Native lifestyle cancel/edit/persistence assertions include both fields but remain unrun.

## Next candidate: critter-power catalog (legacy characterization only)

The shipping picker still uses its original category/tree UI and one integer rating field, minimum1/maximum100/default1. Unrated selections return0; rated selections use the entered value. Creation and career callers insert canonical CritterPower objects, retain their empty-GUID cancellation guard, and own tree grouping, updates, dirty state and Add More. Career also recomputes Possession command availability. A migration must retain these caller responsibilities and use explicit OK/disposal.

Category availability depends on optionalpowers, Free Spirit, Toxic Critters, sprite/A.I./technocritter/protosapient categories, IsFreeSprite and Shapeshifter identity. Optional selections combine the metatype list with deleted innate powers and physical-presence alternatives. Inspect that full policy before extraction; a simple category-equality filter would lose behavior. Free Spirit point costs are fractional doubles parsed with the configured culture and must remain distinct from integer power rating. Existing source/page and alternate-page links must survive. The old category queries do not explicitly apply the standard enabled-book predicate; record and verify source behavior before selecting a migration policy rather than silently treating the old tree as already source-filtered.

`CritterPowerPromptChecks` characterizes the unchanged legacy dialog using isolated Human/Free Spirit models, exact weakness identities, all optional canonical identities, rating bounds/default and accepted Accident cost0.5. It restores the remembered static category. This is preparation for a later bounded migration, not a retired-field or native-acceptance claim; the local numeric baseline remains117.

`CritterPowerEligibility` is now an extracted policy awaiting UI integration. Before replacing the old handler, nine comparisons against that unchanged handler passed: every available Human/Free Spirit category plus an owned Materialization case. `/tmp/chummer-critter-policy-final.log` passes the full headless suite (80688 exit0), and its build has zero errors. An initial extraction used unique-name lookup and failed because Binding, Engulf and Spirit Pact have duplicate catalog names; the corrected policy preserves the old first-match lookup. Optional-list duplicates and the existing physical-presence suppression branch are retained, without a rules correction or source-filter change. The old shipping dialog remains active until the native catalog and actual caller transitions are verified.

## Creation attributes batch in progress

User requests making related control changes first and testing them as a group. The next combined unit is all eleven creation Attributes plus starting-money allocation; do not run a full suite after each individual spinner edit. Use focused baselines for risky rules and perform combined build/headless/native/platform acceptance once the section is coherent. Existing pending catalog/menu work remains preserved.

The initial preparation removes presentation-tree enumeration from the three creation rule/calculation paths. `CreationAttributeInputs` explicitly owns the same eleven references in their former panel order, retaining tooltip order and all existing calculations. This preparation established the controller boundary used by the native section below. The visible inputs are now standard Avalonia controls; legacy declarations remain detached adapters. No numeric declaration is retired, and exact-head native/platform acceptance remains pending for the whole batch.

### Native creation section: local implementation

`CreationInputSection` now presents all eleven attribute inputs in a standard Avalonia grid/ScrollViewer over the fixed, non-scrolling Common-tab attribute region, plus one native money-allocation row. This is a bounded section, not a global overlay. Detached legacy inputs and labels temporarily remain controller adapters: their existing limit/accounting/value events and dynamic captions feed the native controls, but they are not drawn. Explicit owned-input enumeration keeps all three rule/calculation paths independent of the visual parent. The editor disposes the detached adapter objects and unsubscribes their events. No saved fields or rules are changed; the declaration count remains117 because those adapter declarations still exist.

Synchronization suppresses feedback while applying range/value updates, then reads back the controller-approved result after user changes. Cleared inputs do not commit a replacement; leaving an incomplete field restores its last accepted controller value. Money retains decimal model values and existing caps/conversions; its legacy display precision is preserved. Eight compiled native assertions cover twelve maintained controls, detached adapter ownership, loading/ranges, incomplete input, actual attribute/dirty handling, decimal allocation, range clipping and unchanged save round trip. Combined minimum is527 native checks per OS, pending execution.

The full creation batch builds with zero warnings/errors (`/tmp/chummer-creation-batch-checks-build.log`) and passes the full headless suite without repeating the unchanged399-fixture loop (`/tmp/chummer-creation-batch.log`,54578 exit0). Safeguards/diff checks pass. Headless validation exercises the controller boundary, not native rendering. Before publication, add/verify actual BP/Karma budget/max-rule transitions and native focus/keyboard/tab/modal/disposal behavior, light/dark/scaled synthetic visuals and layout fit, and perform exact-head both-platform/full399 acceptance. This is not yet platform-accepted or published.

Creation visual acceptance now has two explicit synthetic PNGs (`native-creation-inputs-dark.png`, `native-creation-inputs-light.png`) at2× rendering scale, captured from the CI-generated creation.chum fixture through the actual main/editor window. Only those exact names are added to success uploads. Two theme/layout assertions require visible native controls to have measured bounds. Local minimum557 (previous555+2), compiled in `/tmp/chummer-creation-visual-checks-build.log` with only10existing vendor warnings/0errors. Current running crash-fix CI39208122659/head082bf still tests555 and does not contain these local additions; new exact-head acceptance remains required. Review all resulting visuals for readability, clipping, money-row fit and native host/tab behavior rather than inferring layout from measured bounds alone.

### Corrected notes-run acceptance and next lifestyle failure

Corrected-head Windows CI39208122659/082bf passed all28 repeated visible contact-notes assertions, then failed the accepted career basic-lifestyle replacement invariant (415checks completed). Artifact12166647512 SHA256fd70a98bf747bfb2afd699e15f9413d2fb166fd334bee9a65ddd8cafd5a5ffc2 was verified before reading its report. This is contact-notes regression evidence on Windows, not an overall green batch or a released fix. The career basic edit branch replaced the lifestyle and refreshed values without marking the editor dirty; the local correction now marks only an accepted basic replacement dirty and updates the title, leaving cancellation untouched. The existing invariant remains required in the next exact-head native run.

Four additional grouped creation checks now compile: actual one-primary-at-maximum rejection with its notice, Ignore Rules acceptance, half-BP primary-budget correction and Karma allocation preview using the existing conversion/improvement accounting. They restore the shared budget option after the isolated test. Combined LOCALminimum561 (=555+2visual+4rule), not yet executed; remote082bf still requires555. `/tmp/chummer-creation-rule-checks-build.log` passes with10existing vendor warnings/0errors. No rule formulas changed.

### Creation lifecycle and visible presenter correction

A local isolated-control baseline `/tmp/chummer-creation-lifecycle-baseline.log` failed because a native value event still reached detached controller state after section disposal (78670exit1). Native value/focus handlers are now named, disconnected on disposal and guarded against disposed sections. The before/after lifecycle checks pass in `/tmp/chummer-creation-lifecycle-fixed.log` (99727exit0). Six further compiled native assertions cover invalid-draft focus restoration, keyboard stepping, Common-tab hide/show, modal block/resume and stale changes after the actual editor closes. LOCALminimum567; these are not yet native-accepted.

CI39209056300/27d21 passes the career lifestyle correction and all28 contact-note assertions on both platforms, then fails the creation Dark measured-layout gate: Mac426checks, Windows419. Mac artifact12167515511 SHA256e398b183f6f2f28ad8d5a7cceae81bff5ac96605d0dd73d700d77e359b338ccb and Windows12166991589 SHA256f02f127019f33500d937d5e23f0947046812a9d2d73c021d26ed9396e470decd were verified. The exact Mac synthetic capture visibly lacks the attributes/money inputs. Source inspection identifies the section attached through the document's unshown backend. No assertion was weakened.

The correction is scoped to CreationInputSection: initialize after document Load/MDI assignment, attach only its two bounded native regions to the actual presenting form, map positions through existing PointToScreen/PointToClient, and follow Common-tab/ancestor/active-document layout and visibility. Editor disposal detaches both regions and subscriptions. Shared NativeControlHost infrastructure is unchanged. A broader shared-host change was rejected by automatic review; this narrower implementation was accepted. Combined `/tmp/chummer-creation-presenter-build.log` builds0warnings/errors and `/tmp/chummer-creation-presenter.log` full headless passes6173exit0. Exact-head native geometry, actual clipping/focus/ownership and visual acceptance are still required before merge or publication.

Seven additional headless-hosted presentation checks now verify that both creation regions belong to the visible presenting form, align with document screen coordinates, follow ancestor movement, hide with Common content, hide/restore when switching documents and are removed on section disposal. `/tmp/chummer-creation-placement-checks.log` passes full headless (52099exit0); its build has10existing vendor warnings/0errors. These exercise actual hosted-form/control APIs and the creation-only integration, but do not prove native measurement or rendering. No production change and no native-count increase; next exact-head native minimum remains567.

A disabled-input callback baseline failed in `/tmp/chummer-disabled-creation-baseline.log` (62751exit1): a queued native value change still wrote controller state after that input was disabled. The creation-only binding now restores controller state instead of accepting such an event. Three grouped headless assertions verify rejection/restoration, legitimate controller refresh while disabled (without feedback writes), and normal edits after re-enable. `/tmp/chummer-disabled-creation-fixed.log` passes full headless63106exit0, with0warnings/errors in its build. One matching actual-native disabled-attribute assertion is compiled and pending; LOCALminimum568, remote27d21 remains561. No rules or field limits changed and no GitHub update was attempted during the cadence hold.

### Creation input domain and column review (local)

Native Base, augmented and metatype-limit headings now share the data grid with their corresponding values; augmented values precede metatype limits. Synthetic headless column/caption checks passed in `/tmp/chummer-creation-column-checks.log`. This is not a platform visual-acceptance claim.

A synthetic fractional attribute edit reproduced an invalid controller write in `/tmp/chummer-creation-integer-baseline.log`. Attribute inputs now use integer parsing and reject fractional queued/programmatic changes, restoring the approved controller value. Money allocation retains decimal parsing and values; the regression explicitly checks 1.5 allocation. This does not change attribute rules or explain the unreproduced Gneiss BP report. Combined headless verification is recorded in `/tmp/chummer-creation-integer-fixed.log`; native and exact-head platform acceptance remain pending before publication.

The actual-native creation suite now also requires fractional attribute rejection to preserve the complete serialized character snapshot and editor dirty state. This assertion is compiled for the next exact-head platform run, not claimed passed locally. The combined native minimum is569; the remote draft remains at its earlier561 minimum until the permitted batch update.

### Next creation section: skills and skill groups

`panActiveSkills`, `panSkillGroups` and `panKnowledgeSkills` are scrolling panels. Their replacement needs coherent native scrolling, clipping, keyboard navigation and tab/modal ownership rather than per-row overlays copied from the fixed Attributes section. Owner handlers currently enumerate panel controls for group membership, accounting and refresh; retain an explicit controller boundary during migration.

Six `CreationSkillInputChecks` assertions characterize both existing rating controls together: model update precedes the owner event, group break clears/disables rating, restoration permits editing, and skill maximum refresh clips the existing rating. Programmatic out-of-range legacy group writes throw without mutating model state or firing an owner event; typed/spin paths constrain separately. The initial test incorrectly expected setter clipping and was corrected to the documented existing rejection contract; no production rule change was made. Full headless `/tmp/chummer-creation-skills-baseline-final.log` passed, build0warnings/errors. These are pre-migration checks, not native skill acceptance or retired declarations.
