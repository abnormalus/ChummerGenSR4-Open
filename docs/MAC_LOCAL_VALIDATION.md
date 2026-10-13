# Local Mac validation — 2026-10-13

Imported the standalone Mac port from ChummerGenSR4-0.1.501 into the upstream-based fork on branch mac-local-build. Original Windows source remains unchanged. Matching Mac data assets are under Chummer.Mac/assets because the upstream checkout uses a different data layout.

- .NET SDK 10.0.401, osx-arm64: Release build succeeded, 0 errors, 7 warnings.
- Existing offscreen smoke checks passed on this Mac, including all 399 fixture round trips.
- Self-contained ARM64 app packaging and ad-hoc signature verification succeeded.
- Direct execution from the Codex sandbox aborted in HIServices _RegisterApplication during Avalonia native startup. GUI automation briefly observed a Chummer window, but subsequent window inspection failed; interactive startup is not confirmed.

Next check: open artifacts/mac/ChummerSR4.app manually in Finder outside the agent launch context and verify the window remains open. If it fails, collect that launch's crash report. Earlier MAC_VALIDATION.txt and migration reports are supplied historical results, not evidence of this local run.
