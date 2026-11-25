#nullable enable
using System;
using Avalonia.Controls;
using Forms = Majorsilence.Forms;

namespace Chummer;

internal static class SpellDetailsLayout
{
    internal static StackPanel Create(Forms.Form owner, Forms.TabPage page,
        Forms.Label sourceHeading, Forms.Label source, Forms.ToolTip tips, Action openSource,
        (Forms.Label Title, Forms.Label Value) dicePool, Forms.Button? rollSpell, Forms.Button? quicken,
        Action<StackPanel> addMagicRows,
        params (Forms.Label Title, Forms.Label Value)[][] fields)
    {
        var rows = new StackPanel { Spacing = 5 };
        foreach (var group in fields)
        {
            var row = new WrapPanel { ItemSpacing = 12, LineSpacing = 4 };
            foreach (var (title, value) in group)
                row.Children.Add(GearReadoutsLayout.BindReadout(owner, page, title, value));
            NativeControlPresentation.ConstrainWrapItems(row);
            rows.Children.Add(row);
        }
        rows.Children.Add(GearReadoutsLayout.BindSource(owner, page, "NativeSpellSource", sourceHeading, source, tips, openSource));
        var dice = new WrapPanel { ItemSpacing = 8, LineSpacing = 4 };
        dice.Children.Add(GearReadoutsLayout.BindReadout(owner, page, dicePool.Title, dicePool.Value));
        if (rollSpell != null)
            dice.Children.Add(ArmorEquipmentLayout.BindCommand(owner, page, rollSpell, tips.GetToolTip(rollSpell)));
        if (quicken != null)
            dice.Children.Add(ArmorEquipmentLayout.BindCommand(owner, page, quicken));
        NativeControlPresentation.ConstrainWrapItems(dice);
        rows.Children.Add(dice);
        addMagicRows(rows);

        return rows;
    }

    internal static void AddMagicRows(Forms.Form owner, Forms.TabPage page, StackPanel rows,
        Forms.Label traditionLabel, Forms.ComboBox tradition, Forms.Label drainLabel, Forms.Label drain,
        Forms.Label drainValue, Forms.Label mentorLabel, Forms.Label mentor, Forms.Label mentorDetails,
        Forms.ToolTip tips, Forms.Button? rollDrain = null)
    {
        rows.Children.Add(TechnomancerPersonaLayout.CreateStreamRow(owner, page, traditionLabel, tradition));
        var drainRow = TechnomancerPersonaLayout.CreateFadingRow(owner, page, drainLabel, drain, drainValue);
        if (rollDrain != null)
            drainRow.Children.Add(ArmorEquipmentLayout.BindCommand(owner, page, rollDrain, tips.GetToolTip(rollDrain)));
        rows.Children.Add(drainRow);
        rows.Children.Add(GearReadoutsLayout.BindReadout(owner, page, mentorLabel, mentor));
        rows.Children.Add(TechnomancerPersonaLayout.BindLabel(owner, page, mentorDetails));
    }
}
