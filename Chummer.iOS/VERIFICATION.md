# Local Xcode verification

November 16, 2026. Xcode 27.2 (27B52), Apple Silicon Mac.

## Passed

- Full XCTest suite: 11 tests, zero failures, including all 399 repository `.chum`
  characters. The first completed XCTest run reported 161.415 seconds. No fixture
  was modified. The Swift Testing runner also reports zero tests because this
  package uses XCTest; that is separate from the 11 executed XCTest tests.
- Generic iOS Simulator app build, including arm64 and x86_64 compilation:
  `xcodebuild -project ChummerDossier.xcodeproj -scheme ChummerDossier
  -destination 'generic/platform=iOS Simulator' CODE_SIGNING_ALLOWED=NO build`.
- Rebuilt successfully after declaring copy-mode document opening and wiring
  incoming file URLs to the same loader as the file picker. Non-file URLs are
  rejected. The opening-in-place configuration warning is resolved.
- Project/Info.plist validation and whitespace checks.

The source includes no changes to DossierCore since these XCTest checks. The app
changes after the checks concern file-open event routing and Info.plist only.

## Not established

The newly installed simulator runtime's first iPhone boot ended with **Data
Migration Failed** after 4 minutes 22 seconds. It reports Booted, but installation
and screen capture did not promptly complete. No simulator erase/reset was used.
Compilation success is not evidence of native launch, visual layout, real touch,
long-press exclusivity, keyboard, VoiceOver, or Files-provider behavior.

The earlier GitHub iOS run 43920109380 was cancelled before execution. Local results
above do not claim successful hosted CI. Inspect the current PR checks separately.
Desktop load/save/mobile reimport and equipment reconciliation remain unverified
and unfinished as described in README and SPECIFICATION.

Local working logs were retained outside the repository:
`/tmp/chummer-dossier-xctest.log` and `/tmp/chummer-dossier-ios-build.log`.
Do not infer later-source acceptance from this record; repeat affected checks.
