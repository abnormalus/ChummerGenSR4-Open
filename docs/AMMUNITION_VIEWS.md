# Shared weapon ammunition views

## Ownership and inventory selection

Reserve ammunition remains ordinary `Gear` in the carried or vehicle inventory. Weapon listings must be read-only projections of those same objects. Never add a backend Gear copy per compatible weapon or count a projection toward price, weight, quantity or expenses. `.chum` fields and identifiers remain unchanged.

`WeaponAmmunition` now supplies the same eligibility selection to the two career reload handlers. It returns canonical objects in inventory order, preserving the prior behavior:

- Positive quantity and category `Ammunition` are required.
- Ordinary weapons use exact canonical `Extra == AmmoCategory`.
- Grenade, missile and mortar launchers use their existing canonical name families and existing culture-sensitive prefix comparisons.
- Carried reload sees inventory roots and direct children, including positive child stock inside an ordinary zero-quantity parent. Exhausted Spare Clip stacks are excluded because their child quantity describes capacity per clip rather than remaining loose stock. Deeper traversal is not introduced implicitly.
- Mounted reload sees only that vehicle's root Gear. It does not borrow carried ammunition or recursively open containers.
- The existing Stick-n-Shock preference is applied to carried reload only. The previous mounted handler did not apply it; this extraction preserves that boundary rather than quietly changing rules.
- External-source and ammo-free weapon behavior remains in the existing reload callers, after inventory selection. Owned ammunition is not hidden merely because its catalog sourcebook is later disabled.

Reload already subtracts reserve quantity when transferring rounds into a magazine and returns unused rounds during a subsequent reload. `AmmoRemaining` and `AmmoLoaded` describe each weapon's active magazine slot. Firing reduces loaded rounds only; it must not subtract reserve a second time. The spare-clip full-load path has its own parent-quantity semantics and must be preserved.

`WeaponAmmunitionChecks` verifies canonical identity shared between compatible weapons, depletion, distinct loaded rounds, inventory pool/containment boundaries, class/category matching, launcher families, the carried-only preference boundary, and legacy GUID/magazine persistence. Policy reads must not mutate the inventory or save snapshot.

## Tree integration and command safety

`WeaponInventoryViews` composes accessory grouping and ammunition projections behind a per-tree reentry guard. Each projection carries `AmmunitionRow(WeaponId, GearId)`, deliberately distinct from ordinary inventory GUID tags. Existing eligible rows retain identity and selection; depleted rows clear selection before removal. Captions include the original container where applicable.

Creation/career handlers reject projection rows before legacy tree-depth assumptions. Selection clears weapon details and disables destructive controls; notes, rename, accessory/modification purchase, clipboard, selling, moving, drag targets, reload and firing cannot mistake a reserve row for its parent weapon. Root purchases remain available. Edit reserve stock from its canonical Gear entry. Inventory refresh/undo rebuilds views from canonical objects without adding serialized records.

## Reload regressions and verification

Synthetic career reload checks reproduced two existing accounting defects: a full magazine taken from an ordinary container left reserve unchanged, and an exhausted spare-clip stack remained eligible through its positive per-clip child quantity. Reload now distinguishes an ordinary container from a full spare clip; selection excludes exhausted clip stacks while preserving ordinary container stock. A suspected filtered-tree null reference did not reproduce, so no speculative null-handling change was added.

Native checks exercise carried/underbarrel/vehicle pools, projection selection and queued-command rejection, depletion, actual creation Gear quantity editing, reload/fire, inventory undo, two-subtype reload cancellation, full ordinary containers, exhausted spare clips and two-slot magazine switching. Cancel preserves the complete snapshot and all projections; slot-two firing leaves slot one untouched. Portable checks verify legacy round trips, canonical identities and unchanged serialization/costs during projection.

Local verification passed 253 native checks, all 399 saved-character fixtures/full smoke and 36 release/safeguard tests. Synthetic creation/career screenshots cover both themes at double-resolution rendering. Cross-platform CI and exact-head visual review remain required before publication; local checks alone do not certify Windows. Double-resolution rendering exercises layout/render scale, not every physical multi-monitor DPI configuration. Leave Stable unchanged.
