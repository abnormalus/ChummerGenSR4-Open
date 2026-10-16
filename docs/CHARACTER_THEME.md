# Character theme colors

Issue #15 was reproduced on a synthetic creation character: after switching from light to dark, an attribute label resolved to foreground `#DEDEDE` on background `#F0F0F0` (1.18:1 contrast). The existing empty-workspace tests could not detect this. Designer panels explicitly assign system colors, while unassigned text follows the host theme.

Two shared boundaries caused the mismatch. `ColorCompatExtensions.ToSKColor` used the drawing package's fixed classic system palette rather than the current UI theme. `Control.BackColor`/`ForeColor` and the window/style wrappers also converted assignments immediately to anonymous pixel colors, losing their system-color identity before a later theme change.

UI conversion now maps system roles, including the distinct `ButtonFace`/`ButtonHighlight`/`ButtonShadow` aliases, to the existing host theme. `ControlStyle` retains system-color declarations and resolves them when read. Literal colors remain literal; a direct Skia or CSS style assignment replaces the retained declaration. Assigning an equal-looking literal color must still replace system-color identity without raising a spurious color-changed event. Drawing-package colors outside these UI conversion paths retain their existing behavior.

`NativeCharacterThemeChecks` opens the synthetic creation fixture in each initial theme, checks live transitions in both directions, and measures actual attribute labels/values and BP labels/values at a minimum 4.5:1 contrast. The reproduced dark case is now 5.99:1; light is 18.93:1. It also checks explicit system colors and preserves literal red/italic styling. Four explicitly named synthetic screenshots support visual inspection on both platforms; no arbitrary character screenshots are added to successful CI artifacts.

This is a shared system-color repair, not a claim that every literal legacy color or disabled state has been redesigned. Continue checking semantic warning contrast and light/dark transitions during each bounded screen migration. Character data, rules and persistence are unchanged.
