# Named source sets

Preferences → Sources contains an editable named-set list, Save set and Load set.
Check the books in the sourcebook tree, type a name and save. Reusing a name asks
before replacing it. Choose a saved name and load to replace the checked selections;
then save preferences to apply them to the selected settings profile. Cancel still
cancels the profile selection changes. Saving a named set is an explicit library
operation and persists immediately, independently of Cancel.

Sets store book codes only. PDF paths, page offsets, the PDF base folder and other
settings are untouched. The core rulebook remains enabled. Unknown book codes are
reported and skipped when loading. Names are case insensitive and may contain
Unicode; they are never used as filesystem paths.

The library is `source-sets.json` in the existing per-user Chummer data directory,
outside the installation, shared across preference profiles. Replacement writes use
a temporary sibling and atomic rename; unreadable libraries are retained and errors
are reported in the Sources report and diagnostics. Character files are unchanged.
