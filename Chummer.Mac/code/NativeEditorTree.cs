#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Forms = Majorsilence.Forms;

namespace Chummer;

internal static class NativeEditorTree
{
    internal static TreeView? Attach(Forms.Form editor, Forms.TreeView source, string name,
        string accessibleName, Action? refreshDetails = null, Action? delete = null, Action? editDetail = null,
        Func<Forms.TreeNode, System.Drawing.Color>? foreground = null, bool followSourceBounds = false, NativeTreeDrag? drag = null,
        Forms.NativeControlHost? layoutHost = null, Func<TreeView, Control>? compose = null)
    {
        if (Application.Current == null || editor.IsDisposed || source.Parent is not { } parent)
            return null;

        var tree = new TreeView { Name = name, ClipToBounds = true, IsEnabled = source.Enabled };
        AutomationProperties.SetName(tree, accessibleName);
        NativeControlPresentation.AddTheme(tree.Styles);
        tree.Styles.Add(new Style(selector => selector.OfType<TreeView>())
        {
            Setters = { new Setter(TreeView.BackgroundProperty, new DynamicResourceExtension("SystemControlBackgroundListLowBrush")) }
        });
        Control presentation = compose?.Invoke(tree) ?? tree;
        bool mdi = layoutHost == null && editor.MdiParent != null;
        Forms.Control region = layoutHost ?? (mdi ? new Forms.Panel() : (Forms.Control)new Forms.NativeControlHost { NativeControl = presentation });
        if (layoutHost == null)
        {
            region.Bounds = source.Bounds;
            region.Anchor = followSourceBounds ? Forms.AnchorStyles.Top | Forms.AnchorStyles.Left : source.Anchor;
            region.Dock = source.Dock;
            region.TabIndex = source.TabIndex;
            region.TabStop = false;
            region.Name = name;
            parent.Controls.Add(region);
        }
        parent.Controls.Remove(source);
        // A tree on an initially hidden tab may never have been created. Its
        // node collections raise invalidation events only once created.
        source.CreateControl();
        SyncVisibility();

        // The responsive creation layout continues to calculate its tree bounds.
        void FollowLayout(object? sender, EventArgs args) => region.Bounds = source.Bounds;
        void FollowEnabled(object? sender, EventArgs args) => tree.IsEnabled = source.Enabled;
        if (followSourceBounds && layoutHost == null)
            source.SizeChanged += FollowLayout;
        source.EnabledChanged += FollowEnabled;
        var presenter = mdi
            ? new CreationRegionPresenter(editor, region, presentation, name,
                compose == null ? null : VisiblePaneBounds, clipToSource: compose != null)
            : null;

        System.Drawing.Rectangle VisiblePaneBounds()
        {
            var workspace = editor.MdiParent!;
            var origin = workspace.PointToClient(region.PointToScreen(System.Drawing.Point.Empty));
            return new System.Drawing.Rectangle(0, 0,
                Math.Clamp(workspace.ClientSize.Width - origin.X, 0, region.Width),
                Math.Clamp(workspace.ClientSize.Height - origin.Y, 0, region.Height));
        }
        var rows = new Dictionary<Forms.TreeNode, TreeViewItem>();
        var owners = new Dictionary<TreeViewItem, ItemCollection>();
        bool updating = false;
        bool pending = false;
        bool disposed = false;

        // Batch the remove/add sequence used for sorting so selection and expansion can
        // be reconciled against the final set of source nodes.
        void RequestRefresh(object? sender, Forms.InvalidateEventArgs args)
        {
            if (pending || updating || disposed)
                return;
            pending = true;
            Dispatcher.UIThread.Post(() =>
            {
                pending = false;
                if (!disposed)
                    Refresh();
            });
        }

        void Refresh()
        {
            updating = true;
            try
            {
                var previous = tree.SelectedItem;
                var visibleNodes = new HashSet<Forms.TreeNode>();
                Reconcile(tree.Items, source.Nodes, visibleNodes);
                foreach (var stale in rows.Keys.Where(node => !visibleNodes.Contains(node)).ToArray())
                {
                    owners.Remove(rows[stale]);
                    rows.Remove(stale);
                }
                tree.SelectedItem = source.SelectedNode is { } selected && rows.TryGetValue(selected, out var row)
                    ? row : null;
                if (previous != null && tree.SelectedItem == null)
                    refreshDetails?.Invoke();
            }
            finally
            {
                updating = false;
            }
        }

        void Reconcile(ItemCollection items, IEnumerable<Forms.TreeNode> nodes, HashSet<Forms.TreeNode> visibleNodes)
        {
            var ordered = nodes.ToArray();
            var members = ordered.ToHashSet();
            for (int index = items.Count - 1; index >= 0; index--)
            {
                if (items[index] is TreeViewItem { Tag: Forms.TreeNode node } && !members.Contains(node))
                    items.RemoveAt(index);
            }

            for (int index = 0; index < ordered.Length; index++)
            {
                var node = ordered[index];
                visibleNodes.Add(node);
                if (!rows.TryGetValue(node, out var row))
                {
                    row = new TreeViewItem { Tag = node };
                    row.PropertyChanged += (_, args) =>
                    {
                        if (!updating && args.Property == TreeViewItem.IsExpandedProperty)
                            node.Expanded = row.IsExpanded;
                    };
                    if (source.CheckBoxes)
                    {
                        var checkBox = new CheckBox();
                        checkBox.Click += (_, _) => tree.SelectedItem = row;
                        checkBox.IsCheckedChanged += (_, _) =>
                        {
                            if (updating)
                                return;
                            node.Checked = checkBox.IsChecked == true;
                            // A legacy BeforeCheck handler may veto the request.
                            if (checkBox.IsChecked != node.Checked)
                                checkBox.IsChecked = node.Checked;
                        };
                        row.Header = checkBox;
                    }
                    rows.Add(node, row);
                }

                int current = items.IndexOf(row);
                if (current != index)
                {
                    if (owners.TryGetValue(row, out var previousOwner))
                        previousOwner.Remove(row);
                    items.Insert(index, row);
                    owners[row] = items;
                }
                if (row.Header is CheckBox headerCheckBox)
                {
                    headerCheckBox.Content = node.Text;
                    headerCheckBox.IsChecked = node.Checked;
                }
                else
                    row.Header = node.Text;
                var color = foreground?.Invoke(node) ?? node.ForeColor;
                if (color.IsEmpty || color.ToArgb() == System.Drawing.SystemColors.WindowText.ToArgb())
                    row.ClearValue(TreeViewItem.ForegroundProperty);
                else if (color.ToArgb() == System.Drawing.SystemColors.GrayText.ToArgb())
                    row.Foreground = Brushes.Gray;
                else
                    row.Foreground = new SolidColorBrush(Color.FromArgb(color.A, color.R, color.G, color.B));
                ToolTip.SetTip(row, string.IsNullOrEmpty(node.ToolTipText) ? null : node.ToolTipText);
                Reconcile(row.Items, node.Nodes, visibleNodes);
                row.IsExpanded = node.IsExpanded;
            }
        }

        ContextMenu? openContext = null;
        tree.SelectionChanged += (_, _) =>
        {
            openContext?.Close();
            openContext = null;
            if (updating)
                return;
            source.SelectedNode = (tree.SelectedItem as TreeViewItem)?.Tag as Forms.TreeNode;
            if (source.SelectedNode == null)
                refreshDetails?.Invoke();
        };
        tree.KeyDown += (_, args) =>
        {
            if (args.Handled)
                return;
            if (args.Key == Key.Space && tree.SelectedItem is TreeViewItem { Header: CheckBox box } &&
                args.Source is not CheckBox)
            {
                box.IsChecked = box.IsChecked != true;
                args.Handled = true;
            }
            else if (args.Key == Key.Delete && delete != null)
            {
                args.Handled = true;
                delete();
            }
        };
        tree.PointerPressed += (_, args) =>
        {
            if (args.Source is ScrollContentPresenter && args.GetCurrentPoint(tree).Properties.IsLeftButtonPressed &&
                RowAt(tree, args.GetPosition(tree)) is { } row)
                tree.SelectedItem = row;
        };
        tree.DoubleTapped += (_, args) =>
        {
            if (editDetail != null && (FindRow(args.Source) ?? RowAt(tree, args.GetPosition(tree)))
                is { Tag: Forms.TreeNode { Level: > 0 } } row)
            {
                tree.SelectedItem = row;
                editDetail();
            }
        };
        tree.ContextRequested += (_, args) =>
        {
            if (drag?.SuppressContextMenu == true && args.TryGetPosition(tree, out Point _))
            {
                args.Handled = true;
                return;
            }
            bool pointerRequest = args.TryGetPosition(tree, out var point);
            var row = pointerRequest
                ? FindRow(args.Source) ?? RowAt(tree, point)
                : tree.SelectedItem as TreeViewItem;
            if (row?.Tag is not Forms.TreeNode { ContextMenuStrip: { } menu })
                return;
            tree.SelectedItem = row;
            if (!pointerRequest)
                point = row.TranslatePoint(new Point(0, row.Bounds.Height), tree) ?? default;
            args.Handled = true;
            openContext?.Close();
            openContext = Forms.AvaloniaContextCommands.Show(tree, point, menu, region,
                () => !disposed && ReferenceEquals(tree.SelectedItem, row) && ReferenceEquals(source.SelectedNode, row.Tag),
                NativeControlPresentation.AddTheme);
        };

        void SyncVisibility()
        {
            if (layoutHost == null)
                region.Visible = source.Visible;
            else
                tree.IsVisible = source.Visible;
        }

        void FollowVisibility(object? sender, EventArgs args)
        {
            SyncVisibility();
            if (source.Visible)
            {
                source.CreateControl();
                Refresh();
            }
        }

        if (drag != null)
        {
            tree.Tag = drag;
            drag.Wire(tree);
        }
        source.Invalidated += RequestRefresh;
        source.VisibleChanged += FollowVisibility;
        editor.Disposed += (_, _) =>
        {
            disposed = true;
            openContext?.Close();
            drag?.Cancel();
            source.Invalidated -= RequestRefresh;
            source.VisibleChanged -= FollowVisibility;
            source.SizeChanged -= FollowLayout;
            source.EnabledChanged -= FollowEnabled;
            presenter?.Dispose();
            source.Dispose();
        };
        Refresh();
        return tree;
    }

    internal static TreeViewItem? RowAt(TreeView tree, Point position)
    {
        if (!new Rect(tree.Bounds.Size).Contains(position))
            return null;
        var hit = tree.InputHitTest(position);
        if (FindRow(hit) is { } item)
            return item;

        // Avalonia can hit the scroll viewport on whitespace to the right of a
        // caption. Use the arranged header band so the whole row is a drop target.
        if (hit is not ScrollContentPresenter)
            return null;
        foreach (var row in tree.GetVisualDescendants().OfType<TreeViewItem>())
        {
            if (!row.IsEffectivelyVisible)
                continue;
            var header = row.GetVisualDescendants().OfType<Control>()
                .FirstOrDefault(control => control.Name == "PART_Header");
            if (header?.TranslatePoint(default, tree) is { } location &&
                position.Y >= location.Y && position.Y < location.Y + header.Bounds.Height)
                return row;
        }
        return null;
    }

    internal static TreeViewItem? FindRow(object? target) => target is Visual visual
        ? visual.GetSelfAndVisualAncestors().OfType<TreeViewItem>().FirstOrDefault()
        : null;

    internal static System.Drawing.Color QualityForeground(Forms.TreeNode node, Func<string, Quality?> findQuality)
    {
        var quality = node.Level > 0 && node.Tag?.ToString() is { } id ? findQuality(id) : null;
        if (quality != null && DetailChoices.RequiresReview(quality.Name, quality.Extra))
            return System.Drawing.Color.Red;
        return node.ForeColor.ToArgb() == System.Drawing.Color.Red.ToArgb()
            ? System.Drawing.Color.Empty : node.ForeColor;
    }
}
