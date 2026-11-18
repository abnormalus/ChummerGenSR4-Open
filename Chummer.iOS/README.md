# SR 4 Dossier for iPhone and iPad

A native at-table companion for existing SR4 Chummer characters. Use it like an
editable character sheet: change values manually, reference your runner's abilities,
and track combat state. Character creation, automatic advancement costs and
catalog-based equipment purchasing are outside the first-release scope.

## Use

Open `ChummerDossier.xcodeproj` in Xcode, choose the ChummerDossier scheme and an
available iPhone/iPad simulator, and run. Minimum iOS/iPadOS version: 17. There are
no external Swift dependencies. Verification uses simulators; device installs use signed Release builds.

Open a `.chum` through Files. Tap the character's name to switch working copies.
On iPhone, Character, Combat, Equipment, Background and Contacts are pages; iPad
uses the compact toolbar page menu. Contacts show Connection/Loyalty and hide
optional statistics behind Stats in each contact header. Professional Rating is omitted.
The Skills header has a Pool > 0 filter; augmentation ratings appear beside names.

- Manually edit attributes, skill ratings/pools, money, contacts and existing
  equipment values. No karma/nuyen is spent automatically. Long-press gear or
  a program to edit that exact instance, including nested items.
- Combat shows initiative/passes, health, weapons, applicable abilities and vehicles.
  Complex Forms appear for technomancers; spells/spirits for magicians; adept
  powers for adepts. Technomancers use the spirit collection for sprites.
- Health uses contiguous damage counts. First eight boxes are fixed; hold optional
  boxes to adjust the tail without erasing marked damage. Penalties are labelled
  every third usable box by default, with saved threshold/ignore abilities applied.
  Wounds adjust skill pools and initiative without duplicating imported penalties.
  Vehicle damage uses Chummer's existing saved field.
- Grenades have a compact Use button and quantity. Use consumes one from the
  linked inventory entry and disables at zero.
- Loaded magazines and shared reserves remain separate. Fire spends from the
  selected supply once. Use an ammunition menu to reload/change ammunition or
  unload a magazine. Loading takes a manually entered round count; capacity and
  action costs are checked at the table.
- Supported weapon formulas use imported attributes, saved enabled bonuses,
  installed mods and loaded ammunition. Optional shown-DV/AP overrides let you
  write a table ruling without changing base stats; blank restores calculation.
- Programs are alphabetical with Loaded checkboxes. Source/page citations support
  lookups; sourcebook content is not included. Earlier unlisted equipment entries
  remain visible as notes; no unfinished Add Equipment purchase flow is exposed.

## Save and exchange

Edits autosave to app-owned working copies after a short pause, before switching,
and on focus loss/backgrounding. The last selected character restores on launch.
Failures retain the current sheet and show an error. Imported source files remain
unchanged. The share icon offers Save Character Copy to export a separate `.chum`
for desktop Chummer, Share Readable Snapshot for manually sharing current values,
plus Print full character and Print official character sheet.

Both sheet previews offer Save PDF through Files and Print through the system
AirPrint controls. Print full character uses the existing Chummer section
arrangement. Print official character sheet fills the embedded, unmodified SR4A
two-page template; additional entries, detailed inventory and source references
continue afterward. Numbered asterisks identify entries too long for a form slot.
The template and its original attribution remain embedded; no separate import is
needed. Long continuation entries span numbered pages with repeated headings. Current pools, weapon DV/AP, ammunition, damage, loaded
programs, source/page references and relevant magical sections are included.
Printing or sharing creates a snapshot; it does not modify the character or sync
changes from another app.

A previous-save backup recovers an unreadable working archive with a notice that
recent edits may be missing. Damaged archives are retained before repair. If both
copies are unreadable, an explicit Start Fresh action keeps recovery copies before
opening an empty library. Reopening the same character creates an independent copy;
matching names are numbered in the switch menu.

Unknown XML, child equipment and human notes are retained. Extra sheet fields live
in the supported dossier notes envelope. Desktop Chummer recalculates derived
rules values when it loads/saves: imported snapshots and manual sheet calculations
are not a replacement for its complete rules engine. Advanced cyberlimb, optional
house-rule, program-load and situational effects require explicit table adjustments.
Imported pools that were already clamped to zero while wounded do not retain all
of their original modifier information; review those pools manually after healing.
No automatic advancement or purchase accounting runs in this companion.

## Verify

Run `CHUMMER_FIXTURE_ROOT=/absolute/path/to/Chummer.Mac/assets/saves swift test` from
this directory. The shared Xcode scheme also runs native UI regressions on an
available simulator and retains screenshots. The CI workflow chooses one phone
and one tablet. Debug-only synthetic fixtures use an isolated archive.

The opt-in [desktop oracle](scripts/DesktopAudit/README.md) compares independent
production-engine values and verifies manual-edit/ammunition desktop round trips.
Private fixtures and output reports stay local, outside tracked source.
See [specification](SPECIFICATION.md), [device feedback](DEVICE_FEEDBACK.md) and
[verification history](VERIFICATION.md) for scope, evidence and known limits.

## Before first release

Finish broader accessibility/rotation/lifecycle acceptance and real-printer
acceptance. Both the full sheet and the embedded official SR4A form are available for PDF
export and simulator-tested printing. Review conditional-rule and special-persona handling explicitly;
physical screen-lock and provider-level conflict testing remain outstanding. A clean build and the
five-character audit do not establish every Shadowrun rule or a Stable release.
