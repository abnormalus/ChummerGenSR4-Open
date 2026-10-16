# Weapon-modification focus binding

## Identity and compatibility

A purchased `Weapon Focus` modification is equipment on a weapon, not a standalone Gear object. `WeaponFocusBindings` enumerates carried weapons, recursive underbarrel weapons and vehicle weapon collections, grouping by canonical modification GUID. The Foci list labels each entry with its parent weapon and Force. Existing Gear and Stacked Focus representations keep their existing paths.

Binding uses the existing `Focus` record: canonical name `Weapon Focus`, the modification GUID in the legacy `gearid` element and its Force in `rating`. No XML fields, encodings, duplicate Gear or custom metadata are added. Older Chummer readers retain the standard focus record and its BP/Karma calculation, though their old Foci interface does not expose weapon-modification bindings. Do not change serialized field names to match new terminology.

## Accounting and effects

Creation and career share Force/count calculations across displayed Gear, Stacked Focus and weapon modifications. Existing creation Ignore Rules behavior is preserved. BP creation uses the configured per-Force focus cost; Karma creation and career use the configured weapon-focus multiplier. Career confirmation precedes a single expense entry whose undo identity is the canonical modification GUID.

The installed weapon modification remains the sole owner of the existing dice-pool modifier. Binding adds no Improvement or second dice bonus and does not purchase a separate Gear focus or infer a missing monetary price. This change preserves the existing installed-modifier behavior; it does not introduce a new activation/bonding gate for that legacy bonus. Audit any future rule change separately.

`Synchronize` runs at focus-list construction and character-information updates. It updates bound Force after rating changes and removes bindings/UI rows for modifications observed during this session that were subsequently deleted, including parent-weapon deletion. Its weak per-character identity set avoids retaining models and avoids guessing the meaning of unknown references in older saves. It updates only changed labels and never dispatches purchase handlers to rebuild a checked row.

## Confirmations and verification

The shared career Karma-spending confirmation now requires explicit Yes; Cancel/title-bar close cannot authorize an expense. Focus unbinding and Karma-expense undo follow the same rule. A native regression failed before this correction because Cancel spent Karma, then passed with the fix. Disabling the existing confirmation preference still deliberately approves spending without a dialog.

`WeaponFocusBindingChecks` covers list visibility, BP/Karma creation accounting, combined equipment Force totals, save/reopen, no duplicated cost/dice/Gear, rating updates, modification/weapon removal and preservation of unknown references. `NativeWeaponFocusChecks` additionally exercises declined/closed/accepted binding, unbind cancellation, expense-undo cancellation and approval, affordability, Force/count limits and modal cleanup. The checks use synthetic models and preserve source files and preferences. Run the full smoke suite and native Mac/Windows checks before release.
