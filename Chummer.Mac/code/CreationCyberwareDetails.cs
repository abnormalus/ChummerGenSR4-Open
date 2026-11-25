#nullable enable
using System;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Forms = Majorsilence.Forms;

namespace Chummer;

public partial class frmCreate
{
    private ScrollViewer CreateCreationCyberwareDetails()
    {
        var rows = new StackPanel { Spacing = 7 };
        var item = new StackPanel { Spacing = 7 };
        AddRow((lblCyberwareNameLabel, lblCyberwareName));
        AddRow((lblCyberwareCategoryLabel, lblCyberwareCategory));

        var grade = NativeSelectionInput.Bind(this, tabCyberware, cboCyberwareGrade);
        var gradeTitle = DetachCaption(lblCyberwareGradeLabel, grade);

        tabCyberware.Controls.Remove(nudCyberwareRating);
        var binding = new NativeInlineNumericBinding(nudCyberwareRating, tipTooltip, wholeNumbers: true);
        binding.Input.ClearValue(NumericUpDown.FontSizeProperty);
        var ratingTitle = DetachCaption(lblCyberwareRatingLabel, binding.Input);
        var adjustments = new WrapPanel { ItemSpacing = 8, LineSpacing = 4 };
        adjustments.Children.Add(NativeControlPresentation.CreateLabeledField(adjustments, gradeTitle, grade));
        adjustments.Children.Add(NativeControlPresentation.CreateLabeledField(adjustments, ratingTitle, binding.Input));
        NativeControlPresentation.ConstrainWrapItems(adjustments);
        item.Children.Add(adjustments);
        AddRow((lblCyberwareEssenceLabel, lblCyberwareEssence), (lblCyberwareCapacityLabel, lblCyberwareCapacity));
        AddRow((lblCyberwareAvailLabel, lblCyberwareAvail), (lblCyberwareCostLabel, lblCyberwareCost));
        var source = new WrapPanel { ItemSpacing = 8, LineSpacing = 4 };
        source.Children.Add(GearReadoutsLayout.BindSource(this, tabCyberware, "NativeCyberwareSource",
            lblCyberwareSourceLabel, lblCyberwareSource, tipTooltip,
            () => lblCyberwareSource_Click(lblCyberwareSource, EventArgs.Empty)));
        source.Children.Add(GearReadoutsLayout.BindOption(this, tabCyberware, chkCyberwareBlackMarketDiscount));
        NativeControlPresentation.ConstrainWrapItems(source);
        item.Children.Add(source);
        GearReadoutsLayout.ShowForSelection(this, item, treCyberware);
        rows.Children.Add(item);

        tabCyberware.Controls.Remove(label1);
        rows.Children.Add(new TextBlock { Text = label1.Text, TextWrapping = TextWrapping.Wrap,
            FontWeight = FontWeight.SemiBold });
        AddTotals((lblCyberwareESSLabel, lblCyberwareESS));
        AddTotals((lblBiowareESSLabel, lblBiowareESS));
        AddTotals((lblEssenceHoleESSLabel, lblEssenceHoleESS));

        Disposed += (_, _) =>
        {
            binding.Dispose();
            nudCyberwareRating.Dispose();
            label1.Dispose();
        };
        return new ScrollViewer
        {
            Name = "NativeCreationCyberwareDetails", Content = rows,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            BringIntoViewOnFocusChange = true
        };

        void AddRow(params (Forms.Label Title, Forms.Label Value)[] fields) => AddReadouts(item, fields);

        void AddTotals((Forms.Label Title, Forms.Label Value) field) => AddReadouts(rows, [field]);

        void AddReadouts(StackPanel target, (Forms.Label Title, Forms.Label Value)[] fields)
        {
            var row = new WrapPanel { ItemSpacing = 12, LineSpacing = 4 };
            foreach (var (title, value) in fields)
                row.Children.Add(GearReadoutsLayout.BindReadout(this, tabCyberware, title, value));
            NativeControlPresentation.ConstrainWrapItems(row);
            target.Children.Add(row);
        }

        TextBlock DetachCaption(Forms.Label label, Control input)
        {
            var caption = TechnomancerPersonaLayout.BindLabel(this, tabCyberware, label);
            AutomationProperties.SetLabeledBy(input, caption);
            return caption;
        }
    }
}
