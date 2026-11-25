using System;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Forms = Majorsilence.Forms;

namespace Chummer;

public partial class frmCreate
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
        sourcing.Children.Add(GearReadoutsLayout.BindOption(this, tabWeapons, chkWeaponBlackMarketDiscount));
        rows.Children.Add(TechnomancerPersonaLayout.BindLabel(this, tabWeapons, lblWeaponRangeLabel));
        AddRow((lblWeaponRangeShortLabel, lblWeaponRangeShort), (lblWeaponRangeMediumLabel, lblWeaponRangeMedium),
            (lblWeaponRangeLongLabel, lblWeaponRangeLong), (lblWeaponRangeExtremeLabel, lblWeaponRangeExtreme));
        var dice = AddRow((lblWeaponDicePoolLabel, lblWeaponDicePool));
        dice.Children[0].PointerEntered += (_, _) =>
            ToolTip.SetTip(dice.Children[0], tipTooltip.GetToolTip(lblWeaponDicePool));

        return new ScrollViewer
        {
            Name = "NativeCreationWeaponDetails",
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
