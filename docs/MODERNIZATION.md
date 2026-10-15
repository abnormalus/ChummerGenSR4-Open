# Modernization audit — October 14, 2026

This is an incremental maintenance policy, not a guarantee that all legacy techniques have been removed. The shipping Mac and Windows application is `Chummer.Mac.sln` on .NET 10. The historical `Chummer/Chummer.csproj` still targets .NET Framework 4.0 and is not part of either desktop release. Do not use that project to build the current Windows version.

## Completed

- Removed the application-wide `SYSLIB0014` suppression. Application compiler/analyzer warnings now fail builds; analysis is pinned to the .NET 10 rule set rather than silently adopting a future major SDK's rules.
- Replaced `WebClient` synchronous downloads with a reused `HttpClient`, bounded timeout, cancellation and asynchronous requests. Update checks now use this fork's Nightly release, not the historical upstream repository. Source ancestry prevents offering an older Nightly to a newer local build. Unknown development builds do not claim that an update is newer. Startup checks run after the window is ready, remain in the background, and only show a dialog when a newer build is established.
- Packaged applications record a source revision. Dirty working copies identify themselves as development builds. Downloads still open the fixed GitHub release page; the application does not replace itself or execute downloaded code. Removed obsolete `Chummer.exe.old` deletion and process scanning from the old self-replacement workflow.
- Replaced obsolete random-number provider construction with `RandomNumberGenerator.Fill`. Kept the existing dice algorithm and nonzero explicit seed behavior.
- Replaced Avalonia `Watermark` with `PlaceholderText` and Skia's obsolete bitmap filtering API with image sampling. Portrait formats, cropping and scale limits stay the same.
- Enabled auditing of all direct/transitive NuGet dependencies. Both published runtimes (`osx-arm64`, `win-x64`) are represented in dependency lock files. CI restores in locked mode. No package version upgrades were made; the SDK pruned the redundant EventLog dependency while regenerating the graph.
- Added a source policy and tests for prohibited APIs, blanket obsolete-warning suppression and growth of documented legacy exceptions. Dispatcher `Control.BeginInvoke(Action)` remains valid; unsupported delegate `BeginInvoke(null, null)` does not.
- Added dependency metadata checks before release and every Monday. Missing feeds, malformed reports and metadata errors fail the audit rather than appearing as a clean result. Reports cover the app, compatibility core and Avalonia backend, including transitive packages. NuGet's published metadata reported no deprecated packages or known vulnerabilities during this audit. That does not prove that every upstream project is actively maintained.
- Added weekly Dependabot proposals for NuGet and GitHub Actions, with no automatic merges. Updates must pass the Mac/Windows checks. Added `global.json` to stay on a serviced .NET 10 feature band and reject prerelease SDKs.

## Existing exceptions and remaining priorities

| Priority | Work | Why it remains |
|---|---|---|
| High | Replace manual message pumping in PACKS import and batch printing with cancellable operations and explicit progress. | Removing `DoEvents` without redesigning these stateful operations could cause hangs or partially applied character changes. Counts are frozen in `scripts/modernization-exceptions.json`: 10 PACKS calls, 1 batch-print call and 11 calls in the old developer validation screen. New occurrences fail CI. |
| High | Inventory compatibility-layer capabilities used by each screen and add real platform interaction tests. | Successful compilation cannot detect every no-op method. Examples still present include `PrintDialog` (no real dialog), portions of `WebBrowser` such as synchronous script invocation, and legacy toolbar merging. Character output uses the real native browser printing path instead. |
| High | Establish an explicit PDF.js vendoring/update check. | Bundled `pdfjs-dist` 6.4.299 is recorded in `Chummer.Mac/pdfjs/VERSION.txt`, with source, integrity and license. NuGet auditing and Dependabot do not automatically replace these bundled files. Updates need reader/security review and sourcebook navigation tests. |
| Medium | Separate rules, file serialization and UI; gradually replace compatibility screens with direct Avalonia controls. | Large `frmCreate`/`frmCareer` classes still mix calculations and interface state. Keep 4e stable; no 3e/5e implementation or language rewrite is part of this work. |
| Medium | Expand nullable reference checking into existing rules/data code. | The legacy application has nullable analysis disabled. New update service/tests enable it locally. Enabling it globally without a staged cleanup would create noise rather than reliable fixes. |
| Medium | Replace developer-only `frmTest` validation with headless checks; audit synchronous dialogs and mutable global caches. | The current fixture suite proves round-trip compatibility, not every rules calculation or interaction. Async event handlers are valid at UI boundaries, but underlying operations should return Tasks and handle cancellation. |
| Low | Review the old zero-seed branch in `MTRNG(uint seed)`. | It generates entropy and then overwrites it with zero. Preserved intentionally in this API modernization to avoid silently changing historical deterministic behavior. |

The vendored NRBF image reader retains one local `SYSLIB5005` experimental-API suppression. It extracts image bytes from legacy resource records without running BinaryFormatter object deserialization. It is a compatibility exception, not a blanket permission to suppress obsolete warnings. Common obsolete-API diagnostics also fail vendor builds. Existing vendor documentation/platform/naming warnings remain upstream compatibility debt; the stricter warning-clean policy applies to application code.

## Verification and maintenance

Run from the repository root:

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
