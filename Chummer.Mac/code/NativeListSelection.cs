#nullable enable
using System;
using System.Collections.Specialized;
using System.Linq;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Forms = Majorsilence.Forms;

namespace Chummer;

internal static class NativeListSelection
{
    internal static ListBox? Attach(Forms.Form owner, Forms.ListBox source, string label, EventHandler? activate = null,
        bool layoutManaged = false)
    {
        if (Application.Current == null)
            return null;

        var list = new ListBox
        {
            Name = "Native" + source.Name,
            FontSize = 12,
            IsTextSearchEnabled = true,
            IsEnabled = source.Enabled,
            IsVisible = source.Visible
        };
        list.Styles.Add(new Style(selector => selector.OfType<ListBoxItem>())
        {
            Setters =
            {
                new Setter(ListBoxItem.MinHeightProperty, 20d),
                new Setter(ListBoxItem.PaddingProperty, new Thickness(5, 2))
            }
        });
        Attach(owner, source, list, source.Items, label, activate, layoutManaged: layoutManaged);
        return list;
    }

    internal static ComboBox? Attach(Forms.Form owner, Forms.ComboBox source, string label, bool fitPopupToItems = false,
        SearchableChoices? search = null, IDataTemplate? itemTemplate = null, Forms.Control? row = null,
        bool layoutManaged = false)
    {
        if (Application.Current == null || (!layoutManaged && source.Parent == null) || source.DropDownStyle == Forms.ComboBoxStyle.Simple)
            return null;

        var combo = new ComboBox
        {
            Name = "Native" + source.Name,
            IsEditable = source.DropDownStyle == Forms.ComboBoxStyle.DropDown,
            FontSize = 12,
            Padding = new Thickness(5, 0),
            MinHeight = 0,
            MinWidth = 0,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch,
            VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center,
            IsTextSearchEnabled = true
        };
        if (itemTemplate != null)
        {
            combo.MaxDropDownHeight = 300;
            combo.ItemTemplate = itemTemplate;
        }
        Popup? popup = null;
        combo.TemplateApplied += (_, args) => popup = args.NameScope.Find<Popup>("PART_Popup");
        combo.DropDownOpened += (_, _) =>
        {
            search?.Refresh();
            if (popup?.Child is not { } content)
                return;

            double width = Math.Max(combo.Bounds.Width, source.DropDownWidth);
            if (fitPopupToItems)
            {
                var measure = new TextBlock { FontSize = combo.FontSize, FontFamily = combo.FontFamily };
                foreach (string item in combo.Items.OfType<string>())
                {
                    measure.Text = item;
                    measure.Measure(Size.Infinity);
                    width = Math.Max(width, measure.DesiredSize.Width + 36);
                }
            }
            if (TopLevel.GetTopLevel(combo) is { ClientSize.Width: > 0 } window)
            {
                double available = Math.Max(combo.Bounds.Width, window.ClientSize.Width - 16);
                content.MinWidth = Math.Min(width, available);
                content.MaxWidth = available;
            }
            else
            {
                content.MinWidth = width;
                content.ClearValue(Control.MaxWidthProperty);
            }
        };
        if (layoutManaged)
        {
            combo.ClearValue(ComboBox.FontSizeProperty);
            combo.ClearValue(ComboBox.PaddingProperty);
            combo.ClearValue(ComboBox.MinHeightProperty);
            combo.ClearValue(ComboBox.MinWidthProperty);
        }
        Attach(owner, source, combo, source.Items, label, preserveItems: itemTemplate != null, row: row, layoutManaged: layoutManaged);
        return combo;
    }

    private static void Attach(Forms.Form owner, Forms.ListControl source, SelectingItemsControl list,
        Forms.ListBox.ObjectCollection items, string label, EventHandler? activate = null, bool preserveItems = false,
        Forms.Control? row = null, bool layoutManaged = false)
    {
        var parent = source.Parent;
        if (!layoutManaged && parent == null)
            return;

        AutomationProperties.SetName(list, label);
        var region = new Border { ClipToBounds = true };
        var editable = list is ComboBox { IsEditable: true } editableCombo ? editableCombo : null;
        if (editable != null)
        {
            region.Styles.Add(new Style(selector => selector.OfType<ComboBox>().Template().OfType<TextBox>())
            {
                Setters =
                {
                    new Setter(TextBox.MinHeightProperty, 0d),
                    new Setter(TextBox.PaddingProperty, new Thickness(4, 0)),
                    new Setter(TextBox.VerticalContentAlignmentProperty, Avalonia.Layout.VerticalAlignment.Center)
                }
            });
        }
        if (list is ComboBox combo)
        {
            region.PropertyChanged += (_, args) =>
            {
                if (args.Property == Visual.IsVisibleProperty && !region.IsVisible)
                    combo.IsDropDownOpen = false;
            };
        }
        NativeControlPresentation.AddTheme(region.Styles);
        bool mdi = !layoutManaged && owner.MdiParent != null;
        Forms.Control? slot = null;
        if (!layoutManaged)
        {
            region.Child = list;
            slot = mdi ? new Forms.Panel() : new Forms.NativeControlHost { NativeControl = region };
            slot.Name = list.Name!;
            slot.Bounds = source.Bounds;
            slot.Anchor = source.Anchor;
            slot.Dock = source.Dock;
            slot.TabIndex = source.TabIndex;
            slot.TabStop = false;
            int index = parent!.Controls.GetChildIndex(source);
            parent.Controls.Remove(source);
            parent.Controls.Add(slot);
            parent.Controls.SetChildIndex(slot, index);
        }
        source.CreateControl();

        CreationRegionPresenter? presenter = null;
        void SyncRowPresenter(object? sender, EventArgs args)
        {
            presenter?.Dispose();
            var form = row == null ? owner : row.FindForm();
            presenter = form?.MdiParent != null
                ? new CreationRegionPresenter(form, slot!, region, slot!.Name, clipToSource: true)
                : null;
        }
        if (mdi)
        {
            SyncRowPresenter(null, EventArgs.Empty);
            if (row != null)
                row.ParentChanged += SyncRowPresenter;
        }
        bool updating = false;
        bool pending = false;
        bool disposed = false;
        bool itemsDirty = true;

        void Refresh()
        {
            if (disposed)
                return;

            pending = false;
            updating = true;
            try
            {
                if (itemsDirty)
                {
                    list.ItemsSource = preserveItems
                        ? items.Cast<object>().ToArray()
                        : items.Cast<object>().Select(source.GetItemText).ToArray();
                    itemsDirty = false;
                }
                int selected = source.SelectedIndex;
                if (editable != null && selected >= 0 && selected < items.Count &&
                    source.Text != source.GetItemText(items[selected]))
                    selected = -1;
                if (list.SelectedIndex != selected)
                {
                    list.SelectedIndex = selected;
                    if (list is ListBox scrollingList && list.SelectedIndex >= 0)
                        scrollingList.ScrollIntoView(list.SelectedIndex);
                }
                if (editable != null && editable.Text != source.Text)
                    editable.Text = source.Text;
                list.IsEnabled = source.Enabled && (slot?.Enabled ?? true);
                if (slot != null)
                    slot.Visible = source.Visible;
                list.IsVisible = source.Visible;
                if (slot is Forms.NativeControlHost host)
                    host.SyncNativeControl();
            }
            finally { updating = false; }
        }

        void QueueRefresh()
        {
            if (pending || disposed)
                return;

            pending = true;
            Dispatcher.UIThread.Post(Refresh, DispatcherPriority.Loaded);
        }

        void ItemsChanged(object? sender, NotifyCollectionChangedEventArgs args)
        {
            itemsDirty = true;
            QueueRefresh();
        }
        void StateChanged(object? sender, EventArgs args) => QueueRefresh();
        void SourceBoundsChanged(object? sender, EventArgs args) => slot!.Bounds = source.Bounds;

        items.CollectionChanged += ItemsChanged;
        if (source is Forms.ListBox listSource)
            listSource.SelectedIndexChanged += StateChanged;
        if (source is Forms.ComboBox comboSource)
            comboSource.SelectedIndexChanged += StateChanged;
        source.DisplayMemberChanged += DisplayMemberChanged;
        void DisplayMemberChanged(object? sender, EventArgs args)
        {
            itemsDirty = true;
            QueueRefresh();
        }
        source.TextChanged += StateChanged;
        source.EnabledChanged += StateChanged;
        source.VisibleChanged += StateChanged;
        if (slot != null)
        {
            source.LocationChanged += SourceBoundsChanged;
            source.SizeChanged += SourceBoundsChanged;
            slot.EnabledChanged += StateChanged;
            slot.VisibleChanged += StateChanged;
            slot.LocationChanged += StateChanged;
            slot.SizeChanged += StateChanged;
        }
        list.SelectionChanged += (_, _) =>
        {
            if (!updating && !itemsDirty && !disposed && (editable == null || list.SelectedIndex >= 0))
                source.SelectedIndex = list.SelectedIndex;
        };
        if (editable != null)
        {
            editable.PropertyChanged += (_, args) =>
            {
                if (args.Property != ComboBox.TextProperty || updating || disposed || editable.Text == source.Text)
                    return;

                string text = editable.Text ?? "";
                var comboSource = (Forms.ComboBox)source;
                int match = comboSource.FindStringExact(text);
                if (match >= 0 && match != comboSource.SelectedIndex)
                    comboSource.SelectedIndex = match;
                else
                    comboSource.Text = text;
            };
        }
        if (activate != null)
        {
            list.DoubleTapped += (_, args) =>
            {
                if (!pending && list.SelectedIndex >= 0 && args.Source is Visual visual &&
                    (visual is ListBoxItem || visual.FindAncestorOfType<ListBoxItem>() != null))
                    activate(source, EventArgs.Empty);
            };
        }

        void DisposeBinding(object? sender, EventArgs args)
        {
            if (mdi && row != null)
                row.ParentChanged -= SyncRowPresenter;
            disposed = true;
            items.CollectionChanged -= ItemsChanged;
            if (source is Forms.ListBox listSource)
                listSource.SelectedIndexChanged -= StateChanged;
            if (source is Forms.ComboBox comboSource)
                comboSource.SelectedIndexChanged -= StateChanged;
            source.DisplayMemberChanged -= DisplayMemberChanged;
            source.TextChanged -= StateChanged;
            source.EnabledChanged -= StateChanged;
            source.VisibleChanged -= StateChanged;
            source.LocationChanged -= SourceBoundsChanged;
            source.SizeChanged -= SourceBoundsChanged;
            if (slot != null)
            {
                slot.EnabledChanged -= StateChanged;
                slot.VisibleChanged -= StateChanged;
                slot.LocationChanged -= StateChanged;
                slot.SizeChanged -= StateChanged;
            }
            presenter?.Dispose();
            if (slot is Forms.NativeControlHost host)
                host.NativeControl = null;
            if (!layoutManaged)
                source.Dispose();
        }
        if (row != null)
            row.Disposed += DisposeBinding;
        else
            owner.Disposed += DisposeBinding;
        Refresh();
    }
}
