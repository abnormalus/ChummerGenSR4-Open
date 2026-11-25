#nullable enable
using System;
using Avalonia.Automation;
using Avalonia.Controls;

namespace Chummer;

public partial class frmCreate
{
    private NativeInlineNumericBinding? vehicleRatingInput;
    private NativeInlineNumericBinding? vehicleQuantityInput;

    private void RefreshSelectedVehicle()
    {
        RefreshSelectedVehicleDetails();
        vehicleRatingInput?.Refresh();
        vehicleQuantityInput?.Refresh();
    }

    private void AddVehicleDetails(StackPanel rows)
    {
        var rating = new WrapPanel { ItemSpacing = 8, LineSpacing = 4 };
        vehicleRatingInput = AddInput(rating, lblVehicleRatingLabel, nudVehicleRating);
        rating.Children.Add(GearReadoutsLayout.BindOption(this, tabVehicles, chkVehicleIncludedInWeapon));
        rating.Children.Add(GearReadoutsLayout.BindOption(this, tabVehicles, chkVehicleWeaponAccessoryInstalled));
        NativeControlPresentation.ConstrainWrapItems(rating);
        rows.Children.Add(rating);

        var quantity = new WrapPanel { ItemSpacing = 8, LineSpacing = 4 };
        vehicleQuantityInput = AddInput(quantity, lblVehicleGearQtyLabel, nudVehicleGearQty);
        quantity.Children.Add(GearReadoutsLayout.BindOption(this, tabVehicles, chkVehicleHomeNode));
        NativeControlPresentation.ConstrainWrapItems(quantity);
        rows.Children.Add(quantity);

        var reference = new WrapPanel { ItemSpacing = 8, LineSpacing = 4 };
        reference.Children.Add(GearReadoutsLayout.BindSource(this, tabVehicles, "NativeVehicleSource",
            lblVehicleSourceLabel, lblVehicleSource, tipTooltip, () => lblVehicleSource_Click(lblVehicleSource, EventArgs.Empty)));
        reference.Children.Add(GearReadoutsLayout.BindOption(this, tabVehicles, chkVehicleBlackMarketDiscount));
        rows.Children.Add(reference);

        MountedWeaponLayout.AddRows(this, tabVehicles, rows, lblVehicleWeaponRangeLabel,
            [(lblVehicleWeaponRangeShortLabel, lblVehicleWeaponRangeShort), (lblVehicleWeaponRangeMediumLabel, lblVehicleWeaponRangeMedium),
             (lblVehicleWeaponRangeLongLabel, lblVehicleWeaponRangeLong), (lblVehicleWeaponRangeExtremeLabel, lblVehicleWeaponRangeExtreme)],
            null,
            [(lblVehicleWeaponNameLabel, lblVehicleWeaponName)], [(lblVehicleWeaponCategoryLabel, lblVehicleWeaponCategory)],
            [(lblVehicleWeaponDamageLabel, lblVehicleWeaponDamage), (lblVehicleWeaponAPLabel, lblVehicleWeaponAP),
             (lblVehicleWeaponModeLabel, lblVehicleWeaponMode), (lblVehicleWeaponAmmoLabel, lblVehicleWeaponAmmo)]);

        NativeInlineNumericBinding AddInput(WrapPanel row, Majorsilence.Forms.Label label, Majorsilence.Forms.NumericUpDown source)
        {
            var heading = TechnomancerPersonaLayout.BindLabel(this, tabVehicles, label);
            tabVehicles.Controls.Remove(source);
            var binding = new NativeInlineNumericBinding(source, tipTooltip, wholeNumbers: true);
            AutomationProperties.SetLabeledBy(binding.Input, heading);
            binding.Input.ClearValue(NumericUpDown.FontSizeProperty);
            row.Children.Add(NativeControlPresentation.CreateLabeledField(row, heading, binding.Input));
            Disposed += (_, _) =>
            {
                binding.Dispose();
                source.Dispose();
            };
            return binding;
        }
    }
}
