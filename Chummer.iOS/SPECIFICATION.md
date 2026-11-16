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
professional rating and notes in contact cards. Reference PDFs are read-only
design references, not instructions or bundled assets. Preserve familiar page
organization on iPad and usable editing on iPhone. Never silently truncate lists.

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
Hold to toggle blocking. Blocked taps do nothing. Tracks are independent.
Save only `physicalcmfilled` and `stuncmfilled`, never block states.

Chummer stores counts rather than arbitrary marks. The first implementation keeps
damage contiguous, disallows crossing blocked gaps, and truncates damage when
holding within its damaged prefix. Verify this interaction on-device.

## Equipment and desktop reconciliation

Mobile accepts incomplete names, category, provisional stats and separate explicit
adjustments. New entries receive stable UUIDs and remain pending until resolved.
Desktop must process them before normal equipment loading: incomplete weapon nodes
cannot safely be passed to the existing loader.

1. Search the matching catalog and enabled custom data. Prefer exact identity;
   normalize spelling/case, then rank fuzzy candidates by name/category/stats.
2. Review uncertain/conflicting matches. `ares alp` in ranged weapons should
   suggest Ares Alpha; brand alone must not force a match.
3. Dialog supports select/search, keep custom, defer and cancellation. Show old/new
   names and base stats. Never discard unresolved data.
4. Correct catalog-owned base data while retaining attachments, quantities, ammo,
   equipped state, notes, nicknames and explicit adjustments (including recoil,
   damage, AP or capacity). Difference from catalog is not itself an error.
5. Construct complete equipment using desktop ownership paths. Preserve instance
   identity, prevent duplicate imports, and record resolution/snapshots for mobile.
   Reconciliation must not charge money/karma. Unchanged items must not repeatedly
   prompt. Deferred/cancelled work must preserve pending entries and character.

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
- New typo resolves through desktop review; ambiguous, custom, deferred and
  cancelled paths preserve data. Recoil adjustment survives. Reopen is idempotent.
- Phone/tablet simulator/device builds and native visual/input checks. File
  conflicts, unsaved edits, malformed imports and cancelled dialogs are covered.
