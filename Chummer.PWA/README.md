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
