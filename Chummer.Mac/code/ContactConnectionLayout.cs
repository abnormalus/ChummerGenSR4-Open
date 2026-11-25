#nullable enable
using System;
using System.Linq;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Forms = Majorsilence.Forms;

namespace Chummer;

public partial class frmSelectContactConnection
{
    private void InitializeNativeLayout()
    {
        if (Application.Current == null)
            return;

        var controllers = Controls.Cast<Forms.Control>().ToArray();
        foreach (var controller in controllers)
            Controls.Remove(controller);

        var rows = new StackPanel { Spacing = 8 };
        var groupName = new NativeInlineTextBinding(txtGroupName, () => true);
        groupName.Input.Name = "NativeGroupName";
        groupName.Input.ClearValue(TextBox.FontSizeProperty);
        groupName.Input.ClearValue(TextBox.MinHeightProperty);
        AddField(lblGroupNameLabel.Text, groupName.Input);

        foreach (var (label, source) in new[]
        {
            (lblMembershipLabel, cboMembership),
            (lblAreaOfInfluenceLabel, cboAreaOfInfluence),
            (lblMagicalResourcesLabel, cboMagicalResources),
            (lblMatrixResourcesLabel, cboMatrixResources)
        })
        {
            var choice = NativeListSelection.Attach(this, source, label.Text, fitPopupToItems: true, layoutManaged: true)!;
            AddField(label.Text, choice);
        }

        var modifier = new TextBlock { Name = "NativeConnectionModifier", TextWrapping = TextWrapping.Wrap };
        void RefreshModifier(object? sender, EventArgs args) =>
            modifier.Text = lblTotalConnectionModifierLabel.Text + " " + lblTotalConnectionModifier.Text;
        lblTotalConnectionModifier.TextChanged += RefreshModifier;
        RefreshModifier(null, EventArgs.Empty);
        rows.Children.Add(modifier);

        var free = new CheckBox
        {
            Name = "NativeFreeContact",
            Content = new TextBlock { Text = chkFreeContact.Text, TextWrapping = TextWrapping.Wrap },
            IsChecked = chkFreeContact.Checked
        };
        void RefreshFree(object? sender, EventArgs args) => free.IsChecked = chkFreeContact.Checked;
        chkFreeContact.CheckedChanged += RefreshFree;
        free.IsCheckedChanged += (_, _) => chkFreeContact.Checked = free.IsChecked == true;
        rows.Children.Add(free);

        var swatch = new Border { Width = 16, Height = 16, VerticalAlignment = VerticalAlignment.Center };
        void RefreshColor(object? sender, EventArgs args)
        {
            var value = cmdChangeColour.BackColor;
            swatch.Background = new SolidColorBrush(Color.FromArgb(value.A, value.R, value.G, value.B));
        }
        cmdChangeColour.BackColorChanged += RefreshColor;
        RefreshColor(null, EventArgs.Empty);
        var colorContent = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*"), ColumnSpacing = 8,
            Children = { swatch }
        };
        var colorButton = CreateCommand(cmdChangeColour, colorContent);
        colorButton.HorizontalContentAlignment = HorizontalAlignment.Stretch;
        rows.Children.Add(colorButton);

        var scroll = new ScrollViewer
        {
            Content = rows,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            AllowAutoHide = false
        };
        var actions = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Right, ItemSpacing = 8, LineSpacing = 4 };
        actions.Children.Add(CreateCommand(cmdCancel));
        actions.Children.Add(CreateCommand(cmdOK));
        NativeControlPresentation.ConstrainWrapItems(actions);
        var layout = new Grid { Margin = new Thickness(12), RowDefinitions = new RowDefinitions("*,Auto"), RowSpacing = 8 };
        layout.Children.Add(scroll);
        Grid.SetRow(actions, 1);
        layout.Children.Add(actions);
        var container = new Border { Child = layout };
        NativeControlPresentation.AddTheme(container.Styles);
        var host = new Forms.NativeControlHost { Name = "NativeContactConnection", Dock = Forms.DockStyle.Fill, NativeControl = container };
        Controls.Add(host);
        host.SizeChanged += (_, _) => host.SyncNativeControl();
        host.LocationChanged += (_, _) => host.SyncNativeControl();
        FormBorderStyle = Forms.FormBorderStyle.Sizable;
        MinimumSize = new System.Drawing.Size(280, 260);
        ClientSize = new System.Drawing.Size(420, 480);
        Disposed += (_, _) =>
        {
            host.NativeControl = null;
            groupName.Dispose();
            chkFreeContact.CheckedChanged -= RefreshFree;
            cmdChangeColour.BackColorChanged -= RefreshColor;
            lblTotalConnectionModifier.TextChanged -= RefreshModifier;
            foreach (var controller in controllers)
                controller.Dispose();
        };

        void AddField(string label, Control input)
        {
            AutomationProperties.SetName(input, label);
            rows.Children.Add(new StackPanel
            {
                Spacing = 4, Children = { new TextBlock { Text = label, TextWrapping = TextWrapping.Wrap }, input }
            });
        }

        Button CreateCommand(Forms.Button source, Grid? content = null)
        {
            var caption = new TextBlock { TextWrapping = TextWrapping.Wrap };
            if (content != null)
            {
                Grid.SetColumn(caption, 1);
                content.Children.Add(caption);
            }
            var button = new Button { Name = "Native" + source.Name, Content = (Control?)content ?? caption };
            void RefreshCaption(object? sender, EventArgs args)
            {
                caption.Text = source.Text;
                AutomationProperties.SetName(button, caption.Text);
            }
            source.TextChanged += RefreshCaption;
            Disposed += (_, _) => source.TextChanged -= RefreshCaption;
            RefreshCaption(null, EventArgs.Empty);
            button.Click += (_, _) => source.PerformClick();
            return button;
        }
    }
}
