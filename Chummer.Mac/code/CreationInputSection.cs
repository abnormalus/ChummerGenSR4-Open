#nullable enable
using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Styling;
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
        var caption = new TextBlock { VerticalAlignment = VerticalAlignment.Center,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap };
        caption.Bind(TextBlock.ForegroundProperty, new DynamicResourceExtension("SystemControlForegroundBaseHighBrush"));
        return caption;
    }
    internal CreationInputSection(Forms.Form? editor, Forms.Panel attributes, Forms.Control moneyParent,
        IEnumerable<(Forms.NumericUpDown Input, Forms.Label Label, Forms.Label Metatype, Forms.Label Augmented)> rows,
        Forms.NumericUpDown money, Forms.Label moneyLabel, Forms.Label moneyTotal, Forms.ToolTip tips,
        (Forms.Label Base, Forms.Label Augmented, Forms.Label Metatype)? headers = null, Action<int>? moneyHeightChanged = null)
    {
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("2.2*,2.2*,0.8*,1.5*"), RowSpacing = 1 };
        grid.ColumnDefinitions[1].MinWidth = 104;
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
            input.ClearValue(Layoutable.WidthProperty);
            input.MinHeight = 26; input.Padding = new Thickness(2, 0);
            Add(grid, label, row, 0); Add(grid, input, row, 1); Add(grid, augmented, row, 2); Add(grid, metatype, row++, 3);
        }
        NativeControlPresentation.AddTheme(grid.Styles);
        // Keep normal rows compact while allowing the editor to grow with its text.
        grid.Styles.Add(new Style(selector => selector.OfType<NumericUpDown>().Template().OfType<TextBox>())
        {
            Setters = {
                new Setter(TextBox.MinHeightProperty, 26d),
                new Setter(TextBox.PaddingProperty, new Thickness(2, 0)),
                new Setter(TextBox.VerticalContentAlignmentProperty, VerticalAlignment.Center)
            }
        });
        var moneyGrid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*"), ColumnSpacing = 6 };
        NativeControlPresentation.AddTheme(moneyGrid.Styles);
        var moneyCaption = Mirror(moneyLabel);
        moneyCaption.MaxLines = 2;
        moneyCaption.TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis;
        ToolTip.SetTip(moneyCaption, moneyCaption.Text);
        var total = Mirror(moneyTotal);
        total.TextWrapping = Avalonia.Media.TextWrapping.NoWrap;
        total.TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis;
        ToolTip.SetTip(total, total.Text);
        EventHandler refreshTotal = (_, _) => ToolTip.SetTip(total, total.Text);
        moneyTotal.TextChanged += refreshTotal;
        cleanup.Add(() => moneyTotal.TextChanged -= refreshTotal);
        Add(moneyGrid, new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            Children = { moneyCaption, total }
        }, 0, 0);
        var allocation = Bind(money, tips);
        allocation.ClearValue(Layoutable.WidthProperty);
        Avalonia.Automation.AutomationProperties.SetLabeledBy(allocation, moneyCaption);
        Add(moneyGrid, allocation, 0, 1);
        if (editor == null) return; // Headless binding checks need no presenter.
        var attributeViewport = new ScrollViewer
        {
            Content = grid,
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
        };
        NativeControlPresentation.AddTheme(attributeViewport.Styles);
        var attributesPresenter = new CreationRegionPresenter(editor, attributes, attributeViewport,
            "nativeCreationAttributes");
        var workspace = editor.MdiParent ?? editor;
        int moneyHeight = 40;
        int MoneyWidth() => Math.Max(0, InventoryTreeLayout.VisiblePageSize(workspace, moneyParent).Width - moneyLabel.Left - 6);
        var moneyPresenter = new CreationRegionPresenter(editor, moneyParent, moneyGrid, "nativeCreationMoney",
            () => new System.Drawing.Rectangle(moneyLabel.Left, moneyLabel.Top, MoneyWidth(), moneyHeight), clipToSource: true);
        bool queued = false;
        void MeasureMoney(object? sender, EventArgs args)
        {
            if (queued)
                return;

            queued = true;
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                queued = false;
                if (disposed || editor.IsDisposed || !moneyParent.Visible)
                    return;

                int width = MoneyWidth();
                if (moneyGrid.Bounds.Width > 0)
                    width = Math.Min(width, (int)moneyGrid.Bounds.Width);
                bool stacked = width < 140;
                if (stacked != (Grid.GetRow(allocation) == 1))
                {
                    moneyGrid.ColumnDefinitions = new ColumnDefinitions(stacked ? "*" : "*,*");
                    moneyGrid.RowDefinitions = new RowDefinitions(stacked ? "Auto,Auto" : "Auto");
                    Grid.SetColumn(allocation, stacked ? 0 : 1);
                    Grid.SetRow(allocation, stacked ? 1 : 0);
                }
                if (!stacked)
                    moneyGrid.ColumnDefinitions[1].Width = new GridLength(width < 200 ? 2 : 1, GridUnitType.Star);
                moneyGrid.ClearValue(Layoutable.WidthProperty);
                moneyGrid.ClearValue(Layoutable.HeightProperty);
                moneyGrid.InvalidateMeasure();
                moneyGrid.Measure(new Size(width, double.PositiveInfinity));
                int height = Math.Max(40, (int)Math.Ceiling(moneyGrid.DesiredSize.Height));
                if (height != moneyHeight)
                {
                    moneyHeight = height;
                    moneyHeightChanged?.Invoke(height);
                }
                moneyPresenter.Refresh();
            }, Avalonia.Threading.DispatcherPriority.Loaded);
        }
        void MoneyTypographyChanged(object? sender, AvaloniaPropertyChangedEventArgs args)
        {
            if (args.Property == Avalonia.Controls.Documents.TextElement.FontSizeProperty ||
                args.Property == Avalonia.Controls.Documents.TextElement.FontFamilyProperty)
                MeasureMoney(sender, EventArgs.Empty);
        }

        void MoneySizeChanged(object? sender, SizeChangedEventArgs args)
        {
            if (args.NewSize.Width != args.PreviousSize.Width)
                MeasureMoney(sender, EventArgs.Empty);
        }

        void MoneyCaptionChanged(object? sender, EventArgs args)
        {
            ToolTip.SetTip(moneyCaption, moneyCaption.Text);
            MeasureMoney(sender, args);
        }

        moneyLabel.TextChanged += MoneyCaptionChanged;
        workspace.SizeChanged += MeasureMoney;
        moneyParent.SizeChanged += MeasureMoney;
        moneyParent.VisibleChanged += MeasureMoney;
        moneyLabel.LocationChanged += MeasureMoney;
        moneyGrid.PropertyChanged += MoneyTypographyChanged;
        moneyGrid.SizeChanged += MoneySizeChanged;
        MeasureMoney(null, EventArgs.Empty);
        cleanup.Add(() =>
        {
            moneyLabel.TextChanged -= MoneyCaptionChanged;
            workspace.SizeChanged -= MeasureMoney;
            moneyParent.SizeChanged -= MeasureMoney;
            moneyParent.VisibleChanged -= MeasureMoney;
            moneyLabel.LocationChanged -= MeasureMoney;
            moneyGrid.PropertyChanged -= MoneyTypographyChanged;
            moneyGrid.SizeChanged -= MoneySizeChanged;
        });
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
        binding.Input.ClearValue(NumericUpDown.FontSizeProperty);
        Inputs.Add(source.Name, binding.Input); cleanup.Add(binding.Dispose);
        Detach(source); binding.Refresh(); return binding.Input;
    }
    public void Dispose()
    { if (disposed) return; disposed = true; foreach (var action in cleanup) action(); cleanup.Clear(); foreach (var control in detached) control.Dispose(); detached.Clear(); }
}
