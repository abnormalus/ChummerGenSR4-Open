#nullable enable
using System;
using System.Drawing;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Forms = Majorsilence.Forms;

namespace Chummer;

public partial class frmCareer
{
    private void InitializeCareerSkillGroups()
    {
        if (Avalonia.Application.Current == null)
            return;

        var pane = splitSkills.Panel1;
        var workspace = MdiParent ?? this;
        void Mount(object? sender, EventArgs args)
        {
            if (!pane.Visible || IsDisposed)
                return;

            pane.VisibleChanged -= Mount;
            Shown -= Mount;
            var heading = TechnomancerPersonaLayout.BindLabel(this, pane, lblSkillGroups);
            heading.Margin = new Avalonia.Thickness(0, 2, 0, 4);
            var rows = new StackPanel { Spacing = 3 };
            foreach (var controller in panSkillGroups.Controls.OfType<SkillGroupControl>())
            {
                controller.CreateControl();
                rows.Children.Add(controller.CreateCareerGroupRow());
            }
            panSkillGroups.AutoScroll = false;
            var scroll = new ScrollViewer { Content = rows, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled };
            NativeControlPresentation.AddTheme(scroll.Styles);
            var layout = new DockPanel { LastChildFill = true };
            DockPanel.SetDock(heading, Avalonia.Controls.Dock.Top);
            layout.Children.Add(heading);
            layout.Children.Add(scroll);
            NativeControlPresentation.AddTheme(layout.Styles);
            var presenter = new CreationRegionPresenter(this, pane, layout, "NativeCareerSkillGroups", () =>
            {
                var available = InventoryTreeLayout.VisiblePageSize(workspace, pane);
                int left = panSkillGroups.Left;
                return new Rectangle(left, 2,
                    Math.Max(0, Math.Min(panActiveSkills.Left - left - 6, available.Width - left)),
                    Math.Max(0, available.Height - 4));
            }, clipToSource: true);
            Disposed += (_, _) => presenter.Dispose();
        }

        pane.VisibleChanged += Mount;
        Shown += Mount;
        Disposed += (_, _) =>
        {
            pane.VisibleChanged -= Mount;
            Shown -= Mount;
        };
        Mount(null, EventArgs.Empty);
    }
}

public partial class SkillGroupControl
{
    internal Grid CreateCareerGroupRow()
    {
        var name = new TextBlock { TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center };
        var rating = new TextBlock { VerticalAlignment = VerticalAlignment.Center };
        var improve = ArmorEquipmentLayout.BindCommand(this, this, cmdImproveSkill, "+");
        Controls.Remove(txtGroupName);
        Controls.Remove(lblGroupRating);
        void Refresh(object? sender, EventArgs args)
        {
            name.Text = txtGroupName.Text;
            rating.Text = lblGroupRating.Text;
            name.Opacity = rating.Opacity = IsEnabled ? 1 : 0.5;
            Avalonia.Automation.AutomationProperties.SetName(improve, name.Text + " " + tipTooltip.GetToolTip(cmdImproveSkill));
            ToolTip.SetTip(name, tipTooltip.GetToolTip(txtGroupName));
            ToolTip.SetTip(improve, tipTooltip.GetToolTip(cmdImproveSkill));
        }
        txtGroupName.TextChanged += Refresh;
        lblGroupRating.TextChanged += Refresh;
        nudSkill.EnabledChanged += Refresh;
        improve.PointerEntered += (_, _) => Refresh(null, EventArgs.Empty);
        Refresh(null, EventArgs.Empty);

        var row = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto"), ColumnSpacing = 6 };
        Grid.SetColumn(rating, 1);
        Grid.SetColumn(improve, 2);
        row.Children.Add(name);
        row.Children.Add(rating);
        row.Children.Add(improve);
        Disposed += (_, _) =>
        {
            txtGroupName.TextChanged -= Refresh;
            lblGroupRating.TextChanged -= Refresh;
            nudSkill.EnabledChanged -= Refresh;
            txtGroupName.Dispose();
            lblGroupRating.Dispose();
        };
        return row;
    }
}
