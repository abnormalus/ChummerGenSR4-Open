#nullable enable
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Styling;
namespace Chummer;

// Shared single-line editor alignment for every direct native themed surface.
// Keep local height/padding rules (including compact attributes) authoritative.
internal static class NativeControlPresentation
{
    internal static void AddTheme(Styles styles)
    {
        styles.Add(new Avalonia.Themes.Fluent.FluentTheme());
        // Standalone text in hosted panels needs the same palette as the themed controls.
        styles.Add(new Style(selector => selector.OfType<TextBlock>())
        {
            Setters =
            {
                new Setter(TextBlock.ForegroundProperty,
                    new Avalonia.Markup.Xaml.MarkupExtensions.DynamicResourceExtension("SystemControlForegroundBaseHighBrush"))
            }
        });
        styles.Add(new Style(s => s.OfType<TextBox>().PropertyEquals(TextBox.AcceptsReturnProperty, false))
        { Setters = { new Setter(TextBox.VerticalContentAlignmentProperty, VerticalAlignment.Center) } });
        styles.Add(new Style(s => s.OfType<Button>())
        { Setters = { new Setter(Button.VerticalContentAlignmentProperty, VerticalAlignment.Center) } });
        styles.Add(new Style(s => s.OfType<Avalonia.Controls.Primitives.ToggleButton>())
        { Setters = { new Setter(Button.VerticalContentAlignmentProperty, VerticalAlignment.Center) } });
        styles.Add(new Style(s => s.OfType<ComboBox>())
        { Setters = { new Setter(ComboBox.VerticalContentAlignmentProperty, VerticalAlignment.Center) } });
        styles.Add(new Style(s => s.OfType<NumericUpDown>())
        { Setters = { new Setter(NumericUpDown.VerticalContentAlignmentProperty, VerticalAlignment.Center) } });
        styles.Add(new Style(s => s.OfType<NumericUpDown>().Template().OfType<TextBox>())
        { Setters = { new Setter(TextBox.VerticalContentAlignmentProperty, VerticalAlignment.Center) } });
        styles.Add(new Style(s => s.OfType<AutoCompleteBox>().Template().OfType<TextBox>())
        { Setters = { new Setter(TextBox.VerticalContentAlignmentProperty, VerticalAlignment.Center) } });
    }
    internal static Grid CreatePromptLayout(StackPanel fields, params Button[] commands)
    {
        var body = new ScrollViewer
        {
            Content = fields,
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            AllowAutoHide = false,
            BringIntoViewOnFocusChange = true
        };
        AddTheme(body.Styles);
        var footer = new WrapPanel { ItemSpacing = 8, LineSpacing = 4, HorizontalAlignment = HorizontalAlignment.Right };
        foreach (var command in commands)
            footer.Children.Add(command);
        ConstrainWrapItems(footer);
        Grid.SetRow(footer, 1);
        return new Grid
        {
            Margin = new Thickness(12),
            RowDefinitions = new RowDefinitions("*,Auto"),
            RowSpacing = 10,
            Children = { body, footer }
        };
    }

    internal static Grid CreateLabeledField(WrapPanel row, TextBlock caption, Control input)
    {
        caption.VerticalAlignment = input.VerticalAlignment = VerticalAlignment.Center;
        var field = new Grid { RowSpacing = 4, ColumnSpacing = 6, Children = { caption, input } };
        void Reflow()
        {
            double width = row.Bounds.Width;
            bool stacked = width < (input.Width > 0 ? input.Width : 120) + caption.FontSize * 9;
            field.ColumnDefinitions = new ColumnDefinitions(stacked ? "*" : "*,Auto");
            field.RowDefinitions = new RowDefinitions(stacked ? "Auto,Auto" : "Auto");
            Grid.SetColumn(input, stacked ? 0 : 1);
            Grid.SetRow(input, stacked ? 1 : 0);
        }
        row.SizeChanged += (_, args) =>
        {
            if (args.NewSize.Width != args.PreviousSize.Width)
                Reflow();
        };
        caption.PropertyChanged += (_, args) =>
        {
            if (args.Property == TextBlock.FontSizeProperty)
                Reflow();
        };
        Reflow();
        return field;
    }

    internal static void ConstrainWrapItems(WrapPanel panel)
    {
        panel.SizeChanged += (_, args) =>
        {
            double width = args.NewSize.Width;
            if (width <= 0 || width == args.PreviousSize.Width) return;
            foreach (var child in panel.Children)
                child.MaxWidth = System.Math.Max(0, width - child.Margin.Left - child.Margin.Right);
        };
    }
}
