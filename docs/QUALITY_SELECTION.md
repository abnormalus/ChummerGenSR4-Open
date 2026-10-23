# Add Quality window

The isolated `add-quality-window-wip` branch connects `frmSelectQuality` to the standard Avalonia catalog window. The old compatibility designer, resource and event-driven presentation are retired. Creation, career and swap callers retain their original quality creation, improvements and BP/Karma accounting. Callers dispose the selector and require explicit OK before applying its result.

## Boundaries

`QualityEligibilityPolicy` retains the original complete `RequirementMet` body unchanged, including rule ordering, Ignore Rules, duplicate limits, required/forbidden rules, swap IgnoreQuality and localized notices. `QualityPickerModel` owns unattached catalog selection and preview state; it revalidates sources and eligibility on acceptance. `QualityPickerWindow` owns standard list, search, keyboard navigation and bound filters. Free is available during creation and career; markup is absent. Source PDF access and owner theme/modality use the shared catalog machinery.

The wrapper preserves SelectedQuality, AddAgain, FreeCost, ForceCategory, IgnoreQuality, synchronous and asynchronous owned dialogs, cancellation defaults and deterministic disposal. ForceCategory constrains search results as well as category browsing. No schema, serialized values or character rules change.

## Evidence

Before retiring the legacy controls, differential headless checks compared actual legacy category, cross-category search and metagenetic identities across BP creation, Karma creation and career; the 91-assertion stage and full suite passed. Those historical logs remain in `outputs/quality-window-verification`. The retired controls are not retained as a second active implementation.

After connection, the full headless suite passes with 86 Quality contract assertions: eligibility limits/exemptions, restricted A.I. choices, forced categories, source changes, literal quoted searches, BP/Karma previews, Free, canonical acceptance/Add Again and complete character snapshot invariance. Tests specific to removed controls have been replaced by model contract checks and actual native dialog checks. No headless claim establishes native modal or rendering acceptance.

39 new native assertions compile successfully. They exercise actual owned dialogs, creation/career Free, absence of markup, selection/keyboard routing, metagenetic filtering, literal search rejection, both owner themes, complete snapshot invariance, cancellation and OK/Add Again results. Actual duplicate rejection after disabling the eligibility list filter must keep the picker open and preserve accounting. Four exact synthetic PNG names are added to the upload whitelist. The native floor increases from 786 to 825; 20-second operations, 300-second suite, completed passed report and nonzero-exit checks remain intact.

## Pending acceptance

The connected build and full headless suite pass. Native acceptance has not executed on either platform. Require exact-source Mac/Windows validation, all 399 file/memory round trips, both Foundry targets, audit and Windows PDF, plus checksum and visual review of the current affected screenshot batch before merge. Keep the branch unaccepted until these pass. No Nightly publication is claimed.
