using Avalonia.Controls;

namespace Chummer;

internal static class NativePickerPanes
{
    internal static Grid Create(Control choices, Control details)
    {
        var panes = new Grid { ColumnDefinitions = new ColumnDefinitions("2*,3*"), ColumnSpacing = 12 };
        panes.Children.Add(choices);
        Grid.SetColumn(details, 1);
        panes.Children.Add(details);
        panes.SizeChanged += (_, _) =>
        {
            bool wide = panes.Bounds.Width >= 560;
            if (wide == (Grid.GetColumn(details) == 1))
                return;
            panes.ColumnDefinitions = new ColumnDefinitions(wide ? "2*,3*" : "*");
            panes.RowDefinitions = new RowDefinitions(wide ? "*" : "*,*");
            panes.RowSpacing = wide ? 0 : 10;
            Grid.SetColumn(details, wide ? 1 : 0);
            Grid.SetRow(details, wide ? 0 : 1);
        };
        return panes;
    }
}
