#nullable enable
using System;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Forms = Majorsilence.Forms;

namespace Chummer;

internal static class CommonMagicAllocation
{
    internal static System.Drawing.Rectangle Bounds(Forms.Form owner, Forms.Control page, int left, int top)
    {
        var viewport = InventoryTreeLayout.VisiblePageSize(owner.MdiParent ?? owner, page);
        return new System.Drawing.Rectangle(left, top, Math.Max(0, viewport.Width - left - 6),
            Math.Min(viewport.Height / 5, Math.Max(0, viewport.Height - top)));
    }

    internal static void Attach(Forms.Form owner, Forms.Label title, Forms.Label magicianCaption,
        Forms.NumericUpDown magician, Forms.Label adeptCaption, Forms.Label adept, Forms.ToolTip tips,
        Func<System.Drawing.Rectangle> bounds, Forms.Control? precedingSection = null)
    {
        if (Application.Current == null || title.Parent is not { } page)
            return;
        Forms.Control[] controllers = [title, magicianCaption, magician, adeptCaption, adept];
        foreach (var controller in controllers)
            page.Controls.Remove(controller);

        var binding = new NativeInlineNumericBinding(magician, tips, wholeNumbers: true);
        binding.Input.ClearValue(Layoutable.WidthProperty);
        binding.Input.ClearValue(NumericUpDown.FontSizeProperty);
        AutomationProperties.SetName(binding.Input, magicianCaption.Text);
        var result = Label(adept.Text);
        var titleText = Label(title.Text);
        var magicianText = Label(magicianCaption.Text);
        var adeptText = Label(adeptCaption.Text);
        AutomationProperties.SetLabeledBy(binding.Input, magicianText);
        var magicianColumn = new StackPanel { Spacing = 3, Children = { magicianText, binding.Input } };
        var columns = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*"), ColumnSpacing = 8 };
        columns.Children.Add(magicianColumn);
        var adeptColumn = new StackPanel { Spacing = 3, Children = { adeptText, result } };
        Grid.SetColumn(adeptColumn, 1);
        columns.Children.Add(adeptColumn);
        var layout = new StackPanel { Spacing = 4, Children = { titleText, columns } };
        var scroll = new ScrollViewer
        {
            Content = layout, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled
        };
        scroll.SizeChanged += (_, args) =>
        {
            bool stacked = args.NewSize.Width < 190;
            if (stacked == (Grid.GetRow(adeptColumn) == 1))
                return;

            columns.ColumnDefinitions = new ColumnDefinitions(stacked ? "*" : "*,*");
            columns.RowDefinitions = new RowDefinitions(stacked ? "Auto,Auto" : "Auto");
            columns.RowSpacing = 6;
            Grid.SetColumn(adeptColumn, stacked ? 0 : 1);
            Grid.SetRow(adeptColumn, stacked ? 1 : 0);
        };
        var surface = new Border { Child = scroll };
        NativeControlPresentation.AddTheme(surface.Styles);
        var presenter = new CreationRegionPresenter(owner, page, surface, "NativeCommonMagicAllocation", bounds, clipToSource: true);
        void Refresh()
        {
            scroll.IsVisible = magician.Visible;
            result.Text = adept.Text;
            adeptColumn.IsVisible = adept.Visible;
            presenter.Refresh();
        }
        void Changed(object? sender, EventArgs args) => Refresh();
        void CaptionsChanged(object? sender, EventArgs args)
        {
            titleText.Text = title.Text;
            magicianText.Text = magicianCaption.Text;
            adeptText.Text = adeptCaption.Text;
        }
        title.TextChanged += CaptionsChanged;
        magicianCaption.TextChanged += CaptionsChanged;
        adeptCaption.TextChanged += CaptionsChanged;
        if (precedingSection != null)
        {
            precedingSection.VisibleChanged += Changed;
            precedingSection.LocationChanged += Changed;
        }
        magician.VisibleChanged += Changed;
        adept.VisibleChanged += Changed;
        adept.TextChanged += Changed;
        Refresh();
        owner.Disposed += (_, _) =>
        {
            if (precedingSection != null)
            {
                precedingSection.VisibleChanged -= Changed;
                precedingSection.LocationChanged -= Changed;
            }
            title.TextChanged -= CaptionsChanged;
            magicianCaption.TextChanged -= CaptionsChanged;
            adeptCaption.TextChanged -= CaptionsChanged;
            magician.VisibleChanged -= Changed;
            adept.VisibleChanged -= Changed;
            adept.TextChanged -= Changed;
            presenter.Dispose();
            binding.Dispose();
            foreach (var controller in controllers)
                controller.Dispose();
        };

        static TextBlock Label(string text) => new() { Text = text, TextWrapping = TextWrapping.Wrap };
    }
}
