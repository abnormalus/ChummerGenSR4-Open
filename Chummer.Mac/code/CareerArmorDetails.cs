using Avalonia.Controls;

namespace Chummer;

public partial class frmCareer
{
    private void AddCareerArmorDetails(StackPanel rows)
    {
        var ballistic = new WrapPanel { Name = "NativeArmorBallisticRow", ItemSpacing = 8, LineSpacing = 4 };
        ballistic.Children.Add(GearReadoutsLayout.BindReadout(this, tabArmor, lblArmorBallisticLabel, lblArmorBallistic));
        ballistic.Children.Add(ArmorEquipmentLayout.BindCommand(this, tabArmor, cmdArmorBIncrease, tipTooltip.GetToolTip(cmdArmorBIncrease)));
        ballistic.Children.Add(ArmorEquipmentLayout.BindCommand(this, tabArmor, cmdArmorBDecrease, tipTooltip.GetToolTip(cmdArmorBDecrease)));
        var impact = new WrapPanel { ItemSpacing = 8, LineSpacing = 4 };
        impact.Children.Add(GearReadoutsLayout.BindReadout(this, tabArmor, lblArmorImpactLabel, lblArmorImpact));
        impact.Children.Add(ArmorEquipmentLayout.BindCommand(this, tabArmor, cmdArmorIIncrease, tipTooltip.GetToolTip(cmdArmorIIncrease)));
        impact.Children.Add(ArmorEquipmentLayout.BindCommand(this, tabArmor, cmdArmorIDecrease, tipTooltip.GetToolTip(cmdArmorIDecrease)));
        var rating = new WrapPanel { ItemSpacing = 12, LineSpacing = 4 };
        rating.Children.Add(GearReadoutsLayout.BindReadout(this, tabArmor, lblArmorRatingLabel, lblArmorRating));
        rating.Children.Add(GearReadoutsLayout.BindReadout(this, tabArmor, lblArmorAvailLabel, lblArmorAvail));
        rating.Children.Add(GearReadoutsLayout.BindReadout(this, tabArmor, lblArmorCostLabel, lblArmorCost));
        rows.Children.Add(ballistic);
        rows.Children.Add(impact);
        rows.Children.Add(rating);
        rows.Children.Add(GearReadoutsLayout.BindReadout(this, tabArmor, lblArmorCapacityLabel, lblArmorCapacity));
    }
}
