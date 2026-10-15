# Bundled PDF.js maintenance

The offline reader uses pdfjs-dist 6.4.299. NuGet auditing does not cover this JavaScript distribution. No library upgrade is included in this safeguard change.

`scripts/pdfjs-manifest.json` records SHA256 for every shipped reader file, including Chummer's custom reader.html, reader.css, reader.mjs and VERSION.txt. The initial baseline's 285 upstream files were compared byte-for-byte against the official npm tarball after verifying its recorded SHA512 integrity. The custom wrapper and provenance are separate reviewed application files.

Run `python scripts/check-pdfjs.py` offline. Added, missing or changed files, symlinks, and inconsistent provenance fail. Both desktop CI jobs run this check and its regression tests before building. This manifest is a review record, not a cryptographic publisher signature; a reviewer must assess intentional code and manifest changes together.

Dependency health runs `python scripts/check-pdfjs.py --upstream` weekly and during releases. It verifies the pinned version's tarball URL/integrity against the official npm registry and fails if the latest stable version differs. Missing/malformed/unavailable metadata fails rather than reporting success. A newer version requires review, not necessarily an emergency upgrade. This is update detection, not a comprehensive JavaScript vulnerability audit.

## Updating deliberately

1. Review Mozilla's release notes/security changes and compatibility with the current reader wrapper and browser engines.
2. Download the exact official npm tarball and verify its SHA512 against official metadata. Replace the complete selected upstream asset families together, retaining licenses. Do not mix versions of worker, display/viewer, decoders or supporting files.
3. Update VERSION.txt and the manifest version/tarball/integrity. Regenerate the file map with SHA256 of all regular files, using POSIX relative paths and sorted keys. Review unexpected removals/additions; never regenerate merely to silence unexplained drift.
4. Run the integrity and upstream checks, `python scripts/tests/test_pdfjs.py`, the shared build and character/UI smoke suite.
5. Test native Mac and Windows sourcebook rendering, selected-page navigation/offsets, text search, unavailable PDFs, byte-range handling, reader close and application Quit. Keep scripting and external-link restrictions intact. Publish through the established verified Nightly process.

Chummer's wrapper changes also require updating their individual manifest hashes and reader tests; the upstream version stays unchanged for wrapper-only edits.
