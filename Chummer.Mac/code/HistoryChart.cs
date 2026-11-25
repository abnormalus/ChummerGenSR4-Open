#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Styling;
using Forms = Majorsilence.Forms;

namespace Chummer;

public sealed class HistoryChart : Forms.Control, ISupportInitialize
{
    private readonly TextBlock heading = CreateLabel();
    private readonly TextBlock maximum = CreateLabel();
    private readonly TextBlock minimum = CreateLabel();
    private readonly Plot plot = new();

    public void BeginInit() { }
    public void EndInit() { }

    internal void AttachNative(Forms.Form editor)
    {
        minimum.VerticalAlignment = VerticalAlignment.Bottom;
        minimum.Margin = new Thickness(0, 0, 0, 2);
        maximum.VerticalAlignment = VerticalAlignment.Top;
        var labels = new Grid { RowDefinitions = new RowDefinitions("Auto,*,Auto") };
        Grid.SetRow(minimum, 2);
        labels.Children.Add(maximum);
        labels.Children.Add(minimum);

        var graph = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*"),
            ColumnSpacing = 6,
            MinHeight = 34
        };
        Grid.SetColumn(plot, 1);
        graph.Children.Add(labels);
        graph.Children.Add(plot);

        var content = new Grid { RowDefinitions = new RowDefinitions("Auto,*"), RowSpacing = 4 };
        Grid.SetRow(graph, 1);
        content.Children.Add(heading);
        content.Children.Add(graph);
        var surface = new Border
        {
            Padding = new Thickness(8, 3),
            Child = new ScrollViewer
            {
                Content = content,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            }
        };

        NativeControlPresentation.AddTheme(surface.Styles);
        surface.Bind(Border.BackgroundProperty, new DynamicResourceExtension("SystemControlBackgroundListLowBrush"));
        var presenter = new CreationRegionPresenter(editor, this, surface, "Native" + Name, clipToSource: true);
        editor.Disposed += (_, _) => presenter.Dispose();
    }

    public void SetEntries(IEnumerable<ExpenseLogEntry> entries, ExpenseType type, string title)
    {
        long total = 0;
        long[] values = entries.Where(entry => entry.Type == type).OrderBy(entry => entry.Date)
            .Select(entry => total += entry.Amount).Prepend(0L).ToArray();
        long high = Math.Max(1, values.Max());
        long low = values.Min();
        heading.Text = title;
        maximum.Text = high.ToString("N0");
        minimum.Text = low.ToString("N0");
        plot.SetValues(values, low, high, type);
    }

    private static TextBlock CreateLabel()
    {
        var label = new TextBlock { TextWrapping = TextWrapping.Wrap };
        label.Bind(TextBlock.ForegroundProperty, new DynamicResourceExtension("SystemControlForegroundBaseHighBrush"));
        return label;
    }

    private sealed class Plot : Control
    {
        private double[] values = Array.Empty<double>();
        private ExpenseType type;

        internal void SetValues(long[] entries, long low, long high, ExpenseType expenseType)
        {
            values = entries.Select(value => (value - (double)low) / (high - (double)low)).ToArray();
            type = expenseType;
            InvalidateVisual();
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            ActualThemeVariantChanged += ThemeChanged;
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            ActualThemeVariantChanged -= ThemeChanged;
            base.OnDetachedFromVisualTree(e);
        }

        private void ThemeChanged(object? sender, EventArgs args) => InvalidateVisual();

        public override void Render(DrawingContext context)
        {
            double width = Bounds.Width - 2;
            double height = Bounds.Height - 2;
            if (width < 8 || height < 8) return;

            bool dark = ActualThemeVariant == ThemeVariant.Dark;
            var axis = new Pen(new SolidColorBrush(dark ? Colors.Silver : Colors.DimGray));
            context.DrawLine(axis, new Point(1, 1), new Point(1, height));
            context.DrawLine(axis, new Point(1, height), new Point(width, height));

            var color = (type, dark) switch
            {
                (ExpenseType.Karma, true) => Color.FromRgb(130, 176, 255),
                (ExpenseType.Karma, false) => Colors.RoyalBlue,
                (_, true) => Color.FromRgb(115, 211, 144),
                _ => Colors.ForestGreen
            };
            var line = new Pen(new SolidColorBrush(color), 2);
            var previous = new Point(1, height);
            for (int index = 0; index < values.Length; index++)
            {
                var point = new Point(1 + (width - 1) * index / Math.Max(1, values.Length - 1),
                    height - (height - 1) * values[index]);
                if (index > 0) context.DrawLine(line, previous, point);
                previous = point;
            }
        }
    }
}
