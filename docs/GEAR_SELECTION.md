# Gear selection modernization audit

## Boundary and current state

The next creation-facing migration is `frmSelectGear`. This audit is based on accepted tree `12e99573f371fb43d98313d4ef3ca3500915e696`, released as Nightly 0.2.147. The isolated `gear-picker-modernization-wip` branch preserves that source. The active Gear selection now uses the IDisposable frmSelectGear adapter over standard GearPickerWindow controls. The legacy designer/resources are retired. Full headless contracts pass; both-platform native execution and visual acceptance remain pending.

Use `CatalogPickerModel` and `CatalogPickerWindow` for the replacement, retaining the existing `GearSelectionContext`, `AmmunitionSelectionPolicy`, `CatalogSearch` and caller purchase paths. Cyberware follows Gear: its cyberware/bioware grades, essence discounts, subsystem/plugin restrictions and character modifiers require a separate characterization stage.

## Existing contract to characterize before replacement

| Boundary | Current behavior and required coverage |
|---|---|
| Identity and browsing | Selection returns canonical name plus category. Search currently encodes both with a caret; native records should retain separate fields. Cover duplicate names, translated names/categories, quoted literal queries, commlink aliases, remembered category and `InitialCategory` fallback. |
| Eligibility | Source books, positive/negative/armor capacity, hardware upgrades, OS bounds and ammunition context constrain browsing, search and acceptance. Existing GearEligibilityChecks cover these restrictions and stale source/context refusal; retain their meaning when retiring reflection into compatibility fields. |
| Rating | Catalog maximum and optional minimum determine the range; absent minimum defaults to 1. Rating zero disables the field; a fixed minimum equal to maximum also disables it. Preserve rating-dependent availability tiers, prices and capacities. |
| Quantity | Selection resets quantity and step to catalog `costfor`, otherwise 1. Minimum is 1 and designer maximum is 100000. Nonempty AllowedCategories disables quantity unless it is exactly Ammunition. Current shared native quantity row hardcodes maximum 1000 and unit step: using it unchanged would reduce this contract. Add an opt-in range/step extension with existing picker defaults preserved. |
| Purchase options | Career presentation enables Free and markup. Stack defaults on for created characters, off/hidden for creation; EnableStack=false clears it. Preserve DIY half-price for Commlink Upgrade, hacked software tenth-price, AI inherent-program eligibility and hacked exclusion, and grenade aerodynamic choice. Character.Created and the constructor career flag have distinct roles. |
| Pricing and capacity | Preview applies quantity divided by pack increment, DIY, markup, hacked cost and caller cost multiplier; variable ranges and tiered cost fields have separate paths. Availability uses caller modifier. Capacity supports Standard, Zero and PerRating, brackets, split capacity, FixedValues and armor capacity. Preserve observed legacy results before considering any rule correction. |
| Acceptance | Revalidate source/context before recording name/category/rating/quantity/markup; stale selection refreshes without silently accepting a replacement. Return AddAgain, FreeCost, DIY, hacked, stack, inherent and aerodynamic flags through the adapter. |
| Modal lifetime | Keep explicit owner, Cancel on title-bar/Escape, owner restoration and deterministic disposal through NativeDialogHost. Snapshot and dirty-state checks must cover cancellation and actual caller paths, including nested and root inventory. |
| PDF | Source short code/page and localized tooltip retain altpage handling and existing CommonFunctions.OpenPDF behavior. |

## Caller audit

Production construction sites are in `frmCreate.cs` and `frmCareer.cs`, including carried, armor/weapon/vehicle attachments and nested gear. The root/nested purchase paths configure selected name, allowed/additional categories, commlink context, capacity display/maximum, ammunition context and child availability/cost modifiers before showing the dialog. Capture configuration at Show time rather than adapter construction.

The root/nested methods around creation line 18325 and career line 23634 retain their existing Gear.Create, charging, expenses, improvement and undo behavior. Some existing callers test the DialogResult property after ShowDialog; migrate each affected caller to an explicitly accepted result and dispose the wrapper, without rewriting accounting. Audit all construction sites rather than only these two examples.

`AddCategory` can seed extra categories before loading. MaximumCapacity currently supplies a display label; do not silently reinterpret it as a new eligibility rule. The extracted GearSelectionContext remains authoritative for its existing constraints.

## Implementation and acceptance sequence

1. Add actual legacy-form characterization for pack quantity/range, rating transitions, options/pricing/capacity and acceptance/cancellation, using synthetic catalog choices and complete character snapshots. Preserve existing eligibility coverage and record a full headless baseline.
2. Extract preview calculations behind an unattached model boundary, preserving established formulas and localized results. Differentially compare against the active form before retiring it. Keep rules and inventory mutation in existing core/callers.
3. Extend shared native controls only where Gear requires them, with existing defaults unchanged; bind standard rating, quantity and conditional options. Connect the IDisposable adapter and audit every production caller. Retire obsolete active designer/presentation only after equivalent contracts exist.
4. Run full headless compatibility and new actual native transitions on both operating systems. Preserve minimum 825, original 20-second operations/300-second suite, completed passed report and nonzero failure exit. Add meaningful assertions and raise the floor for them; never lower gates to fit a migration.
5. Require all399 file/memory round trips, both Foundry targets, audit, Windows PDF, SHA-verified synthetic screenshots and visual review of affected light/dark normal/resized dialogs. No old-head substitution. Publish only accepted exact source through normal draft PR and master release verification, preserving Stable and release cadence.

No new dependency is needed for this boundary. No global spinner overlay, vendor modification, character schema change, portrait work or specialist expansion belongs to this stage.

## Characterization evidence

GearSelectionChecks runs after the existing GearEligibilityChecks and records 32 assertions against actual shown compatibility dialogs. It covers rated/unrated transitions, the 100000 quantity range, ammunition pack resets and proportional cost, grenade-option clearing, Add Again identity/rating/quantity, explicit cancellation, category quantity restrictions, DIY/markup/free/caller modifiers, all three software cost tiers, hacked software, career stacking and complete creation/career snapshot preservation. It restores remembered category after the fixture. Existing eligibility checks remain active and unchanged.

The initial 22-assertion full headless baseline passed in `/tmp/chummer-gear-characterization-smoke.log`; its build recorded zero errors and ten existing vendor XML documentation warnings. The expanded build `/tmp/chummer-gear-options-build.log` passes with zero warnings/errors. Expanded full headless `/tmp/chummer-gear-options-smoke.log` passes with exit 0, including all 32 assertions. These are legacy behavior characterizations, not evidence of a native Gear replacement. Variable-price/capacity variants, additional categories and AI-specific flags still need differential coverage during model extraction. Both-platform native acceptance remains required after connecting the replacement.

## Shared preview extraction

GearPreviewRequest now carries rating, quantity/pack increment, markup, DIY/hacked/free options, caller modifiers and capacity mode into GearPreview.Calculate. GearPreviewResult returns availability, price, availability-test text and capacity; it never creates or installs Gear. The active legacy dialog delegates these calculations to this one path while retaining selection, option visibility, rating updates and caller accounting.

Before delegation, GearPreviewChecks compared 20 draft combinations against the unchanged actual dialog: rated and pack quantities, all capacity modes, caller modifiers, free/DIY/hacked software, six software ratings, variable price ranges, fractional/split capacities, and existing FixedValues/wildcard fallback behavior. Synthetic definitions live only in a cloned catalog. This independent comparison passed the full headless suite in `/tmp/chummer-gear-preview-smoke.log` (exit0). After delegation the same checks verify integration, rather than providing an independent second implementation; the independent pre-delegation log/source hashes are preserved. The earlier 32 expected-behavior assertions remain unchanged.

The extracted calculation statements match the original after explicit input/output and descriptive local-name substitutions, ignoring comments/whitespace. No rounding, variable-price or capacity policy was corrected in this extraction. `/tmp/chummer-gear-preview-shared-build.log` passes with zero warnings/errors, and `/tmp/chummer-gear-preview-shared-smoke.log` passes the full headless suite (exit0). Source safeguards and diff checks pass. A new native Gear model/window remains unconnected and unaccepted; next integrate the shared catalog model with contextual categories and purchase-option draft state before replacing presentation.

## Prepared native model and window

GearPickerModel now uses the shared catalog model and extracted preview with an unattached GearPickerSelection draft. GearPickerOptions captures allowed/additional/preferred categories, source/context restrictions, selected name, modifiers, capacity display and stack configuration at the future Show boundary. This paragraph records the preparation checkpoint. The connected stage below supersedes its pending integration work; platform acceptance is still pending.

The shared model accepts an optional prefiltered category list; other picker defaults are unchanged. Gear preserves explicit hidden-category permissions and AddCategory seeds, source/context checks during search and acceptance, literal aliases, identity/category pairing, 100000-unit quantity range and costfor pack step. The opt-in IRangedQuantityCatalogPicker adds minimum/maximum/increment bindings to the standard native quantity row; existing quantity models keep their 1–1000/unit-step defaults.

GearPickerChecks adds 167 assertions, including all-category and contextual actual-form browse/search parity, source/context rejection, accepted identity/rating/quantity/Add Again, grenade/AI options, custom minimum/rating and hidden-category behavior, and unchanged snapshots. The initial 164-assertion model baseline and final 167-assertion full headless suite both pass (exit0). Final logs are `/tmp/chummer-gear-window-preparation-build.log` (zero warnings/errors) and `/tmp/chummer-gear-window-model-smoke.log`; safeguards/diff pass. These headless checks validate models and shared existing behavior, not native control interaction or visual rendering.

A lexical caller inventory identifies 21 construction sites across frmCreate/frmCareer. Use it as a navigation aid, not proof of disposal or control-flow correctness. Next connect the IDisposable NativeDialogHost adapter, explicitly require OK and dispose it at every affected production call, preserving configuration-before-Show, accounting, repeat/Add Again, cancellation and owner restoration. Update the existing tests that inspect compatibility fields to the new contract and add actual native quantity/conditional-option/modal/source/PDF/snapshot checks before retiring legacy presentation. Both-platform execution and affected synthetic screenshots remain mandatory; no new remote PR or release is claimed.

## Connected native dialog and caller lifetime

frmSelectGear is now an IDisposable adapter over NativeDialogHost, with async and established synchronous modal entry points. Configuration is captured when shown; title-bar cancellation returns Cancel and no purchase draft. Canonical name/category, rating, quantity, markup, Free, Add Again, DIY, hacked, stack, AI inherent and aerodynamic flags come only from an accepted model selection. The wrapper contains no hidden compatibility form.

All21 production construction sites (11creation,10career) use scoped disposal and explicitly require OK before proceeding. A source comparison verifies these are the only changes to those two editor files: creation, charging, improvements, expenses, repeat calls and undo remain byte-for-byte unchanged. Existing native commlink/ammunition/PACKS checks now drive the standard window rather than searching for a legacy Form.

Before retirement, `/tmp/chummer-gear-reference-smoke.log` passed the full suite while recording reference fixtures. Two embedded test-only JSON resources preserve ordinal hashes of public catalog choice lists and20 preview results, so retirement does not replace independent expectations with the implementation under test. No character names, notes or IDs are in those resources. The obsolete designer/resx are removed; legacy-field tests now assert draft contracts or frozen references. Historical32legacy characterizations and167live-model parity checks remain recorded in prior proofs; current model/reference checks include170assertions with additional empty restriction, duplicate-category and oversized-pack contracts.

Full headless `/tmp/chummer-gear-boundary-smoke.log` passes exit0. `/tmp/chummer-gear-native-options-build.log` compiles zero warnings/errors, including49new actual-window assertions. The native floor rises825→874; original20-second operations,300-second suite, completed passed report and nonzero exit gates remain. Coverage includes range/pack stepping, typing/keyboard, empty literal search/recovery, source data, both themes, Add More, Free, DIY, aerodynamic/hacked/AI conditional options, owner restoration and snapshot-preserving cancellation/acceptance. Four exact native-gear-{creation,career}-{dark,light}.png names are added to the success whitelist.

Native assertions have not executed locally or in CI yet. Require exact-source bothOS/fullheadless/all399file+memory/bothFoundry/audit/WindowsPDF/min874 plus SHA and visual review of all40currentbatchPNGs perOS (previous36 plus4Gear), inspecting additional affected frames as necessary. No native acceptance, remote PR or publication is claimed at this checkpoint.

## First exact-head native result and visual correction

PR41/source6d30ff123902dbc61904f549d9da0aef183cc799, exact tree2038c9b9dc6202344fc6c07b09e4b84b317dbdaa, passes CI40275300479: Mac883/Windows876 native assertions, full headless/all399 file+memory/both Foundry/audit and WindowsPDF. Exact screenshot artifact hashes: Mac5186c2cddafcdec06668951c8860e1779356445d8022371847f2e6dc5f88ecec, Windows9571abc1ace8e9cca66e510db3d4d61d91a2bb5e0ad44ee90b0b19fdab1d6ec6. All76native PNGs perOS are preserved.

Review of the four new Gear frames perOS rejected the literal legacy mnemonic in the Stack caption (S&tack). The derived window now reuses the catalog window's existing label conversion, which removes mnemonic markers while preserving escaped literal ampersands. The current native theme assertions also require the clean English Stack label. This is a bounded presentation correction; no model, rules, accounting, deadline or assertion-floor change. The remaining36currentbatch images perOS were prepared but not visually accepted on that source. New exact-head bothOS execution and all40batch PNGs perOS still require SHA and visual acceptance before ready/merge.
