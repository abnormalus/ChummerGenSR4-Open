# Local Xcode verification

November 16, 2026. Xcode 27.2 (27B52), Apple Silicon Mac.

## Passed

- Full XCTest suite: 11 tests, zero failures, including all 399 repository `.chum`
  characters. The first completed XCTest run reported 161.415 seconds. No fixture
  was modified. The Swift Testing runner also reports zero tests because this
  package uses XCTest; that is separate from the 11 executed XCTest tests.
- Generic iOS Simulator app build, including arm64 and x86_64 compilation:
  `xcodebuild -project ChummerDossier.xcodeproj -scheme ChummerDossier
  -destination 'generic/platform=iOS Simulator' CODE_SIGNING_ALLOWED=NO build`.
- Rebuilt successfully after declaring copy-mode document opening and wiring
  incoming file URLs to the same loader as the file picker. Non-file URLs are
  rejected. The opening-in-place configuration warning is resolved.
- Project/Info.plist validation and whitespace checks.

The source includes no changes to DossierCore since these XCTest checks. The app
changes after the checks concern file-open event routing and Info.plist only.

## Not established

The newly installed simulator runtime's first iPhone boot ended with **Data
Migration Failed** after 4 minutes 22 seconds. It reports Booted, but installation
and screen capture did not promptly complete. No simulator erase/reset was used.
Compilation success is not evidence of native launch, visual layout, real touch,
long-press exclusivity, keyboard, VoiceOver, or Files-provider behavior.

The earlier GitHub iOS run 43920109380 was cancelled before execution. Local results
above do not claim successful hosted CI. Inspect the current PR checks separately.
Desktop load/save/mobile reimport and equipment reconciliation remain unverified
and unfinished as described in README and SPECIFICATION.

Local working logs were retained outside the repository:
`/tmp/chummer-dossier-xctest.log` and `/tmp/chummer-dossier-ios-build.log`.
Do not infer later-source acceptance from this record; repeat affected checks.

## Resumed work: unsaved edits guard

November 16, 2026, in the Codex project workspace:

- Added confirmation before replacing a character with edits since import or the
  last successful Save Copy. Both Files URL events and the picker use the guard.
  The incoming file is parsed before prompting, so an invalid import retains the
  current sheet. Keep Editing cancels replacement.
- Successful export records the exported snapshot; failed/cancelled exports do
  not clear unsaved status. Edits made during export remain unsaved.
- Generic iOS Simulator build passed for arm64 and x86_64 using Xcode 27.2.
  `git diff --check` passed. Build log: `/tmp/chummer-resume-build.log`.
- Native confirmation/export interaction checks remain pending. No new core test
  run was needed for this app-only change. Autosave, process-exit recovery and
  file-conflict detection remain unfinished.

## Compact character library and autosave

November 16, 2026. Supersedes the replacement confirmation above: switching saves
rather than prompting to discard. Tap the navigation title to select an imported
character or open another. There is no additional character navigation strip.

- Atomic app-owned archive stores working copies and last selection. Draft saves
  retain incomplete numeric input; desktop export still validates it. Debounced
  saves run after edits and synchronous saves run before switching and on inactive
  or background scene transitions. Originals are not overwritten.
- Three new persistence tests passed: multiple characters/edits/selection restore,
  write failure retains previous archive and state, corrupt archive fails closed.
- Generic iOS Simulator build passed (arm64 and x86_64). Build and test logs:
  `/tmp/chummer-resume-build.log`, `/tmp/chummer-library-tests.log`.
- Single scene enabled to avoid independent archive writers. Character view state
  resets on switching so session-only damage blocks do not leak between runners.
- Native menu, keyboard, screen-lock/relaunch, VoiceOver and phone/tablet layout
  acceptance remain pending. No claims of device lifecycle acceptance from builds.

## Native Xcode simulator checks

November 16, 2026, Xcode 27.2. In this installation the simulator UI is **Device
Hub** at `Xcode.app/Contents/Applications/DeviceHub.app`; the older
`Contents/Developer/Applications/Simulator.app` path does not exist.

- Created and successfully booted a dedicated iPhone 15 simulator on iOS 27.2:
  `84E396B6-E1D5-4B42-908B-DE792CA081D8`. Startup completed in about 21 seconds.
  The earlier first-boot blocker did not recur.
- Installed and launched the built app. Opened two synthetic `.chum` documents
  through simulator file URLs, using the app's incoming-file handler. No user
  characters were used. This does not establish Files-provider picker acceptance.
- Edited QA Bravo's alias through the native field, switched to QA Alpha through
  the title menu, and switched back. The edited alias was retained.
- Used Device Hub's Lock control, woke/unlocked via its Home control, and observed
  the edited dossier retained. This is simulator lock coverage, not physical-device
  protected-data acceptance or proof that the lifecycle callback was the sole save.
- Edited Age to 37, left the field active, backgrounded via Home, terminated the app
  with simctl, and relaunched. The selected QA Bravo Edited dossier and Age 37
  restored visibly. Debounced saves and lifecycle saves can both contribute.
- Reviewed iPhone 15 portrait and landscape. The name menu occupies the navigation
  title, with no character tab strip. Basic fields rendered in both orientations.
- Booted the 11-inch iPad Air (M4) on iPadOS 27.2 and launched the demo; reviewed
  portrait and landscape. Cards adapt to two/three columns. **Open layout issue:**
  the demo banner appears to obscure the native top page-tab controls. Tablet
  navigation acceptance is not complete.
- Attempted `xcodebuild test` with the project DossierCore scheme; it has no test
  action configured. Do not report this attempt as passing XCTest. The earlier
  three Swift-package persistence tests and generic simulator builds remain the
  automated evidence. An Xcode test target/action remains to be wired up.

Remaining checks include Files-provider import/export, direct physical-device lock
saving, accessibility/Dynamic Type, damage gestures, tablet tab layout, and desktop
round trips. These native smoke checks do not establish full acceptance.

## Compact iPad header and Dialtone commlink

November 16, 2026. Removed the demo banner and replaced iPad's separate native page
tab row with a compact toolbar menu. Character switching stays in the title menu;
demo status is a short title suffix and menu explanation. Verified the updated
landscape header and equipment page selector in the 11-inch iPad Air simulator.

The user supplied a private Dialtone.chum for local verification. It was not added
to the repository. The file's saved alias is Kracken; this is the title shown in
the app. A copy was imported into the iPad simulator through its incoming-file
handler; the source character was not edited.

- Added recursive gear projection retaining path-based instance identity and
  parent-child relationships. Program options remain beneath their program.
- Two commlink tests passed, including local private-fixture assertions: Fairlight
  Caliban has 13 children, all 15 Matrix programs and Empathy Software are present,
  unchanged export is byte-identical, and gear survives an unrelated field edit
  and reimport. Test fixture is optional via CHUMMER_DIALTONE_FIXTURE.
- iOS simulator build passed for both architectures. Native iPad equipment view
  showed the 16-program count, commlink, OS, upgrades, nested programs and options.
  The accessibility tree included all programs, including the offscreen entries.
- Equipment editing fields now use disclosure groups to reduce the initial page
  footprint. Nested gear is displayed read-only. Existing top-level editing remains.
- Device stats are saved base fields with upgrade rows shown separately, not a
  claim of desktop-equivalent upgraded totals. Effective matrix calculations and
  rules-dependent program limits remain open. No program is omitted by an assumed
  active-program limit or moved to a different saved parent.
- Logs: /tmp/chummer-commlink-tests.log and /tmp/chummer-resume-build.log.

## Shared structured-row style

November 16, 2026. Added a shared row modifier with alternating light tint and thin
bottom dividers. Applied it to personal-data rows, attributes, skills, qualities,
lifestyles, history facts, contact structured fields, equipment entries and pending
equipment. Structured cards use reduced spacing; prose editors remain plain.

On iPad, gear qualifiers and program options follow the name on the same line,
with ratings/stats trailing. Long option text is horizontally scrollable. Gear
children remain indented and receive their own row bands rather than sharing a
parent's background.

Generic iOS simulator build passed for both architectures. Visually checked
Dialtone's personal/attribute rows and equipment/program rows in the 11-inch iPad
Air landscape simulator. Program names, options and ratings appeared together on
one shaded line. No new core tests were required for this presentation-only edit.
Portrait, Dynamic Type and phone visual checks of this final styling remain open.

## Equipment deduplication, references and Matrix initiative

November 16, 2026. Commlinks, programs and ordinary gear now have disjoint displayed
entries; root gear editing is available via context menu/accessibility action in
place of a duplicate editor list. Source codes/pages from saved equipment fields
are shown inline (including nested gear); no sourcebooks or excerpts are added.
Verified references such as SR4 p. 233 and UN p. 198 in the iPad simulator.

Combat now shows provisional Matrix initiative/passes. Active commlink Response
includes saved Response upgrades and home-node bonus; enabled saved improvements
respect unique groups, precedence and custom/noncustom groups. A living-persona
formula is provided for technomancers; special persona initiative can be supplied
manually. Dialtone computes initiative 12, passes 3, verified visibly on iPad.
Desktop program-load penalties, wounds, advanced persona formulas, and other
settings remain outside full rules parity; response/initiative/pass adjustments
are explicit dossier fields. Desktop is still authoritative.

## Alphabetical programs, Loaded and ammunition controls

November 16, 2026. Programs have one alphabetical table, inline options and a Loaded
checkbox column; removed the count/grouping summary. Checkboxes write equipped on
the original nested gear path. Programs are not repeated in commlink/ordinary gear
views. All 16 Dialtone programs remain available, with saved hierarchy preserved
in XML. Alphabetical order does not depend on Loaded state.

Compatible ammunition appears below its weapons and is omitted from the generic
gear display when assigned. Matches use saved exact category data, not fuzzy names;
this is not a full catalog-based restriction/caliber engine. Unmatched ammunition
stays visible in ordinary gear. Shared reserves remain one underlying instance.
Menus suggest SS/SA/BF/FA round totals with 1/2/3/6/9/15 available as other counts.
Loaded magazine spending changes only ammoremaining[2/3/4]; reserve spending changes
only the selected gear qty. No auto-reloading or mixing reserves into a magazine.

- 10 focused tests passed: equipment interactions (3), Matrix initiative (2),
  commlinks/private Dialtone fixture (2), library persistence (3).
- Generic simulator build passed for arm64 and x86_64; diff whitespace check passed.
- Native iPad: Slivergun menu offered Fire 1/2/3/6; Fire 3 changed its flechette
  reserve from 100 to 97, leaving the Fichetti reserve at 100. Alphabetical table
  displayed Analyze, Armor, Attack; toggling Analyze changed Loaded from 1 to 0.
  The simulator's working copy retains these test edits; original Dialtone was
  never written. No character file is added to the repository.
- Logs: /tmp/chummer-equipment-tests.log, /tmp/chummer-resume-build.log.
- Physical-device, phone/final portrait interaction, desktop reimport and advanced
  ammo compatibility acceptance remain pending.

## Actual desktop/mobile compatibility, November 16

Built the existing desktop Chummer loader/saver from source and ran it with the
headless backend and an isolated user directory. Private inputs and all generated
outputs stayed under /tmp, outside the repository. SDK setup/build logs are retained.

- Full Swift suite: 21 tests, zero failures, all 399 repository characters verified.
- Dialtone: mobile edits/export -> desktop load/save -> desktop load/save -> mobile
  reimport passed. Attribute base edit, age, physical/stun damage, ammo quantity,
  nested program equipped state, plain notes, history/modifier metadata, added
  contact/extended fields and pending equipment adjustments survived both saves.
  Desktop independently reports Matrix initiative 12 and passes 3 before the dossier
  manual adjustment. No user source file was written.
- Willow, Blue Smoke and Mongoose mobile exports passed actual desktop load/save/
  reload/resave and mobile reimport with attribute/damage/history/contact/pending
  equipment checks and complete nested gear identities/counts. Optional program
  state checks passed where present.
- An initial Mongoose skill-count assertion failed: desktop adds five Perception
  variants. An independent unedited control produced the exact same additions,
  and no original skill disappeared. The integration check now compares against
  independently desktop-loaded controls instead of assuming a legacy catalog has
  unchanged skill count. All three private fixtures then pass.
- Test phases live in DesktopRoundTripTests and PrivateCharacterRoundTripTests;
  opt-in environment variables keep private fixtures and exports outside the repo.
  Temporary desktop harness calls production Character.Load/Save with isolated
  settings. This establishes model compatibility, not manual desktop window/input
  acceptance. Recheck against later desktop revisions when serialization changes.
- Evidence: /tmp/chummer-full-tests.log, /tmp/chummer-roundtrip/{prepare,desktop,verify}.log,
  /tmp/chummer-private-results/{prepare,verify,verify-controls}.log and per-case
  desktop outputs/unedited controls. Initial failed evidence is retained.

## November 17 at-table companion acceptance

The current scope is a manually editable at-table sheet, without automatic
advancement charges or a purchase/catalog workflow. Existing pending equipment
remains preserved and visible as notes.

- Full core suite: 37 XCTest cases, eight optional phases skipped, zero failures;
  all 399 repository characters checked read-only. Optional private audit and
  integration phases were also run separately and pass.
- Independent desktop-engine comparison for five private characters initially
  identified three differences: loaded-ammo damage/AP and the no-active-commlink
  Matrix baseline. After corrections, the compared attributes, existing skill
  pools, initiative/passes, condition capacities and weapon DV/AP have zero
  differences. Advanced options and missing/desktop-added skills remain outside
  this representative comparison.
- Production desktop load/save followed by mobile reimport preserves manual
  attribute/rating edits, a reloaded magazine, its reserve and an explicit DV
  override. Karma and nuyen remain unchanged; original files were read-only.
- Reload/fire/unload tests verify conservation, returning old rounds to their
  original reserve, separate magazine counts, active-slot selection, failed-load
  atomicity, installed-mod bonuses and avoiding duplicate flechette bonuses.
- Five native UI scenarios pass on both iPhone 15 and iPad Air simulators:
  character-specific sections, combat/vehicle controls, compact contacts/no
  purchase flow, light/dark equipment/powers, and reload/fire/unload. Synthetic
  screenshots were retained and inspected for the changed layout. No physical
  device was used for this batch.

Current reports, xcresult bundles and private audit outputs are retained under
ignored `artifacts/table-companion/`. The reproducible oracle is documented in
`scripts/DesktopAudit/README.md`. No private character files or signing details
are committed. This acceptance does not establish all optional rules, automatic
wound/situational adjustments, finished printing, recovery/conflict handling or
Stable readiness.
