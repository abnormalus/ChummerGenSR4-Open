# XML catalog ownership and cache boundary

`XmlManager.Load` supplies mutable, caller-owned documents to pickers, model creation and previews. Its process-wide cache retains merged base definitions. These are separate lifetimes: mutating a returned document must not affect later callers, and character edits must not become global catalog edits.

## Current stages

1. Resolve the base file under the application data directory. Locate the cache entry by filename and compare the base file's last-write time.
2. On a cache miss or changed base timestamp, load base definitions, apply matching `override*_` files and merge selected language data. Store this merged document and the base timestamp in the cache.
3. Parse a separate document from the cached XML text, then read matching `custom*_` files into that copy. Existing canonical names suppress duplicate custom items. Return the copy to the caller.

`MacPaths.CustomDataFiles` combines bundled and user data files; a user file with the same basename wins. Override replacement and custom addition are different operations. In particular, overrides are applied during cache rebuild, while custom additions are read on each call. Preserve their order, canonical identity and consumer isolation when extracting stages.

## Characterized public behavior

`XmlCatalogCacheChecks` uses the ordinary public loader and an isolated smoke-test user directory. Its uniquely named custom file is removed in `finally`; it never edits bundled data or user profiles. Seven independent assertions cover returned-document isolation, a new custom item after cache warmup, no duplicate accumulation, custom edits affecting future loads without changing a prior document, canonical-name collision preserving the base definition, malformed custom XML failing without changing prior consumers, and custom removal restoring the original catalog.

These checks do not prove translation or override invalidation, concurrency, file error recovery, or every catalog's merge semantics. Source inspection shows the cache key records filename and the base timestamp; it does not include override timestamps or language identity. Before changing that policy, characterize supported options/restart behavior and independent override/translation references. Avoid silently promising live language or override reload from the custom-file tests.

`NativeCyberwarePurchaseChecks` currently inserts a temporary synthetic ware directly into the private merged cache so picker and purchase callers independently load the same definition. It removes the definition in `finally`. This is a test coupling to the retained cache representation, not an application extension API; account for it when replacing that representation.

`LanguageManager` separately owns global translated strings/data. Its `Load` initializes the selected non-English language once and also localizes controls. A cache extraction must distinguish data selection from UI localization rather than moving both responsibilities into a catalog storage class.

The retained English key `Message_Omae_DataDownloaded` explicitly describes restarting after override downloads. No shared-code caller of that key was found, so it is historical evidence of intent, not proof of a current UI contract. `frmOptions.SaveRegistrySettings` persists the language choice through `SettingsStore` and updates `GlobalOptions.Language`; that method alone does not rebuild the catalog or reload global strings. Follow the complete startup/options flow before changing live language behavior.

Initial five-case unchanged-production baseline passed the complete headless suite, all 399 file/memory round trips and both Foundry targets (`cache-smoke.log`, exit 0, 192.8 seconds). The expanded seven-case baseline builds and all seven assertions pass initially; its complete suite passed (exit 0, 199.1 seconds), including all 399 round trips and both Foundry targets.

## Extracted merge stage

`CustomCatalogMerge.Apply(target, custom)` contains the former custom group loop. It mutates caller-owned scratch documents only: removes custom children whose canonical names already exist in the target, then imports each group. `XmlManager` retains all file enumeration/loading, cache ownership, clone creation and improvements-file exclusion. The helper preserves XPath comparison, file/group order and the legacy behavior for repeated names inside the same group; it does not introduce a new deduplication policy or fix quote handling. Baseline tests remain independent public-loader checks. The extraction builds successfully and passes the same seven baseline assertions plus the full headless suite, all 399 character file/memory round trips, both Foundry targets and shutdown checks (`cache-extracted-smoke.log`, exit 0, 191.1 seconds). The independent expanded baseline passed in 199.1 seconds before extraction. Current-source Mac/Windows acceptance remains required.
