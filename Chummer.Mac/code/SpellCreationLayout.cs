#nullable enable
using System;
using System.Linq;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Forms = Majorsilence.Forms;

namespace Chummer;

public partial class frmCreateSpell
{
    private void InitializeNativeLayout() => NativeCreationFormLayout.Attach(this, "NativeSpellCreation",
        [(lblName, txtName, "NativetxtName"), (lblCategory, cboCategory, "NativecboCategory"),
         (lblType, cboType, "NativecboType"), (lblRange, cboRange, "NativecboRange"),
         (lblDuration, cboDuration, "NativecboDuration")],
        cmdCancel, cmdOK, new System.Drawing.Size(560, 620), appendFields: AddSpellRules);

    private void AddSpellRules(StackPanel fields)
    {
        fields.Children.Add(Option(chkLimited));
        fields.Children.Add(Option(chkArea));
        fields.Children.Add(Option(chkRestricted));
        fields.Children.Add(Option(chkVeryRestricted));
        var restriction = new NativeInlineTextBinding(txtRestriction, () => true);
        AutomationProperties.SetName(restriction.Input, chkRestricted.Text);
        fields.Children.Add(restriction.Input);
        Disposed += (_, _) => restriction.Dispose();

        var drain = Label("");
        drain.Name = "NativeSpellDrain";
        void UpdateDrain(object? sender, EventArgs args) => drain.Text = lblDVLabel.Text + " " + lblDV.Text;
        lblDV.TextChanged += UpdateDrain;
        Disposed += (_, _) => lblDV.TextChanged -= UpdateDrain;
        UpdateDrain(null, EventArgs.Empty);
        fields.Children.Add(drain);
        fields.Children.Add(Label(lblSpellOptions.Text));

        var modifiers = panModifiers.Controls.OfType<Forms.CheckBox>().OrderBy(control => control.Top).ToArray();
        var rows = modifiers.Select(control =>
        {
            var row = new StackPanel { Spacing = 4, Children = { Option(control) } };
            void RefreshRow(object? sender, EventArgs args) => row.IsVisible = control.Visible;
            control.VisibleChanged += RefreshRow;
            Disposed += (_, _) => control.VisibleChanged -= RefreshRow;
            RefreshRow(null, EventArgs.Empty);
            return row;
        }).ToArray();
        foreach (var row in rows)
            fields.Children.Add(row);
        var effects = new NativeInlineNumericBinding(nudNumberOfEffects, null, wholeNumbers: true);
        effects.Input.ClearValue(Layoutable.WidthProperty);
        AutomationProperties.SetName(effects.Input, lblSpellOptions.Text);
        void MoveEffects(object? sender, EventArgs args)
        {
            int index = Array.IndexOf(modifiers, cboCategory.SelectedValue?.ToString() == "Manipulation" ? chkModifier6 : chkModifier3);
            var target = rows[index];
            if (!ReferenceEquals(effects.Input.Parent, target))
            {
                (effects.Input.Parent as Panel)?.Children.Remove(effects.Input);
                target.Children.Add(effects.Input);
            }
        }
        cboCategory.SelectedIndexChanged += MoveEffects;
        MoveEffects(null, EventArgs.Empty);
        Disposed += (_, _) =>
        {
            cboCategory.SelectedIndexChanged -= MoveEffects;
            effects.Dispose();
        };

        CheckBox Option(Forms.CheckBox controller)
        {
            var text = Label(controller.Text);
            var toggle = new CheckBox { Name = "Native" + controller.Name, Content = text };
            void Refresh()
            {
                text.Text = controller.Text;
                AutomationProperties.SetName(toggle, controller.Text);
                toggle.IsVisible = controller.Visible;
                toggle.IsEnabled = controller.Enabled;
                toggle.IsChecked = controller.Checked;
            }
            var observer = new NativeControlObservation(controller, Refresh);
            void Changed(object? sender, EventArgs args) => Refresh();
            controller.TextChanged += Changed;
            controller.CheckedChanged += Changed;
            toggle.IsCheckedChanged += (_, _) => controller.Checked = toggle.IsChecked == true;
            Disposed += (_, _) =>
            {
                observer.Dispose();
                controller.TextChanged -= Changed;
                controller.CheckedChanged -= Changed;
            };
            Refresh();
            return toggle;
        }

        static TextBlock Label(string text) => new() { Text = text, TextWrapping = TextWrapping.Wrap };
    }
}
