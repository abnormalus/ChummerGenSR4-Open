# Installed vehicle sensor transitions

## Owner and callers

The original `frmCreate.ChangeVehicleSensor` and `frmCareer.ChangeVehicleSensor` contained identical model mutation and tree-label code. Their wrappers now delegate mutation to `VehicleSensorTransition` through `VehicleSensorCatalog` and retain only their canonical tree-label update. Creation calls it when adding/removing an Improved Sensor modification. Career also calls it during vehicle-mod sale and purchase undo. The surrounding commands retain modification removal, expense accounting, dirty state and summary refresh; this method itself does not own those operations.

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

The transformation is now separate from label projection, with direction and a replacement factory as explicit inputs and a changed-sensor/null result. Both duplicated bodies have been removed; all six caller sequences remain unchanged. Completion still requires current-source hosted native/image acceptance and the documented caller/failure coverage. No persistence fields or new owned sensor records are required for a size transition.

## Shared transformation prepared after the recorded baseline

`VehicleSensorTransition.Apply(vehicle, increase, createReplacement)` owns size ordering and the eight-field in-place mutation. The replacement factory is an explicit input; the model component reads no controls, catalog files, global cache or expense state. It returns the changed existing Gear, or null for no match/endpoint. Factory failures propagate before those field writes; factory side effects are not rolled back. Identity, children, notes, max rating and collection ownership remain outside the copied field set.

`VehicleSensorCatalog` is the retained adapter. It loads `gear.xml` before transition lookup even for a no-op, preserving loader-error ordering, then uses the existing Gear.Create defaults with temporary TreeNode/weapon lists. That legacy constructor still owns custom definition parsing and any prompts/improvements. Remove the adapter’s control-shaped construction only after a separate model-only constructor migration proves those behaviors. Missing definitions still reach the original constructor’s rejection; no replacement definition is invented.

Both editor methods now contain only the shared call and canonical tree-label update. The latter deliberately follows model mutation, preserving the characterized missing-node failure. Source comparison confirms everything outside the two replaced methods—including all six caller sequences—is unchanged. The old unused placeholder Gear allocations are gone; the constructor itself only assigns a new GUID and its owner.

The original90 editor expectations are unchanged. Five additional independent contracts cover fixed custom constructor values, missing-definition rejection, factory exception identity/no premature sensor writes, endpoint short-circuit without construction, and unknown-entry skipping/custom replacement fields. The separated implementation builds, passes source safeguards and the complete headless suite (198.2s): all90 unchanged editor assertions,5 additional contracts,399 file/memory round trips,both Foundry targets and shutdown. Affected native acceptance remains pending. The earlier192.6s baseline does not establish the new source’s acceptance.

`NativeVehicleSensorChecks` reuses the shown Dodge Scoot fixture from the movement checks rather than opening extra editors. Sixteen assertions cover creation/career, both themes and increase/decrease: fixed names/capacities/signals, canonical node and selected detail labels, identity/children/funds/ledger/dirty preservation, and the complete character snapshot after the inverse transition. Four synthetic captures show the increased sensor, one per editor/theme. The required native minimum rises by16 to1069; current-source hosted execution and88-frame review perOS remain pending. These checks drive the transition and selection; they do not simulate every purchase/sale dialog or claim rollback of custom factory side effects.
