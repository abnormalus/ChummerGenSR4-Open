#nullable enable
using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
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
    private static TextBlock Caption()
    {
        var caption = new TextBlock { VerticalAlignment = VerticalAlignment.Center, FontSize = 12,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap };
        caption.Bind(TextBlock.ForegroundProperty, new DynamicResourceExtension("SystemControlForegroundBaseHighBrush"));
        return caption;
    }
    internal CreationInputSection(Forms.Form? editor, Forms.Panel attributes, Forms.Control moneyParent,
        IEnumerable<(Forms.NumericUpDown Input, Forms.Label Label, Forms.Label Metatype, Forms.Label Augmented)> rows,
        Forms.NumericUpDown money, Forms.Label moneyLabel, Forms.Label moneyTotal, Forms.ToolTip tips,
        (Forms.Label Base, Forms.Label Augmented, Forms.Label Metatype)? headers = null)
    {
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,104,36,76"), RowSpacing = 1 };
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
            input.Width = 104; input.MinHeight = 26; input.Height = 26; input.Padding = new Thickness(2, 0);
            Add(grid, label, row, 0); Add(grid, input, row, 1); Add(grid, augmented, row, 2); Add(grid, metatype, row++, 3);
        }
        grid.Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());
        var moneyGrid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,120,*"), ColumnSpacing = 4 };
        moneyGrid.Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());
        Add(moneyGrid, Mirror(moneyLabel), 0, 0); Add(moneyGrid, Bind(money, tips), 0, 1); Add(moneyGrid, Mirror(moneyTotal), 0, 2);
        if (editor == null) return; // Headless binding checks need no presenter.
        var attributesPresenter = new CreationRegionPresenter(editor, attributes, grid, "nativeCreationAttributes");
        var moneyPresenter = new CreationRegionPresenter(editor, moneyParent, moneyGrid, "nativeCreationMoney",
            () => new System.Drawing.Rectangle(moneyLabel.Left, moneyLabel.Top, Math.Max(200, moneyParent.ClientSize.Width - moneyLabel.Left - 6), 34));
        cleanup.Add(attributesPresenter.Dispose); cleanup.Add(moneyPresenter.Dispose);
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
        var binding = new NativeInlineNumericBinding(source, tips, wholeNumbers);
        Inputs.Add(source.Name, binding.Input); cleanup.Add(binding.Dispose);
        Detach(source); binding.Refresh(); return binding.Input;
    }
    public void Dispose()
    { if (disposed) return; disposed = true; foreach (var action in cleanup) action(); cleanup.Clear(); foreach (var control in detached) control.Dispose(); detached.Clear(); }
}
