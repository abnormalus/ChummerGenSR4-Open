#nullable enable
using System;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Forms = Majorsilence.Forms;

namespace Chummer;

public partial class frmCareer
{
    private Control ComposeImprovementTree(TreeView tree)
    {
        var choices = InventoryTreeLayout.Compose(this, tree, treImprovements,
            cmdAddImprovement, cmdEditImprovement, cmdDeleteImprovement, cmdAddImprovementGroup);
        Forms.Control[] controllers =
        [
            lblImprovementTypeLabel, lblImprovementType, lblImprovementValue, chkImprovementActive,
            cmdImprovementsEnableAll, cmdImprovementsDisableAll
        ];
        foreach (var controller in controllers)
            tabImprovements.Controls.Remove(controller);

        var type = new TextBlock { Name = "NativeImprovementType", TextWrapping = TextWrapping.Wrap };
        var value = new TextBlock { Name = "NativeImprovementValue", TextWrapping = TextWrapping.Wrap };
        var active = new CheckBox { Name = "NativeImprovementActive", Content = chkImprovementActive.Text };
        var group = new WrapPanel { ItemSpacing = 5, LineSpacing = 4 };
        foreach (var command in new[] { cmdImprovementsEnableAll, cmdImprovementsDisableAll })
        {
            var button = new Button
            {
                Name = "Native" + command.Name,
                Content = new TextBlock { Text = command.Text, TextWrapping = TextWrapping.Wrap }
            };
            button.Click += (_, _) => command.PerformClick();
            void Refresh()
            {
                button.IsVisible = command.Visible && treImprovements.SelectedNode != null;
                button.IsEnabled = command.Enabled;
            }
            var observer = new NativeControlObservation(command, Refresh);
            tree.SelectionChanged += (_, _) => Refresh();
            Refresh();
            group.Children.Add(button);
            Disposed += (_, _) => observer.Dispose();
        }
        var details = new StackPanel { Spacing = 6, Children = { type, value, active, group } };
        void RefreshDetails(object? sender, EventArgs args)
        {
            bool selected = treImprovements.SelectedNode is { Level: > 0 };
            type.Text = selected ? lblImprovementTypeLabel.Text + " " + lblImprovementType.Text : "";
            value.Text = selected ? lblImprovementValue.Text : "";
            type.IsVisible = value.IsVisible = active.IsVisible = selected;
            active.IsEnabled = chkImprovementActive.Enabled;
            active.IsChecked = chkImprovementActive.Checked;
        }
        var activeState = new NativeControlObservation(chkImprovementActive, () => RefreshDetails(null, EventArgs.Empty));
        active.IsCheckedChanged += (_, _) => chkImprovementActive.Checked = active.IsChecked == true;
        tree.SelectionChanged += (_, _) => RefreshDetails(null, EventArgs.Empty);
        lblImprovementType.TextChanged += RefreshDetails;
        lblImprovementValue.TextChanged += RefreshDetails;
        chkImprovementActive.CheckedChanged += RefreshDetails;
        RefreshDetails(null, EventArgs.Empty);
        Disposed += (_, _) =>
        {
            lblImprovementType.TextChanged -= RefreshDetails;
            lblImprovementValue.TextChanged -= RefreshDetails;
            chkImprovementActive.CheckedChanged -= RefreshDetails;
            activeState.Dispose();
            foreach (var controller in controllers)
                controller.Dispose();
        };
        var panes = NativePickerPanes.Create(choices, new ScrollViewer
        {
            Content = details, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            BringIntoViewOnFocusChange = true
        });
        NativeControlPresentation.AddTheme(panes.Styles);
        return panes;
    }
}
