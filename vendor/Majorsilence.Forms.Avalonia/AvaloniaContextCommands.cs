using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Threading;
using AvContext = Avalonia.Controls.ContextMenu;
using AvControl = Avalonia.Controls.Control;
using AvItem = Avalonia.Controls.MenuItem;

namespace Majorsilence.Forms;

/// <summary>Presents an existing context command tree through an Avalonia popup.</summary>
public static class AvaloniaContextCommands
{
    /// <summary>Hosts compatibility control context menus in the workspace's Avalonia window.</summary>
    public static void InstallWorkspace(Form workspace, Action<Avalonia.Styling.Styles> applyTheme)
    {
        var window = workspace.ToAvaloniaWindow();
        AvContext? open = null;
        Action? release = null;

        void Close()
        {
            release?.Invoke();
            release = null;
            var popup = open;
            open = null;
            popup?.Close();
        }

        workspace.ShowHostedContextMenu = (menu, anchor, source, screen) =>
        {
            Close();
            var owner = anchor.FindForm();
            var client = workspace.PointToClient(screen);
            var tree = anchor as TreeView;
            var node = tree?.SelectedNode;

            bool IsCurrent() =>
                !workspace.IsDisposed && workspace.Enabled && window.IsEnabled &&
                !anchor.IsDisposed && anchor.Visible && anchor.Enabled && owner == anchor.FindForm() &&
                (owner == workspace || workspace.ActiveMdiChild == owner) &&
                (tree == null || ReferenceEquals(node, tree.SelectedNode));

            var popup = Show(window, new Point(client.X, client.Y), menu, source, IsCurrent, applyTheme);
            if (popup == null)
                return;

            open = popup;
            void AnchorChanged(object? sender, EventArgs args)
            {
                if (!IsCurrent())
                    Close();
            }
            anchor.VisibleChanged += AnchorChanged;
            anchor.EnabledChanged += AnchorChanged;
            anchor.Disposed += AnchorChanged;
            popup.Closed += (_, _) =>
            {
                if (ReferenceEquals(open, popup))
                    Close();
            };
            release = () =>
            {
                anchor.VisibleChanged -= AnchorChanged;
                anchor.EnabledChanged -= AnchorChanged;
                anchor.Disposed -= AnchorChanged;
            };
        };
        workspace.MdiChildActivate += (_, _) => Close();
        workspace.Disposed += (_, _) =>
        {
            workspace.ShowHostedContextMenu = null;
            Close();
        };
    }

    /// <summary>Opens a native menu after running the shared opening handlers.</summary>
    public static AvContext? Show(AvControl anchor, Point point, ContextMenu source, Control? legacyAnchor,
        Func<bool> isCurrentTarget, Action<Avalonia.Styling.Styles> applyTheme)
    {
        if (!source.PrepareHostedContext(legacyAnchor) || !isCurrentTarget())
            return null;

        var menu = new AvContext { Placement = Avalonia.Controls.PlacementMode.Bottom, PlacementRect = new Rect(point, new Size(0, 0)) };
        applyTheme(menu.Styles);
        var icons = new List<Action>();
        bool active = true;
        bool detached = false;
        var reason = ToolStripDropDownCloseReason.AppFocusChange;

        bool Contains(IEnumerable<MenuItem> items, MenuItem target) =>
            items.Any(item => item.Visible && item.Enabled &&
                (ReferenceEquals(item, target) || Contains(item.Items, target)));

        AvControl Entry(MenuItem item)
        {
            if (item is ToolStripSeparator or MenuSeparatorItem)
                return new Avalonia.Controls.Separator();

            var entry = new AvItem
            {
                Header = AvaloniaWorkspaceCommands.AccessText(item.Text),
                IsEnabled = item.Enabled,
                Tag = item
            };
            if (item is ToolStripItem { DisplaysImage: true } visual)
                entry.Icon = AvaloniaWorkspaceCommands.CreateIcon(visual, icons);
            if (item is ToolStripMenuItem shortcut)
                entry.InputGesture = AvaloniaWorkspaceCommands.Gesture(shortcut.ShortcutKeys, OperatingSystem.IsMacOS());
            if (item.Checked || item is ToolStripMenuItem { CheckOnClick: true })
            {
                entry.ToggleType = Avalonia.Controls.MenuItemToggleType.CheckBox;
                entry.IsChecked = item.Checked;
            }

            void Fill()
            {
                entry.Items.Clear();
                foreach (var child in item.Items.Where(child => child.Visible))
                    entry.Items.Add(Entry(child));
            }
            if (item.HasItems)
            {
                Fill();
                entry.SubmenuOpened += (_, args) =>
                {
                    if (ReferenceEquals(args.Source, entry))
                    {
                        if (item is ToolStripDropDownItem dropdown && !dropdown.PrepareHostedDropDown())
                            entry.IsSubMenuOpen = false;
                        else
                            Fill();
                    }
                };
            }
            else
            {
                entry.AddHandler(AvItem.ClickEvent, (_, args) =>
                {
                    if (!ReferenceEquals(args.Source, entry) || !active || !anchor.IsEffectivelyVisible ||
                        !isCurrentTarget() || !Contains(source.Items, item))
                        return;
                    reason = ToolStripDropDownCloseReason.ItemClicked;
                    item.PerformClick();
                    entry.IsChecked = item.Checked;
                }, RoutingStrategies.Bubble, handledEventsToo: true);
            }
            return entry;
        }

        foreach (var item in source.Items.Where(item => item.Visible))
            menu.Items.Add(Entry(item));
        if (menu.ItemCount == 0)
            return null;

        void AnchorDetached(object? sender, VisualTreeAttachmentEventArgs args)
        {
            detached = true;
            menu.Close();
        }
        anchor.DetachedFromVisualTree += AnchorDetached;
        menu.Opened += (_, _) => source.NotifyHostedOpened();
        menu.Closing += (_, args) => args.Cancel = !detached && !source.CanCloseHostedContext(reason);
        menu.Closed += (_, _) =>
        {
            anchor.DetachedFromVisualTree -= AnchorDetached;
            source.NotifyHostedClosed(reason);
            // Allow the routed click already in flight to finish after popup dismissal.
            Dispatcher.UIThread.Post(() =>
            {
                active = false;
                foreach (var dispose in icons)
                    dispose();
                icons.Clear();
            }, DispatcherPriority.Input);
        };
        menu.Open(anchor);
        return menu;
    }
}
