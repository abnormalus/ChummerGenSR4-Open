#nullable enable
using System;
using System.Linq;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Styling;
using Avalonia.Threading;
using Forms = Majorsilence.Forms;

namespace Chummer;

internal static class NativeEditorTabStrip
{
    internal const int TabHeight = 32;
    private const int ScrollbarHeight = 16;
    internal const int HeaderHeight = TabHeight + ScrollbarHeight;

    internal static void Attach(Forms.Form editor, Forms.TabControl source, string label, Control? action = null)
    {
        if (Application.Current == null)
            return;

        string name = "Native" + source.Name;
        var tabs = new TabControl { Name = name, Padding = new Thickness(0) };
        AutomationProperties.SetName(tabs, label);
        var view = CreateViewport(tabs);
        Control header = view;
        if (action != null)
        {
            var row = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), Children = { view, action } };
            NativeControlPresentation.AddTheme(row.Styles);
            Grid.SetColumn(action, 1);
            header = row;
        }
        bool mdi = editor.MdiParent != null;
        Forms.Control slot = mdi ? new Forms.Panel() : new Forms.NativeControlHost { NativeControl = header };
        slot.Name = name;
        slot.Height = HeaderHeight;
        slot.TabStop = false;
        source.InstallHeaderPresentation(slot);

        var setup = new TabSubscriptions(source, tabs);
        var presenter = mdi ? new CreationRegionPresenter(editor, slot, header, name, clipToSource: true) : null;
        setup.KeepUntilDisposed(editor, presenter, slot);
        TrackTypography(editor, tabs, slot, action);
    }

    internal static ScrollViewer CreateViewport(TabControl tabs)
    {
        var view = new ScrollViewer
        {
            Content = tabs,
            AllowAutoHide = false,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled
        };
        NativeControlPresentation.AddTheme(view.Styles);
        view.Styles.Add(new Style(selector => selector.OfType<ScrollBar>()
            .PropertyEquals(ScrollBar.OrientationProperty, Orientation.Horizontal))
        {
            Setters = { new Setter(ScrollBar.HeightProperty, (double)ScrollbarHeight) }
        });
        view.Styles.Add(new Style(selector => selector.OfType<TabItem>())
        {
            Setters =
            {
                new Setter(TabItem.MinHeightProperty, (double)TabHeight),
                new Setter(TabItem.PaddingProperty, new Thickness(8, 4))
            }
        });
        return view;
    }

    internal static void BindHeaderTypography(TabControl tabs, TabItem header)
    {
        header.Bind(TabItem.FontSizeProperty, tabs.GetObservable(TabControl.FontSizeProperty));
        header.Bind(TabItem.FontFamilyProperty, tabs.GetObservable(TabControl.FontFamilyProperty));
    }

    internal static void TrackTypography(Forms.Form owner, TabControl tabs, Forms.Control slot, Control? action = null)
    {
        void Measure()
        {
            if (owner.IsDisposed)
                return;

            var sample = new TextBlock { Text = "Mg", FontSize = tabs.FontSize, FontFamily = tabs.FontFamily };
            sample.Measure(Size.Infinity);
            int tabHeight = Math.Max(TabHeight, (int)Math.Ceiling(sample.DesiredSize.Height) + 8) + ScrollbarHeight;
            action?.Measure(Size.Infinity);
            slot.Height = Math.Max(tabHeight, (int)Math.Ceiling(action?.DesiredSize.Height ?? 0));
        }

        void TypographyChanged(object? sender, AvaloniaPropertyChangedEventArgs args)
        {
            if (args.Property == TabControl.FontSizeProperty || args.Property == TabControl.FontFamilyProperty)
                Measure();
        }

        tabs.PropertyChanged += TypographyChanged;
        void ActionMeasured(object? sender, SizeChangedEventArgs args) => Measure();
        if (action != null)
            action.SizeChanged += ActionMeasured;
        owner.Disposed += (_, _) =>
        {
            tabs.PropertyChanged -= TypographyChanged;
            if (action != null)
                action.SizeChanged -= ActionMeasured;
        };
        Measure();
    }

    private sealed class TabSubscriptions : IDisposable
    {
        private readonly Forms.TabControl source;
        private readonly TabControl tabs;
        private Forms.TabPage[] pages = [];
        private bool syncing, pending, disposed;

        internal TabSubscriptions(Forms.TabControl source, TabControl tabs)
        {
            this.source = source;
            this.tabs = tabs;
            source.ControlAdded += PagesChanged;
            source.ControlRemoved += PagesChanged;
            source.SelectedIndexChanged += SelectionChanged;
            tabs.SelectionChanged += NativeSelected;
            Refresh();
        }

        internal void KeepUntilDisposed(Forms.Form editor, CreationRegionPresenter? presenter, Forms.Control slot)
        {
            editor.Disposed += (_, _) =>
            {
                Dispose();
                presenter?.Dispose();
                if (slot is Forms.NativeControlHost host)
                    host.NativeControl = null;
            };
        }

        private void PagesChanged(object? sender, Forms.ControlEventArgs args)
        {
            if (args.Control is Forms.TabPage)
                QueueRefresh();
        }

        private void SelectionChanged(object? sender, EventArgs args) => QueueRefresh();

        private void QueueRefresh()
        {
            if (pending || disposed)
                return;

            pending = true;
            Dispatcher.UIThread.Post(Refresh, DispatcherPriority.Loaded);
        }

        private void NativeSelected(object? sender, SelectionChangedEventArgs args)
        {
            if (!syncing && !disposed && tabs.SelectedItem is TabItem { Tag: Forms.TabPage page } &&
                source.TabPages.Contains(page))
                source.SelectedTab = page;
            QueueRefresh();
        }

        private void Refresh()
        {
            pending = false;
            if (disposed)
                return;

            syncing = true;
            try
            {
                var current = source.TabPages.Cast<Forms.TabPage>().ToArray();
                if (!current.SequenceEqual(pages))
                {
                    foreach (var page in pages)
                        page.TextChanged -= UpdateTitle;
                    tabs.Items.Clear();
                    pages = current;
                    foreach (var page in pages)
                    {
                        var header = new TabItem
                        {
                            Header = page.Text,
                            Tag = page,
                            Name = "Native" + page.Name
                        };
                        BindHeaderTypography(tabs, header);
                        tabs.Items.Add(header);
                        page.TextChanged += UpdateTitle;
                    }
                }
                var selected = tabs.Items.OfType<TabItem>().FirstOrDefault(item => ReferenceEquals(item.Tag, source.SelectedTab));
                tabs.SelectedItem = selected;
                if (selected != null)
                    Dispatcher.UIThread.Post(() =>
                    {
                        if (!disposed && tabs.SelectedItem == selected)
                            selected.BringIntoView();
                    }, DispatcherPriority.Loaded);
            }
            finally { syncing = false; }
        }

        private void UpdateTitle(object? sender, EventArgs args)
        {
            if (sender is Forms.TabPage page && tabs.Items.OfType<TabItem>().FirstOrDefault(item => ReferenceEquals(item.Tag, page)) is { } item)
                item.Header = page.Text;
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            source.ControlAdded -= PagesChanged;
            source.ControlRemoved -= PagesChanged;
            source.SelectedIndexChanged -= SelectionChanged;
            tabs.SelectionChanged -= NativeSelected;
            foreach (var page in pages)
                page.TextChanged -= UpdateTitle;
            pages = [];
            tabs.Items.Clear();
        }
    }
}
