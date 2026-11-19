# SR 4 Dossier PWA — first preview

Buildless offline-capable at-table companion. Serve `dist/` over HTTPS or localhost.
No analytics, CDN scripts or hosted character database. PDF libraries and fonts are bundled.

Implemented: multiple `.chum` working copies in IndexedDB, compact character selector,
five sections, local autosave, XML export, manual identity/attributes/skills/notes,
condition monitors, initiative, basic weapon formulas, sorted loaded program checkboxes,
contact connection/loyalty with expandable stats, source citations and adaptive themes.
GitHub setup links, explicit snapshot sync and a searchable `characters/` file picker
are included. Revision checks prevent silent overwrites on conflicts and remote deletion.
Network acknowledgements preserve newer local edits. Unknown XML remains in the DOM;
unmodified files export byte-identically, edited files export UTF-8.

## Remaining release work

- Browser GitHub authorization and one-click private repository pairing. The preview
  uses user-entered repository-scoped tokens in memory only; reload/disconnect clears
  the token. Never put tokens in source, storage or logs.
- Browser end-to-end GitHub acceptance. Live repository tests now pass for the same
  GitHub client: private/write verification, create, byte-identical readback, conditional
  update, safe pull, two-sided conflict refusal, uncertain-write recovery and listing.
  Tested November 18, 2026 using a synthetic character; no real characters modified.
- Ammo spending/reloading, vehicle health, complete spell/power detail and editing,
  advanced weapon/ammo modifiers and skill pool overrides.
- History/restore UI, cross-tab edit coordination and desktop production round trips.
- Matrix persona variations and full native calculation parity.

Current weapon formulas include attributes and unarmed improvements, not complete
combat damage rules. Avoid editing the same local working copy in multiple tabs.
Export backups before clearing browser data. Autosave runs while editing and on focus
loss; mobile OS termination cannot guarantee a final save event.

## Tests and publishing

Serve the project root and open `/tests/`. Tests cover XML preservation, formulas,
validation, wounds, conditional saves, pull/conflicts/deletion, uncertain upload recovery
and acknowledgements preserving newer edits. Optional `/tests/?fixtures` uses ignored
private files `tests/fixtures/0.chum` through `3.chum`. Never publish those fixtures.
GitHub Pages hosting and channel promotion are documented in HOSTING.md.
Both PDF print options are available under Export. The official sheet embeds the original
blank two-page form and appends the complete readable snapshot, including overflow and
source references. PDF generation is local and works offline after the assets are cached.

Primary armor uses the highest combined ballistic/impact value among equipped armor,
falling back to owned armor when none is marked equipped (ballistic breaks ties).

Open refreshes the linked repository on every visit and lists `.chum` files in all
folders. Search matches filenames, folder paths and loaded character names/aliases.
Embedded raster portraits load progressively, with initials when absent or unreadable.
The first 100 entries receive previews per refresh to bound download/decode work;
all characters remain selectable. Preview metadata and resized images stay in memory
and are cleared on disconnect. Opening downloads the latest file as a separate local
working copy, preserving existing edits. A new browser session needs reconnection.

The start page includes Install app, with a native prompt when available and platform
instructions otherwise (Safari Add to Home Screen on iPhone/iPad). Close saves local
edits before returning to the start page; saved dossiers remain available through Resume.
A closed selection stays closed after reload. Browser windows are not forcibly closed.
Repository files already present locally switch to their working copy, retaining unsynced
edits. Reimporting known file contents also reuses the local copy; distinct files are not
merged solely because their filenames match. Close saves locally first and requests a sync when connected.

Autosync checks for changed working copies every minute while open and connected,
including closed dossiers. The stored bytes and last-synced hash persist pending work
across reloads. Online and foreground events trigger a catch-up attempt. Tokens remain
memory-only, so reconnecting GitHub is required after restarting the app. Browser
suspension can delay timers; the status distinguishes local saves from GitHub sync.
Conflicts offer Save as new name, Overwrite GitHub, Delete local copy, and Later.
Overwrite requires the reviewed remote SHA and refuses a newer intervening change;
Delete local copy leaves GitHub untouched. No background merge of character XML occurs.

Skills default to Active with Pool > 0 enabled. Compact header buttons select Combat,
Active, Knowledge (including languages), or Magic for magicians and adepts. Categories
use the saved SR4 skill category and knowledge flag. Active includes combat and magical
active skills. The pool toggle applies independently of the selected category.
