using System;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Forms = Majorsilence.Forms;

namespace Chummer;

internal static class CritterPowerDetailsLayout
{
    internal static ScrollViewer Create(Forms.Form owner, Forms.TabPage page, Forms.CheckBox counts,
        Forms.Label sourceHeading, Forms.Label source, Forms.ToolTip tips, Action openSource,
        (Forms.Label Title, Forms.Label Value) cost, params (Forms.Label Title, Forms.Label Value)[] fields)
    {
        var rows = new StackPanel { Spacing = 7 };
        foreach (var (title, value) in fields)
            rows.Children.Add(GearReadoutsLayout.BindReadout(owner, page, title, value));
        rows.Children.Add(GearReadoutsLayout.BindSource(owner, page, "NativeCritterPowerSource",
            sourceHeading, source, tips, openSource));
        rows.Children.Add(GearReadoutsLayout.BindReadout(owner, page, cost.Title, cost.Value));
        rows.Children.Add(GearReadoutsLayout.BindOption(owner, page, counts));
        return new ScrollViewer
        {
            Name = "NativeCritterPowerDetails", Content = rows,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            BringIntoViewOnFocusChange = true
        };
    }
}
