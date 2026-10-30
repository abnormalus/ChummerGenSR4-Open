# Selected quality details

`QualityDetailsPresentation` owns the read-only cost/source/tooltip projection shared by creation and career. The editors retain selection and model ownership; rules, purchase/binding accounting and serialization remain unchanged.

Initialize runs once after constructing the controls and clears the three outputs. Every selection refresh clears them before validating the node and canonical quality ID. No selection, a category node, missing Tag or stale ID leaves empty details. This intentionally fixes initial designer placeholders, the prior quality's cost remaining after category selection and unchecked stale-ID lookup. Empty tooltip text removes the entry through the toolkit's existing API.

The compatibility tree deliberately raises no AfterSelect for `SelectedNode = null`. Initialize observes tree invalidation to clear populated details on that transition. Before creation, invalidation is suppressed; HandleCreated reconciles a no-selection state when the tree becomes live. A nonempty-output guard avoids repeated clearing. Both event sources and captured labels/tooltip belong to the same form lifetime; there is no global subscription or ownership/disposal of caller controls.

Valid creation selections display BP for BP builds and the configured Karma multiplier otherwise. Career always displays Karma, independently of its saved build method. Localized book abbreviation/full-book/page tooltip and current-culture cost formatting retain their original sources. The presenter does not mutate character data or dirty state, open PDFs or apply selection changes.

## Verification and observed defect

The Windows `67e0cf6` initial dark-theme and Mac initial light-theme captures showed `[BP]` and `[Source]`. They are retained as defect evidence rather than accepted as final appearance.

`QualityDetailsChecks` has 28 actual-editor assertions across both modes and BP/Karma builds. It checks constructor defaults, then shows the editor to exercise real invalidation delivery: fixed3BP/6Karma projection, populated-to-cleared selection without AfterSelect, category clearing, stale identity, a missing Tag after repopulating valid details, and snapshot/dirty preservation. The expanded full headless suite passed in197.8s, including90 sensor assertions,399 file/memory round trips,both Foundry targets and shutdown. Source safeguards/build/whitespace validation pass. The earlier197.4s result covered the original24 cases and did not prove populated-to-cleared selection.

`NativeCharacterThemeChecks` adds eight shown-creation-editor checks: empty initial/live theme details and selecting/clearing an existing quality after each capture. The required native minimum is1053; other operation/report gates are retained. Current-source native execution and image acceptance remain pending. Headless state checks do not establish native pixels or input behavior, and prior source captures do not accept a later revision.
