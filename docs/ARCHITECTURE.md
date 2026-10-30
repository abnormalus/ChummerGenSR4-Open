# Shared desktop architecture

This map describes the shared `Chummer.Mac` source, including remaining legacy boundaries. It is not a claim that all rules have been extracted from forms.

## Runtime and presentation

`Chummer.Mac/code/Program.cs` selects ordinary desktop startup, headless verification, native bulk verification and export commands. The shared desktop uses the vendored Forms compatibility toolkit with its Avalonia backend. `frmMain` owns the document workspace; `frmCreate` and `frmCareer` retain substantial mutable rules, command and accounting responsibilities.

Native sections bridge existing controls through adapters such as `NativeInlineTextBinding`, `NativeInlineChoiceBinding`, `NativeInlineNumericBinding` and `NativeInlineCommandBinding`. `NativeControlObservation` owns their common availability, visibility and invalidation subscriptions. Their value validation and input handlers remain separate. Dispose the owning section/binding to detach callbacks; the observer does not own the legacy control. See [MAINTENANCE](MAINTENANCE.md).

## Representative feature: Cyberware purchase

Follow these actual source entry points when changing ware behavior:

1. `frmCreate.cs` and `frmCareer.cs` construct `frmSelectCyberware` for ordinary purchases and vehicle add-ins. These are distinct caller contexts; preserve their existing accounting and plugin restrictions.
2. `frmSelectCyberware.CreateModel` captures configuration. `ShowDialogAsync` creates the owned `CyberwarePickerWindow` and publishes an accepted draft. Browsing and cancellation must not commit a purchase. The disposable adapter is the lifecycle boundary, not the rules engine.
3. `CyberwarePickerModel` uses the shared catalog presentation model and `CyberwareSelectionPolicy`. Catalog identity, category, source eligibility and accepted choices are domain inputs; UI labels must not become identity or XPath expressions.
4. Back in the caller, `CyberwareSelectionPolicy.FindDefinition` resolves accepted canonical name/category and mode. Creation then calls `Cyberware.Create` and adds the result to the appropriate character or parent collection. The career caller separately owns funds, expenses and undo. Changing the picker does not authorize copying or replacing these accounting bodies.
5. `Character.Save` in `clsCharacter.cs` serializes to a unique temporary file through `SaveToFile`/`WriteCharacterSnapshot`, copies an existing destination to `.bak`, then moves the temporary file over the destination. Its `finally` restores the original filename and removes a leftover temporary file. The snapshot serializer calls each ware object's `Save`; `Cyberware.Save` in `clsEquipment.cs` writes its existing fields. `CaptureSnapshot` uses that same serializer in memory, while `Character.Load` reconstructs saved state. Preserve schema, encoding, backup behavior and nested ownership when extracting behavior; selecting a draft is not itself saving the character.

For the ordinary career purchase path in `frmCareer`, inspect the block after `Cyberware.Create`: it calculates availability-dependent cost, checks funds, removes newly created improvements when funds are insufficient, creates a Nuyen expense and its `ExpenseUndo`, and then inserts the ware into its parent/character collection. Free purchases bypass the expense/funds block. These explicit differences belong to the caller and must survive any future create/career consolidation. This map records the observed control flow; it does not claim that all failure paths are transactional.

[CYBERWARE_PICKER](CYBERWARE_PICKER.md) records independent catalog/preview references, cancellation, ownership and actual purchase regressions. Those references must remain independent of the calculator being tested. This trace identifies boundaries; it does not establish that every ware rule or persistence operation is already presentation-independent.

## Data, settings and compatibility

Matching rule catalogs, translations, sheets, sample characters and settings reside under `Chummer.Mac/assets`. [XML catalog ownership](XML_CATALOG_OWNERSHIP.md) records the process cache, caller-owned copies and custom-data merge boundary, including current invalidation limits. Character serialization remains in the legacy model. Settings/sourcebook/PDF associations have separate lifetimes and must not be implicitly reset by a UI migration.

The original `Chummer` project is historical source. `Chummer.Mac.sln` builds the current Mac/Windows application. Vendor changes require reviewing `vendor/LOCAL_PATCHES.md` and the capability inventory in [COMPATIBILITY_CONTROLS](COMPATIBILITY_CONTROLS.md); compilation cannot prove a no-op platform API behaves correctly.

[Installed vehicle movement](VEHICLE_MOVEMENT.md) records the shared final arithmetic and the deliberately separate Speed multiplier/acceleration expression paths. Vehicle model selection and armor eligibility stay model-owned; the arithmetic component reads no controls or persisted state.

[Inventory Copy](CLIPBOARD_COPY.md) separates the temporary Unicode XML wrapper from model Save dispatch and global clipboard policy. The existing Gear failure-order limitation remains explicit; the wrapper does not claim atomic publication or rollback of callback effects.

[Vehicle sensor transitions](VEHICLE_SENSORS.md) now follow a separate model/adapter/projection path. Both editors call `VehicleSensorCatalog.Apply`, which loads the catalog and supplies the legacy `Gear.Create` factory to `VehicleSensorTransition.Apply`. The transition owns seven-size ordering, first-recognized boundary behavior and eight-field mutation of the existing sensor; it reads no controls, catalog files or persistence services. The editor then updates the canonical tree node. The catalog adapter retains constructor prompts/improvements and failure ordering, so this is not a transaction boundary: missing-node failure still follows model mutation, and purchase/sale/undo accounting remains in the six unchanged callers.

[Stacked-focus commands](STACKED_FOCUS.md) now separate `StackedFocusProposal` (selection validation and linked model transfer) from `StackedFocusEditorCommand` (owned modal picker and tree projection). The two editor wrappers retain dirty/summary refresh and persistence. Selection no longer bonds owned gear before minimum/Force validation; this explicitly repairs the recorded one-selection cancellation defect. Model cost/name construction precedes writes, while later UI projection failures do not roll back or save model state. Headless command/proposal assertions pass; full-suite and native acceptance have separate recorded status.

## Verification and remaining architecture work

`MacSmokeTests` orchestrates headless behavior and compatibility checks. Native drivers execute real platform interactions separately; `.github/workflows/desktop-build.yml` builds and verifies Mac/Windows and retains diagnostics. See [CONTRIBUTING](../CONTRIBUTING.md) for commands and verification limits.

[Maintenance responsibilities](MAINTENANCE_RESPONSIBILITIES.md) assigns CI failures and dependency-update review to the repository owner and records retained exceptions/removal conditions, including residual editor commands. [Vehicle sensors](VEHICLE_SENSORS.md) traces six creation/career purchase, deletion, sale and undo call sites with their accounting order. Catalog/global language lifetimes are documented in XML_CATALOG_OWNERSHIP. Broader remaining work is the current section’s platform/visual acceptance, reviewing retained duplication and compatibility debt against these records, and the final code/documentation consistency pass. This map is an entry point; these links and past tests do not themselves declare project completion.

[Selected quality details](QUALITY_DETAILS.md) share a read-only presentation owner for cost, source and tooltip; creation/career explicitly select their existing BP/Karma display convention. Invalid selection clears the projection without changing the character.
