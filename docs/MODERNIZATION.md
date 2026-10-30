# Modernization audit — October 14, 2026

This is an incremental maintenance policy, not a guarantee that all legacy techniques have been removed. The shipping Mac and Windows application is `Chummer.Mac.sln` on .NET 10. The historical `Chummer/Chummer.csproj` still targets .NET Framework 4.0 and is not part of either desktop release. Do not use that project to build the current Windows version.

## Completed

- Removed the application-wide `SYSLIB0014` suppression. Application compiler/analyzer warnings now fail builds; analysis is pinned to the .NET 10 rule set rather than silently adopting a future major SDK's rules.
- Replaced `WebClient` synchronous downloads with a reused `HttpClient`, bounded timeout, cancellation and asynchronous requests. Update checks now use this fork's Nightly release, not the historical upstream repository. Source ancestry prevents offering an older Nightly to a newer local build. Unknown development builds do not claim that an update is newer. Startup checks run after the window is ready, remain in the background, and only show a dialog when a newer build is established.
- Packaged applications record a source revision. Dirty working copies identify themselves as development builds. Downloads still open the fixed GitHub release page; the application does not replace itself or execute downloaded code. Removed obsolete `Chummer.exe.old` deletion and process scanning from the old self-replacement workflow.
- Replaced obsolete random-number provider construction with `RandomNumberGenerator.Fill`. Kept the existing dice algorithm and nonzero explicit seed behavior.
- Replaced Avalonia `Watermark` with `PlaceholderText` and Skia's obsolete bitmap filtering API with image sampling. Portrait formats, cropping and scale limits stay the same.
- Removed unverified trimmable/AOT library declarations from the desktop compatibility projects. Shipping remains untrimmed and reflection-compatible; this also removes the SDK-patch-coupled implicit ILLink tooling dependency that otherwise invalidated locks when CI installed a newer .NET patch.
- Enabled auditing of all direct/transitive NuGet dependencies. Both published runtimes (`osx-arm64`, `win-x64`) are represented in dependency lock files. CI restores in locked mode. No package version upgrades were made; the SDK pruned the redundant EventLog dependency while regenerating the graph.
- Added a source policy and tests for prohibited APIs, blanket obsolete-warning suppression and growth of documented legacy exceptions. Dispatcher `Control.BeginInvoke(Action)` remains valid; unsupported delegate `BeginInvoke(null, null)` does not.
- Added dependency metadata checks before release and every Monday. Missing feeds, malformed reports and metadata errors fail the audit rather than appearing as a clean result. Reports cover the app, compatibility core and Avalonia backend, including transitive packages. NuGet's published metadata reported no deprecated packages or known vulnerabilities during this audit. That does not prove that every upstream project is actively maintained.
- Added weekly Dependabot proposals for NuGet and GitHub Actions, with no automatic merges. Updates must pass the Mac/Windows checks. Added `global.json` to stay on a serviced .NET 10 feature band and reject prerelease SDKs.

- Batch character preparation now yields between characters, supports cancellation, locks its input list, discards partial output after cancellation/failure and loads legacy characters without automatically overwriting them. Cancellation occurs at character boundaries; a single legacy load or serialization still runs synchronously on the UI context. PACKS preparation now runs against an isolated in-memory character and private rule controls, with cooperative cancellation and progress. A complete result replaces the editor as an unsaved change; cancellation/failure keeps the original model and file. The established control-based rule adapter is isolated in `frmCreate.Packs.Rules.cs` for gradual future extraction. Inventory history is carried forward. Quit cancels owned detail prompts and waits for bulk cleanup before the normal close sequence.

## Existing exceptions and remaining priorities

| Priority | Work | Why it remains |
|---|---|---|
| High | Inventory compatibility-layer capabilities used by each screen and add real platform interaction tests. | Successful compilation cannot detect every no-op method. The first audit records reachable capabilities in `COMPATIBILITY_CONTROLS.md`; native menu modal protection is now tested. `PrintDialog`, `PageSetupDialog`, and synchronous browser scripting are unused stubs guarded against new application calls. Toolbar merging is implemented; screen-switch regression coverage remains useful. |
| High | Review PDF.js updates when the upstream check reports them. | File hashes now gate Mac/Windows builds. Dependency health also verifies official package metadata and detects a newer release; updates remain deliberate, with reader/security review and native navigation tests. See `PDFJS_MAINTENANCE.md`. |
| Medium | Separate rules, file serialization and UI; gradually replace compatibility screens with direct Avalonia controls. | Large `frmCreate`/`frmCareer` classes still mix calculations and interface state. Keep 4e stable; no 3e/5e implementation or language rewrite is part of this work. |
| Medium | Expand nullable reference checking into existing rules/data code. | The legacy application has nullable analysis disabled. New update service/tests enable it locally. Enabling it globally without a staged cleanup would create noise rather than reliable fixes. |
| Medium | Replace developer-only `frmTest` validation with headless checks; audit synchronous dialogs and mutable global caches. | The current fixture suite proves round-trip compatibility, not every rules calculation or interaction. Async event handlers are valid at UI boundaries, but underlying operations should return Tasks and handle cancellation. |
| Low | Review the old zero-seed branch in `MTRNG(uint seed)`. | It generates entropy and then overwrites it with zero. Preserved intentionally in this API modernization to avoid silently changing historical deterministic behavior. |

The vendored NRBF image reader retains one local `SYSLIB5005` experimental-API suppression. It extracts image bytes from legacy resource records without running BinaryFormatter object deserialization. It is a compatibility exception, not a blanket permission to suppress obsolete warnings. Common obsolete-API diagnostics also fail vendor builds. Existing vendor documentation/platform/naming warnings remain upstream compatibility debt; the stricter warning-clean policy applies to application code.

The only remaining documented manual message pumping is the developer-only `frmTest` validation screen (11 calls). New production occurrences fail CI. Native bulk checks run on both Mac and Windows, including detail-prompt cancellation and shutdown.

## Verification and maintenance

Run from the repository root. The Python commands below assume `python` selects Python 3; use `python3` on Mac when no `python` alias is installed. Contributor setup and interpreter checks are documented in [CONTRIBUTING](../CONTRIBUTING.md).

```text
python scripts/check-modernization.py
python scripts/tests/test_modernization.py
python scripts/tests/test_archive_nightly.py
dotnet restore Chummer.Mac.sln --locked-mode -p:UsedAvaloniaProducts=
dotnet build Chummer.Mac.sln -c Release --no-restore -p:UsedAvaloniaProducts=
python scripts/audit-dependencies.py artifacts/dependency-audit
dotnet Chummer.Mac/bin/Release/net10.0/ChummerMac.dll --smoke-test artifacts/checks Chummer.Mac/assets/saves
```

A deliberate dependency change requires updating and reviewing its locks with an unlocked restore, then repeating the locked restore and platform checks. Do not add a global warning suppression or increase an exception count just to make a build pass. Fix the cause, or document a specific reviewed limitation. NuGet deprecation and advisory feeds cannot establish whether an unflagged library is abandoned; periodically review upstream activity and support policies too.

The source guard uses a narrow lexical scan; compiler diagnostics remain the primary API check. It cannot prove the absence of every unsupported method, particularly in the compatibility layer. Keep OS smoke testing alongside source/build checks.

Useful primary references:
- https://learn.microsoft.com/en-us/dotnet/fundamentals/syslib-diagnostics/syslib0014
- https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/configuration-options
- https://learn.microsoft.com/en-us/nuget/concepts/auditing-packages
- https://docs.github.com/en/code-security/reference/supply-chain-security/dependabot-options-reference
- https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core

## October 15 follow-up

Native menus now recheck actual window enablement at invocation, including queued clicks, while keeping Quit available for cancellation. Native Mac/Windows CI exercises modal disable/re-enable, stale clicks, preferences and Quit. A local native startup previously failed in Avalonia render-timer registration; a later local run passed. Diagnostics now record the specific upstream numeric rendering code without arbitrary exception messages. No rendering fallback or dependency upgrade was guessed from this intermittent evidence.

The ongoing section-by-section process is defined in [SECTION_MODERNIZATION.md](SECTION_MODERNIZATION.md): assess standard framework components, extract shared behavior and observable presentation, verify meaningful transitions on both platforms, then retire each replaced legacy path. Rendering/layout is a priority because repaint defects have recurred.
