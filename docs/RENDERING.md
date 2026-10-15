# Rendering reliability — first modernization pass

The inventory and selection screens still use cached compatibility controls over Avalonia. This pass repairs shared rendering behavior before attempting a screen migration. No character rules, serialization fields, source selections or inventory action semantics are changed.

## Rendering ownership

`WindowBase.RenderFrame` paints the window background and window Paint handlers. The root adapter composites a form's `FormClientArea`, which owns a separate cached bitmap, and that client area composites its children. Each nested control can reuse its previous bitmap while unchanged. `NeedsPaint` propagates dirty visible descendants up the hierarchy.

The client area's old background override did nothing. Reusing its bitmap therefore retained previous child pixels even when the window underneath was repainted. It now clears that bitmap to transparent before compositing children. This is a pixel operation, not a transparent BackColor: descendants must still inherit the proper ambient background color. The adapter's background behavior remains appropriate to its direct window canvas.

Geometry and visibility changes invalidate the parent composite as well as the affected control. This removes old footprints after movement, shrinking and hiding. `WindowBase.Invalidate(true)` now honors the request to dirty nested children; `Invalidate(false)` preserves unchanged child caches. `Control.DiscardCachedPixels` is the shared internal full-refresh hook. TreeView and ListBox also clear their pending scroll-blit reuse flags through that hook, so a full refresh really repaints their content.

TreeView computes the final scrollbar/clamped top index before filtering its visible rows. Previously collapsing a scrolled branch could filter using the old offset, then reset the offset during layout and paint an empty first frame.

## Full-redraw diagnostic

In Help, toggle **Full Redraw (Diagnostic)**. It is off at startup, is session-only, and is recorded in diagnostic logs as `fullRedraw` with fixed on/off events. The toggle invalidates open windows once. Each subsequently requested frame repaints visible cached controls; list/tree scroll blits are bypassed. Hidden controls stay hidden. It does not create an idle repaint timer or continuously redraw an unchanged window.

Use the same interaction with the option off and on. An artifact that disappears only with it on points toward a missed invalidation/cache dependency; an artifact in both modes can involve layout, clipping, drawing or compositing itself. This is evidence, not an automatic diagnosis. Leave it off for normal use; the option trades performance for investigation and must not hide an unfixed defect by default.

## Verification

`RenderingChecks` runs in the standard headless smoke suite. It compares normal and complete frames after list selection/text edits, shrinking/moving/hiding/restoring details, inventory selection/editing/scrolling/resizing/collapse/expansion, pending scroll blits and tab changes at UI scales 1 and 2. It independently checks pixels outside a resized tree's new bounds, so matching cached/complete frames cannot conceal an uncleared parent composite. It also checks window custom painting and ambient background colors. Failure images are saved alongside smoke output; representative inventory images are retained for inspection.

`NativeRenderingChecks` runs within `--check-bulk` on Mac and Windows CI. It verifies actual native frame delivery, full-child invalidation, ordinary cache reuse, the diagnostic menu toggle, return to cached rendering and hidden-control behavior. The combined native report must contain at least 33 checks. Headless pixels alone cannot establish native repaint scheduling.

The initial nested redraw test failed before the Invalidate correction. Frame comparisons then reproduced movement and scrolled-collapse defects, and the independent footprint check reproduced stale pixels in the form client composite. These failures were corrected at shared ownership boundaries rather than bypassed in individual inventory screens.

## Next bounded migration

Use these checks as acceptance coverage while moving a selected inventory list/detail section to standard Avalonia controls and observable presentation. The legacy editors still use compatibility controls directly in rule handlers, so changing all lists at once would also change rule/UI interfaces. Extract one bounded section first, retain source filtering, editable details, selection, nested add-ins and undo behavior, then remove its old drawing path after native validation. This first pass does not claim that the inventory screens have already been rewritten as standard Avalonia controls.
