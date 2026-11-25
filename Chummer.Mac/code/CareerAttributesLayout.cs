#nullable enable
using System;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Forms = Majorsilence.Forms;

namespace Chummer;

public partial class frmCareer
{
    private void InitializeCareerAttributesLayout()
    {
        var workspace = MdiParent ?? this;
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("2*,*,*,2*,Auto"), ColumnSpacing = 4, RowSpacing = 3 };
        AddCells(tabCommon, 0, lblAttributes, lblAttributesBase, lblAttributesAug, lblAttributesMetatype);
        var rows = new (Forms.Label Name, Forms.Label Base, Forms.Label Augmented, Forms.Label Limits, Forms.Button Improve)[]
        {
            (lblBODLabel, lblBOD, lblBODAug, lblBODMetatype, cmdImproveBOD),
            (lblAGILabel, lblAGI, lblAGIAug, lblAGIMetatype, cmdImproveAGI),
            (lblREALabel, lblREA, lblREAAug, lblREAMetatype, cmdImproveREA),
            (lblSTRLabel, lblSTR, lblSTRAug, lblSTRMetatype, cmdImproveSTR),
            (lblCHALabel, lblCHA, lblCHAAug, lblCHAMetatype, cmdImproveCHA),
            (lblINTLabel, lblINT, lblINTAug, lblINTMetatype, cmdImproveINT),
            (lblLOGLabel, lblLOG, lblLOGAug, lblLOGMetatype, cmdImproveLOG),
            (lblWILLabel, lblWIL, lblWILAug, lblWILMetatype, cmdImproveWIL),
            (lblEDGLabel, lblEDG, lblEDGAug, lblEDGMetatype, cmdImproveEDG),
            (lblMAGLabel, lblMAG, lblMAGAug, lblMAGMetatype, cmdImproveMAG),
            (lblRESLabel, lblRES, lblRESAug, lblRESMetatype, cmdImproveRES)
        };
        for (int index = 0; index < rows.Length; index++)
        {
            var (name, value, augmented, limits, improve) = rows[index];
            int row = index + 1;
            AddCells(panAttributes, row, name, value, augmented, limits);
            var actions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 3 };
            actions.Children.Add(BindCommand(improve, "+", name.Text));
            if (ReferenceEquals(improve, cmdImproveEDG))
                actions.Children.Add(BindCommand(cmdBurnEdge, "−", name.Text));
            Grid.SetRow(actions, row);
            Grid.SetColumn(actions, 4);
            grid.Children.Add(actions);
        }

        var scroll = new ScrollViewer
        {
            Content = grid,
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled
        };
        NativeControlPresentation.AddTheme(scroll.Styles);
        panAttributes.AutoScroll = false;
        var presenter = new CreationRegionPresenter(this, panAttributes, scroll, "NativeCareerAttributes", () =>
            new System.Drawing.Rectangle(System.Drawing.Point.Empty, InventoryTreeLayout.VisiblePageSize(workspace, panAttributes)),
            clipToSource: true);
        Disposed += (_, _) => presenter.Dispose();

        void AddCells(Forms.Control parent, int row, params Forms.Label[] labels)
        {
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            for (int column = 0; column < labels.Length; column++)
            {
                var source = labels[column];
                var text = TechnomancerPersonaLayout.BindLabel(this, parent, source, row == 0 ? FontWeight.SemiBold : null);
                text.Name = "Native" + source.Name;
                text.VerticalAlignment = VerticalAlignment.Center;
                text.PointerEntered += (_, _) => ToolTip.SetTip(text, tipTooltip.GetToolTip(source));
                Grid.SetRow(text, row);
                Grid.SetColumn(text, column);
                grid.Children.Add(text);
            }
        }

        Button BindCommand(Forms.Button source, string caption, string attribute)
        {
            var button = ArmorEquipmentLayout.BindCommand(this, panAttributes, source, caption);
            void RefreshTip()
            {
                string tip = tipTooltip.GetToolTip(source);
                ToolTip.SetTip(button, tip);
                Avalonia.Automation.AutomationProperties.SetName(button, attribute + " " + tip);
            }
            button.PointerEntered += (_, _) => RefreshTip();
            RefreshTip();
            return button;
        }
    }
}
