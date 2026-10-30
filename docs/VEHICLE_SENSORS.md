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

At the recorded pre-extraction baseline, `VehicleSensorChecks` invoked both original editor methods without a replacement policy. The same independent expectations now run against the thin editor wrappers. Fixed names/capacities/signals/source/pages come from inspected catalog definitions: capacities 1/3/5/6/8/12/30; signals 2/3/4/4/4/5/6; source SR4 page333 except Extra-Large AR page138. Both directions at all seven sizes check the eight copied fields, identity/notes/children/collection preservation and label refresh/no-op. Additional cases cover first-recognized boundary behavior, an unknown name and the missing-node partial mutation. There are 90 assertions across creation/career mode.

All 90 baseline assertions passed against the original creation/career methods in the corrected tagged-tree fixture; the containing full headless suite also passed (192.6 seconds), including all 399 file/memory round trips, both Foundry targets and shutdown. It does not establish a shared transformation or native visual acceptance. Catalog construction retains existing custom data behavior; fixed catalog values here describe the bundled test fixture, not a prohibition on custom definitions. Missing catalog entries and modified custom definitions require additional characterization before declaring the entire boundary covered.

## Replacement boundary

The transformation is now separate from label projection, with direction and a replacement factory as explicit inputs and a changed-sensor/null result. Both duplicated bodies have been removed; all six caller sequences remain unchanged. Completion still requires current-source hosted native/image acceptance and the documented caller/failure coverage. No persistence fields or new owned sensor records are required for a size transition.

## Shared transformation prepared after the recorded baseline

`VehicleSensorTransition.Apply(vehicle, increase, createReplacement)` owns size ordering and the eight-field in-place mutation. The replacement factory is an explicit input; the model component reads no controls, catalog files, global cache or expense state. It returns the changed existing Gear, or null for no match/endpoint. Factory failures propagate before those field writes; factory side effects are not rolled back. Identity, children, notes, max rating and collection ownership remain outside the copied field set.

`VehicleSensorCatalog` is the retained adapter. It loads `gear.xml` before transition lookup even for a no-op, preserving loader-error ordering, then uses the existing Gear.Create defaults with temporary TreeNode/weapon lists. That legacy constructor still owns custom definition parsing and any prompts/improvements. Remove the adapter’s control-shaped construction only after a separate model-only constructor migration proves those behaviors. Missing definitions still reach the original constructor’s rejection; no replacement definition is invented.

Both editor methods now contain only the shared call and canonical tree-label update. The latter deliberately follows model mutation, preserving the characterized missing-node failure. Source comparison confirms everything outside the two replaced methods—including all six caller sequences—is unchanged. The old unused placeholder Gear allocations are gone; the constructor itself only assigns a new GUID and its owner.

The original90 editor expectations are unchanged. Five additional independent contracts cover fixed custom constructor values, missing-definition rejection, factory exception identity/no premature sensor writes, endpoint short-circuit without construction, and unknown-entry skipping/custom replacement fields. The separated implementation builds, passes source safeguards and the complete headless suite (198.2s): all90 unchanged editor assertions,5 additional contracts,399 file/memory round trips,both Foundry targets and shutdown. Affected native acceptance remains pending. The earlier192.6s baseline does not establish the new source’s acceptance.

`NativeVehicleSensorChecks` reuses the shown Dodge Scoot fixture from the movement checks rather than opening extra editors. Sixteen assertions cover creation/career, both themes and increase/decrease: fixed names/capacities/signals, canonical node and selected detail labels, identity/children/funds/ledger/dirty preservation, and the complete character snapshot after the inverse transition. Four synthetic captures show the increased sensor, one per editor/theme. The required native minimum rises by16 to1069; current-source hosted execution and88-frame review perOS remain pending. These checks drive the transition and selection; they do not simulate every purchase/sale dialog or claim rollback of custom factory side effects.


## Hosted fixture correction and evidence at c15558b

The first extracted-source hosted run (`2e03e84`, run41070791776) passed full headless checks on both platforms, then failed before the new native assertions: the movement fixture used `Vehicle.Create(createChildren:false)`, so no bundled sensor existed. The native fixture now explicitly creates and attaches the catalog Vehicle Sensor, following the bundled-gear cost/quantity/max-rating/node setup without importing unrelated vehicle modifications. Product code and assertions are unchanged by this correction.

At `c15558b`, run41072323487, Windows passes all1071 native assertions, full302.7s headless checks, preview/PDF and dependency audit. The Mac full310.0s headless run passes; its hash-verified native report records1078 passing assertions, including all16 sensor assertions, but the process misses the outer300-second exit deadline. Mac CI therefore remains failed despite the complete report; the gate is not waived or increased. The queued documentation-only `dad0771` follow-up has its own platform verification pending.

All88 named synthetic frames perOS at c15558b have review coverage from hash-verified artifacts. Mac has82 exact raw-RGBA transfers from previously reviewed same-name frames plus6 direct inspections (all4 new sensor frames, natural-weapon dark and quality-creation light). Windows has84 exact transfers plus direct inspection of all4 sensor frames. Tree and selected details show Extra-Large Vehicle Sensor, capacity30 and AR138 source across modes/themes. Existing narrow/scrollable/cropped sidebars remain; this is not a general no-clipping finding or acceptance of later source. Visual coverage does not satisfy the failed Mac process-exit gate, and full caller purchase/sale/undo regressions remain outstanding.


## Caller verification follow-up

`VehicleSensorDeletionChecks` now invokes the actual `cmdDeleteVehicle_Click` in both editors with a catalog Improved Sensor Array and tagged vehicle/sensor/modification nodes. Eight assertions pass, and the full containing headless suite passes in217.9s (399 file/memory cases, both Foundry targets and shutdown). Rejecting confirmation preserves the complete snapshot, ownership and clean state. Accepting it lowers Extra-Large to Vehicle Sensor (capacity12/signal5), removes the canonical modification/node, marks dirty and preserves funds/expense count. The initial synthetic modification lacked a valid availability field and failed during selection refresh before assertions; the corrected fixture uses the real catalog constructor.

`NativeSensorTransactionChecks` is prepared and compiles, but remains unexecuted. Its fixed purchase/sale expectations use the catalog cost1000, career25% markup1250 and50% sale500. It drives both actual purchase callers, cancellation, unaffordable purchase, canonical expense undo, sale cancellation/acceptance and snapshot identity. The separate `--check-sensor-transactions` mode shares the native report/cleanup infrastructure and uses its own test-user directory. CI requires process exit within180s, a passing report and at least17 assertions. The original native bulk step is byte-identical, retaining its300s bound and1069 minimum; the new suite is additional required coverage, not a replacement subset. Hosted execution is still required before claiming these caller paths pass.


## Supplemental native startup correction

At `75b3717`, run41080196607, both full headless suites pass (Mac334.6s/Windows307.4s), and both native bulk steps pass (Mac1078/Windows1071). The additional sensor transaction processes hit180s before producing their first progress/report file. Hash-verified diagnostics on both platforms contain only session start, main-window initialization and `character.open`: `frmMain` treated the new verification flag as a character filename, before Program could attach the driver to the shown window.

`Program.IsNativeVerificationArgument` now supplies the common marker recognition to Program and frmMain's file-opening exclusion, including bulk, sensor-transactions and focus-command modes. Ordinary character filename handling and the existing recognized commands remain unchanged. The correction builds successfully; its native transaction results remain unproven until a new hosted run. The180s/min17 gate is unchanged; the failed run establishes no purchase/sale/undo acceptance.
