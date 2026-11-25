#nullable enable
using System;
using Avalonia.Controls;
using Forms = Majorsilence.Forms;

namespace Chummer;

internal static class MountedWeaponLayout
{
    internal static void AddRows(Forms.Form owner, Forms.TabPage page, StackPanel rows, Forms.Label rangeHeading,
        (Forms.Label Title, Forms.Label Value)[] ranges, Action<StackPanel>? addCommands,
        params (Forms.Label Title, Forms.Label Value)[][] groups)
    {
        foreach (var group in groups)
            AddRow(group);
        rows.Children.Add(TechnomancerPersonaLayout.BindLabel(owner, page, rangeHeading));
        AddRow(ranges);
        addCommands?.Invoke(rows);
        void AddRow((Forms.Label Title, Forms.Label Value)[] fields)
        {
            var row = new WrapPanel { ItemSpacing = 10, LineSpacing = 4 };
            foreach (var (title, value) in fields)
                row.Children.Add(GearReadoutsLayout.BindReadout(owner, page, title, value));
            NativeControlPresentation.ConstrainWrapItems(row);
            rows.Children.Add(row);
        }
    }
}

public partial class frmCareer
{
    private void AddMountedWeaponCommands(StackPanel rows)
    {
        var rounds = new WrapPanel { ItemSpacing = 8, LineSpacing = 4 };
        rounds.Children.Add(GearReadoutsLayout.BindReadout(this, tabVehicles, lblVehicleWeaponAmmoRemainingLabel,
            lblVehicleWeaponAmmoRemaining));
        rounds.Children.Add(ArmorEquipmentLayout.BindCommand(this, tabVehicles, cmdFireVehicleWeapon));
        rounds.Children.Add(ArmorEquipmentLayout.BindCommand(this, tabVehicles, cmdReloadVehicleWeapon));
        var ammoType = NativeSelectionInput.Bind(this, tabVehicles, cboVehicleWeaponAmmo);
        var ammoHeading = TechnomancerPersonaLayout.BindLabel(this, tabVehicles, lblVehicleWeaponAmmoTypeLabel);
        Avalonia.Automation.AutomationProperties.SetLabeledBy(ammoType, ammoHeading);
        var types = new StackPanel { Name = "NativeMountedAmmoType", Spacing = 4, Children = { ammoHeading, ammoType } };
        var dice = new WrapPanel { ItemSpacing = 8, LineSpacing = 4 };
        dice.Children.Add(GearReadoutsLayout.BindReadout(this, tabVehicles, lblVehicleWeaponDicePoolLabel, lblVehicleWeaponDicePool));
        dice.Children.Add(ArmorEquipmentLayout.BindCommand(this, tabVehicles, cmdRollVehicleWeapon,
            tipTooltip.GetToolTip(cmdRollVehicleWeapon)));
        NativeControlPresentation.ConstrainWrapItems(rounds);
        NativeControlPresentation.ConstrainWrapItems(dice);
        rows.Children.Add(rounds);
        rows.Children.Add(types);
        rows.Children.Add(dice);
    }
}
