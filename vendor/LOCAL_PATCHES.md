# Local UI-library corrections

Native screen conversion is separate from framebuffer scaling. WindowBase.ScreenCoordinateScaling derives the native mapping from backend PointToScreen; Control's point and rectangle conversions use it. Retina macOS screen points are 1x while the framebuffer is 2x. Using framebuffer scale doubled menu-anchor offsets, as shown in user screenshots. Regression coverage simulates that independent 2x/1x mapping and checks the inverse conversion.

MenuDropDown now assigns every current item to its dropdown before layout. Entries added after the first opening (including the generated MDI character list) previously retained the parent menu bar or no owner, producing incorrect preferred sizes and clipped menus. Reopening with late entries is tested at scales 1 and 2. Native Mac validation shows the complete Window character entry and direct activation.

NativeFileDialogs exposes owner-aware Avalonia file pickers. Synchronous save requests use the existing modal dispatcher loop rather than blocking the UI thread with Task.Wait.

Source: Majorsilence.Forms 26.11.4, Git commit 2999abd025b324890f385fa505d07d065fef30cb. License and third-party attributions are retained alongside the source. The application uses locally compiled desktop backend and core projects; drawing primitives and headless backend remain pinned packages. The enum and drawing resource shims are built and embedded using the upstream targets. Standalone project files target net10.0 and pin original dependency versions.

- Avalonia window host: tag the physical framebuffer at 96 DPI to avoid repeated Retina magnification.
- NumericUpDown: arrow-strip width is 18 logical units, consistent with control bounds and mouse events. Its renderer converts the arrow rectangles into physical pixels only when painting. The transparent arrow child now also occupies the correct logical bounds.

Regression checks in MacSmokeTests exercise arrow-strip geometry and routed pointer clicks at UI scales 1 and 2, followed by the existing 399-character fixture suite. All passed locally. The native Retina character-creation window was inspected with arrows beside, rather than over, the values.

## Hosted character-window input

MdiChildWindow receives logical coordinates from the outer window and now converts its interior positions to the child input entry point's device pixels. Press, release, move and wheel share that mapping. Hosted forms also derive screen origin and desktop scale from their visible presentation window, rather than an unused native backend. This makes cross-window mouse capture releases land on the control that received the press.

A regression check failed at scale 2 before the forwarding correction. Final checks exercise both frame-level dispatch and full outer-window press/captured-release routing at scales 1 and 2, with restored and maximized children. All passed. Native Retina validation: clicking the visible Body up arrow changed 1 to 2 and spent 10 BP; clicking Add Quality opened its selection dialog.
