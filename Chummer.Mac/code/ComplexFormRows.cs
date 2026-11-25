using Avalonia.Automation;
using Avalonia.Controls;

namespace Chummer;

public partial class frmCreate
{
    private WrapPanel CreateComplexFormRating() => TechnomancerPersonaLayout.CreateRatingRow(this,
        tabTechnomancer, lblComplexFormRatingLabel, nudComplexFormRating, tipTooltip);

    private void AddComplexFormRows(StackPanel rows)
    {
        rows.Children.Add(TechnomancerPersonaLayout.CreateStreamRow(this, tabTechnomancer, lblStreamLabel, cboStream));
        rows.Children.Add(TechnomancerPersonaLayout.CreateFadingRow(this, tabTechnomancer,
            lblFadingAttributesLabel, lblFadingAttributes, lblFadingAttributesValue));
    }
}

public partial class frmCareer
{
    private WrapPanel CreateComplexFormRating()
    {
        var row = TechnomancerPersonaLayout.CreateRatingRow(this, tabTechnomancer,
            lblComplexFormRatingLabel, nudComplexFormRating, tipTooltip, lblComplexFormRating);
        var improve = ArmorEquipmentLayout.BindCommand(this, tabTechnomancer, cmdImproveComplexForm,
            LanguageManager.Instance.GetString("Label_Options_ImproveComplexForm"));
        improve.PointerEntered += (_, _) => ToolTip.SetTip(improve, tipTooltip.GetToolTip(cmdImproveComplexForm));
        row.Children.Add(improve);
        return row;
    }

    private void AddComplexFormRows(StackPanel rows)
    {
        var skill = NativeSelectionInput.Bind(this, tabTechnomancer, cboComplexFormSkill);
        var attribute = NativeSelectionInput.Bind(this, tabTechnomancer, cboComplexFormAttribute);
        AutomationProperties.SetName(skill, lblComplexFormSkillLabel.Text);
        AutomationProperties.SetName(attribute, LanguageManager.Instance.GetString("String_ExpenseAttribute"));
        var roll = ArmorEquipmentLayout.BindCommand(this, tabTechnomancer, cmdRollComplexForm,
            tipTooltip.GetToolTip(cmdRollComplexForm));
        var dicePool = GearReadoutsLayout.BindReadout(this, tabTechnomancer, lblComplexFormDicePoolLabel, lblComplexFormDicePool);
        rows.Children.Add(new WrapPanel { ItemSpacing = 8, LineSpacing = 4, Children = { skill, attribute, dicePool, roll } });
        rows.Children.Add(TechnomancerPersonaLayout.CreateStreamRow(this, tabTechnomancer, lblStreamLabel, cboStream));
        var fading = TechnomancerPersonaLayout.CreateFadingRow(this, tabTechnomancer,
            lblFadingAttributesLabel, lblFadingAttributes, lblFadingAttributesValue);
        fading.Children.Add(ArmorEquipmentLayout.BindCommand(this, tabTechnomancer, cmdRollFading,
            tipTooltip.GetToolTip(cmdRollFading)));
        rows.Children.Add(fading);
    }
}
