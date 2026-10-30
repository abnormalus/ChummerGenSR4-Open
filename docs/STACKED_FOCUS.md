# Stacked-focus command characterization

At the recorded baseline, `frmCreate.cmdCreateStackedFocus_Click` and `frmCareer.cmdCreateStackedFocus_Click` owned the same modal command. They collect unbonded Foci/Metamagic Foci, mark each accepted choice bonded, validate the minimum count and optional Force-six cap, then replace component gear with a linked stacked-focus record and refresh the editor. Selection, mutation, validation and tree projection are still coupled.

## Observed cancellation defect

`StackedFocusCancellationChecks` invokes both original handlers through their actual modal selection and minimum-selection notice. It supplies two synthetic unbonded foci and responds through the public headless dispatcher, without a production dialog hook. A timer replies only to a visible modal with no result; a twenty-second deadline bounds each command and late replies are ignored after cleanup.

The eight assertions passed locally in the complete `focus-modal` run on October30 (200.6s), including all 399 file/memory compatibility cases, both Foundry targets and shutdown checks. They establish:

- Cancelling before selecting any component leaves both unbonded, creates no stack, preserves the complete snapshot and leaves accounting and dirty state unchanged.
- Selecting one component and then cancelling returns through the minimum-selection notice, retains both original gear objects and creates no stack, but leaves the selected component bonded. Money and expense entries are unchanged and the editor remains clean.

The second result is a verified defect in the original command, not a Shadowrun rule or desired compatibility contract. These characterization expectations make an eventual repair explicit; they must not be cited as approval to preserve the defect indefinitely. The observed bonded flag is model state, but this check does not claim a saved-file round trip for the failed command or native Mac/Windows modal acceptance.

## Remaining work before sharing the command

The additional Force-cap scenario selects both components at ratings four and three with the higher-Force option disabled. All four added assertions passed in the local `focus-force` run: both editors return through a notice, restore both Bonded flags, retain original inventory/accounting/clean state and preserve the complete snapshot. The complete suite passed in 202.6s with all twelve rejection assertions, all 399 file/memory cases, both Foundry targets and shutdown. The earlier eight-case run completed in 200.6s. This establishes the contrast with the minimum-count rejection at runtime.

The `focus-success` extension now has all thirty original-handler assertions passing locally, with the complete suite passing in 209.9s, including all 399 file/memory compatibility cases, both Foundry targets and shutdown. It covers two components at combined Force four, two at Force seven with the higher-Force option enabled, and three at Force six, in both modes. Expected costs are fixed at 300/600. Checks establish ordered original component references and Bonded flags, an initially unbonded stack, canonical linked gear/tree identity, SM page84 metadata, unchanged funds/expenses, dirty state and linked/component identity through the existing snapshot format. These success expectations do not call a replacement command or policy.

Native Mac/Windows modal acceptance remains required for a later command change. The catalog/rule-validity of arbitrary focus combinations and every external improvement are outside these synthetic ownership cases.

A repair should stage selection without changing owned gear, validate the complete proposal, then commit model changes once. Keep the proposal/rules component independent of modal controls and persistence, with explicit inputs for the higher-Force option. The editor should own prompts and projection. Update the recorded cancellation expectations as an intentional bug fix and verify affected native interactions before section acceptance. The prepared replacement below is an explicit behavior repair and still needs its own verification.


## Prepared model and editor replacement

`StackedFocusProposal` copies the selected object references, validates minimum count, duplicate/reference ownership, current unbonded/category eligibility and the explicit higher-Force option. It reads no controls, settings services, files or expense ledger. Invalid proposals reject before writes. Cost/name evaluation and both linked records are constructed before owned Bonded/collection changes; malformed custom costs are explicitly tested for unchanged state. Original unchecked integer aggregation is retained. This does not promise rollback of allocation failures, unrelated global effects or later UI projection failures.

`StackedFocusEditorCommand` owns the disposable modal picker and tree projection. Available/selected lists are private drafts; selecting one item no longer bonds it. The option callback is read after the modal sequence. Cancel after at least two selections still completes that original selection sequence; fewer than two rejects without edits. Success delegates one model transfer, then removes component nodes and adds the linked item node. Both form handlers are eight-line wrappers retaining their own dirty flag, focus list, character summary and window-title updates. At the7d4cda7 focus extraction checkpoint, source hashes prove all code outside the two replaced125-line bodies unchanged. A subsequent independent vehicle-sale completion repair changes career code outside that focus boundary and has its own verification record.

`StackedFocusCommandChecks` retains fixed original success/Force references, with one intentional expectation change: one-selection cancellation now requires both components unbonded and the complete snapshot unchanged. `StackedFocusProposalChecks` adds nine independent model contracts for rejection, staleness, malformed cost and ordered commit/accounting. The proposal-only binary passed the complete suite in218.2s. The switched editor code passes the complete suite in216.9s, including all39 focus checks, eight actual sensor deletion checks,399 file/memory cases,both Foundry targets and shutdown. Original baseline success does not accept this replacement. Native Mac/Windows modal interactions remain required before section completion.


## Native acceptance prepared

`--check-focus-command` runs the same fixed command cases on the native dispatcher in Light and Dark with real MDI owners. Sixty command assertions plus four selected-name/cost projection assertions and synchronization/source-file checks require at least66 assertions. Four synthetic captures show the successful two-component stack in creation/career and both themes; they join the existing88 captures for92 named images perOS. The selected gear must display the fixed synthetic name and cost300 before capture.

The native-only driver compiles with zero warnings/errors (8.73s), but has not executed locally or on hosted platforms. Its own isolated test-user directory,180-second process bound, passing-report/minimum66 and image review are additional required gates. Existing bulk300s/min1069 and sensor-transaction180s/min17 steps remain byte-identical. The headless216.9s result does not accept later native-only driver code or real platform modal/rendering behavior.


The supplemental modes also share startup recognition with frmMain's character-file path. A prior sensor-only hosted attempt exposed the missing exclusion before any driver assertion. The startup correction includes the focus mode; successful compilation does not replace its pending native66 run or image review.
