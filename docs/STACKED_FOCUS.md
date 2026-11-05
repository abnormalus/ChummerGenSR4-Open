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

At this historical preparation checkpoint the native-only driver compiled with zero warnings/errors (8.73s) before hosted execution. Subsequent4351319 both-platform native66 results and the rejected capture finding are recorded below. Its own isolated test-user directory,180-second process bound, passing-report/minimum66 and image review are additional required gates. Existing bulk300s/min1069 and sensor-transaction180s/min17 steps remain byte-identical. The headless216.9s result does not accept later native-only driver code or real platform modal/rendering behavior.


The supplemental modes also share startup recognition with frmMain's character-file path. A prior sensor-only hosted attempt exposed the missing exclusion before any driver assertion. The startup correction includes the focus mode; successful compilation does not replace its pending native66 run or image review.


## First native lifecycle finding

At d486a59, Windows passes the full310.7s headless suite,1071 native bulk assertions and all17 sensor transaction assertions, including the sale dirty-state repair. The new focus process passes its synchronization check and the first zero-selection cancellation case, then fails in `PointToScreen`/modal centering when scenario1 needs a second picker display. The shared adapter reused the same picker after its native backing window closed on the first accepted choice.

The follow-up creates and disposes one picker window per choice while retaining the available/selected lists in the model draft. It does not change minimum/Force rules, accounting, cancellation expectations or the66-assertion gate. The lifecycle correction builds successfully (16.35s, zero errors and ten known vendor warnings) and passes the full local regression suite in 213.6s, including all39 focus checks,8 actual sensor deletions,95 sensor contracts,28 quality cases,399 file/memory round trips, both Foundry targets and shutdown checks. Fresh hosted modal/image acceptance remains pending. The earlier native driver's existence/compilation did not establish this behavior. No editor-attachment delay or graphics fallback was added.


## Visual acceptance finding after native command success

Source4351319 passes native focus66 on both platforms (Mac bulk1078/sensor17/full249.4s; Windows bulk1071/sensor17/full198.5s/PDF). Hash-verified image review rejects its four focus captures perOS: they show Common rather than selected Street Gear/Gear. Hidden legacy labels had the expected name/cost, so their Visible flags were insufficient proof of actual presentation. The native fixture now awaits its scenario callback, yields for host tab synchronization and requires the actual selected Street Gear/Gear pages visible and both labels with nonzero bounds before capture. The headless entry retains synchronous completion when no callback exists. Command expectations and minimum66/deadline180s remain unchanged; fresh build/headless/native captures are required. This is a driver correction, with no production command or save-format changes.

The asynchronous shared-fixture revision passes the complete local suite in223.9s, including all39 focus cases,399 file/memory round trips,bothFoundry/shutdown. A final native-only predicate source review replaced an incorrect newly-created-presenter lookup with actual selected-page identity/visibility and label bounds; that predicate requires its own compilation and hosted visual acceptance. No production behavior changed.


## Corrected capture acceptance at e942000

[CI41973151199](https://github.com/abnormalus/ChummerGenSR4-Open/actions/runs/41973151199) passes on Mac (full241.9s/native1078/sensor17/focus66) and Windows (full320.3s/native1071/sensor17/focus66/PDF), plus dependency audit. Hash-verified92-image batches perOS are reviewed: Mac82 exact RGBA transfers plus10 direct inspections; Windows88 transfers plus4 direct. Each focus image now shows Street Gear/Gear, the selected synthetic stack name and cost300 in both modes/themes. The435 Common-tab captures remain rejected. Existing narrow/scrollable/cropped sidebars remain outside any full-layout claim. Later native-reader changes require their own source-specific results; this acceptance does not establish a merge or release.
