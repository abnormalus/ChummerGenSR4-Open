# Legacy-compatible accessory quantities

The user chose exact legacy-compatible character files. The closed optional-quantity prototype (PR #8) is reference only: do not merge its persistence changes. Ordinary `accessory` records, their GUIDs and existing per-unit expense undo remain authoritative. No quantity field, metadata or schema migration is used.

## Purchasing

The shared catalog picker offers a standard Avalonia integer control for Spare Clips, Speed Loader, Ammo Drum/Box and the existing 2050 variants. Other accessories remain single-unit purchases. The UI permits 1–1000 units per operation; this bounds work rather than changing an inventory rule. Selection rechecks the existing source and mount context, and cancellation never commits a plan.

`WeaponAccessoryBatch.Prepare` builds an unattached purchase plan. Every unit has its own ordinary accessory and tree node. Incremental weapon cost, markup rounding and restricted/forbidden multipliers follow the current purchase order. Temporary attachments used for calculation are removed in `finally`, including malformed costs and overflow. Preparing a plan does not spend money, add expenses or commit inventory.

`TryPurchase` checks the entire batch's affordability before committing any unit. It serves creation/career and carried/vehicle callers. Career purchases retain one ordinary expense and undo identity per unit, including existing per-unit rounding; free purchases retain existing accounting behavior. The inventory history sees the batch as one operation, so inventory Undo can restore the whole purchase. Expense Undo continues to undo one unit. Fixed structural diagnostics record acceptance, insufficient funds and preparation failure without item names, GUIDs, quantities or prices.

## Grouped presentation

`Groups` is a read-only projection. It compares existing serialized fields except the accessory's own GUID, so notes, installed/included state, discounts, catalog version and nested gear remain distinct. No record is merged, rewritten or removed. Child identities deliberately remain significant; grouping never discards independently customized add-ons.

`AccessoryGroupViews` is a temporary compatibility-tree adapter, not a new tree renderer. Equivalent units appear in a collapsed row such as `Spare Clips ×3`. Expand it to select, edit or delete an individual ordinary unit. Headers have no inventory identity or item menu; selecting one clears item details and Delete does nothing. Unit menus and GUIDs remain unchanged. Customizing a unit separates it from its group; restoring equivalent persisted values groups it again. Reloading and inventory undo derive groups from ordinary model records.

The adapter runs on the UI thread, guards reentry, preserves unit selection/expanded groups where possible and stores per-node state weakly. It removes stale views only for known accessory identities and does not invent missing item menus. Model ownership, costs and rules remain in the existing equipment objects.

Grouped-unit deletion resolves the accessory's canonical parent rather than inferring it from its extra presentation level. Career accessory expense undo likewise resolves the ordinary GUID across carried, underbarrel and mounted weapons, then refreshes the view. Closing its confirmation now requires explicit Yes to proceed; Cancel leaves money and units intact.

## Verification

`AccessoryBatchChecks` covers preparation without mutation, distinct identities, incremental rounding, read-only grouping, customization distinctions, ordinary-record round trips, bounds, cleanup after cost failure and one-action inventory undo. Native picker checks exercise the standard quantity input and single-unit fallback. Native inventory checks drive all four purchase paths, grouped headers, customized-unit separation, carried/vehicle unit deletion, per-unit expense undo, insufficient funds and cancellation. CI requires at least 178 combined native checks on both platforms, alongside all 399 saved-character fixtures.

Shared ammunition views are a separate queued feature. Accessory groups do not clone ammunition or change loading/consumption semantics.
