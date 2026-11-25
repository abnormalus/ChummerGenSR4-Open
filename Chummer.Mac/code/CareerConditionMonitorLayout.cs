#nullable enable
using System;
using System.Linq;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Forms = Majorsilence.Forms;

namespace Chummer;

public partial class frmCareer
{
    private void InitializeConditionMonitorLayout()
    {
        if (Application.Current == null)
            return;

        var controllers = tabConditionMonitor.Controls.Cast<Forms.Control>().ToArray();
        foreach (var controller in controllers)
            tabConditionMonitor.Controls.Remove(controller);

        var content = new StackPanel { Spacing = 8, Margin = new Thickness(8) };
        foreach (var (caption, value) in new[]
        {
            (lblCMPenaltyLabel, lblCMPenalty), (lblCMBallisticArmorLabel, lblCMBallisticArmor),
            (lblCMImpactArmorLabel, lblCMImpactArmor), (lblCMDamageResistancePoolLabel, lblCMDamageResistancePool)
        })
        {
            var text = Label("");
            void Update(object? sender, EventArgs args) => text.Text = caption.Text + " " + value.Text;
            value.TextChanged += Update;
            Update(null, EventArgs.Empty);
            content.Children.Add(text);
            Disposed += (_, _) => value.TextChanged -= Update;
        }
        content.Children.Add(Label(lblPhysicalCMLabel.Text));
        content.Children.Add(CreateDamageTrack(panPhysicalCM, lblPhysicalCMLabel.Text, physical: true));
        content.Children.Add(Label(lblStunCMLabel.Text));
        content.Children.Add(CreateDamageTrack(panStunCM, lblStunCMLabel.Text, physical: false));
        content.Children.Add(Label(lblEDGInfo.Text));
        var actions = new WrapPanel { ItemSpacing = 5, LineSpacing = 4 };
        foreach (var command in new[] { cmdEdgeGained, cmdEdgeSpent })
        {
            var button = new Button { Name = "Native" + command.Name, Content = Label(command == cmdEdgeGained ? "+" : "−") };
            string tip = tipTooltip.GetToolTip(command);
            AutomationProperties.SetName(button, string.IsNullOrEmpty(tip) ? command.Text : tip);
            ToolTip.SetTip(button, tip);
            button.Click += (_, _) => command.PerformClick();
            void Refresh() => button.IsEnabled = command.Enabled;
            var state = new NativeControlObservation(command, Refresh);
            Refresh();
            actions.Children.Add(button);
            Disposed += (_, _) => state.Dispose();
        }
        content.Children.Add(actions);

        var scroll = new ScrollViewer { Content = content, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
        NativeControlPresentation.AddTheme(scroll.Styles);
        var presenter = new CreationRegionPresenter(this, tabConditionMonitor, scroll, "NativeCareerConditionMonitor",
            () => tabConditionMonitor.ClientRectangle, clipToSource: true);
        Disposed += (_, _) =>
        {
            presenter.Dispose();
            foreach (var controller in controllers)
                controller.Dispose();
        };

        static TextBlock Label(string text) => new() { Text = text, TextWrapping = TextWrapping.Wrap };
    }

    private Control ComposeVehicleTree(TreeView tree)
    {
        tabVehicles.Controls.Remove(panVehicleCM);
        var track = CreateDamageTrack(panVehicleCM, lblPhysicalCMLabel.Text, physical: false);
        var scroll = new ScrollViewer
        {
            Name = "NativeVehicleDamage", Content = track, MaxHeight = 104,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };
        var actions = InventoryTreeLayout.CreateCommands(this, treVehicles, cmdAddVehicle, cmdDeleteVehicle, cmdAddVehicleLocation);
        var layout = new Grid { RowDefinitions = new RowDefinitions("Auto,*,Auto"), RowSpacing = 4 };
        Grid.SetRow(tree, 1);
        layout.Children.Add(tree);
        Grid.SetRow(scroll, 2);
        layout.Children.Add(scroll);
        layout.Children.Add(actions);
        layout.SizeChanged += (_, _) =>
        {
            // Leave most of the visible pane available for selecting a vehicle.
            scroll.MaxHeight = Math.Min(104, layout.Bounds.Height / 3);
        };
        NativeControlPresentation.AddTheme(layout.Styles);
        void Refresh(object? sender, EventArgs args) => scroll.IsVisible = panVehicleCM.Visible;
        panVehicleCM.VisibleChanged += Refresh;
        Refresh(null, EventArgs.Empty);
        Disposed += (_, _) =>
        {
            panVehicleCM.VisibleChanged -= Refresh;
            panVehicleCM.Dispose();
        };
        return layout;
    }

    private WrapPanel CreateDamageTrack(Forms.Panel panel, string caption, bool physical)
    {
        var track = new WrapPanel { Name = "Native" + panel.Name, ItemSpacing = 4, LineSpacing = 4 };
        foreach (var box in panel.Controls.OfType<Forms.CheckBox>().OrderBy(box => Convert.ToInt32(box.Tag)))
        {
            int index = Convert.ToInt32(box.Tag);
            var toggle = new ToggleButton { Name = "Native" + box.Name, MinWidth = 29, MinHeight = 27 };
            string title = caption + " " + index;
            string overflowText = LanguageManager.Instance.GetString("String_SpellOverflowDamage");
            void Refresh()
            {
                toggle.IsVisible = box.Visible;
                toggle.IsEnabled = box.Enabled;
                toggle.IsChecked = box.Checked;
                toggle.Content = string.IsNullOrEmpty(box.Text) ? index.ToString() : box.Text;
                bool overflow = physical && index > _objCharacter.PhysicalCM;
                toggle.Opacity = overflow && !box.Checked ? 0.7 : 1;
                string description = overflow ? title + " · " + overflowText : title;
                if (!string.IsNullOrEmpty(box.Text))
                    description += " (" + box.Text + ")";
                AutomationProperties.SetName(toggle, description);
                ToolTip.SetTip(toggle, description);
            }
            var state = new NativeControlObservation(box, Refresh);
            void Changed(object? sender, EventArgs args) => Refresh();
            box.CheckedChanged += Changed;
            box.TextChanged += Changed;
            if (physical) lblCMPhysical.TextChanged += Changed;
            toggle.IsCheckedChanged += (_, _) => box.Checked = toggle.IsChecked == true;
            Refresh();
            track.Children.Add(toggle);
            Disposed += (_, _) =>
            {
                state.Dispose();
                box.CheckedChanged -= Changed;
                box.TextChanged -= Changed;
                if (physical) lblCMPhysical.TextChanged -= Changed;
            };
        }
        return track;
    }
}
