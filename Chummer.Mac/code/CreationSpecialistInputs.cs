#nullable enable
using System;
using Avalonia.Automation;
using Avalonia.Controls;
using Forms = Majorsilence.Forms;

namespace Chummer;

public partial class frmCreate
{
    private void InitializeCreationSpecialistInputs()
    {
        if (_packsWorkspace)
            return;
        CommonMagicAllocation.Attach(this, lblMysticAdeptAssignment, lblMysticAdeptAssignmentMagician,
            nudMysticAdeptMAGMagician, lblMysticAdeptAssignmentAdept, lblMysticAdeptMAGAdept, tipTooltip, CreationMagicBounds, lblRatingLabel);
        CommonAiAttributes.Attach(this, tipTooltip,
            [(lblRatingLabel, lblRating), (lblSystemLabel, lblSystem), (lblFirewallLabel, lblFirewall)],
            [(lblResponseLabel, nudResponse), (lblSignalLabel, nudSignal)]);

    }

    private void AddCreationArmorDetails(StackPanel rows)
    {
        rows.Children.Add(GearReadoutsLayout.BindReadout(this, tabArmor, lblArmorBallisticLabel, lblArmorBallistic));
        rows.Children.Add(GearReadoutsLayout.BindReadout(this, tabArmor, lblArmorImpactLabel, lblArmorImpact));

        tabArmor.Controls.Remove(nudArmorRating);
        var title = TechnomancerPersonaLayout.BindLabel(this, tabArmor, lblArmorRatingLabel);
        var binding = new NativeInlineNumericBinding(nudArmorRating, tipTooltip, wholeNumbers: true);
        binding.Input.ClearValue(NumericUpDown.FontSizeProperty);
        AutomationProperties.SetLabeledBy(binding.Input, title);
        var rating = new WrapPanel { ItemSpacing = 8, LineSpacing = 4 };
        rating.Children.Add(NativeControlPresentation.CreateLabeledField(rating, title, binding.Input));
        NativeControlPresentation.ConstrainWrapItems(rating);
        rating.Children.Add(GearReadoutsLayout.BindReadout(this, tabArmor, lblArmorAvailLabel, lblArmorAvail));
        rating.Children.Add(GearReadoutsLayout.BindReadout(this, tabArmor, lblArmorCostLabel, lblArmorCost));
        rows.Children.Add(rating);
        var capacity = new WrapPanel { ItemSpacing = 8, LineSpacing = 4 };
        capacity.Children.Add(GearReadoutsLayout.BindReadout(this, tabArmor, lblArmorCapacityLabel, lblArmorCapacity));
        capacity.Children.Add(GearReadoutsLayout.BindOption(this, tabArmor, chkArmorBlackMarketDiscount));
        rows.Children.Add(capacity);
        Disposed += (_, _) =>
        {
            binding.Dispose();
            nudArmorRating.Dispose();
        };
    }
}
