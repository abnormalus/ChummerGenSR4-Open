using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.Styling;
using AvMenu = Avalonia.Controls.Menu;
using AvItem = Avalonia.Controls.MenuItem;
using AvStack = Avalonia.Controls.StackPanel;
using AvKeyEventArgs = Avalonia.Input.KeyEventArgs;

namespace Majorsilence.Forms
{
    /// <summary>Presents the current workspace command strips with Avalonia controls.</summary>
    public sealed class AvaloniaWorkspaceCommands : IDisposable
    {
        private readonly Form owner;
        private readonly MenuStrip menus;
        private readonly ToolStrip toolbar;
        private readonly Window window;
        private readonly bool hasSystemMenu;
        private readonly NativeControlHost menuHost, toolbarHost;
        private const int MenuScrollbarHeight = 16;
        private readonly AvMenu menu = new () { Name = "WorkspaceMenu" };
        private readonly WrapPanel tools = new () { Name = "WorkspaceToolbar", ItemSpacing = 2, LineSpacing = 2 };
        private readonly AvStack windowButtons = new () { Orientation = Avalonia.Layout.Orientation.Horizontal };
        private readonly List<Action> unsubscribe = new ();
        private Majorsilence.Forms.MenuItem[] displayedMenus = [];
        private ToolStripItem[] displayedTools = [];
        private bool pending, toolbarMeasurePending, menuMeasurePending, disposed;

        public AvaloniaWorkspaceCommands (Form owner, MenuStrip menus, ToolStrip toolbar,
            bool hasSystemMenu, Func<Majorsilence.Forms.MenuItem, bool> secondaryAction, Action<Avalonia.Styling.Styles> applyTheme)
        {
            this.owner = owner;
            this.menus = menus;
            this.toolbar = toolbar;
            this.hasSystemMenu = hasSystemMenu;
            window = owner.ToAvaloniaWindow ();
            SecondaryAction = secondaryAction;

            var menuRow = new DockPanel ();
            DockPanel.SetDock (windowButtons, Dock.Right);
            menuRow.Children.Add (windowButtons);
            var menuViewport = new ScrollViewer
            {
                Content = menu, AllowAutoHide = false,
                HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled
            };
            menuViewport.Styles.Add (new Avalonia.Styling.Style (selector => selector
                .OfType<Avalonia.Controls.Primitives.ScrollBar> ()
                .PropertyEquals (Avalonia.Controls.Primitives.ScrollBar.OrientationProperty, Orientation.Horizontal))
            {
                Setters = { new Avalonia.Styling.Setter (Avalonia.Controls.Primitives.ScrollBar.HeightProperty, (double)MenuScrollbarHeight) }
            });
            menuRow.Children.Add (menuViewport);
            applyTheme (menuRow.Styles);
            applyTheme (tools.Styles);
            AutomationProperties.SetName (menu, "Workspace menu");
            AutomationProperties.SetName (tools, "Workspace toolbar");
            menuHost = Mount (menus, menuRow, "NativeWorkspaceMenu", 32);
            toolbarHost = Mount (toolbar, new Border { Child = tools }, "NativeWorkspaceToolbar", 30);
            toolbarHost.SizeChanged += ToolbarSizeChanged;
            tools.PropertyChanged += ToolbarTypographyChanged;
            menu.PropertyChanged += MenuTypographyChanged;
            menuHost.VisibleChanged += MenuVisibilityChanged;
            menuHost.SizeChanged += MenuVisibilityChanged;
            menus.HasNativePresentation = true;
            menus.ItemAdded += CollectionChanged;
            menus.ItemRemoved += CollectionChanged;
            toolbar.ItemAdded += CollectionChanged;
            toolbar.ItemRemoved += CollectionChanged;
            window.PropertyChanged += WindowChanged;
            window.AddHandler (InputElement.KeyDownEvent, MenuKeyDown, RoutingStrategies.Tunnel);
            UpdateMenuVisibility ();
            Refresh ();
        }

        private Func<Majorsilence.Forms.MenuItem, bool> SecondaryAction { get; }

        private NativeControlHost Mount (ToolStrip source, Avalonia.Controls.Control content, string name, int height)
        {
            var host = new NativeControlHost { Name = name, Dock = source.Dock, Height = height, NativeControl = content };
            var index = owner.Controls.GetChildIndex (source);
            owner.Controls.Add (host);
            owner.Controls.SetChildIndex (host, index);
            source.Visible = false;
            host.VisibleChanged += (_, _) => host.SyncNativeControl ();
            host.LocationChanged += (_, _) => host.SyncNativeControl ();
            host.SizeChanged += (_, _) => host.SyncNativeControl ();
            return host;
        }

        private void CollectionChanged (object? sender, ToolStripItemEventArgs e) => RefreshSoon ();

        private void RefreshSoon ()
        {
            if (pending || disposed)
                return;
            pending = true;
            Dispatcher.UIThread.Post (() => { if (pending) Refresh (); }, DispatcherPriority.Loaded);
        }

        private void WindowChanged (object? sender, AvaloniaPropertyChangedEventArgs args)
        {
            if (args.Property == Window.WindowStateProperty)
                UpdateMenuVisibility ();
        }

        private void UpdateMenuVisibility ()
            => menuHost.Visible = !hasSystemMenu || window.WindowState == WindowState.FullScreen;

        private void MenuKeyDown (object? sender, AvKeyEventArgs args)
        {
            if (menuHost.Visible && args.Key == Key.F10 && args.KeyModifiers == KeyModifiers.None &&
                menu.Items.OfType<AvItem> ().FirstOrDefault (item => item.IsEnabled && item.IsVisible) is { } first)
            {
                first.Focus ();
                args.Handled = true;
            }
        }

        private void Refresh ()
        {
            pending = false;
            if (disposed)
                return;

            var currentMenus = menus.Items.ToArray ();
            var currentTools = toolbar.Items.OfType<ToolStripItem> ().ToArray ();
            // Nested entries are projected when their submenu opens. They can notify the
            // strip while open without replacing its roots or taking focus from the popup.
            if (!currentMenus.SequenceEqual (displayedMenus) || !currentTools.SequenceEqual (displayedTools))
            {
                displayedMenus = currentMenus;
                displayedTools = currentTools;
                Clear ();
                foreach (var item in currentMenus)
                {
                    if (item is MdiControlItem control)
                    {
                        if (control.ControlKind != MdiControlItem.Kind.System)
                            windowButtons.Children.Add (CreateButton (control, control.ToolTipText, menus));
                        else
                            menu.Items.Add (CreateMenuItem (item, "Window controls"));
                    }
                    else
                        menu.Items.Add (CreateMenuItem (item));
                }
                foreach (var item in currentTools)
                {
                    if (item is ToolStripSeparator)
                    {
                        var separator = new Avalonia.Controls.Separator { Width = 6, Height = 20, VerticalAlignment = VerticalAlignment.Center };
                        Observe (item, () =>
                        {
                            separator.IsVisible = item.Visible;
                            QueueToolbarMeasure ();
                        }, unsubscribe);
                        tools.Children.Add (separator);
                    }
                    else
                        tools.Children.Add (CreateButton (item, null, toolbar));
                }
            }
            menuHost.SyncNativeControl ();
            toolbarHost.SyncNativeControl ();
            QueueToolbarMeasure ();
            QueueMenuMeasure ();
        }

        private void ToolbarSizeChanged (object? sender, EventArgs args) => QueueToolbarMeasure ();

        private void ToolbarTypographyChanged (object? sender, AvaloniaPropertyChangedEventArgs args)
        {
            if (args.Property == Avalonia.Controls.Documents.TextElement.FontSizeProperty ||
                args.Property == Avalonia.Controls.Documents.TextElement.FontFamilyProperty)
                QueueToolbarMeasure ();
        }

        private void MenuVisibilityChanged (object? sender, EventArgs args) => QueueMenuMeasure ();

        private void MenuTypographyChanged (object? sender, AvaloniaPropertyChangedEventArgs args)
        {
            if (args.Property == Avalonia.Controls.Documents.TextElement.FontSizeProperty ||
                args.Property == Avalonia.Controls.Documents.TextElement.FontFamilyProperty)
                QueueMenuMeasure ();
        }

        private void QueueMenuMeasure ()
        {
            if (menuMeasurePending || disposed)
                return;

            menuMeasurePending = true;
            Dispatcher.UIThread.Post (() =>
            {
                menuMeasurePending = false;
                if (disposed || !menuHost.Visible)
                    return;

                menu.Measure (Size.Infinity);
                windowButtons.Measure (Size.Infinity);
                var sample = new TextBlock { Text = "Mg", FontSize = menu.FontSize, FontFamily = menu.FontFamily };
                sample.Measure (Size.Infinity);
                double desiredHeight = Math.Max (sample.DesiredSize.Height + 8,
                    Math.Max (menu.DesiredSize.Height, windowButtons.DesiredSize.Height));
                double availableWidth = Math.Max (0, menuHost.Width - windowButtons.DesiredSize.Width);
                int scrollbarHeight = menu.DesiredSize.Width > availableWidth ? MenuScrollbarHeight : 0;
                menuHost.Height = Math.Max (32, (int)Math.Ceiling (desiredHeight)) + scrollbarHeight;
            }, DispatcherPriority.Loaded);
        }

        private void QueueToolbarMeasure ()
        {
            if (toolbarMeasurePending || disposed)
                return;

            toolbarMeasurePending = true;
            Dispatcher.UIThread.Post (() =>
            {
                toolbarMeasurePending = false;
                if (disposed || toolbarHost.Width <= 0)
                    return;

                foreach (var button in tools.Children.OfType<Avalonia.Controls.Button> ())
                    button.MaxWidth = toolbarHost.Width;
                tools.Measure (new Size (toolbarHost.Width, double.PositiveInfinity));
                int height = Math.Max (30, (int)Math.Ceiling (tools.DesiredSize.Height) + 2);
                if (toolbarHost.Height != height)
                    toolbarHost.Height = height;
            }, DispatcherPriority.Loaded);
        }

        private AvItem CreateMenuItem (Majorsilence.Forms.MenuItem item, string? title = null, List<Action>? cleanup = null)
        {
            cleanup ??= unsubscribe;
            var entry = new AvItem { Tag = item };
            var fontSize = entry.Bind (AvItem.FontSizeProperty, menu.GetObservable (AvMenu.FontSizeProperty));
            var fontFamily = entry.Bind (AvItem.FontFamilyProperty, menu.GetObservable (AvMenu.FontFamilyProperty));
            cleanup.Add (fontSize.Dispose);
            cleanup.Add (fontFamily.Dispose);
            if (item is ToolStripItem { DisplaysImage: true } visual)
                entry.Icon = CreateIcon (visual, cleanup);
            var children = new List<Action> ();
            cleanup.Add (() => Release (children));
            void UpdateChildren ()
            {
                entry.Items.Clear ();
                Release (children);
                foreach (var child in item.Items)
                {
                    if (child is ToolStripSeparator or MenuSeparatorItem)
                    {
                        var separator = new Avalonia.Controls.Separator ();
                        Observe (child, () => separator.IsVisible = child.Visible, children);
                        entry.Items.Add (separator);
                    }
                    else
                        entry.Items.Add (CreateMenuItem (child, cleanup: children));
                }
            }
            void Update ()
            {
                entry.Header = AccessText (title ?? item.Text);
                entry.IsEnabled = item.Enabled;
                entry.IsVisible = item.Visible;
                entry.IsChecked = item.Checked;
                entry.ToggleType = item.Checked || item is ToolStripMenuItem { CheckOnClick: true }
                    ? MenuItemToggleType.CheckBox : MenuItemToggleType.None;
                if (item is ToolStripMenuItem shortcut)
                    entry.InputGesture = Gesture (shortcut.ShortcutKeys, hasSystemMenu);
            }

            if (item.HasItems || item.MdiList || ReferenceEquals (menus.MdiWindowListItem, item))
            {
                UpdateChildren ();
                entry.SubmenuOpened += (_, args) =>
                {
                    if (!ReferenceEquals (args.Source, entry))
                        return;
                    if (item is ToolStripDropDownItem dropdown && !dropdown.PrepareHostedDropDown ())
                    {
                        entry.IsSubMenuOpen = false;
                        return;
                    }
                    item.PopulateMdiWindowList ();
                    UpdateChildren ();
                };
            }
            else
            {
                entry.AddHandler (AvItem.ClickEvent, (_, args) =>
                {
                    if (ReferenceEquals (args.Source, entry) && CanInvoke (menus, item))
                        item.PerformClick ();
                    Update ();
                }, RoutingStrategies.Bubble, handledEventsToo: true);
                entry.AddHandler (InputElement.PointerPressedEvent, (_, args) =>
                {
                    if (args.GetCurrentPoint (entry).Properties.IsRightButtonPressed && CanInvoke (menus, item))
                        args.Handled = SecondaryAction (item);
                }, RoutingStrategies.Tunnel);
            }
            Observe (item, Update, cleanup);
            return entry;
        }

        internal static Avalonia.Controls.Image? CreateIcon (ToolStripItem item, List<Action> cleanup)
        {
            if (item.ImageSK is not { } bitmap)
                return null;

            var key = item.ImageTransparentColor;
            if (!key.IsEmpty)
                bitmap = ColorKeyedBitmaps.Apply (bitmap, new SkiaSharp.SKColor (key.R, key.G, key.B, key.A));
            using var pixels = bitmap.Encode (SkiaSharp.SKEncodedImageFormat.Png, 100);
            using var stream = pixels.AsStream ();
            var icon = new Bitmap (stream);
            cleanup.Add (icon.Dispose);
            return new Avalonia.Controls.Image { Width = 16, Height = 16, Source = icon };
        }

        private Avalonia.Controls.Button CreateButton (ToolStripItem item, string? title, ToolStrip source)
        {
            var image = CreateIcon (item, unsubscribe);
            var text = new TextBlock { VerticalAlignment = VerticalAlignment.Center, TextWrapping = Avalonia.Media.TextWrapping.Wrap };
            var content = new DockPanel { LastChildFill = true };
            if (image != null) {
                DockPanel.SetDock (image, Dock.Left);
                image.Margin = new Thickness (0, 0, 4, 0);
                content.Children.Add (image);
            }
            content.Children.Add (text);
            var button = new Avalonia.Controls.Button
            {
                Tag = item, Content = content, Padding = new Thickness (5, 2), MinWidth = 0, MinHeight = 0,
                VerticalAlignment = VerticalAlignment.Center
            };
            button.Click += (_, _) =>
            {
                if (CanInvoke (source, item))
                    item.PerformClick ();
            };
            Observe (item, () =>
            {
                string label = Caption (title ?? item.Text);
                if (image != null)
                    image.IsVisible = item.DisplaysImage;
                text.IsVisible = image?.IsVisible != true || item.DisplaysText;
                text.Text = item is MdiControlItem mdi ? mdi.ControlKind switch {
                    MdiControlItem.Kind.Minimize => "−", MdiControlItem.Kind.Restore => "❐", _ => "×"
                } : label;
                button.IsEnabled = item.Enabled;
                button.IsVisible = item.Visible;
                AutomationProperties.SetName (button, label);
                Avalonia.Controls.ToolTip.SetTip (button, string.IsNullOrEmpty (item.ToolTipText) ? label : item.ToolTipText);
                if (ReferenceEquals (source, toolbar))
                    QueueToolbarMeasure ();
            }, unsubscribe);
            return button;
        }

        private bool CanInvoke (ToolStrip source, Majorsilence.Forms.MenuItem item)
            => !disposed && owner.Enabled && window.IsEnabled && Contains (source is MenuStrip strip ? strip.Items : source.Items, item);

        private static bool Contains (IEnumerable<Majorsilence.Forms.MenuItem> items, Majorsilence.Forms.MenuItem target)
            => items.Any (item => item.Visible && item.Enabled &&
                (ReferenceEquals (target, item) || Contains (item.Items, target)));

        private static void Observe (Majorsilence.Forms.MenuItem source, Action update, List<Action> cleanup)
        {
            update ();
            if (source is not ToolStripItem item)
                return;
            void Changed (object? sender, EventArgs args) => update ();
            item.TextChanged += Changed;
            item.VisibleChanged += Changed;
            item.EnabledChanged += Changed;
            if (item is ToolStripMenuItem menuItem)
                menuItem.CheckedChanged += Changed;
            cleanup.Add (() =>
            {
                item.TextChanged -= Changed;
                item.VisibleChanged -= Changed;
                item.EnabledChanged -= Changed;
                if (item is ToolStripMenuItem menuItem)
                    menuItem.CheckedChanged -= Changed;
            });
        }

        internal static string AccessText (string text) => text.Replace ("_", "__").Replace ("&&", "\0").Replace ("&", "_").Replace ("\0", "&");
        private static string Caption (string text) => text.Replace ("&&", "\0").Replace ("&", "").Replace ("\0", "&");

        internal static KeyGesture? Gesture (Keys shortcut, bool mac)
        {
            if (!Enum.TryParse<Key> ((shortcut & Keys.KeyCode).ToString (), out var key) || key == Key.None)
                return null;
            var modifiers = KeyModifiers.None;
            if ((shortcut & Keys.Control) != 0) modifiers |= mac ? KeyModifiers.Meta : KeyModifiers.Control;
            if ((shortcut & Keys.Shift) != 0) modifiers |= KeyModifiers.Shift;
            if ((shortcut & Keys.Alt) != 0) modifiers |= KeyModifiers.Alt;
            return new KeyGesture (key, modifiers);
        }

        private static void Release (List<Action> subscriptions)
        {
            foreach (var remove in subscriptions)
                remove ();
            subscriptions.Clear ();
        }

        private void Clear ()
        {
            menu.Items.Clear ();
            tools.Children.Clear ();
            windowButtons.Children.Clear ();
            Release (unsubscribe);
        }

        public void Dispose ()
        {
            if (disposed)
                return;
            disposed = true;
            menus.HasNativePresentation = false;
            menus.ItemAdded -= CollectionChanged;
            menus.ItemRemoved -= CollectionChanged;
            toolbar.ItemAdded -= CollectionChanged;
            toolbar.ItemRemoved -= CollectionChanged;
            toolbarHost.SizeChanged -= ToolbarSizeChanged;
            tools.PropertyChanged -= ToolbarTypographyChanged;
            menu.PropertyChanged -= MenuTypographyChanged;
            menuHost.VisibleChanged -= MenuVisibilityChanged;
            menuHost.SizeChanged -= MenuVisibilityChanged;
            window.PropertyChanged -= WindowChanged;
            window.RemoveHandler (InputElement.KeyDownEvent, MenuKeyDown);
            Clear ();
            displayedMenus = [];
            displayedTools = [];
            menuHost.NativeControl = null;
            toolbarHost.NativeControl = null;
        }
    }
}
