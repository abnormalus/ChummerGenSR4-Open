using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;

namespace Majorsilence.Forms
{
    /// <summary>Projects shared command items into the Mac system menu bar.</summary>
    public static class DesktopMenus
    {
        public static string? ApplicationName { get; set; }
        internal static bool CommandShortcutsEnabled { get; private set; }
        public static void Install(Form form, MenuStrip source, MenuItem preferences, MenuItem about,
            MenuItem quit)
        {
            var window = form.ToAvaloniaWindow();
            CommandShortcutsEnabled = true;
            var root = new NativeMenu();
            string Caption(string text) => (text ?? "").Replace("&&", "\u0001").Replace("&", "").Replace("\u0001", "&");
            // A native menu can outlive a modal transition or queue a click before it.
            // Respect the actual host window state as well as the logical item state.
            // Quit remains available so the application can cancel and unwind owned work.
            bool CanExecute(MenuItem item) => item.Enabled && item.Visible &&
                (ReferenceEquals(item, quit) || window.IsEnabled);
            Action refreshApplicationCommands = () => { };
            NativeMenuItem Entry(MenuItem item)
            {
                var entry = new NativeMenuItem { Header = Caption(item.Text), IsEnabled = CanExecute(item), IsVisible = item.Visible };
                if (item is ToolStripMenuItem shortcut && shortcut.ShortcutKeys != Keys.None)
                {
                    int key = (int)(shortcut.ShortcutKeys & Keys.KeyCode);
                    var modifiers = KeyModifiers.None;
                    if ((shortcut.ShortcutKeys & Keys.Control) != 0) modifiers |= KeyModifiers.Meta;
                    if ((shortcut.ShortcutKeys & Keys.Shift) != 0) modifiers |= KeyModifiers.Shift;
                    if ((shortcut.ShortcutKeys & Keys.Alt) != 0) modifiers |= KeyModifiers.Alt;
                    if (Enum.TryParse<Key>(((Keys)key).ToString(), out var nativeKey)) entry.Gesture = new KeyGesture(nativeKey, modifiers);
                }
                if (item.HasItems)
                {
                    entry.Menu = new NativeMenu();
                    item.ParentControl ??= source;
                    item.PopulateMdiWindowList();
                    Fill(entry.Menu, item.Items);
                    entry.Menu.NeedsUpdate += (_, _) => { item.PopulateMdiWindowList(); Fill(entry.Menu, item.Items); };
                }
                else entry.Click += (_, _) => {
                    if (CanExecute(item)) item.PerformClick();
                };
                if (item.Checked) { entry.ToggleType = MenuItemToggleType.CheckBox; entry.IsChecked = true; }
                return entry;
            }
            void Fill(NativeMenu target, System.Collections.Generic.IEnumerable<MenuItem> items)
            {
                target.Items.Clear();
                foreach (var item in items.ToArray())
                {
                    if (!item.Visible || item is MdiControlItem || item == preferences || item == about || item == quit) continue;
                    if (item is ToolStripSeparator || item is MenuSeparatorItem) target.Items.Add(new NativeMenuItemSeparator());
                    else target.Items.Add(Entry(item));
                }
            }
            // Avalonia's existing application-menu object is already observed by the native exporter.
            // Update it in place and retain system Services/Hide entries, replacing its default About/Quit.
            if (Avalonia.Application.Current is { } application)
            {
                var app = NativeMenu.GetMenu(application) ?? new NativeMenu();
                var systemItems = app.Items.Skip(1).Where(item => item is not NativeMenuItem command ||
                    (command.Gesture?.Key != Key.Q && command.Gesture?.Key != Key.OemComma && command.Header != "Quit")).ToArray();
                app.Items.Clear();
                var aboutEntry = Entry(about);
                app.Items.Add(aboutEntry);
                app.Items.Add(new NativeMenuItemSeparator());
                var settings = Entry(preferences); settings.Gesture = new KeyGesture(Key.OemComma, KeyModifiers.Meta);
                app.Items.Add(settings);
                foreach (var item in systemItems) {
                    if (item is NativeMenuItemSeparator && app.Items.LastOrDefault() is NativeMenuItemSeparator) continue;
                    app.Items.Add(item);
                }
                if (app.Items.LastOrDefault() is not NativeMenuItemSeparator) app.Items.Add(new NativeMenuItemSeparator());
                var exit = Entry(quit); exit.Gesture = new KeyGesture(Key.Q, KeyModifiers.Meta); app.Items.Add(exit);
                refreshApplicationCommands = () => {
                    aboutEntry.IsEnabled = CanExecute(about);
                    settings.IsEnabled = CanExecute(preferences);
                    exit.IsEnabled = CanExecute(quit);
                };
                EventHandler<EventArgs> updateApplicationCommands = (_, _) => refreshApplicationCommands();
                app.NeedsUpdate += updateApplicationCommands;
                form.FormClosed += (_, _) => app.NeedsUpdate -= updateApplicationCommands;
                if (NativeMenu.GetMenu(application) == null) NativeMenu.SetMenu(application, app);
            }
            void Refresh()
            {
                root.Items.Clear();
                FillTop();
            }
            void FillTop()
            {
                foreach (var item in source.Items.ToArray())
                    if (item.Visible && item is not MdiControlItem) root.Items.Add(Entry(item));
            }
            Refresh();
            root.NeedsUpdate += (_, _) => Refresh();
            window.PropertyChanged += (_, change) => {
                if (change.Property == Avalonia.Input.InputElement.IsEnabledProperty) {
                    Refresh();
                    refreshApplicationCommands();
                }
            };
            NativeMenu.SetMenu(window, root);
            source.Visible = false;
            // Keep the source command tree for MDI merging and shortcut behavior, without drawing Windows chrome.
            form.MdiChildActivate += (_, _) => Refresh();
        }
    }
}
