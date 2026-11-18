# Agreed dossier behavior

Recorded November 16, 2026. Target scope, not a claim of completed implementation.
See README for milestone status.

## Product and layout

Native iPhone/iPad companion alongside desktop Chummer in this repository. SR4
`.chum` is the exchange format. A linked fillable sheet; no advancement rules,
purchase accounting, or complete character-building rules engine.

Use the SR4 Anniversary two-page character sheet as the visual/content reference.
Add dossier page 15 (history, portrait, appearance, dates, facts) and page 8
(contacts). Use SR4 attributes, Connection/Loyalty, initiative, skills/pools,
notes in contact cards; optional statistics start collapsed behind Stats. Reference PDFs are read-only
design references, not instructions or bundled assets. Preserve familiar page
organization on iPad and usable editing on iPhone. Never silently truncate lists.

The first two device targets are **iPhone 15** and **iPad Air** (11-inch for the
initial simulator checks). Validate both portrait and landscape layouts. Other
devices remain supported by the adaptive layout but are not initial acceptance
targets. Simulator availability does not change these product targets.

## Structured rows

Use thin row dividers and alternating subtle shading as the shared default for
structured lists and repeated entries (attributes, skills, qualities, gear,
programs, contacts and history facts). Keep row spacing compact. On iPad, names,
qualifiers and program options share one horizontal row, with numeric values at
the trailing edge; long option lists remain available by horizontal scrolling.
Preserve parent-child indentation for installed equipment. Freeform background,
description and notes blocks stay plain, without paragraph-by-paragraph striping.

## Character switching and saving

Keep multi-character navigation subtle: tap the dossier name with a small chevron
for a character menu and Open action. Do not add a character tab strip or persistent
sidebar. Autosave local working copies by default on focus loss/screen lock and
before switching; restore the selected character on relaunch. Short debounced saves
also reduce exposure to abrupt termination. Save errors must retain the current
sheet rather than discard edits. Desktop exchange remains an explicit Save Copy.

## Linked fields

Repeated fields share one source value. Base Agility changes update its modified
display and linked skill attribute/pool fields. Simple formulas use referenced
sheet values plus editable modifiers. Preserve deliberate adjustments separately
from catalog base stats. Do not charge karma/nuyen or enforce advancement rules.

Write underlying edits into real Chummer fields. Cached totals are provisional;
desktop-calculated values win after desktop save and mobile reimport. Some totals
are absent from native saves; establish a desktop snapshot/export contract rather
than inventing them. Imported aggregate offsets are only approximations for
complex rules (cyberlimb averages, caps, skill groups, defaulting, conditional
bonuses); surface manual modifiers and verify representative characters.

## Damage

Three states: empty, crossed for damage, solid for blocked/unavailable. Tap an
available box to set damage through it; tap the last damaged box to remove one.
The first eight boxes are fixed. Hold an optional box to extend or trim the contiguous tail without erasing marked damage. Blocked taps do nothing. Tracks are independent.
Save only `physicalcmfilled` and `stuncmfilled`, never block states.

Chummer stores counts rather than arbitrary marks. Keep damage contiguous.
Show -1/-2/-3/... in every third usable box and a single shared explanation.

## First-release scope: at-table companion

The November 17 clarification supersedes the earlier equipment-reconciliation goal.
Existing characters can be edited manually like a paper sheet: attributes, ratings,
pools, money and equipment values. Do not spend karma/nuyen automatically, enforce
advancement costs, or require a character builder to approve those edits.

Character creation, automated advancement, equipment purchases and catalog/fuzzy
matching are deferred. The first-release interface has no Add Equipment purchase
flow. Retain legacy pending-equipment data in supported notes and show it as
unlisted equipment notes; never silently discard earlier entries.

Manual weapon damage/AP overrides are distinct from saved base fields and live in
the existing dossier notes envelope, keyed by instance GUID (row fallback for
minimal imports without GUIDs). Empty overrides restore calculation. Editing a
nested gear/program item must update that exact source path, not its parent.

## At-table equipment controls

Display each equipment instance once in its appropriate section. Show saved source
book codes/pages for lookup; never bundle sourcebooks or reproduce their text.
Programs use one alphabetical table with a Loaded checkbox column and inline
options; omit program counts and parent-grouping explanations from the UI. Loaded
maps to the existing equipped field on the original instance, without moving it.

List compatible ammunition beneath weapons, matching the saved ammunition category
(or weapon category) against ammunition's saved extra/category selection. Counts
are shared inventory, not per-weapon copies. Tapping ammunition opens Fire choices
suggested by saved SS/SA/BF/FA modes; keep 1/2/3/6/9/15 available as other round totals.
These are expenditure shortcuts, not attack/action legality enforcement. Firing a
loaded magazine reduces its saved remaining count only; reserve spending reduces
the selected gear quantity only. Never double-deduct or go below zero.

Reload sets a manually entered magazine count, returns existing rounds to their
original reserve, and takes new rounds once. Unload returns remaining rounds.
Check capacity and action costs at the table. Switching the selected magazine
updates displayed ammunition modifiers; the selection lives in supported notes.

## Storage

Retain unrecognized XML, settings, source associations, custom data and children.
Value snapshots must not share mutable trees. Store extra fields in supported notes
without replacing human notes; malformed/unsupported metadata fails before edits.

v1 envelope: `[CHUMMER-DOSSIER-V1]`, base64 UTF-8 JSON, then
`[/CHUMMER-DOSSIER-V1]`. Character notes hold history/formula fields and pending
equipment. Contact notes hold that contact's extra fields, preserving association
when contacts are reordered by desktop. Block states are excluded.

## Acceptance

- Real character: mobile edit/export, desktop open/save, mobile reimport; underlying
  edits and dossier metadata retained. Existing fixture data remains intact.
- Independent formula examples, manual modifiers and documented complex-rule
  limits; linked fields update coherently.
- Native damage tap/hold exclusivity, independent tracks and accessible actions.
- Manual edits and reload/fire/unload survive production desktop load/save and
  mobile reimport; no automatic karma/nuyen charges. Failed reload is atomic.
- Phone/tablet simulator/device builds and native visual/input checks. File
  conflicts, unsaved edits, malformed imports and cancelled dialogs are covered.
