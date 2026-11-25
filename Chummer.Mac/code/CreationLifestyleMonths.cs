#nullable enable
using System;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Forms = Majorsilence.Forms;

namespace Chummer;

public partial class frmCreate
{
    private bool refreshingLifestyleMonths;

    private Lifestyle? SelectedLifestyle() => treLifestyles.SelectedNode is { Level: > 0, Tag: not null } node
        ? _objFunctions.FindLifestyle(node.Tag.ToString(), _objCharacter.Lifestyles)
        : null;

    private Grid CreateLifestyleMonths()
    {
        var binding = new NativeInlineNumericBinding(nudLifestyleMonths, tipTooltip, wholeNumbers: true,
            available: () => SelectedLifestyle() != null);
        binding.Input.ClearValue(Layoutable.WidthProperty);
        binding.Input.ClearValue(NumericUpDown.FontSizeProperty);
        var caption = new TextBlock { VerticalAlignment = VerticalAlignment.Center, TextWrapping = Avalonia.Media.TextWrapping.Wrap };
        var total = new TextBlock { Name = "nativeLifestyleTotalCost", VerticalAlignment = VerticalAlignment.Center, TextWrapping = Avalonia.Media.TextWrapping.Wrap };
        foreach (TextBlock label in new[] { caption, total })
            label.Bind(TextBlock.ForegroundProperty, new DynamicResourceExtension("SystemControlForegroundBaseHighBrush"));
        var row = new Grid { Name = "NativeLifestyleMonths", ColumnSpacing = 6, RowSpacing = 4,
            Children = { binding.Input, caption, total } };
        void Reflow()
        {
            bool stacked = row.Bounds.Width < Math.Max(290, binding.Input.FontSize * 20);
            row.ColumnDefinitions = new ColumnDefinitions(stacked ? "*" : "2*,2*,3*");
            if (!stacked) row.ColumnDefinitions[0].MinWidth = 104;
            row.RowDefinitions = new RowDefinitions(stacked ? "Auto,Auto,Auto" : "Auto");
            Grid.SetRow(binding.Input, stacked ? 1 : 0);
            Grid.SetRow(caption, 0);
            Grid.SetColumn(caption, stacked ? 0 : 1);
            Grid.SetRow(total, stacked ? 2 : 0);
            Grid.SetColumn(total, stacked ? 0 : 2);
        }
        row.SizeChanged += (_, args) =>
        {
            if (args.NewSize.Width != args.PreviousSize.Width) Reflow();
        };
        row.PropertyChanged += (_, args) =>
        {
            if (args.Property == Avalonia.Controls.Documents.TextElement.FontSizeProperty) Reflow();
        };
        Reflow();
        AutomationProperties.SetLabeledBy(binding.Input, caption);

        void SyncCaption(object? sender, EventArgs args) => caption.Text = lblLifestyleMonthsLabel.Text;
        void SyncTotal(object? sender, EventArgs args) => total.Text = lblLifestyleTotalCost.Text;
        lblLifestyleMonthsLabel.TextChanged += SyncCaption;
        lblLifestyleTotalCost.TextChanged += SyncTotal;
        SyncCaption(null, EventArgs.Empty);
        SyncTotal(null, EventArgs.Empty);

        tabLifestyle.Controls.Remove(nudLifestyleMonths);
        tabLifestyle.Controls.Remove(lblLifestyleMonthsLabel);
        tabLifestyle.Controls.Remove(lblLifestyleTotalCost);
        binding.Refresh();
        Disposed += (_, _) =>
        {
            binding.Dispose();
            lblLifestyleMonthsLabel.TextChanged -= SyncCaption;
            lblLifestyleTotalCost.TextChanged -= SyncTotal;
            nudLifestyleMonths.Dispose();
            lblLifestyleMonthsLabel.Dispose();
            lblLifestyleTotalCost.Dispose();
        };
        return row;
    }
}
