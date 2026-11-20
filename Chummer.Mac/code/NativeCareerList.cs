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
        string accessibleName, Action activate, Action<int>? sort = null, bool followSourceBounds = false)
    {
        if (Application.Current == null || editor.IsDisposed || source.Parent is not { } parent)
            return;

        var headers = new Grid();
        var list = new ListBox { Name = name };
        AutomationProperties.SetName(list, accessibleName);
        var content = new DockPanel { ClipToBounds = true };
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

        var presenter = new CreationRegionPresenter(editor, placeholder, content, name);
        bool pending = false, updating = false, disposed = false;

        void AddColumns(Grid grid)
        {
            for (int index = 0; index < source.Columns.Count; index++)
            {
                var width = index == source.Columns.Count - 1
                    ? new GridLength(1, GridUnitType.Star)
                    : new GridLength(Math.Max(0, source.Columns[index].Width), GridUnitType.Pixel);
                grid.ColumnDefinitions.Add(new ColumnDefinition(width));
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
            var columns = new Grid();
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

        list.SelectionChanged += (_, _) =>
        {
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
            menu.Show(placeholder, (int)point.X, (int)point.Y);
        };

        source.Invalidated += QueueRefresh;
        editor.Disposed += (_, _) =>
        {
            disposed = true;
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
