using System;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Forms = Majorsilence.Forms;

namespace Chummer;

public partial class frmCareer
{
    private ScrollViewer CreateCareerCyberwareDetails()
    {
        var rows = new StackPanel { Spacing = 7 };
        var item = new StackPanel { Spacing = 7 };
        AddRow((lblCyberwareNameLabel, lblCyberwareName));
        AddRow((lblCyberwareCategoryLabel, lblCyberwareCategory));
        AddRow((lblCyberwareGradeLabel, lblCyberwareGrade), (lblCyberwareRatingLabel, lblCyberwareRating));
        AddRow((lblCyberwareEssenceLabel, lblCyberwareEssence), (lblCyberwareCapacityLabel, lblCyberwareCapacity));
        AddRow((lblCyberwareAvailLabel, lblCyberwareAvail), (lblCyberwareCostLabel, lblCyberwareCost));
        item.Children.Add(GearReadoutsLayout.BindSource(this, tabCyberware, "NativeCyberwareSource",
            lblCyberwareSourceLabel, lblCyberwareSource, tipTooltip,
            () => lblCyberwareSource_Click(lblCyberwareSource, EventArgs.Empty)));
        AddRow((lblCyberwareResponseLabel, lblCyberwareResponse), (lblCyberwareSignalLabel, lblCyberwareSignal),
            (lblCyberwareSystemLabel, lblCyberwareSystem), (lblCyberwareFirewallLabel, lblCyberwareFirewall));
        item.Children.Add(GearReadoutsLayout.BindOption(this, tabCyberware, chkActiveCyberCommlink));

        GearReadoutsLayout.ShowForSelection(this, item, treCyberware);
        rows.Children.Add(item);

        tabCyberware.Controls.Remove(label2);
        rows.Children.Add(new TextBlock { Text = label2.Text, TextWrapping = TextWrapping.Wrap,
            FontWeight = FontWeight.SemiBold });
        Disposed += (_, _) => label2.Dispose();
        AddTotals((lblCyberwareESSLabel, lblCyberwareESS));
        AddTotals((lblBiowareESSLabel, lblBiowareESS));
        AddTotals((lblEssenceHoleESSLabel, lblEssenceHoleESS));

        return new ScrollViewer
        {
            Name = "NativeCareerCyberwareDetails",
            Content = rows,
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
    }
}
