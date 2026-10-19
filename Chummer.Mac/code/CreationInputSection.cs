#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Forms = Majorsilence.Forms;

namespace Chummer;

// A bounded native section. Detached legacy inputs remain the controller adapter
// during this migration; they are never drawn and retain the existing rule events.
internal sealed class CreationInputSection : IDisposable
{
    private readonly List<Action> cleanup = new();
    private readonly List<Forms.Control> detached = new();
    internal Dictionary<string, NumericUpDown> Inputs { get; } = new();
    private static TextBlock Caption() => new() { VerticalAlignment = VerticalAlignment.Center, FontSize = 12 };
    internal CreationInputSection(Forms.Panel attributes, Forms.Control moneyParent,
        IEnumerable<(Forms.NumericUpDown Input, Forms.Label Label, Forms.Label Metatype, Forms.Label Augmented)> rows,
        Forms.NumericUpDown money, Forms.Label moneyLabel, Forms.Label moneyTotal, Forms.ToolTip tips)
    {
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,74,76,48"), RowSpacing = 2 };
        grid.Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());
        int row = 0;
        foreach (var item in rows)
        {
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            var label = Mirror(item.Label); var metatype = Mirror(item.Metatype); var augmented = Mirror(item.Augmented);
            var input = Bind(item.Input, tips);
            Add(grid, label, row, 0); Add(grid, input, row, 1); Add(grid, metatype, row, 2); Add(grid, augmented, row++, 3);
        }
        var scroller = new ScrollViewer { Content = grid, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled };
        attributes.Controls.Add(new Forms.NativeControlHost { Name = "nativeCreationAttributes", Dock = Forms.DockStyle.Fill, NativeControl = scroller });
        var moneyGrid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,74,*"), ColumnSpacing = 4 };
        moneyGrid.Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());
        int left = moneyLabel.Left, top = Math.Min(moneyLabel.Top, money.Top);
        Add(moneyGrid, Mirror(moneyLabel), 0, 0); Add(moneyGrid, Bind(money, tips), 0, 1); Add(moneyGrid, Mirror(moneyTotal), 0, 2);
        moneyParent.Controls.Add(new Forms.NativeControlHost { Name = "nativeCreationMoney", Left = left, Top = top,
            Width = Math.Max(200, moneyParent.ClientSize.Width - left - 6), Height = 34, NativeControl = moneyGrid });
    }
    private static void Add(Grid grid, Control control, int row, int column)
    { Grid.SetRow(control, row); Grid.SetColumn(control, column); grid.Children.Add(control); }
    private void Detach(Forms.Control control)
    { control.Parent?.Controls.Remove(control); detached.Add(control); }
    private TextBlock Mirror(Forms.Label source)
    {
        var target = Caption();
        void Sync() { target.Text = source.Text; target.IsEnabled = source.Enabled; target.IsVisible = source.Visible; }
        EventHandler changed = (_, _) => Sync();
        source.TextChanged += changed; source.EnabledChanged += changed; source.VisibleChanged += changed;
        cleanup.Add(() => { source.TextChanged -= changed; source.EnabledChanged -= changed; source.VisibleChanged -= changed; });
        Detach(source); Sync(); return target;
    }
    private NumericUpDown Bind(Forms.NumericUpDown source, Forms.ToolTip tips)
    {
        var input = new NumericUpDown { Name = "native" + source.Name, Width = 74, MinHeight = 24, FontSize = 12,
            Padding = new Thickness(2), ClipValueToMinMax = true, ParsingNumberStyle = NumberStyles.Number };
        Inputs.Add(source.Name, input);
        bool syncing = false;
        void Sync()
        {
            if (syncing) return;
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
        input.ValueChanged += (_, _) =>
        {
            if (syncing || input.Value is not decimal value) return;
            syncing = true;
            try { source.Value = Math.Clamp(value, source.Minimum, source.Maximum); }
            finally { syncing = false; Sync(); }
        };
        // An incomplete inline edit does not commit a fallback. Leaving it restores
        // the last controller-approved value, as the legacy inline field did.
        input.LostFocus += (_, _) => { if (!input.Value.HasValue) Sync(); };
        cleanup.Add(() => { source.ValueChanged -= changed; source.EnabledChanged -= changed; source.VisibleChanged -= changed; source.Invalidated -= invalidated; });
        Detach(source); Sync(); return input;
    }
    public void Dispose()
    { foreach (var action in cleanup) action(); cleanup.Clear(); foreach (var control in detached) control.Dispose(); detached.Clear(); }
}
