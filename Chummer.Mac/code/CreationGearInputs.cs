#nullable enable
using System;
using Avalonia.Automation;
using Avalonia.Controls;
using Forms = Majorsilence.Forms;

namespace Chummer;

public partial class frmCreate
{
    private NativeInlineNumericBinding? gearRatingInput;
    private NativeInlineNumericBinding? gearQuantityInput;

    public void RefreshSelectedGear()
    {
        RefreshSelectedGearDetails();
        gearRatingInput?.Refresh();
        gearQuantityInput?.Refresh();
    }

    private StackPanel CreateGearDetails()
    {
        var ratingRow = new WrapPanel { ItemSpacing = 12, LineSpacing = 4 };
        var capacityRow = new WrapPanel { ItemSpacing = 12, LineSpacing = 4 };
        var quantityRow = new WrapPanel { ItemSpacing = 8, LineSpacing = 4 };
        gearRatingInput = Attach(ratingRow, nudGearRating, lblGearRatingLabel);
        ratingRow.Children.Add(GearReadoutsLayout.BindReadout(this, tabGear, lblGearAvailLabel, lblGearAvail));
        ratingRow.Children.Add(GearReadoutsLayout.BindReadout(this, tabGear, lblGearCostLabel, lblGearCost));
        capacityRow.Children.Add(GearReadoutsLayout.BindReadout(this, tabGear, lblGearCapacityLabel, lblGearCapacity));
        capacityRow.Children.Add(GearReadoutsLayout.BindOption(this, tabGear, chkGearBlackMarketDiscount));
        gearQuantityInput = Attach(quantityRow, nudGearQty, lblGearQtyLabel);

        var rows = new StackPanel
        {
            Spacing = 4,
            Children =
            {
                GearReadoutsLayout.BindReadout(this, tabGear, lblGearNameLabel, lblGearName),
                GearReadoutsLayout.BindReadout(this, tabGear, lblGearCategoryLabel, lblGearCategory),
                ratingRow, capacityRow, quantityRow
            }
        };
        GearReadoutsLayout.AddReadouts(this, tabGear, rows, lblGearSourceLabel, lblGearSource, tipTooltip,
            () => lblGearSource_Click(lblGearSource, EventArgs.Empty),
            [(lblGearResponseLabel, lblGearResponse), (lblGearSignalLabel, lblGearSignal),
             (lblGearSystemLabel, lblGearSystem), (lblGearFirewallLabel, lblGearFirewall)],
            [(lblGearDamageLabel, lblGearDamage), (lblGearAPLabel, lblGearAP)]);
        GearReadoutsLayout.AddOptions(this, tabGear, rows, chkGearEquipped, chkGearHomeNode, chkActiveCommlink);
        return rows;

        NativeInlineNumericBinding Attach(WrapPanel row, Forms.NumericUpDown source, Forms.Label label)
        {
            tabGear.Controls.Remove(source);
            var heading = TechnomancerPersonaLayout.BindLabel(this, tabGear, label);
            var binding = new NativeInlineNumericBinding(source, tipTooltip, wholeNumbers: true);
            binding.Input.ClearValue(NumericUpDown.FontSizeProperty);
            AutomationProperties.SetLabeledBy(binding.Input, heading);
            row.Children.Add(NativeControlPresentation.CreateLabeledField(row, heading, binding.Input));
            NativeControlPresentation.ConstrainWrapItems(row);
            Disposed += (_, _) =>
            {
                binding.Dispose();
                source.Dispose();
            };
            return binding;
        }
    }
}
