#nullable enable
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Interactivity;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Forms = Majorsilence.Forms;

namespace Chummer;

internal static class NativeCareerListChecks
{
    internal static async Task RunAsync(frmMain main, Character fixture, Action<bool, string> check)
    {
        var character = new Character();
        character.LoadSnapshot(fixture.CaptureSnapshot());
        character.Created = true;
        using var editor = new frmCareer(character) { MdiParent = main };
        editor.Show();
        T Field<T>(string name) => (T)typeof(frmCareer).GetField(name,
            BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(editor)!;
        var outer = Field<Forms.TabControl>("tabCharacterTabs");

        outer.SelectedTab = Field<Forms.TabPage>("tabKarma");
        var window = Forms.AvaloniaHostInterop.ToAvaloniaWindow(main);
        var app = Avalonia.Application.Current!;
        var originalTheme = app.RequestedThemeVariant;
        try
        {
            foreach (var theme in new[] { Avalonia.Styling.ThemeVariant.Dark, Avalonia.Styling.ThemeVariant.Light })
            {
                app.RequestedThemeVariant = theme;
                await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                using var capture = new Avalonia.Media.Imaging.RenderTargetBitmap(
                    new Avalonia.PixelSize((int)window.ClientSize.Width, (int)window.ClientSize.Height), new Avalonia.Vector(96, 96));
                capture.Render(window);
                using var encoded = new System.IO.MemoryStream();
                capture.Save(encoded, Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
                encoded.Position = 0;
                using var pixels = SkiaSharp.SKBitmap.Decode(encoded);
                foreach (string name in new[] { "Karma", "Nuyen" })
                {
                    var chart = Field<HistoryChart>("cht" + name);
                    var origin = main.PointToClient(chart.PointToScreen(System.Drawing.Point.Empty));
                    var fill = pixels.GetPixel(origin.X + chart.Width - 5, origin.Y + 5);
                    var host = main.Controls.OfType<Forms.NativeControlHost>().Single(item => item.Name == "Nativecht" + name);
                    var surface = (Border)host.NativeControl!;
                    var mean = (fill.Red + fill.Green + fill.Blue) / 3;
                    check(chart.Visible && chart.Width > 20 && chart.Height > 20 && surface.IsEffectivelyVisible &&
                        surface.Background is Avalonia.Media.ISolidColorBrush &&
                        (theme == Avalonia.Styling.ThemeVariant.Dark ? mean < 128 : mean >= 128),
                        $"{name} remaining chart renders its native background in {theme.Key} mode [pixel={fill}]");
                }
            }
        }
        finally { app.RequestedThemeVariant = originalTheme; }

        foreach (string name in new[] { "Karma", "Nuyen" })
        {
            var host = main.Controls.OfType<Forms.NativeControlHost>()
                .Single(item => item.Name == "Nativecht" + name);
            var chart = (Border)host.NativeControl!;
            var scroll = (ScrollViewer)chart.Child!;
            var content = (Grid)scroll.Content!;
            var heading = content.Children.OfType<TextBlock>().Single();
            var graph = content.Children.OfType<Grid>().Single();
            var axisLabels = graph.Children.OfType<Grid>().Single().Children.OfType<TextBlock>().ToArray();
            double normalFont = heading.FontSize;
            try
            {
                TextElement.SetFontSize(chart, normalFont * 1.5);
                await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                check(chart.IsEffectivelyVisible && heading.FontSize == normalFont * 1.5 &&
                    axisLabels.All(label => label.FontSize == heading.FontSize) &&
                    heading.Bounds.Bottom <= graph.Bounds.Top &&
                    axisLabels[0].Bounds.Bottom <= axisLabels[1].Bounds.Top &&
                    graph.Children.Any(child => child is not Grid && child.Bounds.Width > 8 && child.Bounds.Height > 8),
                    $"{name} chart lays out inherited 150% text above the plot and separates its axis labels");
            }
            finally
            {
                chart.ClearValue(TextElement.FontSizeProperty);
            }
        }

        var split = Field<Forms.SplitContainer>("splitKarmaNuyen");
        int originalHeight = split.Height;
        foreach (int height in new[] { 255, 130, originalHeight })
        {
            split.Height = height;
            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
            foreach (string name in new[] { "Karma", "Nuyen" })
            {
                var chart = Field<HistoryChart>("cht" + name);
                var ledger = Field<Forms.ListView>("lst" + name);
                var pane = name == "Karma" ? split.Panel1 : split.Panel2;
                check(ledger.Height > 0 && ledger.Bottom <= pane.ClientSize.Height &&
                    (chart.Visible ? ledger.Bottom < chart.Top && chart.Bottom <= pane.ClientSize.Height : height == 130),
                    $"{name} expense ledger retains space when its pane is {height} pixels tall");
            }
        }

        var workspaceSize = main.ClientSize;
        try
        {
            main.ClientSize = new System.Drawing.Size(660, 380);
            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
            foreach (var (name, pane) in new[] { ("Karma", split.Panel1), ("Nuyen", split.Panel2) })
            {
                var visible = InventoryTreeLayout.VisiblePageSize(main, pane);
                var ledger = Field<Forms.ListView>("lst" + name);
                var chart = Field<HistoryChart>("cht" + name);
                var host = main.Controls.OfType<Forms.NativeControlHost>().Single(item => item.Name == "NativeCareer" + name);
                var rows = ((DockPanel)host.NativeControl!).Children.OfType<ListBox>().Single();
                check(visible.Width >= 200 && rows.Bounds.Height > 0 &&
                    ledger.Height > 0 && ledger.Right <= visible.Width && ledger.Bottom <= visible.Height &&
                    (!chart.Visible || chart.Right <= visible.Width && chart.Bottom <= visible.Height),
                    $"{name} ledger fits the short and narrow workspace [ledger={ledger.Bounds}, viewport={visible}, rows={rows.Bounds}, chart={chart.Visible}/{chart.Bounds}]");
            }
        }
        finally
        {
            main.ClientSize = workspaceSize;
            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
        }

        foreach (string name in new[] { "Karma", "Nuyen", "Calendar" })
        {
            bool calendar = name == "Calendar";
            outer.SelectedTab = Field<Forms.TabPage>(calendar ? "tabCalendar" : "tabKarma");
            var source = Field<Forms.ListView>("lst" + name);
            var host = main.Controls.OfType<Forms.NativeControlHost>().Single(item => item.Name == "NativeCareer" + name);
            var content = (DockPanel)host.NativeControl!;
            var list = content.Children.OfType<ListBox>().Single();
            if (calendar)
            {
                var changeStart = Field<Forms.Button>("cmdChangeStartWeek");
                string originalText = changeStart.Text;
                try
                {
                    main.ClientSize = new System.Drawing.Size(480, 380);
                    changeStart.Text = string.Join(" ", Enumerable.Repeat("Change the starting week of this character calendar", 6));
                    await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                    var calendarActions = content.Children.OfType<WrapPanel>().Single();
                    var header = content.Children.OfType<Grid>().Single();
                    var visible = InventoryTreeLayout.VisiblePageSize(main, outer.SelectedTab!);
                    check(host.Width <= visible.Width && host.Height <= visible.Height &&
                        calendarActions.Bounds.Bottom <= header.Bounds.Top && header.Bounds.Bottom <= list.Bounds.Top &&
                        list.Bounds.Height > 40 && list.Bounds.Bottom <= content.Bounds.Height,
                        $"Short Calendar wraps its commands above its headers and rows [host={host.Bounds}, actions={calendarActions.Bounds}, header={header.Bounds}, list={list.Bounds}]");
                }
                finally
                {
                    changeStart.Text = originalText;
                    main.ClientSize = workspaceSize;
                    await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                }
            }
            var first = new Forms.ListViewItem("1/1/2070 12:00 AM");
            var second = new Forms.ListViewItem("1/2/2070 12:00 AM");
            source.Items.Add(first);
            source.Items.Add(second);
            if (!calendar) source.Sort();
            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);

            bool ListIsReady() => host.Visible && list.IsEffectivelyVisible && list.Bounds.Height > 0 &&
                list.Items.Cast<Forms.ListViewItem>().SequenceEqual(source.Items.Cast<Forms.ListViewItem>());
            for (int attempt = 0; attempt < 50 && !ListIsReady(); attempt++)
                await Task.Delay(20);
            check(source.Parent == null && ListIsReady(),
                name + $" displays the native list with the editor's order [detached={source.Parent == null}, " +
                $"host={host.Visible}/{host.Bounds}, list={list.IsEffectivelyVisible}/{list.Bounds}, " +
                $"counts={list.Items.Count}/{source.Items.Count}, window={main.ClientSize}]");
            list.SelectedItem = second;
            check(source.SelectedItems.Count == 1 && ReferenceEquals(source.SelectedItems[0], second),
                name + " selection identifies the original editor row");
            var headers = content.Children.OfType<Grid>().Single();
            check(headers.ColumnDefinitions.All(column => column.Width.IsStar && column.ActualWidth > 0) &&
                headers.Children.All(heading => heading.Bounds.Right <= headers.Bounds.Width + 1),
                name + " reserves measured space for every ledger column");
            var actions = content.Children.OfType<WrapPanel>().Single();
            var gained = Field<Forms.Button>(calendar ? "cmdChangeStartWeek" : "cmd" + name + "Gained");
            string caption = gained.Text;
            gained.Text = string.Join(" ", Enumerable.Repeat("Record a new gain", 9));
            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
            check(actions.Children.Count == 3 && actions.Bounds.Bottom <= content.Children.OfType<Grid>().Single().Bounds.Top &&
                list.Bounds.Height > 0,
                name + " ledger wraps its commands above the table headers");
            gained.Text = caption;
            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
            if (!calendar)
            {
                var header = content.Children.OfType<Grid>().Single().Children.OfType<Button>().First();
                var before = source.Items.Cast<Forms.ListViewItem>().ToArray();
                header.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                check(!source.Items.Cast<Forms.ListViewItem>().SequenceEqual(before) &&
                    list.Items.Cast<Forms.ListViewItem>().SequenceEqual(source.Items.Cast<Forms.ListViewItem>()) &&
                    ReferenceEquals(list.SelectedItem, second), name + " header sorting retains the selected row");
            }
            using var context = new Forms.ContextMenuStrip();
            context.Items.Add(new Forms.ToolStripMenuItem("Synthetic list command"));
            bool opened = false, closed = false;
            context.Opened += (_, _) => opened = ReferenceEquals(source.SelectedItems[0], second);
            context.Closed += (_, _) => closed = true;
            var originalMenu = source.ContextMenuStrip;
            source.ContextMenuStrip = context;
            list.RaiseEvent(new ContextRequestedEventArgs { RoutedEvent = InputElement.ContextRequestedEvent });
            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
            check(opened, name + " opens its native context menu for the selected row");
            list.SelectedItem = first;
            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
            check(closed, name + " dismisses its native menu when the selected row changes");
            source.ContextMenuStrip = originalMenu;
            list.SelectedItem = second;

            var overflow = Enumerable.Range(0, 60).Select(index => new Forms.ListViewItem("Extra row " + index)).ToArray();
            foreach (var item in overflow)
                source.Items.Add(item);
            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
            var scroller = list.GetVisualDescendants().OfType<ScrollViewer>().First();
            var cells = list.GetVisualDescendants().OfType<Grid>().First(grid => grid.Name == "NativeLedgerCells" && grid.Bounds.Width > 0);
            check(scroller.Extent.Height > scroller.Viewport.Height &&
                Math.Abs(headers.Bounds.X - cells.TranslatePoint(default, content)!.Value.X) < 1 &&
                Math.Abs(headers.Bounds.Width - cells.Bounds.Width) < 1,
                name + " headers align with the realized columns when the list scrolls");
            foreach (var item in overflow)
                source.Items.Remove(item);

            source.Items.Remove(second);
            source.Items.Remove(first);
            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
            check(list.SelectedItem == null && !list.Items.Contains(second),
                name + " refresh clears a removed row");
        }
        editor.Close();
    }
}
