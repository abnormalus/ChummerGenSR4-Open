# SR 4 Dossier for iPhone and iPad

An early native SwiftUI companion inside the Chummer SR4 repository. Open an
existing `.chum` character, edit a linked sheet, and save a separate XML copy.
Desktop Chummer remains the authority for full Shadowrun rules.

## Run

1. Install full Xcode, finish first-launch setup, and install iOS support.
2. Open `ChummerDossier.xcodeproj`, select **ChummerDossier**, and choose an iPhone
   or iPad simulator. The minimum deployment target is iOS/iPadOS 17.
   The first layout and interaction targets are **iPhone 15** and **11-inch iPad Air**,
   in portrait and landscape.
3. Run and open an existing SR4 `.chum` character from Files.
4. Tap the character name and small chevron to switch between imported characters
   or open another file. There is no character tab strip.
5. Edits autosave to private on-device working copies after a short pause, before
   switching characters, and when the app loses focus or backgrounds (including
   screen lock). The last character restores on launch. Save failures keep the
   current character open and show an error.
6. Use **Save Copy** to export changes for desktop Chummer. Imported source files
   are not overwritten; local autosave and desktop export are separate.

There are no external Swift dependencies. `DossierCore` is a local package.
Physical-device signing requires selecting your development team in Xcode.
The synthetic demo is for exploring the sheet, not a complete desktop character;
demo export is disabled.

## First milestone

- Five adaptive native pages: character/skills, combat, equipment, history, contacts.
  iPad uses a compact page menu beside the title instead of a second tab row;
  demo status lives in the title/menu rather than a persistent banner.
  Blue panels follow the SR4 Anniversary sheet's organization. History and contact
  content follows dossier pages 15 and 8, adapted to SR4. This is an initial layout,
  not finished artwork or an exact PDF reproduction.
- UTF-8/UTF-16 import. Unedited documents retain original bytes; edited documents
  serialize as UTF-8. Unknown fields, attributes, comments, CDATA and element order
  are retained. Whitespace, quote style and entity spelling are not byte guarantees.
- Base attribute changes flow into modified attributes and linked skill pools
  using imported modifier offsets. Skill rating/pool edits and simple initiative
  formulas with editable modifiers are supported.
- Physical/Stun: tap to fill damage through a box; tap the last marked box to
  remove one. The first eight boxes are fixed. Hold optional boxes to extend/trim
  a contiguous tail without erasing damage. Penalties appear every third usable
  box. Only damage counts persist; capacity adjustments are session-only.
- Combat includes weapons, applicable powers/spells/forms/spirits and vehicles.
  Vehicle/drone damage persists in existing Chummer save fields. See
  [device feedback and verification](DEVICE_FEEDBACK.md) for behavior and limits.
- History and extended contact data live in versioned supported notes fields.
- New equipment can be entered freely with a separate recoil adjustment and is
  preserved as pending data in notes. Existing top-level equipment fields can be
  edited. Numeric export checks reject text in known numeric fields.

## Remaining work

The target behavior is recorded in [SPECIFICATION.md](SPECIFICATION.md).

- **Desktop equipment matching/review is not implemented.** Pending entries do
  not yet become desktop equipment; no incomplete weapon nodes are injected.
- Complete field/formula mapping: primary equipment links, matrix initiative,
  effective commlink totals, nested-gear editing, skill-group/defaulting transitions,
  modifiers and missing snapshots. Nested gear/programs are now displayed in their
  saved hierarchy, including ratings, device base stats, upgrades and program options.
- Actual desktop load/save/mobile reimport verification. Swift XML round trips
  alone do not prove desktop acceptance.
- Portrait editing, pending-equipment editing, deleting existing equipment,
  overflow/print layout, zoom/page mode, PDF export, and native visual/input QA.
- Native lifecycle/lock and relaunch acceptance checks remain pending. Local
  working copies are not cloud sync; source-file conflict detection, library
  removal/renaming, import deduplication and multiwindow editing remain open.
  The app uses one window to prevent independent library writers.

Programs appear alphabetically with Loaded checkboxes. Weapons show matching
ammunition with tap-to-spend menus; loaded magazines and reserves are separate.
Saved equipment source codes/pages support at-table lookup without sourcebooks.
Matrix initiative/passes are provisional, with editable adjustments.

No user character or reference PDF is bundled. Desktop behavior and packaging
are unchanged. No advancement enforcement, purchases or automatic karma charges.

## Verify

With full Xcode selected, or `DEVELOPER_DIR` set to its developer directory:

```sh
CHUMMER_FIXTURE_ROOT="$PWD/../Chummer.Mac/assets/saves" swift test
xcodebuild -project ChummerDossier.xcodeproj -scheme ChummerDossier \
  -destination 'generic/platform=iOS Simulator' CODE_SIGNING_ALLOWED=NO build
```

The tests cover linked values, manual offsets, independent value snapshots,
damage behavior, metadata, unsupported XML, numeric validation and the repository
character corpus. CI runs core tests and builds the simulator app. Native gesture,
VoiceOver, phone/tablet visual review and desktop round trips are separate gates.

See [local Xcode verification](VERIFICATION.md) for the passing XCTest/corpus and
simulator build results, and the remaining first-boot simulator limitation.
