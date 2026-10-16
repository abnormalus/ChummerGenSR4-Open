# Legacy dropdown placement bridge

ComboBox previously always showed its separate popup below the field, even behind the Mac Dock or Windows taskbar. Both knowledge type and searchable knowledge name use this shared path. PopupWindow.ShowBelow now anchors dropdowns within the control monitor's working area: open below when it fits, flip above when that provides more room, otherwise constrain list dimensions and retain scrolling. Other popup/menu placement is unchanged.

The native catalog screens use standard Avalonia controls. Replacing every legacy ComboBox with a native overlay would require a separate focus, editing, scrolling and lifetime migration. Avalonia's ManagedPopupPositioner is marked PrivateApi in the pinned 12.1.1 source, so this bridge deliberately avoids depending on that unstable contract. Keep this small backend-neutral geometry policy until the affected screens migrate to standard controls. No dependency or speculative graphics fallback is added.

Screen anchors and working areas are physical pixels. Requested/backend window sizes are logical units and are converted with the presenting window's desktop scale; never scale absolute monitor origins. Screen.FromRectangle selects the anchor monitor, including negative-origin monitors. Empty working areas fall back to monitor bounds. The desired dropdown size is recalculated on every open, so a previously constrained popup can grow again.

DropDownPlacementChecks exercises ordinary downward placement, upward placement above reserved screen space, oversized lists, negative origins and 1x/2x geometry. NativeDropDownChecks opens actual dropdown windows near the bottom, checks the actual popup bounds, keyboard access to the final entry, Escape dismissal and oversized sizing on Mac and Windows. Combined native CI minimum: 109 checks. Character schema, item selection/filter rules and settings are unchanged.

Framework reference: [Avalonia 12.1.1 ManagedPopupPositioner](https://github.com/AvaloniaUI/Avalonia/blob/12.1.1/src/Avalonia.Controls/Primitives/PopupPositioning/ManagedPopupPositioner.cs).
