# Native weapon picker

`WeaponPickerWindow` uses standard Avalonia layout, lists, scrolling, prefix typing and bindings. `WeaponPickerModel` owns observable presentation state and literal search across enabled sourcebooks. Browsing creates an unattached core Weapon for established calculated values; it never purchases or changes inventory.

`frmSelectWeapon` is the compatibility boundary for existing creation and career handlers. Acceptance returns name, category, free purchase, markup and Add More. Name plus category identifies duplicate catalog names correctly. The callers retain purchase, add-in and inventory rules. Character file fields and encoding are unchanged. The old picker designer and resource file are removed from the Mac/Windows application; historical original sources remain unchanged.

The preferred entry point is asynchronous. Existing synchronous handlers use the backend modal loop until their commands can migrate independently. `NativeDialogHost` records native dialogs for diagnostics and Quit cancellation and synchronizes owner availability with desktop menu guards. It always removes registration and restores owner availability on completion or failure.

When replacing list rows, the model ignores only synchronous selection clearing caused by ItemsSource replacement, then publishes its intended selection. Detail changes never republish ItemsSource. Keep these ordered notifications: a blanket all-properties notification can clear a valid selection through two-way binding.

Invalid catalog definitions clear details, display a localized error and disable acceptance. Acceptance rechecks enabled sources. Search uses literal comparisons rather than user-interpolated XPath. The English localization fallback includes new UI strings. No dependency is added.

## Verification

`WeaponPickerChecks` runs in the headless smoke suite: source filtering, quoted search, calculated details, free/marked-up cost, duplicate names, malformed entries and unchanged character snapshots. `NativeWeaponPickerChecks` runs in the combined Mac/Windows bulk checks: real bindings, cumulative typing, empty search, resizing, Add More, cancellation, Quit and the existing synchronous creation handler. CI requires at least 41 combined native checks. The generated native picker screenshot supports visual inspection of the smaller layout.

Future migrations should keep rules out of the window and move callers to asynchronous commands incrementally. Other equipment pickers still use the compatibility controls; they are separate bounded migrations.
