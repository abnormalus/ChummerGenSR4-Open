# Creation finalization and save recovery

Save As destination cancellation was corrected in 0.2.137. Failed-write recovery was corrected in 0.2.138. Both changes preserve the existing character schema/UTF16 encoding, settings/source/PDF associations and public direct validation/confirmation behavior.

## Save ordering

1. **Choose the destination.** Save As uses the standard native chooser before creation confirmation or validation. Canceling it returns without replacing money, attaching a fallback lifestyle or setting Created. Existing-path Save and Save As then share SaveCharacterToPath.
2. **Collect accepted choices.** Creation confirmation requires explicit Yes. Existing validation, Karma carryover and starting-money prompts must be accepted. CreationFinalizationSelection captures those choices and the affected skill groups without applying them to the live model. The pre-career backup runs after accepted prompts and before finalization; its temporary filename is restored in finally.
3. **Prepare an isolated payload.** PreparedCreationSave loads an exact serializer snapshot into a separate Character. Complete XML equality is checked before applying accepted choices. Preparation includes Created, starting Karma/Nuyen expenses and their undo, plus existing EssencePenalty adjustments to enabled MAG/RES. Unknown or lossy snapshot normalization is rejected rather than silently dropping state.
4. **Write once.** CharacterFileDialogs saves the complete prepared payload through Character.Save. The existing file helper uses a sibling temporary file, prior-file backup and replacement, with filename restoration and temporary cleanup. Rejected writes do not commit prepared money, lifestyle, expenses, group state or Created to the editor's model.
5. **Commit accepted changes.** Only a successful write applies the selection and transfers the prepared starting expense objects. Existing inventory, skills, groups, expense and undo objects retain their identity; no LoadSnapshot rollback replaces the live editor graph. The prepared lifestyle tree node is attached after success. Career reopening performs no second save. The escape-after-save flag suppresses reopening while retaining the complete career payload on disk.

Preparation/write failures are handled before live commit. UI work after the durable write is outside that handler, so a later UI error cannot misreport a completed write as a rejected write. The save-copy mechanism is independent of PACKS editor replacement and history transfer.

## Exact-copy guard

Ordinary loading intentionally raises cached skill-group rating maxima for Ignore Rules/career mode. A live creation editor can still contain its earlier serialized maxima after Ignore Rules changes. Preparation restores those exact owner maxima **only on the isolated copy**, then performs the complete XML equality check. Normal load rules are unchanged; equality is not relaxed. A nonreloadable synthetic skill still causes preparation to reject the payload without changing the live character.

## Verified behavior

Native tests on both Mac and Windows cover chooser cancellation, chosen destination followed by creation rejection, accepted UTF16 Save As, failed Save As to a directory, failed existing-path Save, and actual career reopening. Failure assertions compare the complete model snapshot and existing inventory references. Successful reopening verifies one durable starting-money entry and no second-write `.bak` for a fresh destination.

Headless checks additionally cover successful retry after rejection, starting ledger/undo contents, preservation of existing expense/undo references, repeated commit without duplicate starting entries, exact group limits and rejection of unsupported snapshot loss. All 399 saved-character fixtures and both Foundry target contracts pass. These are the exercised transitions; they are not a claim to have simulated every operating-system/storage failure.

PR #30 baseline head 342b9dd233f2430bc1850961ce9a1a250cbef542 failed the prefinalization snapshot/reference invariant on both platforms in CI 39163360122. Final head 4d099efad1544f0c87180dfcbb3d9a774dc7d4f8 passed CI 39166147067: both platforms, minimum 415 native checks, all 399 fixtures/full smoke, both Foundry contracts, dependency audit and Windows preview/PDF. The integration and merged master 3f7e8c1513e8624286d8e0a2406e7c161ff44c14 share tree 9c97a6182ad495b8478ac67f06ded3bf5b8175d2. Four exact synthetic character light/dark screenshots were checksum-verified and visually reviewed. Earlier intermediate failures are retained with their diagnostic receipts in the original task's outputs/pr30-verification; the final exact-head pass supersedes their pending gates.

Release 39166903571 succeeded for Nightly 0.2.138. Source/tag/version/four assets, the prior 7e7017c8 daily archive and retained Nightly BUILD/source/SHA256 checks were verified. Stable remained ab0bf7bfc4f49e46e7b85c773f26415cdc9e546e. Release, archive, retained-package and final-CI receipts are preserved in outputs/pr30-verification. Original user character files were not used or changed.
