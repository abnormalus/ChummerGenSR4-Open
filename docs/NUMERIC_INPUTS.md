# Native numeric input rollout

Use standard Avalonia NumericUpDown for modernized numeric fields. NativeIntegerInput supplies consistent logical sizing, whole-number parsing, step size, formatting and framework range clipping. Existing native catalog markup controls now use this same factory. Decimal fields need an appropriate decimal configuration; never round currency, multipliers or fractional ratings simply to reuse the integer factory.

The shared catalog window supports IRatedCatalogPicker. Item models own rating limits, clamping and calculations; the standard control owns text entry, spin buttons, keyboard behavior and layout. CatalogSelection captures the accepted rating together with the item and purchase options. Notifications publish limits and the model's already-clamped value together. During that publication, the model ignores transient feedback from the control coercing an old value against new limits.

## Completed

Armor-modification selection now uses this native rating editor and the shared catalog picker. Fixed modifications disable rating changes. Search, source links, safe empty/error states, window resizing and cancellation use the same framework as weapon, armor and accessory selection. The former armor-modification form/designer/resources are retired from the shipping desktop application; the historical original source is untouched.

ArmorModPickerModel retains rating-based cost/availability, Armor Cost expressions, FixedValues capacity, the parent's Standard/PerRating/Zero display style, markup and free-purchase options. As before, the availability test uses the marked-up catalog cost even when the display is free; purchase handlers own charging. General modifications plus the parent's allowed categories are eligible, and every category now respects enabled sources. The old query's unparenthesized OR allowed some explicitly matched categories to bypass sources. Selection returns canonical identity/rating; non-rated inventory modifications still receive rating zero through the established purchase handlers. Character schema and encoding do not change.

## Remaining rollout

At this stage, the shipping desktop designers contain 130 legacy NumericUpDown declarations. This is an inventory baseline, not a count of user-visible defects. The largest groups are preferences (62), creation (11), nexus selection (6), career (5), and improvement creation (4). Gear selection has three; other dialogs include vehicles, quantities, dice, calendars and rule-specific inputs.

Replace these during bounded screen migrations, prioritizing gear/cyberware/vehicle ratings and frequently edited character attributes. Preserve minimum/maximum values, integer versus decimal precision, increments, disabled/read-only states, cost/rule notifications and cancel behavior. Include direct typing, both spin directions, keyboard changes, limits, item switches, empty results, scaling and real purchase/undo checks as relevant. Use the shared factory for new integer controls instead of copying configuration.

The compatibility backend supports native overlays, but globally overlaying every spinner would introduce focus, clipping, scrolling and lifetime integration work across unmigrated screens. Keep that as an explicitly tested bridge option if a specific screen needs it sooner; the preferred path is native control/layout migration. No global replacement is claimed in this stage.

## Verification and framework references

ArmorModPickerChecks verifies limits, cost, FixedValues and parent capacity styles, fixed-item state, source revalidation, all 62 catalog definitions at maximum rating and unchanged character data. NativeArmorModPickerChecks exercises actual text entry, the framework increase button, keyboard decrease, fixed-item transitions, empty results, Add More/cancellation, a 2x screenshot and creation/career purchase/cancel behavior. CI requires at least 95 combined native checks on both platforms, alongside all 399 saved-character fixtures.

Framework behavior was checked against the pinned Avalonia 12.1.1 source: [NumericUpDown](https://github.com/AvaloniaUI/Avalonia/blob/12.1.1/src/Avalonia.Controls/NumericUpDown/NumericUpDown.cs) and [ButtonSpinner](https://github.com/AvaloniaUI/Avalonia/blob/12.1.1/src/Avalonia.Controls/ButtonSpinner.cs). Keyboard events originate from the template's text box and bubble through its spinner; tests must exercise that actual route. Do not add parallel custom stepping logic.
