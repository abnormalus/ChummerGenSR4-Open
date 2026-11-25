using System;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Forms = Majorsilence.Forms;

namespace Chummer;

internal static class VehicleSummaryLayout
{
    internal static ScrollViewer Create(Forms.Form owner, Forms.TabPage page, Forms.TreeView tree, Action<StackPanel> addDetails,
        params (Forms.Label Title, Forms.Label Value)[][] groups)
    {
        var fields = new StackPanel { Spacing = 5 };
        foreach (var group in groups)
        {
            var row = new WrapPanel { ItemSpacing = 10, LineSpacing = 4 };
            foreach (var (title, value) in group)
                row.Children.Add(GearReadoutsLayout.BindReadout(owner, page, title, value));
            NativeControlPresentation.ConstrainWrapItems(row);
            fields.Children.Add(row);
        }

        addDetails(fields);
        GearReadoutsLayout.ShowForSelection(owner, fields, tree);
        return new ScrollViewer
        {
            Name = "NativeVehicleSummary", Content = fields,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            BringIntoViewOnFocusChange = true
        };
    }
}
