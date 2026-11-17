# SR 4 Dossier for iPhone and iPad

A native at-table companion for existing SR4 Chummer characters. Use it like an
editable character sheet: change values manually, reference your runner's abilities,
and track combat state. Character creation, automatic advancement costs and
catalog-based equipment purchasing are outside the first-release scope.

## Use

Open `ChummerDossier.xcodeproj` in Xcode, choose the ChummerDossier scheme and an
available iPhone/iPad simulator, and run. Minimum iOS/iPadOS version: 17. There are
no external Swift dependencies. Current work uses simulators only.

Open a `.chum` through Files. Tap the character's name to switch working copies.
On iPhone, Character, Combat, Equipment, Background and Contacts are pages; iPad
uses the compact toolbar page menu. Contacts show Connection/Loyalty and hide
optional statistics behind Stats. Professional Rating is omitted.

- Manually edit attributes, skill ratings/pools, money, contacts and existing
  equipment values. No karma/nuyen is spent automatically. Long-press gear or
  a program to edit that exact instance, including nested items.
- Combat shows initiative/passes, health, weapons, applicable abilities and vehicles.
  Complex Forms appear for technomancers; spells/spirits for magicians; adept
  powers for adepts. Technomancers use the spirit collection for sprites.
- Health uses contiguous damage counts. First eight boxes are fixed; hold optional
  boxes to adjust the tail without erasing marked damage. Penalties are labelled
  every third usable box. Vehicle damage uses Chummer's existing saved field.
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
unchanged. Save Copy exports a separate `.chum` for desktop Chummer.

Unknown XML, child equipment and human notes are retained. Extra sheet fields live
in the supported dossier notes envelope. Desktop Chummer recalculates derived
rules values when it loads/saves: imported snapshots and manual sheet calculations
are not a replacement for its complete rules engine. Advanced cyberlimb, optional
house-rule, program-load and situational effects require explicit table adjustments.
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

Finish printable SR4A sheet/PDF export, save recovery/conflict handling, and
broader accessibility/rotation/lifecycle acceptance. Review wounded-character,
conditional-rule and special-persona handling explicitly. A clean build and the
five-character audit do not establish every Shadowrun rule or a Stable release.
