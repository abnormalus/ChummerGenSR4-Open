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

public partial class frmSelectMetamagic
{
    private void InitializeNativeLayout()
    {
        if (Application.Current == null)
            return;

        var controllers = Controls.Cast<Forms.Control>().ToArray();
        foreach (var controller in controllers)
            Controls.Remove(controller);

        var list = NativeListSelection.Attach(this, lstMetamagic, Text, lstMetamagic_DoubleClick, layoutManaged: true)!;
        list.ClearValue(ListBox.FontSizeProperty);
        var source = new Button
        {
            Name = "NativeMetamagicSource", HorizontalAlignment = HorizontalAlignment.Left,
            ContentTemplate = new Avalonia.Controls.Templates.FuncDataTemplate<string>((text, _) => Label(text ?? ""))
        };
        source.Click += (_, _) => lblSource_Click(lblSource, EventArgs.Empty);
        void RefreshSource(object? sender, EventArgs args)
        {
            source.Content = lblSource.Text;
            source.IsEnabled = lstMetamagic.SelectedIndex >= 0 && !string.IsNullOrWhiteSpace(lblSource.Text) && !lblSource.Text.StartsWith("[", StringComparison.Ordinal);
            AutomationProperties.SetName(source, lblSourceLabel.Text + " " + lblSource.Text);
            ToolTip.SetTip(source, tipTooltip.GetToolTip(lblSource));
        }
        lblSource.TextChanged += RefreshSource;
        list.SelectionChanged += (_, _) => RefreshSource(null, EventArgs.Empty);
        RefreshSource(null, EventArgs.Empty);

        var limit = new CheckBox
        {
            Name = "NativeLimitList",
            Content = Label(chkLimitList.Text),
            IsChecked = chkLimitList.Checked
        };
        AutomationProperties.SetName(limit, chkLimitList.Text);
        limit.IsCheckedChanged += (_, _) => chkLimitList.Checked = limit.IsChecked == true;

        var details = new StackPanel
        {
            Spacing = 6, Children =
            {
                limit,
                Label(lblSourceLabel.Text),
                source
            }
        };
        var detailView = new ScrollViewer
        {
            Content = details,
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            BringIntoViewOnFocusChange = true
        };
        NativeControlPresentation.AddTheme(detailView.Styles);
        var panes = NativePickerPanes.Create(list, detailView);
        var actions = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Right, ItemSpacing = 8, LineSpacing = 4 };
        foreach (var command in new[] { cmdOKAdd, cmdCancel, cmdOK })
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
        var host = new Forms.NativeControlHost { Name = "NativeMetamagicPicker", Dock = Forms.DockStyle.Fill, NativeControl = surface };
        Controls.Add(host);
        host.SizeChanged += (_, _) => host.SyncNativeControl();
        FormBorderStyle = Forms.FormBorderStyle.Sizable;
        MinimumSize = new System.Drawing.Size(300, 300);
        ClientSize = new System.Drawing.Size(620, 400);
        Disposed += (_, _) =>
        {
            lblSource.TextChanged -= RefreshSource;
            host.NativeControl = null;
            foreach (var controller in controllers)
                controller.Dispose();
        };

        static TextBlock Label(string text) => new() { Text = text, TextWrapping = TextWrapping.Wrap };
    }
}
