# Foundry Shadowrun 4e actor export

The character print preview includes **Foundry JSON**. This creates a single actor JSON document, not a collection of documents or a Chummer-specific interchange file. Multi-character previews let the user choose which character to export. The button remains usable if the browser preview or printer is unavailable. Saving from the preview writes through a temporary file and replaces the destination only after serialization succeeds.

## Compatibility and reference implementation

Choose the Shadowrun 4e **system** version, not the Foundry core version. Output currently targets Foundry core 14 and system 1.6.0 or 1.26.1. The dialog defaults to the current supported 1.26.1 system; select legacy 1.6 explicitly for older worlds. No runtime dependency on Node, a running Foundry server, or online services is introduced.

Field mappings are adapted from the MIT-licensed [Shadowrun4-FoundryVTT](https://github.com/Tarislowatsch/Shadowrun4-FoundryVTT) importer, including its attribute, weapon attack-skill, implant grade, spell and character mappings. Reference revisions are v1.6.0 (`adeb69d0bb541d309f93893ca3be1c8edab98578`) and v1.26.1 (`3cc30f2`, inspected source). The MIT notice is bundled in `branding/licenses/Shadowrun4-FoundryVTT.txt`.

Actor and embedded-item IDs are generated for the export. Foundry world IDs, user ownership IDs, and UUIDs from the supplied examples are never copied. Each item's `flags.shadowrun4e.chummerExport` retains its original item XML and parent ID for traceability. User-provided character files and exports must not be committed as regression fixtures.

## Augmentation and activation

The exporter uses calculated augmented attributes and effective skill ratings. It separates supported activation bonuses from the inactive attribute/initiative baseline, so enabling an exported implant effect reconstructs the current augmented values without adding them twice. Permanently installed augmentation stays included in the baseline.

Currently recognized activation profiles are Wired Reflexes and Move-by-Wire System, including edition-labelled variants. Unknown numeric ware receives a snapshot/activation notice rather than a guessed toggle. These use the existing Foundry implant-effect checkbox, labelled **Activate …**; linked Action buttons in the inspected Foundry versions perform rolls rather than toggle effects. An unknown ware activation model is never invented from its numeric bonuses.

Measurements use the existing Chummer improvement calculations, synchronously on the UI thread. Improvement `Enabled` flags are restored in a `finally` block, including when calculations throw; measurement performs no I/O or event pumping. Every combination of up to four recognized profiles is checked for linearity. Capped or nonadditive fields retain their correct current snapshot and receive item-level manual instructions instead of an incorrect additive effect. Larger or mixed-state groups receive explicit manual notes.

The newer system exposes skill-specific bonus effects, but its inspected defense dialog still uses stored skill ratings. Consequently Move-by-Wire's Dodge bonus is retained in the effective Dodge rating with an explicit numeric notice explaining how to adjust it on deactivation/reactivation. It is never mapped to the broader Dodge defense modifier, which would also benefit Gymnastics. Skillwire behavior is also described as manual.

Changing the underlying attributes, ware rating or stacking rules after import can require re-exporting or recalculating the effect. The activation effects represent the character's exported configuration; they do not replace the Chummer rules engine inside Foundry.

## Item notes and other limitations

Original ware notes are preserved verbatim. Generated notices appear in **Chummer export — manual handling required**, identify the affected benefit and explain whether it is already included. Notices remain on the affected implant, including nested components. Repeated exports do not modify the original notes or accumulate generated text in the character. Fully supported effects do not get a generic warning.

Nested implant/gear containment is retained in metadata and notes; Foundry presents those records separately. Capacity-consuming child implants do not incur a second Essence charge. Version 1.26 output reverses its grade multiplier and supplies the correct Essence baseline because that version recalculates implant loss; version 1.6 receives current Essence directly.

Weapons and armor carry their current calculated values and linked attachment/modification records. Attachment bonus fields are zero when their bonuses are already included in the parent values, preventing double counting. Notes explain that changing Strength, ammunition or modifications requires recalculation. Special damage/AP expressions and conditional item rules are retained in notes/metadata rather than silently guessed. Vehicle records are retained as inventory records with a manual-setup notice, rather than creating additional vehicle actors as an unexpected side effect. Adept powers and special spell rules likewise carry manual notices where automation is incomplete.

Legacy active-skill internal names are normalized for 1.6’s exact defense lookup; their localization labels retain readable names.

Age is always numeric for Foundry validation; a nonnumeric age is retained in notes and metadata with numeric age 0. Portraits are embedded as normalized PNG data URLs. Character `.chum` file format and encoding remain unchanged.

## Verification

Portable regression checks exercise supported and inactive activation, nonadditive initiative stacking, numeric age, unique embedded IDs, preserved notes, repeated exports and restoration after a thrown calculation. An independent contract check validates emitted numeric types, whole-number ratings, item/effect types and attachment/container references against the upstream field constraints. Both target formats are exported from all bundled character fixtures, followed by a serialization comparison proving the original character state did not change. Native Foundry import/roll behavior still requires validation in a running world; schema-shaped JSON alone is not evidence of successful in-world automation.

## Stored API values versus prepared sheet values (issue18)

In the pinned 1.6.0 source, `derivedStats.finalStats` is declared with default fields but is not populated or consumed by the actor calculation. Chummer does not export `derivedStats`; Foundry owns preparation. Executing the actual pinned `computeDerivedStats` with synthetic nondefault inputs produces the expected initiative and monitors while `finalStats` remains default. Its defaults alone are therefore insufficient evidence of an import error.

The user confirmed that the reported character's expected Body/Agility and 6 ballistic/5 impact armor display correctly in the live sheet, while the connector document read still contains default derived values and zero armor. Treat the visible/prepared values and the API representation separately. This does not verify initiative, all active effects, or every downstream consumer. Check the exact system version, supported prepared-data API and equipped/activation state before exporting guessed derived caches or changing input totals. See ISSUE_TRIAGE.md for remaining investigation.

## Identity, traditions and the Willow comparison

The live comparison world reports system 1.26.1; importing JSON targeted at 1.6 is not an equivalent version test. The export dialog now defaults to 1.26 while retaining an explicit legacy choice. Actor name remains the alias; `descriptionAndNotes.realName` receives the separate character name on 1.26. Version 1.6 has no such schema field, so the name is retained in notes and export metadata instead.

Tradition values follow the pinned models: 1.6 uses lowercase `hermetic`, `shaman`, `wicca`, `chaos`; 1.26 uses its complete uppercase enumeration, including `NORSE`, `SHAMANIC` and `CHAOS_MAGIC`. Unsupported/custom traditions remain in metadata and a manual-handling note, with their independent drain attribute preserved. Do not guess a custom tradition from a substring.

Willow's exported Spellcasting rating 7 includes a current +2 bonus above base 5, consistent with its bonded rating-2 Power Focus. The inspected 1.26 `getSkillDicePool` adds exported skill rating and effective Magic plus modifier tables; no Power Focus name/rating automation is present. Its imported focus has no transferred effects. Keep the effective rating instead of dropping the bonus or adding another effect. Bonded Power Focus notes explain this snapshot behavior and the need to re-export/adjust ratings after activation or bonding changes. This is source/payload evidence; a live roll chooser still needs user confirmation, and the XML importer remains an independent comparison path.

The native export dialog check exercises the current default, selecting the legacy target and Cancel without character changes. Synthetic Power Focus checks use a catalog-created focus and its actual MAG-linked bonus, preserving the base5/effective7 distinction for both targets.
