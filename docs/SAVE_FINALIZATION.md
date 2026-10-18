# Creation finalization and save recovery

## Destination cancellation (released in 0.2.137)

Save As chooses a destination before creation confirmation or validation. Canceling the chooser returns without replacing starting money, adding a fallback lifestyle or setting Created. The synthetic native baseline `/tmp/chummer-save-as-baseline/report.json` reproduced the old ordering defect; PR29 passed both platforms and all 399 saved-character fixtures. The optional internal chooser dependency uses the standard native chooser by default. Original user saves were not involved.

## Failed writes (local implementation, platform verification pending)

Source inspection found a separate boundary: finalization changed the live character before writing, and career reopening added starting expenses/Essence adjustments followed by a second save. A local native attempt and retry stopped before any window with the known render-timer -6661; no local failed-write assertion report was produced. No graphics fallback was introduced. Draft PR30/run39163360122 then reproduced the exact failure on both Mac and Windows against released production source, head342b9dd233f2430bc1850961ce9a1a250cbef542. Both reports fail `Native failed creation Save As preserves prefinalization model filename and inventory references` (Mac362 checks, Windows355); checksummed reports are `/tmp/chummer-save-recovery-baseline-{mac,windows}-report.json`. The baseline is not mergeable.

The local implementation uses these boundaries:

1. Save and Save As share SaveCharacterToPath. Explicit creation confirmation and the existing validation/starting-money prompts must be accepted. ValidateCharacter can return accepted choices without applying finalization. Public direct validation/confirmation behavior is retained for existing callers.
2. CreationFinalizationSelection captures starting money, the existing Karma carryover decision, the prepared fallback lifestyle and skill groups that must be broken. Preparation does not apply those changes to the live editor. Its existing pre-career backup runs after accepted prompts and before finalization.
3. PreparedCreationSave builds a separate Character using the exact serializer and verifies complete XML equality before applying choices. It prepares Created, starting Karma/Nuyen expense entries and their undo, plus existing EssencePenalty MAG/RES adjustments. Unsupported snapshot normalization is rejected instead of silently dropping state. The loader rebuilds catalog skills/groups, so tests use canonical definitions; no schema/rules changes are inferred from deliberately malformed fixtures.
4. The complete prepared payload is written once through CharacterFileDialogs. Character.Save already uses a sibling temporary file, prior-file backup and replacement, with filename restoration and temporary cleanup in finally. A rejected write does not commit the prepared money, lifestyle, ledger, groups or Created state to the live model.
5. Only successful writes commit accepted fields and the same new starting-expense objects. Existing skill/group/inventory/undo references remain live; no LoadSnapshot rollback is performed on the editor. The prepared lifestyle node is attached after success. Career reopening performs no second save. The escape-after-save flag suppresses reopening while retaining a complete durable career payload.

Preparation/file errors are handled before committing. Post-durable UI follow-up is outside that error handler, so a successful disk write cannot be reported as a rejected write merely because a later UI action fails. Exact native transition coverage remains required; this implementation is not yet a verified or published recovery fix.

## Verification

- `/tmp/chummer-prepared-save-smoke.log`: direct directory-write rejection, successful retry with both starting ledger entries/undo and UTF16 encoding, reference preservation, repeated commit without duplicate expenses; all 399 saved-character fixtures and both Foundry target contracts pass (session20778 exit0).
- `/tmp/chummer-prepared-save-python.log`: all 36 release/safeguard tests pass. Modernization source safeguards and diff checks pass.
- `/tmp/chummer-prepared-save-final-build.log`: zero warnings/errors for the integrated implementation. `/tmp/chummer-career-transition-check-build.log`: subsequent native-only coverage builds with 10 existing vendor XML documentation warnings, no errors.
- Native checks cover both failed Save As and existing-path Save, full model snapshots/inventory identity, accepted Save As and actual career reopening. The latter checks one durable starting-money entry and no second-write `.bak` on a fresh destination. Required combined minimum is 415 (Mac adds seven projection checks); baseline PR30 uses the prior 414 gate. Fixed-head native checks and exact-head platform CI are pending; the original Save As failure baseline is confirmed on both platforms.

Method-bounded review of CalculateBP and CalculateNuyen found presentation assignments but no direct character assignments. Previously suspected KnowledgeSkillPoints/MAGAdept/spirit/program writes belong to the separate UpdateCharacterInfo method. Getter behavior and actual modal transitions still require the native snapshot/reference tests; source review is not their substitute.

PACKS uses a cloned model and initialized replacement editor with selected-tab/history transfer. It is not an in-place rollback facility. This save flow deliberately retains the current editor graph until writing succeeds. Preserve UTF16/schema, settings/source/PDF associations, pre-career backup behavior and original user files throughout further validation.

Additional headless recovery checks preserve an existing expense and its original undo object alongside the new starting entry, and reject a deliberately nonreloadable synthetic active skill without changing the live snapshot. `/tmp/chummer-recovery-ledger-guard-smoke.log` passes the complete headless suite (session58558 exit0; this follow-up omits the unchanged 399-fixture loop). Its build has only the existing 10 vendor XML documentation warnings, no errors; source safeguards and diff checks pass.
