# Context-aware inventory actions

## Commlink add-ons

The gear picker has an optional per-instance InitialCategory. PreferCategoryForParent selects Commlink Operating System when the parent is a Commlink. Creation/career callers apply it to carried, armor-mounted and other nested gear contexts. It overrides remembered category only when that category survives the normal allowed-category and enabled-source filtering. Otherwise the existing fallback remains. Other categories remain available; this is a starting location, not a forced restriction.

## Melee accessories

WeaponAccessoryPolicy is shared by creation/career and carried/vehicle accessory handlers. Ordinary melee weapons can open accessory selection despite their legacy allowaccessory=false flag. Cyberware weapons remain excluded; ranged weapons retain their catalog permission. Existing mount filtering is unchanged, so a melee weapon with no mounting points offers mount-free accessories rather than rail-mounted parts. Existing purchase costs, included gear and character serialization remain authoritative. Accessory dialogs now receive an explicit editor owner.

## Buy Ammunition

WeaponAmmunitionMenu adds a localized right-click action to carried and vehicle weapon menus. Visibility and the click handler both verify that the selected item is a ranged weapon requiring ammunition. The command reads Weapon.AmmoCategory, including explicit ammunition-category overrides, rather than guessing from the display name or raw weapon category.

Both editors reuse their existing gear purchase flow, with the Ammunition category and canonical weapon class supplied. The previous gear selection is restored afterward, and purchases target root inventory rather than attaching to whichever gear was selected earlier. Add More repeats for the same weapon class. The existing career Buy Ammo button delegates to the same path. Pricing, quantities, career expenses and inventory undo continue through their established handlers.

AmmunitionSelectionPolicy centralizes the previous spare-clip family and optional Stick-n-Shock exclusions. The gear picker's category list and search results both enforce this context alongside enabled-source filtering. Unrelated gear browsing keeps its existing behavior. This does not rewrite ammunition rules; review catalog-specific compatibility separately when needed.

Gear.Create assigns a supplied known ammunition category directly, without opening a one-choice category prompt. Ordinary ammo purchases without context retain the existing category picker. The canonical category remains in the same Extra character field, with no format migration.

## Modal cancellation correction

Native testing reproduced a shared compatibility bug: closing a dialog without an explicit button returned Cancel but left the DialogResult property at None. Legacy callers checking that property could proceed with an empty catalog selection. Form.CompleteClose now assigns Cancel to the backing field before completing the task, so both caller styles agree. Explicit OK/Cancel results and canceled closing events remain unchanged; nonmodal forms are unaffected.

## Verification

InventoryEnhancementChecks verifies commlink defaults/fallbacks, ammo family filtering in browsing and search, canonical rifle/pistol ammo fields, melee accessory round trips and modal result consistency. NativeInventoryEnhancementChecks drives real creation and career handlers: commlink cancellation, melee accessory purchasing, the right-click ammo menu, correct inventory destination and career charge, and purchase cancellation. Combined native CI coverage requires at least 83 checks on Mac and Windows. The full saved-character suite continues to verify compatibility.

## Personalized Grip identity and bonuses

Weapon modification selection displays one **Personalized Grip** entry. The attached weapon type chooses the original catalog definition: `Personalized Grip, Melee` supplies the established +1 melee dice pool; `Personalized Grip` supplies +1 recoil compensation for ranged weapons. Creation, career, carried and vehicle-mounted weapon callers all supply this context. Sourcebook filtering remains in effect. Search matches the display label and original catalog name using literal text.

This is a presentation alias, not a file-format migration. WeaponMod.Name, saved names, recoil and dice-pool fields retain their legacy values, allowing older Chummer versions to load new purchases. Existing modifications are displayed with the common label without rewriting their stored definitions or bonuses. PersonalizedGripChecks covers both bonuses, source filtering, quoted search and snapshot round trips; native checks purchase both variants in creation and career editors.

## Catalog identity and search safety

Accessory purchase handlers resolve the parent weapon from its canonical model name and category, never its displayed tree label. Vehicle weapon labels can contain translations or custom names; looking those labels up as catalog names previously rejected valid accessory purchases. Creation/career and carried/vehicle paths share WeaponAccessoryPolicy.FindDefinition. Purchase charging, expense logs and cancellation remain in their established handlers.

Gear search now shares literal localized-name matching with native catalog pickers through CatalogSearch. Typed quotes/operators are ordinary text rather than XPath expressions. Existing sourcebook, category and ammunition filters remain in place. When a search has no selected result, the picker clears previous cost/capacity/source and device details, disables acceptance and guards direct acceptance against a null selection; selecting a valid result restores purchase controls. This is a bounded safety fix, not the full gear-screen migration. The shared capacity/device eligibility policy below is now in place; remaining legacy identity lookups should be reviewed during the full gear migration.

Regression coverage includes the reproduced quoted-search failure, recovery after empty results and canonical parent-weapon lookup. Native checks exercise quoted search during real ammo buying, and decorated vehicle weapon accessory purchases/cancellation in creation and career modes, including career funds.

## Shared gear eligibility

`GearSelectionContext` is an immutable, presentation-independent policy for the existing capacity flags and parent device limits. `frmSelectGear.EligibleDefinitions` combines it with enabled sourcebooks and the established ammo-family policy. Category discovery, browsing, search and final confirmation use this common gate. Category visibility/explicit allowlists remain separate; a typed query never relaxes purchase constraints. Category discovery materializes eligible category names once instead of repeating the entire catalog query for each category.

The reproduced defect allowed a rating-3 commlink to search for Response 1 despite browsing excluding it. Search also skipped positive/negative capacity constraints. Hardware upgrades retain the old greater-than-current and at-most-current-plus-two comparisons, including the existing custom-commlink exception when both caller limits are 2. OS upgrades retain the existing greater-than-current Firewall/System test, without adding a new upper limit. Device categories retain precedence over capacity flags. Armor capacity requires the same bracket marker as browsing; otherwise positive capacity excludes brackets and installed capacity requires them. Missing numeric values compare false as in the former XPath predicates.

Confirmation rechecks sources and context. If the selected definition is no longer eligible, the list refreshes and the dialog remains open; it never buys an automatically selected replacement. No character fields, cost calculations, source associations or core installation rules change.

`GearEligibilityChecks` covers lower/upper hardware bounds, OS upgrades, positive/installed/armor capacity, custom-device exceptions, stale context/source rejection and unchanged character state. `NativeGearEligibilityChecks` exercises incompatible search results and eligible confirmation through actual owned dialogs on Mac and Windows. This policy is reusable when the remaining gear UI moves to native controls.
