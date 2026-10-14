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

## Diagnostics

Help → Open Diagnostic Logs opens the local log folder. On Mac it is under Library/Application Support/ChummerGenSR4Mac/logs. Logs contain UTC time, command ID, assembly version, unique compiled-module build ID, OS/runtime/architecture, exception types/HRESULTs, and method stacks without source-file paths. Exception messages, character XML, aliases, and character filenames are omitted. No logs are uploaded automatically.

The active diagnostics.jsonl file rotates at approximately 2 MiB with three retained archives. Disk/permission failures in logging never block app operations. Caught file failures and main-loop failures are recorded; native menu setup failures retain the ordinary menu as a fallback. Diagnostic coverage is intentionally incremental: additional caught errors should explicitly call AppDiagnostics.Record with a fixed category.

## Validation

Use the installed .NET 10 SDK to build Chummer.Mac.sln. Run the application with --smoke-test OUTPUT_DIRECTORY Chummer.Mac/assets/saves to check existing character-file compatibility and meaningful UI regressions. The test user directory isolates preferences, saves, and logs.

.github/workflows/desktop-build.yml builds and checks the shared application on Mac and Windows, then publishes platform-specific output. These checks run after pushing the workflow to GitHub; local Mac checks do not establish Windows usability. Test native menus and window interactions on the relevant OS as well as the automated checks.

Keep vendor/LOCAL_PATCHES.md updated when changing the vendored toolkit. Each behavioral patch should have a regression check or a recorded native verification. Review those patches before upgrading toolkit dependencies.

## Localization and native menus

Menu labels come from the existing language keys. New commands have English keys so other language packs fall back safely and can translate them later. Mac gestures use Command while the command implementation stays shared with Windows Control shortcuts. Mac About/Settings/Quit use the observed application-menu object, preserving system Services/Hide entries. Quit uses the shared application close sequence, including unsaved-character prompts. Window commands and menu enabled states refresh from the current source tree.

## Branding and hang traces

`Chummer.Mac/branding/chummer.png` is the supplied dragon emblem. The ICO supplies shared form branding and the Windows executable icon; packaging/mac/Chummer.icns supplies Finder/Dock branding. About loads the same PNG. Action icons retain their functional meaning.

Each diagnostic line now includes a process ID and random session ID, allowing multiple builds/processes to be distinguished. Quit logs start, completion/cancellation, and failures. Character close logs unsaved-prompt opening/answer and save progress. A background watchdog posts bounded UI probes every two seconds and records a single unresponsive event after 15 seconds without a UI reply, followed by recovery when detected. Worker timer gaps caused by sleep are excluded. Cached open-form count and modal form type contain no character names or document paths. The watchdog identifies a stall and last command; it cannot reconstruct a killed process's call stack or guarantee logging if the whole process or filesystem is stalled.
