# Local UI-library corrections

Source: Majorsilence.Forms 26.11.4, Git commit 2999abd025b324890f385fa505d07d065fef30cb. License and third-party attributions are retained alongside the source. The application uses locally compiled desktop backend and core projects; drawing primitives and headless backend remain pinned packages. The enum and drawing resource shims are built and embedded using the upstream targets. Standalone project files target net10.0 and pin original dependency versions.

- Avalonia window host: tag the physical framebuffer at 96 DPI to avoid repeated Retina magnification.
- NumericUpDown: arrow-strip width is 18 logical units, consistent with control bounds and mouse events. Its renderer converts the arrow rectangles into physical pixels only when painting. The transparent arrow child now also occupies the correct logical bounds.

Regression checks in MacSmokeTests exercise arrow-strip geometry and routed pointer clicks at UI scales 1 and 2, followed by the existing 399-character fixture suite. All passed locally. The native Retina character-creation window was inspected with arrows beside, rather than over, the values.
