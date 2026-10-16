# Shared native catalog pickers

Weapon, armor and weapon-accessory selection use one native selection framework. The item-specific model supplies established calculations and a small window configuration lists detail fields. There is no new dependency or parallel legacy selection screen in the desktop application.

## Ownership and extension points

`CatalogPickerModel<TDetails>` owns category browsing, literal cross-category search, enabled-source filtering, ordered selection notifications, markup/free options, acceptance validation and user-visible catalog errors. `CatalogCategory`, `CatalogEntry` and `CatalogSelection` carry name and category together, retaining identity for duplicate item names. Search is a string comparison, not user-interpolated XPath. Source settings are queried during refresh and rechecked at acceptance.

A derived model supplies its XML collection path, optional search exclusions, details calculation and remembered category. Its constructor calls Refresh after initialization; the base constructor never invokes item-specific calculations. Separate static category memories avoid changing one picker's initial category by using another. Weapon search continues to exclude Cyberware and Gear categories; armor search covers its catalog categories.

`CatalogPickerWindow<TModel,TDetails>` owns standard Avalonia layout, wrapping rows with inset rounded neutral selection, cumulative keyboard navigation, bindings, source links, resizing and acceptance/cancellation. `ICatalogPickerDetails` exposes the source link shared by all pickers; the remaining detail fields are configured by the item-specific window. Add screen-specific behavior through a bounded extension rather than copying the whole window. Keep item rules and inventory changes out of this class.

Replacing ItemsSource can synchronously clear a two-way SelectedItem binding. The model ignores selection feedback only during that replacement and publishes its intended selection afterward. Detail updates never republish ItemsSource. Preserve this ordering when extending the model.

`frmSelectWeapon`, `frmSelectArmor` and `frmSelectWeaponAccessory` retain the existing caller contract without hidden compatibility forms. Their preferred entry point is asynchronous. Existing synchronous creation/career handlers use the established backend modal loop until command migration reaches those handlers. Both resolve the accepted name/category exactly and dispose their dialog lifetime. `NativeDialogHost` handles owner availability, diagnostics and Quit cleanup.

## Armor behavior preserved

The armor preview creates an unattached core Armor with preview mode enabled. It builds included modifications/gear, skips price/detail prompts and improvements, and reads core TotalAvail. Existing included-item availability policy remains unchanged; an old picker comment suggesting all built-in Chemical Seals add availability did not describe the current core implementation. No rules were changed to fit that comment.

Ballistic/impact fields retain the catalog values. Capacity retains the previous preview formula when no explicit capacity exists: at least 6 or the higher protection value times 1.5, rounded upward. Fixed costs apply markup; variable costs display their existing range and use the minimum for the availability test. The actual purchase retains its existing price prompt and cost rules. Free purchase clears preview cost and is returned to career callers. Character schema, encoding and inventory calculations are unchanged.

Malformed definitions clear stale details, display a localized error and disable acceptance. Generic picker strings use English fallback until translations are supplied. Historical weapon-only localization keys remain for translation compatibility.

## Weapon accessory behavior preserved

Accessories use the same list, search, detail bindings, source links and dialog lifetime without a category selector. `CatalogPickerModel` supports uncategorized catalogs explicitly and applies an item-eligibility hook during both browsing and search. `AccessoryPickerModel` owns mount eligibility: mount-free entries are always eligible; mounted entries require at least one exact mount token supported by the parent weapon. Melee weapons retain mount-free choices. Search cannot bypass eligibility or enabled sourcebooks.

The adapter captures AllowedMounts and WeaponCost at Show time because existing callers set these after construction. It returns the canonical name, original mount alternatives, markup, free-purchase flag and Add More intent. The four carried/vehicle creation/career callers retain their purchasing and attachment paths and dispose the adapter. Selected definitions are resolved by literal catalog identity rather than constructing XPath from a selected name.

Preview pricing retains the original Weapon Cost token, integer rounding before markup and final integer rounding. Purchase rules remain responsible for weapon accessory multipliers, restricted/forbidden multipliers, charging, expenses and undo. The old picker accepted an accessory-multiplier parameter but never applied it to its preview; this migration preserves that behavior instead of silently changing prices. Source changes are rechecked on acceptance. Empty results and malformed definitions clear details and disable acceptance, replacing the old selection handler's assumption that a node always exists. No saved accessory fields change.

## Verification

WeaponPickerChecks, ArmorPickerChecks and AccessoryPickerChecks run in the headless smoke suite. They cover source changes, literal quoted searches, established calculations, price ranges, purchase options, malformed definitions and unchanged character snapshots. Weapon duplicate-name coverage remains in place.

NativeWeaponPickerChecks, NativeArmorPickerChecks and NativeAccessoryPickerChecks exercise actual framework bindings, empty results, purchase controls, resizing, Add More, title-bar cancellation and real creation handlers. Weapon tests also verify cumulative typing and Quit through the shared native host. Accessory checks also cover mount alternatives, mount-free melee choices, weapon-relative prices and malformed definitions; the existing native inventory checks exercise real creation and career accessory purchases. Screenshots record each picker at its smaller supported size, plus armor in the light theme at 2x resolution. Row spacing uses logical units, minimum height and automatic measurement rather than fixed pixel sizing. Selected rows use theme-specific neutral grays and semibold text, with distinct hover/pressed shades. NativeCharacterSwitchChecks covers cross-character Paste refresh in addition to toolbar ownership. CI requires at least 71 combined native checks on Mac and Windows. Reports include fixed failed-check labels without exposing exception messages or character data.

## Remaining migrations

Armor modifications, gear, cyberware and vehicle pickers still use compatibility controls. Assess their rating controls, nested choices and variable-price behavior before extending this framework. Preserve the established rules and custom-entry behavior. A native numeric/detail prompt and fully asynchronous purchase commands remain later work; migrating a picker does not imply its downstream purchase prompts are all native yet.
