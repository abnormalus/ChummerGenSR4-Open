#nullable enable
using System;
using System.Linq;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Forms = Majorsilence.Forms;

namespace Chummer;

internal static class CommonAiAttributes
{
    internal static System.Drawing.Rectangle Bounds(Forms.Form owner, Forms.Control page, Forms.Label anchor)
    {
        var viewport = InventoryTreeLayout.VisiblePageSize(owner.MdiParent ?? owner, page);
        return new System.Drawing.Rectangle(anchor.Left, anchor.Top, Math.Max(0, viewport.Width - anchor.Left - 6),
            Math.Min(viewport.Height / 4, Math.Max(0, viewport.Height - anchor.Top)));
    }

    internal static void Attach(Forms.Form owner, Forms.ToolTip tips,
        (Forms.Label Caption, Forms.Label Value)[] values, (Forms.Label Caption, Forms.NumericUpDown Input)[] numbers)
    {
        var anchor = values[0].Caption;
        if (Application.Current == null || anchor.Parent is not { } page)
            return;

        var fields = new StackPanel { Spacing = 2 };
        var scroll = new ScrollViewer
        {
            Content = fields, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled
        };
        void RefreshVisibility() => scroll.IsVisible = fields.Children.Any(row => row.IsVisible);
        foreach (var (caption, value) in values)
        {
            var text = new TextBlock { Name = "Native" + value.Name, TextWrapping = TextWrapping.Wrap };
            var row = Row(caption, text);
            void Refresh()
            {
                text.Text = value.Text;
                row.IsVisible = caption.Visible && value.Visible;
                RefreshVisibility();
            }
            var captionState = new NativeControlObservation(caption, Refresh);
            var valueState = new NativeControlObservation(value, Refresh);
            void TextChanged(object? sender, EventArgs args) => Refresh();
            value.TextChanged += TextChanged;
            Refresh();
            owner.Disposed += (_, _) =>
            {
                value.TextChanged -= TextChanged;
                captionState.Dispose();
                valueState.Dispose();
                value.Dispose();
            };
            page.Controls.Remove(value);
        }
        foreach (var (caption, input) in numbers)
        {
            page.Controls.Remove(input);
            var binding = new NativeInlineNumericBinding(input, tips, wholeNumbers: true);
            binding.Input.ClearValue(Layoutable.WidthProperty);
            binding.Input.ClearValue(NumericUpDown.FontSizeProperty);
            AutomationProperties.SetName(binding.Input, caption.Text);
            var row = Row(caption, binding.Input);
            void Refresh()
            {
                row.IsVisible = input.Visible;
                RefreshVisibility();
            }
            var state = new NativeControlObservation(input, Refresh);
            Refresh();
            owner.Disposed += (_, _) => { state.Dispose(); binding.Dispose(); input.Dispose(); };
        }

        NativeControlPresentation.AddTheme(scroll.Styles);
        var presenter = new CreationRegionPresenter(owner, page, new Border { Child = scroll }, "NativeCommonAiAttributes",
            () => Bounds(owner, page, anchor), clipToSource: true);
        void Move(object? sender, EventArgs args) => presenter.Refresh();
        anchor.LocationChanged += Move;
        owner.Disposed += (_, _) =>
        {
            anchor.LocationChanged -= Move;
            presenter.Dispose();
        };

        Grid Row(Forms.Label caption, Control value)
        {
            page.Controls.Remove(caption);
            var label = new TextBlock
            {
                Name = "Native" + caption.Name, Text = caption.Text, TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center
            };
            ToolTip.SetTip(label, tips.GetToolTip(caption));
            void RefreshCaption(object? sender, EventArgs args) => label.Text = caption.Text;
            caption.TextChanged += RefreshCaption;
            var row = new Grid { ColumnDefinitions = new ColumnDefinitions(value is NumericUpDown ? "*,2*" : "3*,2*"), ColumnSpacing = 6 };
            row.Children.Add(label);
            Grid.SetColumn(value, 1);
            row.Children.Add(value);
            if (value is NumericUpDown)
            {
                scroll.SizeChanged += (_, args) =>
                {
                    bool stacked = args.NewSize.Width < 210;
                    if (stacked == (Grid.GetRow(value) == 1))
                        return;

                    row.ColumnDefinitions = new ColumnDefinitions(stacked ? "*" : "*,2*");
                    row.RowDefinitions = new RowDefinitions(stacked ? "Auto,Auto" : "Auto");
                    row.RowSpacing = 3;
                    Grid.SetColumn(value, stacked ? 0 : 1);
                    Grid.SetRow(value, stacked ? 1 : 0);
                };
            }
            fields.Children.Add(row);
            owner.Disposed += (_, _) =>
            {
                caption.TextChanged -= RefreshCaption;
                caption.Dispose();
            };
            return row;
        }
    }
}
