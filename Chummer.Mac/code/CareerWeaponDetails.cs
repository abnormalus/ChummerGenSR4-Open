using System;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Forms = Majorsilence.Forms;

namespace Chummer;

public partial class frmCareer
{
    private ScrollViewer CreateWeaponDetails()
    {
        var rows = new StackPanel { Spacing = 7 };
        AddRow((lblWeaponNameLabel, lblWeaponName));
        AddRow((lblWeaponCategoryLabel, lblWeaponCategory));
        AddRow((lblWeaponDamageLabel, lblWeaponDamage), (lblWeaponRCLabel, lblWeaponRC), (lblWeaponAPLabel, lblWeaponAP));
        AddRow((lblWeaponReachLabel, lblWeaponReach), (lblWeaponModeLabel, lblWeaponMode), (lblWeaponAmmoLabel, lblWeaponAmmo));
        AddRow((lblWeaponAvailLabel, lblWeaponAvail), (lblWeaponCostLabel, lblWeaponCost), (lblWeaponConcealLabel, lblWeaponConceal));
        var equipment = AddRow((lblWeaponSlotsLabel, lblWeaponSlots));
        equipment.Children.Add(GearReadoutsLayout.BindOption(this, tabWeapons, chkIncludedInWeapon));
        equipment.Children.Add(GearReadoutsLayout.BindOption(this, tabWeapons, chkWeaponAccessoryInstalled));
        var sourcing = AddRow();
        sourcing.Children.Add(GearReadoutsLayout.BindSource(this, tabWeapons, "NativeWeaponSource",
            lblWeaponSourceLabel, lblWeaponSource, tipTooltip, () => lblWeaponSource_Click(lblWeaponSource, EventArgs.Empty)));
        sourcing.Children.Add(ArmorEquipmentLayout.BindCommand(this, tabWeapons, cmdWeaponMoveToVehicle,
            tipTooltip.GetToolTip(cmdWeaponMoveToVehicle)));
        rows.Children.Add(TechnomancerPersonaLayout.BindLabel(this, tabWeapons, lblWeaponRangeLabel));
        AddRow((lblWeaponRangeShortLabel, lblWeaponRangeShort), (lblWeaponRangeMediumLabel, lblWeaponRangeMedium),
            (lblWeaponRangeLongLabel, lblWeaponRangeLong), (lblWeaponRangeExtremeLabel, lblWeaponRangeExtreme));
        var ammo = AddRow((lblWeaponAmmoRemainingLabel, lblWeaponAmmoRemaining));
        ammo.Children.Add(ArmorEquipmentLayout.BindCommand(this, tabWeapons, cmdFireWeapon));
        ammo.Children.Add(ArmorEquipmentLayout.BindCommand(this, tabWeapons, cmdReloadWeapon));
        ammo.Children.Add(ArmorEquipmentLayout.BindCommand(this, tabWeapons, cmdWeaponBuyAmmo,
            tipTooltip.GetToolTip(cmdWeaponBuyAmmo)));
        var ammoType = NativeSelectionInput.Bind(this, tabWeapons, cboWeaponAmmo);
        var ammoTypeLabel = TechnomancerPersonaLayout.BindLabel(this, tabWeapons, lblWeaponAmmoTypeLabel);
        Avalonia.Automation.AutomationProperties.SetLabeledBy(ammoType, ammoTypeLabel);
        rows.Children.Add(new StackPanel { Spacing = 4, Children = { ammoTypeLabel, ammoType } });
        var dice = AddRow((lblWeaponDicePoolLabel, lblWeaponDicePool));
        dice.Children[0].PointerEntered += (_, _) =>
            ToolTip.SetTip(dice.Children[0], tipTooltip.GetToolTip(lblWeaponDicePool));

        dice.Children.Add(ArmorEquipmentLayout.BindCommand(this, tabWeapons, cmdRollWeapon,
            tipTooltip.GetToolTip(cmdRollWeapon)));

        return new ScrollViewer
        {
            Name = "NativeCareerWeaponDetails",
            Content = rows,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            BringIntoViewOnFocusChange = true
        };

        WrapPanel AddRow(params (Forms.Label Title, Forms.Label Value)[] fields)
        {
            var row = new WrapPanel { ItemSpacing = 12, LineSpacing = 4 };
            foreach (var (title, value) in fields)
                row.Children.Add(GearReadoutsLayout.BindReadout(this, tabWeapons, title, value));
            NativeControlPresentation.ConstrainWrapItems(row);
            rows.Children.Add(row);
            return row;
        }
    }
}
