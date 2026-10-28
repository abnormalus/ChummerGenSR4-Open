# Maintenance responsibilities and retained exceptions

This records implementation ownership and verification entry points. It does not appoint a human maintainer: no CODEOWNERS file was found in the inspected `.github` inventory. Confirm reviewer responsibility when assigning work; automated proposals do not authorize merging.

| Area | Maintained entry point | Required follow-through |
|---|---|---|
| Mac/Windows desktop acceptance | `.github/workflows/desktop-build.yml` | Inspect the current source's restore/build/headless/native/PDF results and affected images. Retain logs on failure. |
| Dependency metadata and bundled PDF.js | `.github/workflows/dependency-audit.yml`, `scripts/audit-dependencies.py`, `scripts/check-pdfjs.py` | Audit runs on workflow calls and Mondays at 14:00 UTC. Investigate feed errors and update findings; a clean advisory report does not establish upstream maintenance. |
| Dependency proposals | `.github/dependabot.yml` | Weekly NuGet and Actions proposals; review platform support, locks and actual tests. No automatic approval is implied. |
| API/exception growth | `scripts/check-modernization.py`, `scripts/modernization-exceptions.json` | Keep specific reasons and regression evidence. Do not raise exception counts to make CI pass. |

## Exceptions confirmed in the current source

| Retained boundary | Reason and verification | Removal condition |
|---|---|---|
| Eleven `Application.DoEvents` calls in `frmTest.cs` | Developer-only legacy XML validation; count and reason are recorded in the source guard manifest. This is not permission for production pumping. | Characterize each validation and replace the screen's operations with independent headless checks before removing its implementation and manifest entry. |
| Local `SYSLIB5005` suppression in `vendor/Majorsilence.Forms/Drawing/NrbfResourceReader.cs` | The reviewed NRBF reader extracts legacy image resources; source policy permits this specific location/diagnostic. | Reassess when the supported reader API no longer requires the experimental diagnostic or a verified replacement preserves resource compatibility. Do not broaden suppression. |
| Toolkit `PrintDialog`, `PageSetupDialog`, synchronous `WebBrowser.InvokeScript` stubs | No application construction/synchronous-script calls were found in this audit. The source guard rejects their introduction. The real preview path uses async browser printing. | Remove vendor debt only with upstream/local compatibility review, or implement a capability when an actual supported caller requires it and platform tests prove it. Presence alone is not a user-facing defect. |
| Form-owned rules/accounting and detached controller adapters | Creation/career still own substantial behavior. A native picker is not a replacement rules engine. See [ARCHITECTURE](ARCHITECTURE.md). | Extract characterized shared behavior section by section, preserving actual accounting, independent references and serialized fields; retire each replaced path after acceptance. |

The catalog cache and its caller-owned copies have a separate [ownership and merge-stage record](XML_CATALOG_OWNERSHIP.md), including invalidation limits and the native fixture that currently accesses private cache entries. Keep file access, custom merge policy and control localization distinct when extending that boundary.

This is a bounded inventory, not an exhaustive review of every vendor stub, nullable exclusion, global cache or duplicated calculation. Broader exception review and explicit human CI/dependency responsibility remain completion work. Use [MODERNIZATION](MODERNIZATION.md) and [COMPATIBILITY_CONTROLS](COMPATIBILITY_CONTROLS.md) alongside the current source rather than treating dated checkpoints as current verification.

## Developer validation replacement inventory

`frmTest` remains reachable from `frmMain` when the second application argument is `/test`; it is not an ordinary character-edit command. Its Test action constructs catalog objects and evaluates their properties, collecting failures in the output textbox. The existing 399-character round-trip sweep does not by itself cover every catalog construction/property probe.

| Existing phase | Permitted message-pump calls | Replacement scope |
|---|---:|---|
| TestVehicles | 2 | Vehicle definitions and vehicle modifications; construction plus total cost, acceleration, armor, body, handling and speed probes. |
| TestWeapons | 3 | Weapon definitions, accessories and modifications, preserving creation and property-error reporting. |
| TestArmor | 2 | Armor definitions and modifications, preserving creation and property-error reporting. |
| TestGear | 1 | Gear definitions and their construction/property probes. |
| TestCyberware | 1 | Both cyberware and bioware catalog selection through the same method. |
| TestQuality | 1 | Quality construction, including generated weapons/nodes. |
| TestMetatype | 1 | Metatypes and critters, including dependent qualities, powers, skills, forms and nested gear. |

Before retiring a phase, record the exact catalog inputs and probes, independent expected failures/results, prompt dependencies and diagnostics. Move bounded non-UI validation behind a headless entry point and retain explicit failure reporting; constructor success alone is insufficient. Remove the corresponding UI path and update the specific guard-manifest exception only after that replacement is verified. The count remains eleven; this inventory grants no new pumping or claim that the screen has been replaced.
