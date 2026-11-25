using System;
using Avalonia.Controls;

namespace Chummer;

public partial class frmCareer
{
    private void AddVehicleDetails(StackPanel rows)
    {
        var rating = new WrapPanel { ItemSpacing = 8, LineSpacing = 4 };
        rating.Children.Add(GearReadoutsLayout.BindReadout(this, tabVehicles, lblVehicleRatingLabel, lblVehicleRating));
        rating.Children.Add(GearReadoutsLayout.BindOption(this, tabVehicles, chkVehicleIncludedInWeapon));
        rating.Children.Add(GearReadoutsLayout.BindOption(this, tabVehicles, chkVehicleWeaponAccessoryInstalled));
        rows.Children.Add(rating);

        var quantity = new WrapPanel { ItemSpacing = 8, LineSpacing = 4 };
        quantity.Children.Add(GearReadoutsLayout.BindReadout(this, tabVehicles, lblVehicleGearQtyLabel, lblVehicleGearQty));
        quantity.Children.Add(ArmorEquipmentLayout.BindCommand(this, tabVehicles, cmdVehicleGearReduceQty,
            LanguageManager.Instance.GetString("Tip_DecreaseGearQty")));
        quantity.Children.Add(GearReadoutsLayout.BindOption(this, tabVehicles, chkVehicleHomeNode));
        rows.Children.Add(quantity);

        var reference = new WrapPanel { ItemSpacing = 8, LineSpacing = 4 };
        reference.Children.Add(GearReadoutsLayout.BindSource(this, tabVehicles, "NativeVehicleSource",
            lblVehicleSourceLabel, lblVehicleSource, tipTooltip, () => lblVehicleSource_Click(lblVehicleSource, EventArgs.Empty)));
        reference.Children.Add(ArmorEquipmentLayout.BindCommand(this, tabVehicles, cmdVehicleMoveToInventory,
            tipTooltip.GetToolTip(cmdVehicleMoveToInventory)));
        rows.Children.Add(reference);

        MountedWeaponLayout.AddRows(this, tabVehicles, rows, lblVehicleWeaponRangeLabel,
            [(lblVehicleWeaponRangeShortLabel, lblVehicleWeaponRangeShort), (lblVehicleWeaponRangeMediumLabel, lblVehicleWeaponRangeMedium),
             (lblVehicleWeaponRangeLongLabel, lblVehicleWeaponRangeLong), (lblVehicleWeaponRangeExtremeLabel, lblVehicleWeaponRangeExtreme)],
            AddMountedWeaponCommands,
            [(lblVehicleWeaponNameLabel, lblVehicleWeaponName)], [(lblVehicleWeaponCategoryLabel, lblVehicleWeaponCategory)],
            [(lblVehicleWeaponDamageLabel, lblVehicleWeaponDamage), (lblVehicleWeaponAPLabel, lblVehicleWeaponAP),
             (lblVehicleWeaponModeLabel, lblVehicleWeaponMode), (lblVehicleWeaponAmmoLabel, lblVehicleWeaponAmmo)]);
    }
}
