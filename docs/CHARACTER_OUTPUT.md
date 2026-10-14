# Character sheet output

Print opens a separate character-sheet preview using the bundled XSL templates and the native browser engine. Character `.chum` files and their encoding are unchanged. The preview can export self-contained UTF-8 HTML and print XML; portraits are embedded PNG data URLs, so exports do not rely on temporary image files.

On macOS, **Print Character** opens the native print panel. Choose **PDF > Save as PDF** there to save the paginated sheet. There is no second PDF button. Windows also offers a direct **Save PDF** button through WebView2. The preview supports the usual Print keyboard shortcut.

Rendering snapshots character XML on the UI thread and transforms that snapshot asynchronously. A generation check discards stale results after switching sheets. Printing waits until the current document, images and fonts finish loading. Errors leave the character open and record diagnostic event IDs without character contents or paths.

The browser adapter exposes printing as an optional capability. An unavailable browser leaves HTML/XML exports usable. Mac uses WKWebView's asynchronous AppKit print operation, including print CSS and paper pagination; its ordinary `createPDF` API captures screen layout instead. Shared system print settings are copied before use. Windows uses the native WebView2 print APIs. No additional browser or PDF conversion dependency is required.

## Reproducing output problems

Run the executable with `--print-preview /path/to/character.chum` to open that character's preview directly. For an automated native-engine PDF check, run `--export-pdf /path/to/character.chum /path/to/output.pdf`; it exits after exporting, with a nonzero exit code on failure. Use disposable test characters and a fresh output path. The export command does not send a physical print job.

Portable smoke checks cover transformation, embedded portraits, template path confinement, repeated refreshes and missing print capabilities. Verify the native dialog and long-sheet pagination on each target platform before release. Diagnostic events beginning `character-sheet-`, `character-print-` and `character-pdf-` identify render, dialog and export failures.
