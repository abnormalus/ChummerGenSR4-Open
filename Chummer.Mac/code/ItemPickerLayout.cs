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

public partial class frmSelectItem
{
    private void InitializeNativeLayout()
    {
        if (Application.Current == null || DialogResult != Forms.DialogResult.None)
            return;

        var controllers = Controls.Cast<Forms.Control>().ToArray();
        foreach (var controller in controllers)
            Controls.Remove(controller);

        var choice = NativeListSelection.Attach(this, cboAmmo, lblAmmoLabel.Text,
            fitPopupToItems: true, layoutManaged: true)!;
        var description = Label(lblDescription.Text);
        description.Name = "NativeItemDescription";
        description.IsVisible = !string.IsNullOrWhiteSpace(description.Text);
        void RefreshDescription(object? sender, EventArgs args)
        {
            description.Text = lblDescription.Text;
            description.IsVisible = !string.IsNullOrWhiteSpace(description.Text);
        }
        lblDescription.TextChanged += RefreshDescription;
        var fields = new StackPanel { Spacing = 6, Children = { description, Label(lblAmmoLabel.Text), choice } };
        var scroll = new ScrollViewer
        {
            Content = fields,
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled
        };
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
        layout.Children.Add(scroll);
        Grid.SetRow(actions, 1);
        layout.Children.Add(actions);
        var surface = new Border { Child = layout };
        NativeControlPresentation.AddTheme(surface.Styles);
        var host = new Forms.NativeControlHost { Name = "NativeItemPicker", Dock = Forms.DockStyle.Fill, NativeControl = surface };
        Controls.Add(host);
        host.SizeChanged += (_, _) => host.SyncNativeControl();
        FormBorderStyle = Forms.FormBorderStyle.Sizable;
        MinimumSize = new System.Drawing.Size(270, 165);
        ClientSize = new System.Drawing.Size(440, 225);
        Disposed += (_, _) =>
        {
            lblDescription.TextChanged -= RefreshDescription;
            host.NativeControl = null;
            foreach (var controller in controllers)
                controller.Dispose();
        };

        static TextBlock Label(string text) => new() { Text = text, TextWrapping = TextWrapping.Wrap };
    }
}
