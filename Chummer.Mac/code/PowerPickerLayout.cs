#nullable enable
using System;
using System.Linq;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Forms = Majorsilence.Forms;

namespace Chummer;

public partial class frmSelectPower
{
    private void InitializeNativeLayout()
    {
        if (Application.Current == null)
            return;

        var controllers = Controls.Cast<Forms.Control>().ToArray();
        foreach (var controller in controllers)
            Controls.Remove(controller);

        var list = NativeListSelection.Attach(this, lstPowers, Text, lstPowers_DoubleClick, layoutManaged: true)!;
        list.ClearValue(ListBox.FontSizeProperty);
        var source = new Button
        {
            Name = "NativePowerSource", HorizontalAlignment = HorizontalAlignment.Left,
            ContentTemplate = new Avalonia.Controls.Templates.FuncDataTemplate<string>((text, _) => Label(text ?? ""))
        };
        source.Click += (_, _) => lblSource_Click(lblSource, EventArgs.Empty);
        var points = Label("");
        points.Name = "NativePowerPoints";
        void RefreshDetails(object? sender, EventArgs args)
        {
            bool selected = lstPowers.SelectedIndex >= 0;
            points.Text = selected ? lblPowerPoints.Text : "";
            source.Content = selected ? lblSource.Text : "";
            source.IsEnabled = selected && !string.IsNullOrWhiteSpace(lblSource.Text) && !lblSource.Text.StartsWith("[", StringComparison.Ordinal);
            AutomationProperties.SetName(source, lblSourceLabel.Text + " " + lblSource.Text);
            ToolTip.SetTip(source, tipTooltip.GetToolTip(lblSource));
        }
        lblSource.TextChanged += RefreshDetails;
        list.SelectionChanged += (_, _) => RefreshDetails(null, EventArgs.Empty);
        RefreshDetails(null, EventArgs.Empty);

        var search = new TextBox { Name = "NativePowerSearch", Text = txtSearch.Text };
        AutomationProperties.SetName(search, lblSearchLabel.Text);
        search.TextChanged += (_, _) => txtSearch.Text = search.Text ?? "";
        search.AddHandler(InputElement.KeyDownEvent, (_, args) =>
        {
            if (args.Key is not (Key.Down or Key.Up))
                return;

            txtSearch_KeyDown(txtSearch, new Forms.KeyEventArgs(args.Key == Key.Down ? Forms.Keys.Down : Forms.Keys.Up));
            args.Handled = true;
        }, Avalonia.Interactivity.RoutingStrategies.Tunnel);
        var searchRow = new StackPanel { Spacing = 4, Children = { Label(lblSearchLabel.Text), search } };

        var details = new StackPanel
        {
            Spacing = 6, Children =
            {
                Label(lblPowerPointsLabel.Text),
                points,
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
            string caption = command == cmdOKAdd
                ? LanguageManager.Instance.GetString("Button_PowerPicker_AddMore")
                : command.Text.Replace("&", "");
            var button = new Button { Name = "Native" + command.Name, Content = Label(caption) };
            AutomationProperties.SetName(button, caption);
            button.Click += (_, _) => command.PerformClick();
            actions.Children.Add(button);
        }
        NativeControlPresentation.ConstrainWrapItems(actions);
        var layout = new Grid { Margin = new Thickness(12), RowDefinitions = new RowDefinitions("Auto,*,Auto"), RowSpacing = 10 };
        layout.Children.Add(searchRow);
        Grid.SetRow(panes, 1);
        layout.Children.Add(panes);
        Grid.SetRow(actions, 2);
        layout.Children.Add(actions);
        var surface = new Border { Child = layout };
        NativeControlPresentation.AddTheme(surface.Styles);
        var host = new Forms.NativeControlHost { Name = "NativePowerPicker", Dock = Forms.DockStyle.Fill, NativeControl = surface };
        Controls.Add(host);
        host.SizeChanged += (_, _) => host.SyncNativeControl();
        FormBorderStyle = Forms.FormBorderStyle.Sizable;
        MinimumSize = new System.Drawing.Size(300, 300);
        ClientSize = new System.Drawing.Size(620, 400);
        Disposed += (_, _) =>
        {
            lblSource.TextChanged -= RefreshDetails;
            host.NativeControl = null;
            foreach (var controller in controllers)
                controller.Dispose();
        };

        static TextBlock Label(string text) => new() { Text = text, TextWrapping = TextWrapping.Wrap };
    }
}
