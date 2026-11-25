#nullable enable
using System;
using System.Linq;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Forms = Majorsilence.Forms;

namespace Chummer;

internal static class NativeCareerList
{
    internal static void Attach(frmCareer editor, Forms.ListView source, string name,
        string accessibleName, Action activate, Action<int>? sort = null, bool followSourceBounds = false, params Forms.Button[] commands)
    {
        if (Application.Current == null || editor.IsDisposed || source.Parent is not { } parent)
            return;

        var headers = new Grid();
        var list = new ListBox { Name = name };
        ScrollViewer.SetHorizontalScrollBarVisibility(list, Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled);
        AutomationProperties.SetName(list, accessibleName);
        var content = new DockPanel { ClipToBounds = true };
        if (commands.Length > 0)
        {
            var actions = new WrapPanel { Name = "NativeLedgerActions", ItemSpacing = 5, LineSpacing = 3 };
            DockPanel.SetDock(actions, Dock.Top);
            content.Children.Add(actions);
            int bottom = source.Bottom;
            source.Top = commands[0].Top;
            source.Height = Math.Max(0, bottom - source.Top);
            foreach (var command in commands)
            {
                parent.Controls.Remove(command);
                var caption = new TextBlock { TextWrapping = TextWrapping.Wrap, MaxLines = 3, TextTrimming = TextTrimming.CharacterEllipsis };
                var button = new Button { Name = "Native" + command.Name, Content = caption };
                void RefreshCommand()
                {
                    caption.Text = command.Text;
                    ToolTip.SetTip(button, command.Text);
                    button.IsEnabled = command.Enabled;
                    button.IsVisible = command.Visible;
                    AutomationProperties.SetName(button, command.Text);
                }
                var observer = new NativeControlObservation(command, RefreshCommand);
                void CaptionChanged(object? sender, EventArgs args) => RefreshCommand();
                command.TextChanged += CaptionChanged;
                button.Click += (_, _) => command.PerformClick();
                actions.Children.Add(button);
                RefreshCommand();
                editor.Disposed += (_, _) =>
                {
                    command.TextChanged -= CaptionChanged;
                    observer.Dispose();
                    command.Dispose();
                };
            }
            NativeControlPresentation.ConstrainWrapItems(actions);
        }
        DockPanel.SetDock(headers, Dock.Top);
        content.Children.Add(headers);
        content.Children.Add(list);
        NativeControlPresentation.AddTheme(content.Styles);
        list.Styles.Add(new Style(selector => selector.OfType<ListBoxItem>())
        {
            Setters = { new Setter(ListBoxItem.HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch) }
        });
        var placeholder = new Forms.Panel
        {
            Bounds = source.Bounds,
            Anchor = followSourceBounds ? Forms.AnchorStyles.Top | Forms.AnchorStyles.Left : source.Anchor,
            TabStop = false
        };
        parent.Controls.Add(placeholder);
        parent.Controls.Remove(source);
        source.CreateControl();

        void FollowLayout(object? sender, EventArgs args) => placeholder.Bounds = source.Bounds;
        if (followSourceBounds)
            source.SizeChanged += FollowLayout;

        var workspace = editor.MdiParent ?? editor;
        var presenter = new CreationRegionPresenter(editor, placeholder, content, name,
            () => new System.Drawing.Rectangle(System.Drawing.Point.Empty, InventoryTreeLayout.VisiblePageSize(workspace, placeholder)),
            clipToSource: true);
        bool pending = false, updating = false, disposed = false;

        void AddColumns(Grid grid)
        {
            for (int index = 0; index < source.Columns.Count; index++)
            {
                // Keep date and amount readable while reserving space for the reason or notes.
                double weight = source.Columns.Count == 2
                    ? (index == 0 ? 1 : 3)
                    : index switch { 0 => 2, 1 => 1, _ => 3 };
                grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(weight, GridUnitType.Star)));
            }
        }

        AddColumns(headers);
        for (int index = 0; index < source.Columns.Count; index++)
        {
            var header = source.Columns[index];
            int column = index;
            var text = new TextBlock { Text = header.Text, TextTrimming = TextTrimming.CharacterEllipsis };
            Control heading = text;
            if (sort != null)
            {
                var button = new Button { Content = text, HorizontalAlignment = HorizontalAlignment.Stretch };
                button.Click += (_, _) =>
                {
                    var selected = list.SelectedItem as Forms.ListViewItem;
                    sort(column);
                    if (selected != null && source.Items.Contains(selected))
                        selected.Selected = true;
                };
                heading = button;
            }
            else
                text.Margin = new Thickness(8, 4);
            Grid.SetColumn(heading, column);
            headers.Children.Add(heading);
        }

        list.ItemTemplate = new FuncDataTemplate<Forms.ListViewItem>((item, _) =>
        {
            var columns = new Grid { Name = "NativeLedgerCells" };
            AddColumns(columns);
            if (item == null)
                return columns;
            for (int index = 0; index < source.Columns.Count && index < item.SubItems.Count; index++)
            {
                string value = item.SubItems[index].Text;
                var text = new TextBlock { Text = value, TextTrimming = TextTrimming.CharacterEllipsis };
                ToolTip.SetTip(text, value);
                Grid.SetColumn(text, index);
                columns.Children.Add(text);
            }
            return columns;
        });

        // The realized row reflects both item padding and the scrollbar gutter.
        // Use that same horizontal inset for the independently docked header.
        void AlignHeader(object? sender, EventArgs args)
        {
            var row = list.GetVisualDescendants().OfType<Grid>()
                .FirstOrDefault(grid => grid.Name == "NativeLedgerCells" && grid.Bounds.Width > 0);
            var inset = default(Thickness);
            if (row?.TranslatePoint(default, content) is { } origin)
                inset = new Thickness(Math.Max(0, origin.X), 0,
                    Math.Max(0, content.Bounds.Width - origin.X - row.Bounds.Width), 0);
            if (headers.Margin != inset)
                headers.Margin = inset;
        }
        list.LayoutUpdated += AlignHeader;

        void Refresh()
        {
            updating = true;
            try
            {
                var items = source.Items.Cast<Forms.ListViewItem>().ToArray();
                var selected = source.SelectedItems.Cast<Forms.ListViewItem>().FirstOrDefault();
                if (list.ItemsSource is not Forms.ListViewItem[] previous || !previous.SequenceEqual(items))
                    list.ItemsSource = items;
                list.SelectedItem = selected;
            }
            finally { updating = false; }
        }

        void QueueRefresh(object? sender, Forms.InvalidateEventArgs args)
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

        ContextMenu? openContext = null;
        list.SelectionChanged += (_, _) =>
        {
            openContext?.Close();
            openContext = null;
            if (updating)
                return;
            foreach (var previous in source.SelectedItems.Cast<Forms.ListViewItem>().ToArray())
                if (!ReferenceEquals(previous, list.SelectedItem))
                    previous.Selected = false;
            if (list.SelectedItem is Forms.ListViewItem selected)
                selected.Selected = true;
        };

        list.DoubleTapped += (_, args) =>
        {
            if (FindRow(args.Source) is { } row)
            {
                list.SelectedItem = row.Content;
                activate();
            }
        };
        list.KeyDown += (_, args) =>
        {
            if (args.Key != Key.Enter || list.SelectedItem == null)
                return;
            args.Handled = true;
            activate();
        };
        list.ContextRequested += (_, args) =>
        {
            if (source.ContextMenuStrip is not { } menu)
                return;
            bool pointerRequest = args.TryGetPosition(content, out var point);
            if (pointerRequest)
            {
                if (FindRow(args.Source) is not { } row)
                    return;
                list.SelectedItem = row.Content;
            }
            if (list.SelectedItem == null)
                return;
            if (!pointerRequest)
                point = list.TranslatePoint(new Point(0, 0), content) ?? default;
            args.Handled = true;
            var selected = list.SelectedItem;
            openContext?.Close();
            openContext = Forms.AvaloniaContextCommands.Show(content, point, menu, placeholder,
                () => !disposed && ReferenceEquals(list.SelectedItem, selected) &&
                    selected is Forms.ListViewItem item && item.Selected && source.Items.Contains(item),
                NativeControlPresentation.AddTheme);
        };

        source.Invalidated += QueueRefresh;
        editor.Disposed += (_, _) =>
        {
            disposed = true;
            openContext?.Close();
            list.LayoutUpdated -= AlignHeader;
            source.Invalidated -= QueueRefresh;
            source.SizeChanged -= FollowLayout;
            presenter.Dispose();
            source.Dispose();
        };
        Refresh();

        static ListBoxItem? FindRow(object? target) => target is Visual visual
            ? visual.GetSelfAndVisualAncestors().OfType<ListBoxItem>().FirstOrDefault()
            : null;
    }
}
