# Adept Power Point summary

The creation and career editors previously duplicated Magic-budget selection, improvement addition, remaining-point subtraction and summary formatting in their private `CalculatePowerPoints` methods. They now delegate arithmetic to `AdeptPowerPointBudget.Calculate` and display formatting to `AdeptPowerPointSummary.Format`.

## Responsibilities and preserved differences

The budget is an immutable value supplied with full/split Magic, a Mystic Adept flag, already-resolved bonus points and already-priced spending. It does not read controls, mutate a character, save data, cache results or own resources. The formatter receives the budget and localized Remaining label, using the current culture. No new serialized fields or localization keys are introduced.

Each editor still owns its PowerControl collection. It reads each control's `PowerPoints` before calling `UpdatePointsPerLevel`, then resolves the applicable Magic branch and enabled improvements. The ordinary branch reads full Magic; the Mystic Adept branch reads its assigned adept portion. Unused Magic getters are not evaluated. Control lifecycle and refresh stay with their editor rather than entering the pure budget component.

`Power` remains the owner of individual pricing, including its Adept Way/Geas discounts and attribute-dependent double-cost behavior. The extraction accepts those existing costs; it does not replace them with rating multiplied by a fixed price. `ImprovementManager.ValueOf` retains enabled/custom/type handling. Creation/career accounting and character persistence are unchanged.

The formatter deliberately retains legacy decimal scale and length-limited zero trimming. For example, an undiscounted four-point power against a three-point budget shows `-1.0` in en-US and `-1,0` in de-DE. This was observed against the original implementation after an initial handwritten expectation incorrectly assumed `-1`. Formatting normalization would be a separate behavior change.

## Verification evidence and limits

`PowerPointSummaryChecks` invokes the actual editor methods with isolated synthetic rows and fixed expected labels. Before extraction, 44 assertions passed across both editors and en-US/de-DE: empty/multiple powers, fractional/over-budget spending, ordinary versus split Magic, enabled/disabled bonus improvements, combined discounts, snapshot and clean-editor preservation, and stale row-label refresh. The independent expected labels were retained unchanged for extraction verification; the tests do not call the new calculator to generate expectations.

The original baseline passed the complete headless suite, all 399 file/memory round trips and both Foundry targets (`artifacts/power-baseline/expanded-smoke.log`, exit 0, 185.7 seconds). The extraction build passed and those 44 assertions passed again; the full extraction suite also passed (`artifacts/power-baseline/extracted-smoke.log`, exit 0, 189.5 seconds), including all 399 round trips and both Foundry targets. Current-source Mac/Windows CI and affected native interactions remain required before acceptance. Earlier draft CI belongs to its recorded source and does not validate this uncommitted extraction.

No replaced budget/formatting implementation remains in either editor. The two small control-aggregation loops remain intentionally editor-owned to preserve row refresh sequencing. The underlying pricing rules are not claimed as newly extracted or comprehensively revalidated by this summary-only change.
