#nullable enable
using System;
using System.Drawing;
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

    private void InitializeCreationLifestyleMonths()
    {
        if (Avalonia.Application.Current == null || IsDisposed) return;

        var binding = new NativeInlineNumericBinding(nudLifestyleMonths, tipTooltip, wholeNumbers: true,
            available: () => SelectedLifestyle() != null);
        binding.Input.Width = 90;
        var caption = new TextBlock { VerticalAlignment = VerticalAlignment.Center, TextWrapping = Avalonia.Media.TextWrapping.Wrap };
        var total = new TextBlock { Name = "nativeLifestyleTotalCost", VerticalAlignment = VerticalAlignment.Center };
        foreach (TextBlock label in new[] { caption, total })
        {
            label.FontSize = 12;
            label.Bind(TextBlock.ForegroundProperty, new DynamicResourceExtension("SystemControlForegroundBaseHighBrush"));
        }
        var row = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"), ColumnSpacing = 6, ClipToBounds = true };
        Grid.SetColumn(caption, 1);
        Grid.SetColumn(total, 2);
        row.Children.Add(binding.Input);
        row.Children.Add(caption);
        row.Children.Add(total);
        NativeControlPresentation.AddTheme(row.Styles);
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
        var presenter = new CreationRegionPresenter(this, tabLifestyle, row, "nativeCreationLifestyleMonths",
            () => new Rectangle(nudLifestyleMonths.Left, nudLifestyleMonths.Top,
                Math.Max(0, tabLifestyle.ClientSize.Width - nudLifestyleMonths.Left - 6), 32));
        Disposed += (_, _) =>
        {
            presenter.Dispose();
            binding.Dispose();
            lblLifestyleMonthsLabel.TextChanged -= SyncCaption;
            lblLifestyleTotalCost.TextChanged -= SyncTotal;
            nudLifestyleMonths.Dispose();
            lblLifestyleMonthsLabel.Dispose();
            lblLifestyleTotalCost.Dispose();
        };
    }
}
