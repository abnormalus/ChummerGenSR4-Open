# Bulk-operation stability

## PACKS

The selection dialog retains Add Again, implemented as an iterative async loop rather
than recursive imports. Each kit is a separate transaction. Successfully imported
previous kits remain if a later selection is cancelled.

The live character is serialized directly into memory using the existing UTF-16
serializer. No temporary character file is created, no filename is changed, and no
save or migration is applied to the original file. A private workspace loads this
snapshot read-only and runs the established PACKS item/rating rules against the copy.
The adapter is isolated in `frmCreate.Packs.Rules.cs`; its controls are not shown,
registered in inventory history, or merged into application menus. This is an
incremental boundary around legacy rules, not a second independent rules engine.

The owned progress dialog disables the other application windows. Safe entry and
nested-gear boundaries check cancellation and yield to the UI dispatcher. Individual
legacy constructors and item-rule dialogs remain synchronous; no character model or
UI control is moved to a worker thread. Cancel/close/quit cancels operation-owned
detail dialogs so nested prompts can unwind before cleanup finishes. Invalid or
missing catalog data raises a recoverable failure and discards the copy.

Before commit, the live snapshot, filename and saved rules-profile hash must still
match the inputs. A queued/native edit or changed profile discards the prepared copy
and asks the user to retry, preserving the newer work. Only a complete result is
shown as a fully initialized replacement editor. The old
editor is then retired without saving; identity, filename and selected tab are kept,
and the result is dirty until explicitly saved. Existing inventory history is carried
forward, with the kit inventory change added within the five-action limit. Inventory
Undo retains its existing scope: it does not undo kit attributes, skills or qualities.
Cancelling or failing preparation never replaces the original editor or model.

## Batch output

See `CHARACTER_OUTPUT.md`. Preparation locks the file list, yields at character
boundaries, and discards partial output. Read-only loading suppresses both legacy
quality conversion saves and movement's lazy automatic save. Interactive loads and
explicit saves retain their existing behavior.

## Shutdown and diagnostics

Quit first requests cancellation and awaits active PACKS and batch cleanup. It then
uses the existing single close/unsaved-prompt sequence. Unsaved changes can still
cancel Quit. Logs record fixed `packs.*`, `batch-output-*`, and quit lifecycle IDs;
character names, paths and contents are not logged or uploaded.

## Verification

Headless checks cover memory snapshots, all six bundled kit families, nested gear,
partial-preparation cancellation/failure, editor handoff and inventory undo. Every
bundled character fixture also undergoes file and memory round trips and both Foundry
export contract checks. The headless backend's modal test loop delivers posted window
events; production bulk operations contain no manual UI event pumping.

For real dispatcher checks, run the platform apphost (STA `.exe` on Windows):

`ChummerMac --check-bulk /absolute/path/to/generated-creation.chum /absolute/path/to/report.json`

Use the generated `creation.chum` from smoke checks. Preferences/logs are isolated
under `native-bulk-user` beside the report. The check exercises native editor commit,
Cancel during a large kit, Quit while a Fake SIN detail prompt is open, and batch
cleanup; it asserts the source character bytes are unchanged and exits. A simulated
unsaved-close guard keeps the application alive long enough to finish its checks;
normal Quit then closes the test windows. The CI report and nonzero failures gate
both Mac and Windows packages, with a 120-second process timeout.
