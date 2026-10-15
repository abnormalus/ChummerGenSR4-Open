# Local UI-library corrections

TreeNode.Text now invalidates its owning TreeView when changed. It previously updated the model without dirtying the cached control image, leaving edited quality/gear text stale until another interaction. A rendered-frame regression checks that an accepted detail change appears without mouse interaction. Quality trees also disable connector lines in creation and career, matching their flat grouped presentation and avoiding connectors crossing text when branch glyphs are hidden.

Hosted capture releases now retain the computed click count, and double-click matching requires the same mouse button. Previously every captured MDI release was forced to a single click. TreeNode.ContextMenuStrip now aliases the actual ContextMenu used by right-click dispatch. Together these corrections enable quality/gear detail editing by double-click and context menu. Pointer-routing checks cover restored/maximized MDI children at scales 1 and 2.

Form.ShowDialogAsync retains its completion source in a local variable. A Load handler can accept and close a dialog synchronously, clearing the dialog_task field before the show call returns. Returning that cleared field's Task crashed forced license selection and reapplying saved license improvements. The quality/gear persistence suite now exercises this path.

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

Searchable detail combos opt into `KeepInputFocusOnDropDown`: their popup uses the existing nonactivating popup path, keeping subsequent typing in the edit field. The default remains unchanged for other combos.

ListBox item insertion no longer shifts provisional focus zero past the last item when rebuilding an empty list; empty lists ignore keyboard navigation. This fixes arrow-key crashes after narrowing a searchable combo. Chummer opts into root-owner modal centering; ScreenBounds converts logical size using native screen coordinate scale for Retina placement.

TreeView now supports horizontal scrolling for expanded text, with translated node bounds, trackpad X gestures, and scrollbar placement within the viewport. StatusStrip AutoSize items measure current rendered text. TreeView exposes its existing pre-render node-formatting hook as an event so Chummer can retain review colors from model data.

DesktopMenus projects shared menu items into native Mac menus, preserving default system app-menu entries while replacing About/Settings/Quit in place. Avalonia bootstrap accepts the application name before native setup. An opt-in Command-key mapping keeps shared text-editing shortcuts functional on Mac. MenuItem.CommandStarting reports command invocation before user handlers for privacy-conscious diagnostics.

ComboBox exposes DropDownOpening before popup measurement. Searchable providers use it to restore the complete category list for browsing; typing opens filtered suggestions without taking input focus. Switching between these modes recreates the popup with the appropriate activation behavior while retaining its list control.

Form.ApplicationIconOverride is an opt-in shared branding icon. Chummer sets it before constructing forms, including dialogs, and it replaces designer-provided legacy icons without changing other consumers' defaults.

PopupWindow.Show(Control, x, y) maps through Control.PointToScreen, respecting an MDI document's visible host and native screen scale. ListBox has opt-in cumulative prefix selection (1.2-second idle reset, mouse/focus/navigation reset). Chummer enables it for all lists so successive m/a/g keys search for mag instead of separate initials.

AutoSize Label changes invalidate the parent background when resizing, clearing pixels outside the new bounds when a displayed value gets shorter (cyberware Capacity/Cost).

MenuItem routes command-handler exceptions through the existing Application.ThreadException boundary when subscribed, including native menu commands; consumers without a handler retain exception propagation.

Application.Exit closes only still-open forms after one successful FormClosing approval pass, without raising closing handlers twice. It does not cancel the loop if a form remains open.

DesktopMenus checks actual Avalonia host enablement when projecting and executing native commands. Modal owner disabling previously did not flow through MenuItem.Enabled; queued native clicks could still invoke the source command. About/Preferences and document commands disable during modal work and recover afterward; Quit stays available to cancel owned operations. NativeControlChecks drives the real exporter click bridge, including stale selections, on both desktop CI platforms.

## Shared repaint ownership

FormClientArea clears its own cached bitmap to transparent before recompositing children, without changing ambient BackColor inheritance or covering the window's Paint output. Control geometry/visibility changes invalidate parent composites to erase old footprints. WindowBase.Invalidate(true) now dirties all nested children. Control.DiscardCachedPixels lets ListBox/TreeView discard pending scroll-blit reuse flags on full refresh. RenderingDiagnostics.ForceFullRedraw bypasses control pixels on requested frames without scheduling an idle loop. TreeView filters rows only after scrollbar clamping, fixing the first frame after collapsing a scrolled branch. RenderingChecks contains before/after frame comparisons and independent footprint/custom-paint checks at scales 1 and 2; NativeRenderingChecks verifies actual repaint delivery and the session diagnostic toggle on both desktop platforms. See docs/RENDERING.md for ownership and maintenance boundaries.
