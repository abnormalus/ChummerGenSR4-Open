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

Matching rule catalogs, translations, sheets, sample characters and settings reside under `Chummer.Mac/assets`. Character serialization remains in the legacy model. Settings/sourcebook/PDF associations have separate lifetimes and must not be implicitly reset by a UI migration.

The original `Chummer` project is historical source. `Chummer.Mac.sln` builds the current Mac/Windows application. Vendor changes require reviewing `vendor/LOCAL_PATCHES.md` and the capability inventory in [COMPATIBILITY_CONTROLS](COMPATIBILITY_CONTROLS.md); compilation cannot prove a no-op platform API behaves correctly.

## Verification and remaining architecture work

`MacSmokeTests` orchestrates headless behavior and compatibility checks. Native drivers execute real platform interactions separately; `.github/workflows/desktop-build.yml` builds and verifies Mac/Windows and retains diagnostics. See [CONTRIBUTING](../CONTRIBUTING.md) for commands and verification limits.

Remaining work includes tracing other representative sections, inventorying duplicate rules/accounting and global mutable caches, documenting retained exceptions and their removal conditions, and recording CI/dependency ownership. Review those findings alongside the section migration process; this map is an initial maintained entry point, not project completion evidence.
