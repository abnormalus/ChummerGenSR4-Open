# Stacked-focus command characterization

Both `frmCreate.cmdCreateStackedFocus_Click` and `frmCareer.cmdCreateStackedFocus_Click` currently own the same modal command. They collect unbonded Foci/Metamagic Foci, mark each accepted choice bonded, validate the minimum count and optional Force-six cap, then replace component gear with a linked stacked-focus record and refresh the editor. Selection, mutation, validation and tree projection are still coupled.

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

A repair should stage selection without changing owned gear, validate the complete proposal, then commit model changes once. Keep the proposal/rules component independent of modal controls and persistence, with explicit inputs for the higher-Force option. The editor should own prompts and projection. Update the recorded cancellation expectations as an intentional bug fix and verify affected native interactions before section acceptance. No such repair or shared command is implemented by these checks.
