# Inventory Copy serialization boundary

Creation and career each retain the same Copy command body, with nine item-serialization sites selected by the main/subtab and canonical tree selection. They create a Unicode XML document rooted at `character`, invoke existing model `Save` methods, parse the completed stream and publish the application clipboard. This is separate from whole-character snapshots and operating-system clipboard APIs.

Keep selection eligibility, exact Commlink/OperatingSystem/Gear dispatch, attached-weapon inclusion and Paste availability with their callers. A shared XML wrapper should own writer/stream lifetime and document construction, not object lookup or clipboard policy. No interchangeable item serializer was found in the existing whole-character snapshot implementation.

## Observed failure ordering

Gear Copy sets `ClipboardContentType` during its model-save branch, before an associated weapon and the final document are serialized. A dangling WeaponID causes the existing command to retain the previous clipboard document while leaving its type changed to Gear. Both editors' actual Copy commands have now demonstrated this behavior in headless characterization. The surrounding catch suppresses that failure.

Do not describe current clipboard publication as atomic. Preserve this ordering during a behavior-neutral extraction, or repair it as an explicit separately characterized behavior change. Merely moving publication into a new helper must not hide that decision.

`ClipboardCopyChecks` also verifies successful character-root UTF-16 XML with escaped/non-Latin/supplementary-Unicode notes, unchanged character snapshots on success/failure, and a stale selected ID that leaves both clipboard values unchanged. The stale-ID case changes the current node's Tag without raising AfterSelect: deliberately selecting an absent model invokes a different editor-refresh failure before Copy executes. Global clipboard state is restored in `finally`.

All ten assertions passed in the corrected baseline, followed by the complete headless suite, all 399 file/memory round trips, both Foundry targets and shutdown (187.6 seconds). This matrix does not yet establish every copied item family or native clipboard/Paste interaction. The locally prepared `ClipboardDocument.Create` now owns the repeated wrapper at all 18 sites; item-save callbacks and code outside the Copy methods are mechanically unchanged. The extraction builds and passed the same ten editor assertions, five independent wrapper contracts and the full headless/399/both Foundry/shutdown suite (184.4 seconds). Current-source platform verification remains separate.

The wrapper owns and disposes the writer, reader and temporary stream on success and failure. It retains Unicode writer formatting and BOM-aware reading. Callbacks can still mutate caller state and throw; the helper does not read global state, publish clipboard data, swallow errors or roll back callback effects. Five independent contracts cover fixed escaped/multi-item XML, empty output, successful resource closure, callback exception identity and parse-failure cleanup. Those are separate from the actual-editor baseline and do not derive expected documents through the wrapper.

Source review verifies all 18 current callback bodies against recorded originals after indentation normalization, and verifies source outside each Copy method unchanged. This covers Save dispatch, attached-weapon writes and in-callback type side effects; it does not turn the ten actual-editor assertions into coverage of every selection family. Both original and extracted paths retain those explicit coverage limits.
