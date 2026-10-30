# Desktop maintenance and diagnostics

The shared desktop application currently lives in Chummer.Mac. It can be published for Mac or Windows; the original Chummer project remains the upstream reference. Avoid adding platform checks to character rules or serializers.

## Shared boundaries

- AppPlatform owns platform presentation and opening the diagnostic folder.
- DesktopMenus projects the existing menu command tree into native Mac menus. The original handlers remain authoritative; Windows retains in-window menus.
- MenuItem.CommandStarting provides one pre-handler observation point for both menu and toolbar commands. Diagnostics record fixed command IDs, never captions or character data.
- CharacterFileDialogs owns native file-picker behavior and invokes the existing serializer.
- PromptDetails owns normalization/editing. DetailChoices supplies quality suggestions and filters enabled sources; SearchableChoices supplies reusable editable combo filtering for quality details and knowledge names.
- `quality-detail-choices.xml` includes the supplied allergy recommendations, filtered by the quality's Common/Uncommon rarity. Severity does not change the list. Custom details remain allowed.
- `knowledge-suggestions.xml` supplements the existing ruleset skills with the supplied `knowledge skills.csv` and `interests.csv` entries. The CSV category `Interests` maps to the existing `Interest` category. Suggestions are optional names, not new rules or source attributions; the category is chosen first and curates the available names. The arrow restores the full category list, while typing filters it. Add Specialty uses the shared free-text prompt and the existing career Karma flow. Keep these suggestions separate from `skills.xml` so ruleset updates preserve them.
- Keep extracting rules from window handlers when changing those areas, rather than rewriting the rules engine during unrelated UI fixes.

### Native inline controller adapters

`NativeInlineTextBinding`, `NativeInlineChoiceBinding`, `NativeInlineNumericBinding` and
`NativeInlineCommandBinding` bridge existing controller controls into native sections.
They share `NativeControlObservation` for EnabledChanged, VisibleChanged and Invalidated
subscriptions. Dispose the binding when its owning section closes: the observer detaches
only its own callbacks and never disposes the controller control. It performs no initial
refresh; each adapter synchronizes after initializing its native input and handlers.

Text/value events, feedback guards, validation and native input handlers stay in the
individual adapters because their semantics differ. This consolidation replaces four
copies of common subscription/cleanup code without sharing their domain policies.
`NativeControlObservationChecks` exercises forwarding, independent observers and repeated
disposal in the headless suite; existing section checks cover edits and controller state.
Changes to these adapters also need the affected native section checks on Mac and Windows
before release.

## Adept Power Point summaries

Creation and career share pure `AdeptPowerPointBudget` arithmetic and a separate culture-sensitive `AdeptPowerPointSummary` formatter. Existing `Power` pricing and editor-owned row refresh remain authoritative. See [ADEPT_POWER_POINTS.md](ADEPT_POWER_POINTS.md) for inputs, ownership, preserved formats and independent baseline evidence.

## Diagnostics

Help → Open Diagnostic Logs opens the local log folder. On Mac it is under Library/Application Support/ChummerGenSR4Mac/logs. Logs contain UTC time, command ID, assembly version, unique compiled-module build ID, OS/runtime/architecture, exception types/HRESULTs, and method stacks without source-file paths. The specific numeric Avalonia render-timer registration error is recorded when present; arbitrary exception messages, character XML, aliases, and character filenames are omitted. No logs are uploaded automatically.

The active diagnostics.jsonl file rotates at approximately 2 MiB with three retained archives. Disk/permission failures in logging never block app operations. Caught file failures and main-loop failures are recorded; native menu setup failures retain the ordinary menu as a fallback. Diagnostic coverage is intentionally incremental: additional caught errors should explicitly call AppDiagnostics.Record with a fixed category.

## Validation

### Bounded execution and interrupted work

Use `scripts/run-bounded.py` for long verification stages. It streams child output,
prints elapsed/idle status every 15 seconds, and stops the process tree when the total
deadline or the no-output limit is reached. Exit 124 means timeout, never success;
ordinary failure codes are preserved. The default no-output limit is 180 seconds.
Raise it explicitly for a known quiet operation, with a recorded reason, while retaining
an overall deadline. The limits are execution safeguards, not evidence that a stage passed.

```text
python3 scripts/run-bounded.py --label restore --timeout 300 -- dotnet restore Chummer.Mac.sln --locked-mode -p:UsedAvaloniaProducts=
python3 scripts/run-bounded.py --label build --timeout 600 -- dotnet build Chummer.Mac.sln -c Release --no-restore --disable-build-servers -p:UsedAvaloniaProducts=
python3 scripts/run-bounded.py --label headless --timeout 600 -- dotnet Chummer.Mac/bin/Release/net10.0/ChummerMac.dll --smoke-test artifacts/checks Chummer.Mac/assets/saves
```

The examples use the Mac `python3` command. On Windows substitute `python` if that selects the installed Python 3 interpreter; verify its version first.

Pass `--log-file artifacts/verification/STAGE.log` to retain raw child output and runner
status together. Parent directories are created before launching the command; each write
is flushed, and an existing stage log is overwritten so a retry cannot mix old success
with new failures. Use a separate output directory per source revision when retaining
multiple attempts. Logs can contain everything the child prints; use the same handling
as the underlying verification output. Desktop CI uploads these logs on success or failure.

After a timeout, retain the command, source revision, log and last completed check.
Investigate that stage before repeating it; do not rerun an unchanged hung command
indefinitely or treat a smaller passing subset as full acceptance. Build-server stalls
can be isolated with `--disable-build-servers` and `-p:UseSharedCompilation=false`.
Cached dependencies may support local investigation but do not establish a fresh locked
restore or vulnerability audit. CI uses the same wrapper for restore/build/headless and
also has outer job/step limits; native operation/report/count gates remain separate.

Before ending a work session, record completed work, its verification limits, the active
branch/PR and the next concrete action. These checkpoints support resumption; they do
not themselves schedule unattended work.

Use the installed .NET 10 SDK to build Chummer.Mac.sln. Run the application with --smoke-test OUTPUT_DIRECTORY Chummer.Mac/assets/saves to check existing character-file compatibility and meaningful UI regressions. The test user directory isolates preferences, saves, and logs.

.github/workflows/desktop-build.yml builds and checks the shared application on Mac and Windows, then publishes platform-specific output. These checks run after pushing the workflow to GitHub; local Mac checks do not establish Windows usability. Test native menus and window interactions on the relevant OS as well as the automated checks.

Keep vendor/LOCAL_PATCHES.md updated when changing the vendored toolkit. Each behavioral patch should have a regression check or a recorded native verification. Review those patches before upgrading toolkit dependencies.

## Localization and native menus

Menu labels come from the existing language keys. New commands have English keys so other language packs fall back safely and can translate them later. Mac gestures use Command while the command implementation stays shared with Windows Control shortcuts. Mac About/Settings/Quit use the observed application-menu object, preserving system Services/Hide entries. Quit uses the shared application close sequence, including unsaved-character prompts. Window commands and menu enabled states refresh from the current source tree.

## Branding and hang traces

`Chummer.Mac/branding/chummer.png` is the supplied dragon emblem. The ICO supplies shared form branding and the Windows executable icon; packaging/mac/Chummer.icns supplies Finder/Dock branding. About loads the same PNG. Action icons retain their functional meaning.

Each diagnostic line now includes a process ID and random session ID, allowing multiple builds/processes to be distinguished. Quit logs start, completion/cancellation, and failures. Character close logs unsaved-prompt opening/answer and save progress. A background watchdog posts bounded UI probes every two seconds and records a single unresponsive event after 15 seconds without a UI reply, followed by recovery when detected. Worker timer gaps caused by sleep are excluded. Cached open-form count and modal form type contain no character names or document paths. The watchdog identifies a stall and last command; it cannot reconstruct a killed process's call stack or guarantee logging if the whole process or filesystem is stalled.

## Recoverable commands

AppErrors subscribes to the toolkit's UI ThreadException boundary. Pointer/timer handlers and shared menu commands (including native-menu invocations) report managed command exceptions without terminating the application. The notice advises reviewing the character before saving: arbitrary commands are not transactional, so recovery does not imply rollback. Failures outside these event boundaries still use the fatal main-loop path. Cyberware selection explicitly rejects missing parent/rules entries and non-OK picker results. Plugin pickers curate subsystem categories and cancel with an informational notice when the enabled sources offer no compatible candidates.

Records on File uses the shared detail picker with 52 organization suggestions from `Shadowrun_2060_Records_on_File_Options.csv`. Organization names remain the stored detail; type, region, and 2060 status are retained as suggestion metadata. Legacy organizations stay available for historical records, and custom entries remain allowed.

Explicit Quit uses one shared command on Mac and Windows. Closing handlers get one approval pass; cancelled Quit keeps the loop alive and forbids process termination. After accepted Quit closes all forms and the main loop/cleanup return, Program logs process-exit and terminates the process, including any surviving native or foreground worker threads. Closing an ordinary window does not set the explicit-Quit flag.

## Character portraits

CharacterPortraits provides shared import/edit behavior for creation and career views. PortraitEditor is a native Avalonia window with a Fluent theme, crop/move modes, zoom, full/portrait/square framing, and output scaling. PortraitImage owns Skia decoding (including JPEG orientation), crop clamping and PNG export; source input has no prescribed dimensions or aspect ratio. Decoding has a memory budget and stored output is capped at a 4096-pixel longest edge to keep character files practical; the UI displays the actual output dimensions. Changes are applied only after Use Portrait and stored in the existing base64 mugshot field. Cancel retains the current image. Preview uses proportional fitting. No character file schema changes are required.

### Grouped appearance colors

Hair, eyes, and skin use `AppearanceColors` with the editable `SearchableChoices` behavior in creation and career modes. Hair and eyes use `Chummer.Mac/assets/data/appearance-colors.xml`; skin uses its independent `skin-colors.xml` palette with the same rendering and filtering behavior. The `skin` argument selects that palette. For both palettes, family order follows the data, with shades sorted alphabetically. Families are selectable values. Searches match shade names or families and retain the family row above matching shades. Owner drawing supplies indentation and bold family rows; stored character fields contain plain names or custom text. `SearchableChoices.AttachFiltered` accepts a query-aware provider for other grouped lists without knowing their domain. Model handlers ignore intermediate list-refresh events, preserving character values while filtering.

### Sourcebook PDF reader

`CommonFunctions.OpenPDF` delegates to `SourcebookReader`, which resolves localized book codes and applies the existing `SourcebookInfo.Offset`. Sourcebook links and Settings → sourcebook PDF Test use the same path. An external executable is no longer required; existing linked paths, offsets, and character files remain compatible.

The reader hosts bundled Mozilla PDF.js 6.4.299 in the toolkit’s existing native WebBrowser control (WKWebView on macOS, WebView2 on Windows). The distribution is pinned with its npm integrity hash and Apache-2.0 license in `Chummer.Mac/pdfjs/VERSION.txt` and `LICENSE`. Its legacy display/viewer builds, worker, CMaps, standard fonts, image assets, and WASM decoders are included locally, without CDN access or a Chromium bundle. To update, verify the official npm tarball integrity, replace all matching-version files together, and retest native rendering/search plus byte-range and shutdown checks.

Each reader owns a `PdfLocalServer` using .NET 10 Kestrel (bundled via the ASP.NET Core framework reference) on IPv4 loopback with an unpredictable route. It binds port zero directly on Mac and Windows without requiring HTTP.sys URL ACLs. Request logging is disabled to avoid recording paths or route tokens. It exposes only the selected book and bundled assets, streams PDF byte ranges, rejects traversal, and stops when the form closes, including application Quit. PDF scripting and external document links are disabled. Documents are not copied, uploaded, or written by the reader. Missing files, invalid references, unavailable engines, password cancellation, and parsing failures have recoverable messages; lifecycle/error events use the existing privacy-preserving diagnostics.

For a reproducible native check without modifying book preferences, launch `ChummerMac --pdf /absolute/path/test.pdf 2`. Verify the requested page, previous/next, text search and zoom, then Quit. Windows requires the WebView2 runtime; native Windows validation remains separate from the Mac build.

### Windows MSI packaging

Cross-publish the shared project with `dotnet publish Chummer.Mac/Chummer.Mac.csproj -c Release -r win-x64 --self-contained true -p:UsedAvaloniaProducts= -o artifacts/windows-publish`. On macOS, install Homebrew `msitools`, then run `python3 packaging/windows/build-msi.py artifacts/windows-publish artifacts/ChummerSR4.msi`. The script also works on Linux with `wixl` installed. Keep the stable UpgradeCode and path-derived component GUIDs; pass `--version` when bumping the application version. Same-version previews may replace each other; newer versions block downgrades.

The MSI embeds the full publish output, installs per user into `%LOCALAPPDATA%\Programs\ChummerSR4`, registers a Start menu shortcut and dragon icon, and supports standard Windows uninstall. HKCU component key paths avoid per-user MSI repair ambiguity. .NET is bundled; WebView2 remains a Windows prerequisite for the PDF reader. Character files and the separate roaming user settings directory are outside the install tree. Verify the MSI's x64 summary, shortcut and upgrade tables, then extract its cabinet and compare every file hash to the publish output. Native Windows install/launch/upgrade/uninstall and PDF behavior require a Windows test machine.

Windows executables must embed `packaging/windows/app.manifest` via `ApplicationManifest`. The explicit Windows 10/11 compatibility declaration permits the layered child windows used by Avalonia's native PDF browser host; an unmanifested build can fail in `Win32NativeControlHost.DumbWindow` before the PDF loads. Keep standard-user (`asInvoker`) privileges and per-monitor DPI awareness. Check the published PE's RT_MANIFEST resource, not just the source XML, when diagnosing this failure.

Compatibility capabilities and remaining interaction checks are recorded in `COMPATIBILITY_CONTROLS.md`. Bundled reader integrity and update procedures are in `PDFJS_MAINTENANCE.md`.

Rendering ownership, the Help → Full Redraw (Diagnostic) option, visual regressions and the next bounded control migration are documented in [RENDERING.md](RENDERING.md).
