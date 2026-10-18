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

The first bounded migrations are the weapon, armor and weapon-accessory pickers. They share catalog presentation and standard controls, documented in [CATALOG_PICKERS.md](CATALOG_PICKERS.md). The weapon path binds standard Avalonia controls to `WeaponPickerModel`, while `frmSelectWeapon` preserves existing purchase callers. See [WEAPON_PICKER.md](WEAPON_PICKER.md). Character close now explicitly removes its tab header before disposing the editor; regression checks cover canceled, inactive, active and final closes. The character-switching audit now covers repeated creation/career transitions, active tab synchronization, inactive close and final Close Character command availability. Main-window toolbar ownership is explicit and shared by both editor types; see [CHARACTER_SWITCHING.md](CHARACTER_SWITCHING.md). Cross-character Paste availability and shared modal cancellation have also been corrected, with contextual inventory actions documented in [INVENTORY_CONTEXT_ACTIONS.md](INVENTORY_CONTEXT_ACTIONS.md). The weapon-accessory dialog now uses the shared native picker, including mount filtering, source revalidation and safe empty/error states. Canonical accessory parent lookup and literal gear search now have purchase/cancellation regressions, including decorated vehicle weapon labels. Gear category browsing, search and confirmation now share an extracted capacity/device eligibility policy, with source/context revalidation and native regressions. Armor-modification selection now adds reusable native rating support; standard NumericUpDown is the target for every remaining legacy spinner, tracked in [NUMERIC_INPUTS.md](NUMERIC_INPUTS.md). Continue the audit with remaining edit commands and downstream purchase prompts. Select subsequent screen migrations from confirmed defects and integration cost. Keep Shadowrun rules, character compatibility and Mac/Windows usability as acceptance criteria throughout.

## Final consistency pass

After the section migrations are complete, review the modernized code and maintenance documentation together for consistent terminology, descriptive variable names, stale comments, missing explanations of non-obvious blocks, architecture boundaries and reproducible verification instructions. Documentation and naming are acceptance work for each section as well as this final pass; they should not accumulate until the end of the project.

## Empty workspace artwork

The supplied `branding/workspace-emblem.png` is shown only while the main MDI workspace has no character windows. `frmMain.InitializeEmptyWorkspace` uses the existing aspect-preserving centered Zoom layout and MDI activation notifications, including final-character closure. Artwork load failures are diagnostic-only; closing the main window releases the image. This does not change character or settings persistence. Verify startup, opening a character, closing the final character, and resizing at different display scales before release.

Native checks verify artwork before opening a character, hide it while an editor is open and restore it after final closure. Empty-workspace screenshots at two window sizes support visual inspection without capturing character fields.

## Interaction stability acceptance

Actual PACKS replacement followed by Add Armor/Add Gear cancellation is part of native validation. Menu refresh errors fail the regression rather than being swallowed behind a passing test. Structural command/editor/modal diagnostics supplement errors without recording character content. See [CHARACTER_SWITCHING.md](CHARACTER_SWITCHING.md). The intermittent blocked-input report remains under observation; the confirmed menu restoration index defect is covered.

## Theme acceptance

Every migrated section must respect the application/system light and dark theme, including live changes. Prefer theme resources to fixed presentation colors; explicit semantic colors such as review-required warnings still need readable contrast. Workspace artwork must use real alpha transparency rather than a baked light/dark rectangle. Native checks exercise the maintained host-theme bridge for the legacy workspace; native screen migrations also need light/dark visual review. The top-level legacy window explicitly starts `MajorsilenceFormsTheme.FollowHost` because it is not an embedded presenter. This does not claim all legacy designer colors have already been migrated.

The workspace uses the user's transparent circular dragon PNG unchanged. Native checks verify transparent corners and capture the empty workspace at two sizes in both themes, so an opaque background cannot silently return.

Weapon-modification foci now share the existing binding record and limit/accounting paths without duplicate Gear or new save fields. Regression coverage includes BP/Karma, lifecycle and confirmation cancellation; see [WEAPON_FOCUS_BINDING.md](WEAPON_FOCUS_BINDING.md). The installed modifier remains the single source of its legacy dice bonus.

Accessory quantities retain individual legacy records while grouping equivalent units in the tree. The standard native quantity input and shared purchase plan avoid new save fields; canonical unit deletion and expense undo no longer depend on presentation depth. See [ACCESSORY_QUANTITIES.md](ACCESSORY_QUANTITIES.md). Shared ammunition reserve projections are documented below.

Cyberware deletion now removes actual nested rows, and stale selections clear safely in creation/career mode; see [CYBERWARE_SELECTION.md](CYBERWARE_SELECTION.md). Release identity is shared by About, diagnostics, binaries and package metadata, with source and consistency checks documented in [BUILD_IDENTITY.md](BUILD_IDENTITY.md).

Character attribute and BP-summary contrast now has actual-screen light/dark transition coverage. Shared system-color declarations retain their identity and resolve against the host theme instead of the drawing package's fixed classic palette. See [CHARACTER_THEME.md](CHARACTER_THEME.md).

Daily GitHub issue review and current evidence are tracked in [ISSUE_TRIAGE.md](ISSUE_TRIAGE.md). Shared ammunition uses canonical reserve projections with command guards and actual reload/cancellation/magazine-switch acceptance; cross-platform release verification is tracked in [AMMUNITION_VIEWS.md](AMMUNITION_VIEWS.md).

Direct native dialogs now follow their owner theme, including live changes and closure cleanup; see [NATIVE_DIALOG_THEME.md](NATIVE_DIALOG_THEME.md). This is independent of the legacy drawing palette bridge.

## Publication cadence

Batch related changes locally and push incremental GitHub updates no more often than every 4–6 hours of modernization progress, following the user's October 17 instruction. Local commits, builds, tests and documentation continue within that window. A finished small migration is not by itself a reason to push, merge or publish Nightly. Revalidate the combined exact source before publishing a batch, retain only the verified Stable/latest Nightly package sets, and keep Stable unchanged until full testing and user promotion.
