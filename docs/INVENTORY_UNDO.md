# Inventory Undo

Each open character editor keeps the five most recent inventory changes in memory. Use **Undo inventory change** from Edit or an inventory item's/background's right-click menu; the number shows how many changes remain. Gear, weapons, armor, cyberware/bioware, vehicles, lifestyles, locations and focus configurations are covered, including nested add-ins. Cancelled or unchanged prompts do not consume history.

The character view queues observation after its normal refresh/title callbacks, so synchronous updates within one command are captured together. A snapshot contains the existing inventory XML, IDs, related improvements and purchase bookkeeping. Restoration stages all inventory objects before replacing collections and rebuilds the existing trees while preserving selection and expansion where possible.

Undo reverses the inventory action's Nuyen/Karma and ledger changes as deltas. It retains later unrelated rewards, description edits and other non-inventory changes. It also restores relevant direct Magic/Resonance changes. Failing restoration rolls back to the current inventory and reports a recoverable error; diagnostics include action/error types, not serialized character data. Capture errors reset history and establish a fresh baseline when capture becomes possible again.

History is per editor and is not saved into `.chum` files. Closing/reopening a character starts a new history; saving while the editor stays open does not clear it. There is no redo. Validation includes five-action retention, nested deletion restoration, unrelated reward preservation, creation/career context commands, and inventory restoration against the upstream character fixtures.
