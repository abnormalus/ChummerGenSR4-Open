# Section-by-section modernization process

As each application section is repaired or improved, review whether it recreates a capability already supplied by the modern framework or an appropriate maintained library. Prefer established components where they reduce maintenance and preserve the required behavior. This is an incremental process, not a wholesale rewrite or an edition expansion.

## For each section

1. **Record its behavior.** Identify the character data/rules it reads and changes, source filtering, saved fields, commands, validation, undo requirements and platform interactions. Preserve custom entries and existing character-file compatibility.
2. **Identify reusable machinery.** Review the areas below. Check the existing shared implementation before introducing another component. Do not add a dependency merely because one exists; assess platform support, maintenance, licensing, size and integration/testing cost.
3. **Choose a bounded replacement.** Keep rules and persistence separate from presentation. Prefer standard Avalonia controls and framework-managed layout/rendering over recreating controls in the compatibility layer. Use direct modern UI for new or substantially rebuilt sections when practical; leave unrelated working screens intact.
4. **Extract shared behavior.** Put commands, validation, searchable editors and data-change notifications behind reusable interfaces/components. Avoid new copies of screen-specific fixes. Introduce observable view models in bounded sections rather than attempting to convert the entire mutable character model at once.
5. **Verify actual transitions.** Test the section's meaningful operations and failures, not only its initial appearance. Include keyboard/mouse interaction, cancellation, selection changes, long-to-short text, resizing/scaling, tab/document switching and source-setting changes as relevant. Add regressions for observed defects. For migrated rendering, use targeted native visual checks on both Mac and Windows, alongside headless checks where useful.
6. **Retire the old path after validation.** Remove obsolete drawing, manual synchronization and duplicate handlers for the migrated section once its required behavior passes. Keep explicit, documented adapters where legacy rules still require them; avoid maintaining two active implementations of the same screen.
7. **Finish documentation and naming.** Document the purpose of meaningful code blocks, non-obvious rules and assumptions, data flow, ownership, error/cancellation handling and how to verify the section. Explain why choices exist rather than paraphrasing every statement. Replace unclear abbreviations and generic temporary names with descriptive names as the affected code is modernized; avoid unrelated bulk renaming. Preserve serialized field names, reflection-sensitive identifiers, external contracts and localization keys unless explicitly migrated and tested. Refresh section-level maintenance guidance and remove stale comments.
8. **Publish through the established process.** Run checks appropriate to the change plus required release checks, preserve prior downloads in the archive, and describe remaining limitations accurately.

## Reusable areas to assess

| Area | Preferred direction |
|---|---|
| Rendering, layout, lists and trees | Standard Avalonia controls and framework-managed measurement, clipping, selection, scrolling and repainting. |
| Screen/data synchronization | Observable view models and bindings, with explicit boundaries around legacy mutable rules. |
| Menus, toolbar buttons and shortcuts | Shared commands with one availability policy and error boundary. |
| Input and validation | Reusable searchable/custom-entry editors, shared validation and consistent confirmation/cancellation behavior. |
| Undo/redo | Reversible shared actions or appropriately bounded snapshots; preserve the existing inventory history while designing any expansion. |
| Long-running work | Task-based operations, progress, cancellation, isolated preparation and safe commit/cleanup. |
| Persistence and recovery | Atomic saves, backups and recovery behavior, retaining existing character encoding/schema and settings/PDF associations. |
| Diagnostics, dependencies and testing | Structured privacy-preserving logs, reviewed dependency updates and meaningful automated/platform regressions. |

## Rendering priority

Repeated repaint defects justify examining shared invalidation/caching behavior before adding more screen-specific refresh calls. Prioritize the frequently affected item lists and changing detail values. The first shared repaint corrections and session-only full-redraw diagnostic are implemented and documented in [RENDERING.md](RENDERING.md). Use the cached-versus-complete frame checks as acceptance coverage for the next bounded standard-control migration. Prefer event-driven correctness and standard controls over continuously repainting the whole window to hide defects.

The first bounded migrations are the weapon, armor and weapon-accessory pickers. They share catalog presentation and standard controls, documented in [CATALOG_PICKERS.md](CATALOG_PICKERS.md). The weapon path binds standard Avalonia controls to `WeaponPickerModel`, while `frmSelectWeapon` preserves existing purchase callers. See [WEAPON_PICKER.md](WEAPON_PICKER.md). Character close now explicitly removes its tab header before disposing the editor; regression checks cover canceled, inactive, active and final closes. The character-switching audit now covers repeated creation/career transitions, active tab synchronization, inactive close and final Close Character command availability. Main-window toolbar ownership is explicit and shared by both editor types; see [CHARACTER_SWITCHING.md](CHARACTER_SWITCHING.md). Cross-character Paste availability and shared modal cancellation have also been corrected, with contextual inventory actions documented in [INVENTORY_CONTEXT_ACTIONS.md](INVENTORY_CONTEXT_ACTIONS.md). The weapon-accessory dialog now uses the shared native picker, including mount filtering, source revalidation and safe empty/error states. Canonical accessory parent lookup and literal gear search now have purchase/cancellation regressions, including decorated vehicle weapon labels. Continue the audit with remaining edit commands and downstream purchase prompts. Select subsequent screen migrations from confirmed defects and integration cost. Keep Shadowrun rules, character compatibility and Mac/Windows usability as acceptance criteria throughout.

## Final consistency pass

After the section migrations are complete, review the modernized code and maintenance documentation together for consistent terminology, descriptive variable names, stale comments, missing explanations of non-obvious blocks, architecture boundaries and reproducible verification instructions. Documentation and naming are acceptance work for each section as well as this final pass; they should not accumulate until the end of the project.
