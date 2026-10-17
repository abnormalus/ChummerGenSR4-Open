# Native dialog owner themes

Owned Avalonia windows are separate theme roots. `ShowDialog(owner)` establishes modality but does not guarantee inheritance of an explicit owner theme override. The armor regression reproduced a dark owner opening a light picker when the application default was light; the original host failed the native check before the fix.

`NativeDialogHost.ShowAsync` now applies the actual native owner's theme before opening and listens for `ActualThemeVariantChanged` while the dialog is open. This shared boundary covers native catalog pickers and expanded detail dialogs without fixed screen colors. The handler is removed in `finally`, the dialog's prior requested theme is restored, and existing modality/input cleanup remains in place even when showing fails. Do not subscribe globally per dialog or leave closed windows attached to an owner.

Native armor checks cover opposite owner/application themes, light-to-dark and dark-to-light changes while open, subscription cleanup and restored owner input. Explicitly whitelisted synthetic Armor Jacket screenshots render both themes at double resolution. Source/character data and settings are unchanged. Verify the native suite and both platform screenshots before publication; this does not claim every legacy color or physical-monitor DPI scenario has been migrated.
