#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Interactivity;
using Forms = Majorsilence.Forms;

namespace Chummer;

// A bounded native section. Detached legacy inputs remain the controller adapter
// during this migration; they are never drawn and retain the existing rule events.
internal sealed class CreationInputSection : IDisposable
{
    private bool disposed;
    private readonly List<Action> cleanup = new();
    private readonly List<Forms.Control> detached = new();
    internal Dictionary<string, NumericUpDown> Inputs { get; } = new();
    private static TextBlock Caption() => new() { VerticalAlignment = VerticalAlignment.Center, FontSize = 12,
        TextWrapping = Avalonia.Media.TextWrapping.Wrap };
    internal CreationInputSection(Forms.Form? editor, Forms.Panel attributes, Forms.Control moneyParent,
        IEnumerable<(Forms.NumericUpDown Input, Forms.Label Label, Forms.Label Metatype, Forms.Label Augmented)> rows,
        Forms.NumericUpDown money, Forms.Label moneyLabel, Forms.Label moneyTotal, Forms.ToolTip tips,
        (Forms.Label Base, Forms.Label Augmented, Forms.Label Metatype)? headers = null)
    {
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,74,48,76"), RowSpacing = 2 };
        grid.Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());
        int row = 0;
        if (headers is { } heading)
        {
            // Header and data cells share framework columns, including scrollbar
            // space and translated text wrapping. Legacy headings are detached.
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            Add(grid, Mirror(heading.Base), row, 1);
            Add(grid, Mirror(heading.Augmented), row, 2);
            Add(grid, Mirror(heading.Metatype), row++, 3);
        }
        foreach (var item in rows)
        {
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            var label = Mirror(item.Label); var metatype = Mirror(item.Metatype); var augmented = Mirror(item.Augmented);
            var input = Bind(item.Input, tips, wholeNumbers: true);
            Add(grid, label, row, 0); Add(grid, input, row, 1); Add(grid, augmented, row, 2); Add(grid, metatype, row++, 3);
        }
        var scroller = new ScrollViewer { Content = grid, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled };
        var attributeHost = new Forms.NativeControlHost { Name = "nativeCreationAttributes", NativeControl = scroller };
        var moneyGrid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,74,*"), ColumnSpacing = 4 };
        moneyGrid.Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());
        int left = moneyLabel.Left, top = Math.Min(moneyLabel.Top, money.Top);
        Add(moneyGrid, Mirror(moneyLabel), 0, 0); Add(moneyGrid, Bind(money, tips), 0, 1); Add(moneyGrid, Mirror(moneyTotal), 0, 2);
        var moneyHost = new Forms.NativeControlHost { Name = "nativeCreationMoney", NativeControl = moneyGrid };
        // Only these two bounded creation regions live on the actual presenting
        // form. The shared host is unchanged; document state remains editor-owned.
        if (editor == null) return; // Headless lifecycle checks need no presentation.
        var presenter = editor.MdiParent ?? editor;
        var root = presenter.Controls[0].Parent!;
        presenter.Controls.Add(attributeHost); presenter.Controls.Add(moneyHost);
        void SyncPresentation()
        {
            if (disposed) return;
            bool active = editor.Visible && (editor.MdiParent == null || editor.MdiParent.ActiveMdiChild == editor);
            var point = root.PointToClient(attributes.PointToScreen(System.Drawing.Point.Empty));
            attributeHost.Bounds = new System.Drawing.Rectangle(point, attributes.Size);
            attributeHost.Visible = active && attributes.Visible;
            point = root.PointToClient(moneyParent.PointToScreen(new System.Drawing.Point(left, top)));
            moneyHost.Bounds = new System.Drawing.Rectangle(point, new System.Drawing.Size(Math.Max(200, moneyParent.ClientSize.Width - left - 6), 34));
            moneyHost.Visible = active && moneyParent.Visible;
            attributeHost.SyncNativeControl(); moneyHost.SyncNativeControl();
        }
        EventHandler refresh = (_, _) => SyncPresentation();
        editor.Shown += refresh; presenter.MdiChildActivate += refresh;
        presenter.SizeChanged += refresh; presenter.LocationChanged += refresh;
        var ancestors = new List<Forms.Control>();
        for (Forms.Control? control = attributes; control != null; control = control.Parent) ancestors.Add(control);
        for (Forms.Control? control = moneyParent; control != null; control = control.Parent)
            if (!ancestors.Contains(control)) ancestors.Add(control);
        foreach (var control in ancestors) { control.VisibleChanged += refresh; control.SizeChanged += refresh; control.LocationChanged += refresh; }
        cleanup.Add(() => {
            editor.Shown -= refresh; presenter.MdiChildActivate -= refresh;
            presenter.SizeChanged -= refresh; presenter.LocationChanged -= refresh;
            foreach (var control in ancestors) { control.VisibleChanged -= refresh; control.SizeChanged -= refresh; control.LocationChanged -= refresh; }
            attributeHost.NativeControl = null; moneyHost.NativeControl = null;
            presenter.Controls.Remove(attributeHost); presenter.Controls.Remove(moneyHost);
            attributeHost.Dispose(); moneyHost.Dispose();
        });
        SyncPresentation();
    }
    private static void Add(Grid grid, Control control, int row, int column)
    { Grid.SetRow(control, row); Grid.SetColumn(control, column); grid.Children.Add(control); }
    private void Detach(Forms.Control control)
    { control.Parent?.Controls.Remove(control); detached.Add(control); }
    private TextBlock Mirror(Forms.Label source)
    {
        var target = Caption();
        void Sync() { if (disposed) return; target.Text = source.Text; target.IsEnabled = source.Enabled; target.IsVisible = source.Visible; }
        EventHandler changed = (_, _) => Sync();
        source.TextChanged += changed; source.EnabledChanged += changed; source.VisibleChanged += changed;
        cleanup.Add(() => { source.TextChanged -= changed; source.EnabledChanged -= changed; source.VisibleChanged -= changed; });
        Detach(source); Sync(); return target;
    }
    private NumericUpDown Bind(Forms.NumericUpDown source, Forms.ToolTip tips, bool wholeNumbers = false)
    {
        var input = new NumericUpDown { Name = "native" + source.Name, Width = 74, MinHeight = 24, FontSize = 12,
            Padding = new Thickness(2), ClipValueToMinMax = true, ParsingNumberStyle = wholeNumbers ? NumberStyles.Integer : NumberStyles.Number };
        Inputs.Add(source.Name, input);
        bool syncing = false;
        void Sync()
        {
            if (disposed || syncing) return;
            syncing = true;
            try
            {
                // Expand before narrowing so neither endpoint temporarily clamps a
                // model value during metatype/Magic/Resonance refresh.
                input.Maximum = Math.Max(input.Maximum, source.Maximum);
                input.Minimum = source.Minimum; input.Maximum = source.Maximum;
                input.Increment = source.Increment; input.FormatString = source.DecimalPlaces == 0 ? "0" : "F" + source.DecimalPlaces;
                input.Value = source.Value; input.IsEnabled = source.Enabled; input.IsVisible = source.Visible;
                ToolTip.SetTip(input, tips.GetToolTip(source));
            }
            finally { syncing = false; }
        }
        EventHandler changed = (_, _) => Sync();
        Forms.InvalidateEventHandler invalidated = (_, _) => Sync();
        source.ValueChanged += changed; source.EnabledChanged += changed; source.VisibleChanged += changed; source.Invalidated += invalidated;
        void NativeChanged(object? sender, NumericUpDownValueChangedEventArgs args)
        {
            if (disposed || syncing) return;
            // A queued edit can arrive after a quality/metatype refresh disabled
            // this input. Controller-originated refreshes still flow through Sync.
            if (!source.Enabled) { Sync(); return; }
            if (input.Value is not decimal value) return;
            // Attributes have an integer domain even when a queued/programmatic
            // change bypasses text parsing. Money deliberately retains decimals.
            if (wholeNumbers && decimal.Truncate(value) != value) { Sync(); return; }
            syncing = true;
            try { source.Value = Math.Clamp(value, source.Minimum, source.Maximum); }
            finally { syncing = false; Sync(); }
        }
        input.ValueChanged += NativeChanged;
        // An incomplete inline edit does not commit a fallback. Leaving it restores
        // the last controller-approved value, as the legacy inline field did.
        void NativeLostFocus(object? sender, RoutedEventArgs args) { if (!disposed && !input.Value.HasValue) Sync(); }
        input.LostFocus += NativeLostFocus;
        cleanup.Add(() => { input.ValueChanged -= NativeChanged; input.LostFocus -= NativeLostFocus;
            source.ValueChanged -= changed; source.EnabledChanged -= changed; source.VisibleChanged -= changed; source.Invalidated -= invalidated; });
        Detach(source); Sync(); return input;
    }
    public void Dispose()
    { if (disposed) return; disposed = true; foreach (var action in cleanup) action(); cleanup.Clear(); foreach (var control in detached) control.Dispose(); detached.Clear(); }
}
