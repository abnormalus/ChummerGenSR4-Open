#nullable enable
using System;
using System.Linq;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Forms = Majorsilence.Forms;

namespace Chummer;

public partial class frmMetatype
{
    private void InitializeNativeLayout()
    {
        if (Application.Current == null)
            return;

        var host = new Forms.NativeControlHost { Name = "NativeMetatypePicker", Dock = Forms.DockStyle.Fill };
        var controllers = Controls.Cast<Forms.Control>().ToArray();
        foreach (var controller in controllers)
            Controls.Remove(controller);

        var list = NativeListSelection.Attach(this, lstMetatypes, Text, lstMetatypes_DoubleClick, layoutManaged: true)!;
        list.ClearValue(ListBox.FontSizeProperty);
        var category = NativeListSelection.Attach(this, cboCategory, Text, layoutManaged: true)!;
        var selection = new Grid { RowDefinitions = new RowDefinitions("Auto,*"), RowSpacing = 4 };
        selection.Children.Add(category);
        Grid.SetRow(list, 1);
        selection.Children.Add(list);

        var details = new StackPanel { Spacing = 6 };
        foreach (var (caption, value) in new[]
        {
            (Label1, lblBP), (Label3, lblBOD), (Label5, lblAGI), (Label7, lblREA), (Label9, lblSTR),
            (Label11, lblCHA), (Label13, lblINT), (Label15, lblLOG), (Label17, lblWIL), (Label19, lblINI)
        })
            AddValue(caption, value);
        details.Children.Add(Label(label2.Text));
        details.Children.Add(NativeListSelection.Attach(this, cboMetavariant, label2.Text, layoutManaged: true)!);
        AddValue(label6, lblMetavariantBP);
        AddValue(label4, lblMetavariantQualities);

        var force = new NativeInlineNumericBinding(nudForce, tipTooltip, wholeNumbers: true);
        force.Input.ClearValue(Layoutable.WidthProperty);
        AutomationProperties.SetName(force.Input, lblForceLabel.Text);
        var forceRow = new StackPanel { Spacing = 4, Children = { Label(lblForceLabel.Text), force.Input } };
        void UpdateForce(object? sender, EventArgs args) => forceRow.IsVisible = nudForce.Visible;
        nudForce.VisibleChanged += UpdateForce;
        UpdateForce(null, EventArgs.Empty);
        details.Children.Add(forceRow);
        details.Children.Add(Option(chkBloodSpirit));
        details.Children.Add(Option(chkPossessionBased));
        details.Children.Add(NativeListSelection.Attach(this, cboPossessionMethod, chkPossessionBased.Text, layoutManaged: true)!);

        var panes = NativePickerPanes.Create(selection, new ScrollViewer
        {
            Content = details,
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled
        });
        var actions = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Right, ItemSpacing = 8, LineSpacing = 4 };
        foreach (var command in new[] { cmdCancel, cmdOK })
        {
            var button = new Button { Name = "Native" + command.Name, Content = Label(command.Text) };
            AutomationProperties.SetName(button, command.Text);
            button.Click += (_, _) => command.PerformClick();
            actions.Children.Add(button);
        }
        NativeControlPresentation.ConstrainWrapItems(actions);
        var layout = new Grid { Margin = new Thickness(12), RowDefinitions = new RowDefinitions("*,Auto"), RowSpacing = 10 };
        layout.Children.Add(panes);
        Grid.SetRow(actions, 1);
        layout.Children.Add(actions);
        var surface = new Border { Child = layout };
        NativeControlPresentation.AddTheme(surface.Styles);
        host.NativeControl = surface;
        Controls.Add(host);
        host.SizeChanged += (_, _) => host.SyncNativeControl();
        FormBorderStyle = Forms.FormBorderStyle.Sizable;
        MinimumSize = new System.Drawing.Size(300, 300);
        ClientSize = new System.Drawing.Size(680, 510);
        Disposed += (_, _) =>
        {
            nudForce.VisibleChanged -= UpdateForce;
            force.Dispose();
            host.NativeControl = null;
            foreach (var controller in controllers)
                controller.Dispose();
        };

        void AddValue(Forms.Label caption, Forms.Label value)
        {
            var text = Label("");
            text.Name = "Native" + value.Name;
            void Update(object? sender, EventArgs args) => text.Text = caption.Text + " " + value.Text;
            value.TextChanged += Update;
            Update(null, EventArgs.Empty);
            details.Children.Add(text);
            Disposed += (_, _) => value.TextChanged -= Update;
        }

        CheckBox Option(Forms.CheckBox controller)
        {
            var toggle = new CheckBox { Name = "Native" + controller.Name, Content = Label(controller.Text) };
            AutomationProperties.SetName(toggle, controller.Text);
            void Refresh()
            {
                toggle.IsChecked = controller.Checked;
                toggle.IsVisible = controller.Visible;
                toggle.IsEnabled = controller.Enabled;
            }
            var observer = new NativeControlObservation(controller, Refresh);
            void Changed(object? sender, EventArgs args) => Refresh();
            controller.CheckedChanged += Changed;
            toggle.IsCheckedChanged += (_, _) => controller.Checked = toggle.IsChecked == true;
            Refresh();
            Disposed += (_, _) => { observer.Dispose(); controller.CheckedChanged -= Changed; };
            return toggle;
        }

        static TextBlock Label(string text) => new() { Text = text, TextWrapping = TextWrapping.Wrap };
    }
}
