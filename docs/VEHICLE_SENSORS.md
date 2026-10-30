# Installed vehicle sensor transitions

## Existing owner and callers

`frmCreate.ChangeVehicleSensor` and `frmCareer.ChangeVehicleSensor` currently contain identical model mutation and tree-label code. Creation calls it when adding/removing an Improved Sensor modification. Career also calls it during vehicle-mod sale and purchase undo. The surrounding commands retain modification removal, expense accounting, dirty state and summary refresh; this method itself does not own those operations.

The size order is Microdrone, Minidrone, Small Drone, Medium Drone, Large Drone, Vehicle, Extra-Large Vehicle Sensor. Lookup visits vehicle Gear in order and stops at the first recognized name, including a boundary that cannot move. Unknown names are skipped. At the smallest/largest endpoint, a request beyond the range makes no change and does not continue to another sensor.

For an actual transition, the method loads the matching Sensors entry from `gear.xml`, constructs temporary Gear and copies only Name, Rating, Capacity, Signal, Avail, Cost, Source and Page into the existing object. It retains internal identity, children, notes and the vehicle collection. It then finds the existing tree node by internal ID and updates its short display label. The existing path can mutate the model before failing when that node is missing; it is not transactional. Do not silently reorder or repair that behavior during a parity extraction.

## Caller ordering that stays outside the transformation

| Caller | Existing ordering to preserve during a model-only extraction |
|---|---|
| Creation/career `cmdDeleteVehicle_Click` | Confirm removal, lower a recognized Improved Sensor, then handle any Obsolete/Obsolescent retrofit prompt and remove the modification. A later prompt can cancel after the sensor call for a custom definition combining those bonuses; no rollback is supplied by the sensor method. |
| Creation `tsVehicleAddMod_Click` | Create the modification, accept its identity, clamp applicable armor rating, add model/tree ownership and refresh the selected vehicle, then process bonus text/sensor change and mark dirty. |
| Career `tsVehicleAddMod_Click` | Temporarily add the modification, reject capacity/funds failures with removal, calculate markup/restricted costs, debit funds and record canonical purchase undo, add its tree node and refresh, then change the sensor and mark dirty. A sensor failure can follow accounting; do not claim transactionality. |
| Career `tsVehicleSell_Click` | Accept the sale percentage and retain original total vehicle cost, lower the sensor before removing its modification, then calculate the sale from the total-cost difference and credit the expense ledger/funds. Moving the sensor later can alter custom catalog pricing. |
| Career `tsUndoNuyenExpense_Click`, `AddVehicleMod` case | Resolve the modification through the recorded canonical ID, lower the sensor before removing the modification/tree node, then continue the existing expense-undo flow. Keep this distinct from a new purchase or sale command. |

These are source-reviewed boundaries, not additional runtime caller acceptance. The 90 baseline assertions below exercise the transition method itself, including its tree mutation; they do not simulate purchase dialogs, capacity/funds rejection, sale percentages or the entire expense-undo command. Before declaring this section complete, use explicit caller regressions for those paths and verify affected native labels. A refactor should leave these six call sites and their surrounding ordering unchanged; a transactional repair needs a separately stated behavior change and independent expectations.

## Independent baseline

`VehicleSensorChecks` invokes both original editor methods without a replacement policy. Fixed names/capacities/signals/source/pages come from inspected catalog definitions: capacities 1/3/5/6/8/12/30; signals 2/3/4/4/4/5/6; source SR4 page333 except Extra-Large AR page138. Both directions at all seven sizes check the eight copied fields, identity/notes/children/collection preservation and label refresh/no-op. Additional cases cover first-recognized boundary behavior, an unknown name and the missing-node partial mutation. There are 90 assertions across creation/career mode.

All 90 baseline assertions passed against the original creation/career methods in the corrected tagged-tree fixture; the containing full headless suite also passed (192.6 seconds), including all 399 file/memory round trips, both Foundry targets and shutdown. It does not establish a shared transformation or native visual acceptance. Catalog construction retains existing custom data behavior; fixed catalog values here describe the bundled test fixture, not a prohibition on custom definitions. Missing catalog entries and modified custom definitions require additional characterization before declaring the entire boundary covered.

## Replacement boundary

After the active Copy/native verification finishes, separate the model transformation from label projection. Inputs should identify the existing vehicle/character, direction and caller-owned catalog data; output should distinguish an unchanged boundary/no match from a changed sensor. Preserve catalog constructor semantics and field-copy scope, and keep caller accounting outside the transformation. Delete both original implementations only after independent parity, caller checks and affected native acceptance. No persistence fields or new sensor records are required for a size transition.
