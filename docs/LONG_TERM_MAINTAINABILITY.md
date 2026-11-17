# Long-term maintainability goal

Continue the existing section-by-section modernization until Chummer SR4 is straightforward for a new contributor to understand, change, test and support on Mac and Windows. Deduplication, explanatory code comments and contributor guidance are acceptance work throughout the project.

This goal supplements [SECTION_MODERNIZATION.md](SECTION_MODERNIZATION.md) and [MODERNIZATION.md](MODERNIZATION.md). Existing rules, character-file compatibility, independent regression references and native acceptance gates remain in force. Progress and release claims must be based on the current source and verified results, rather than historical checkpoint summaries.

## Delivery order

1. Establish the current state from source, open/merged changes and their exact-source checks. Finish or verify the active migration before starting another overlapping replacement.
2. Inventory duplicated behavior in the affected section: rules/calculations, catalog filtering, selection validation, create/career commands, persistence, dialog ownership and rendering. Record callers and meaningful differences before extracting anything.
3. Capture existing behavior with independent expected results. Consolidate genuinely shared behavior into the smallest clear component; express legitimate differences with explicit inputs. Preserve domain-specific behavior when sharing would obscure it.
4. Switch callers in bounded changes. Keep rules and persistence independent of presentation where practical, with thin documented adapters around remaining legacy dependencies.
5. Remove the replaced implementation and obsolete handlers after acceptance. Search for residual callers and documentation references. Temporary parallel paths need a stated reason, removal condition and tracked follow-up.
6. Update comments and maintenance guidance in the same change. Run appropriate regressions and required platform acceptance before calling the section complete.

## Code explanation standard

Add comments throughout affected code at meaningful boundaries: a class's purpose, a multi-step operation's phases, non-obvious rules, compatibility constraints and lifecycle transitions. Explain intent and reasoning. Use descriptive names and small methods so comments can remain concise.

For a shared component, document inputs and outputs, whether it mutates character state, who owns/disposes resources, and how errors, cancellation and stale input are handled. Where a calculation depends on a particular Shadowrun rule or preserved legacy behavior, identify the rule/source or link the relevant characterization record. Distinguish a verified rule from a compatibility exception.

Document why separate implementations remain when they look duplicative. Remove comments made stale by extraction. Avoid comments that merely repeat an obvious assignment or add decorative sections to otherwise unclear code.

## Deduplication evidence

For each consolidation, record the old implementations/callers, the new owning component, preserved differences and the regressions proving parity. Prefer existing shared policies, models, controls and framework facilities before adding another abstraction or dependency.

Prioritize repeated behavior that causes inconsistent fixes or rules results. Do not optimize for a duplication percentage or a minimal file count: clarity and correct ownership take precedence. Do not edit generated designer files or vendored libraries merely to satisfy a duplication target.

Independent fixtures and expected results must remain independent of the implementation under test. Consolidating production logic must not turn parity tests into comparisons between two calls to the same calculator.

## Section completion checklist

- Shared behavior has one maintained owner; remaining differences are explicit and justified.
- Replaced paths, unused handlers and temporary migration scaffolding are removed or tracked with a clear removal condition.
- Rules, presentation, persistence and lifecycle responsibilities have understandable boundaries.
- Meaningful code sections explain non-obvious intent, assumptions and compatibility decisions.
- Regression coverage checks changed behavior, failures and cancellation; affected native interactions and visual states pass the established Mac/Windows gates.
- Character compatibility and create/career accounting remain covered where applicable.
- Section documentation identifies entry points, data flow, ownership, extension points, known limitations and reproducible verification.
- Changes are small enough to review and progress records distinguish prepared, tested, merged and released work.

## Project completion evidence

After section migrations, perform the existing final consistency pass across code and documentation. Produce a current architecture map and contributor instructions covering setup, supported SDK/dependencies, build/test commands, feature entry points, rules/data changes, diagnostics and release/recovery procedures. Verify these instructions from a clean checkout.

Review outstanding duplication and compatibility exceptions, recording why each remains and what would justify future removal. Confirm dependency maintenance and CI ownership. A new maintainer should be able to trace a representative feature from UI command through validation/rules to persistence, make a bounded change and run its verification without relying on chat history.

Long-term supportability is the goal; completion does not mean every legacy file has been rewritten or that future maintenance will be unnecessary.
