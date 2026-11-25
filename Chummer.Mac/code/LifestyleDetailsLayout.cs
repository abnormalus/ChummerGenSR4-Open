using System;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Forms = Majorsilence.Forms;

namespace Chummer;

internal static class LifestyleDetailsLayout
{
    internal static ScrollViewer Create(Forms.Form owner, Forms.TabPage page, Forms.TreeView tree, Control months,
        Forms.Label sourceHeading, Forms.Label source, Forms.ToolTip tips, Action openSource,
        (Forms.Label Title, Forms.Label Value) cost, (Forms.Label Title, Forms.Label Value)? startingNuyen,
        params (Forms.Label Title, Forms.Label Value)[] details)
    {
        var rows = new StackPanel { Spacing = 7 };
        var costs = new WrapPanel { ItemSpacing = 12, LineSpacing = 4 };
        costs.Children.Add(GearReadoutsLayout.BindReadout(owner, page, cost.Title, cost.Value));
        if (startingNuyen is { } nuyen)
            costs.Children.Add(GearReadoutsLayout.BindReadout(owner, page, nuyen.Title, nuyen.Value));
        NativeControlPresentation.ConstrainWrapItems(costs);
        rows.Children.Add(costs);
        rows.Children.Add(months);
        rows.Children.Add(GearReadoutsLayout.BindSource(owner, page, "NativeLifestyleSource",
            sourceHeading, source, tips, openSource));
        foreach (var (title, value) in details)
            rows.Children.Add(GearReadoutsLayout.BindReadout(owner, page, title, value));
        GearReadoutsLayout.ShowForSelection(owner, rows, tree);
        return new ScrollViewer
        {
            Name = "NativeLifestyleDetails",
            Content = rows,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            BringIntoViewOnFocusChange = true
        };
    }

}

public partial class frmCreate
{
    private ScrollViewer CreateLifestyleDetails() => LifestyleDetailsLayout.Create(this, tabLifestyle, treLifestyles,
        CreateLifestyleMonths(), lblLifestyleSourceLabel, lblLifestyleSource, tipTooltip,
        () => lblLifestyleSource_Click(lblLifestyleSource, EventArgs.Empty),
        (lblLifestyleCostLabel, lblLifestyleCost), (lblLifestyleStartingNuyenLabel, lblLifestyleStartingNuyen),
        (lblLifestyleComfortsLabel, lblLifestyleComforts), (lblLifestyleEntertainmentLabel, lblLifestyleEntertainment),
        (lblLifestyleNecessitiesLabel, lblLifestyleNecessities), (lblLifestyleNeighborhoodLabel, lblLifestyleNeighborhood),
        (lblLifestyleSecurityLabel, lblLifestyleSecurity), (lblLifestyleQualitiesLabel, lblLifestyleQualities));
}

public partial class frmCareer
{
    private ScrollViewer CreateLifestyleDetails()
    {
        var months = new StackPanel { Name = "NativeCareerLifestyleMonths", Spacing = 4 };
        months.Children.Add(GearReadoutsLayout.BindReadout(this, tabLifestyle, lblLifestyleMonthsLabel, lblLifestyleMonths));
        var commands = new WrapPanel { ItemSpacing = 8, LineSpacing = 4 };
        commands.Children.Add(ArmorEquipmentLayout.BindCommand(this, tabLifestyle, cmdIncreaseLifestyleMonths,
            tipTooltip.GetToolTip(cmdIncreaseLifestyleMonths)));
        commands.Children.Add(ArmorEquipmentLayout.BindCommand(this, tabLifestyle, cmdDecreaseLifestyleMonths,
            tipTooltip.GetToolTip(cmdDecreaseLifestyleMonths)));
        NativeControlPresentation.ConstrainWrapItems(commands);
        months.Children.Add(commands);
        return LifestyleDetailsLayout.Create(this, tabLifestyle, treLifestyles, months, lblLifestyleSourceLabel, lblLifestyleSource,
            tipTooltip, () => lblLifestyleSource_Click(lblLifestyleSource, EventArgs.Empty),
            (lblLifestyleCostLabel, lblLifestyleCost), null,
            (lblLifestyleComfortsLabel, lblLifestyleComforts), (lblLifestyleEntertainmentLabel, lblLifestyleEntertainment),
            (lblLifestyleNecessitiesLabel, lblLifestyleNecessities), (lblLifestyleNeighborhoodLabel, lblLifestyleNeighborhood),
            (lblLifestyleSecurityLabel, lblLifestyleSecurity), (lblLifestyleQualitiesLabel, lblLifestyleQualities));
    }
}
