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

public partial class frmSelectMentorSpirit
{
    private void InitializeNativeLayout()
    {
        if (Application.Current == null)
            return;

        var host = new Forms.NativeControlHost { Name = "NativeMentorPicker", Dock = Forms.DockStyle.Fill };
        var controllers = Controls.Cast<Forms.Control>().ToArray();
        foreach (var controller in controllers)
            Controls.Remove(controller);

        var list = NativeListSelection.Attach(this, lstMentor, Text, lstMentor_DoubleClick, layoutManaged: true)!;
        list.ClearValue(ListBox.FontSizeProperty);
        var category = NativeListSelection.Attach(this, cboCategory, lblCategory.Text, layoutManaged: true)!;
        var selection = new Grid { RowDefinitions = new RowDefinitions("Auto,Auto,*"), RowSpacing = 4 };
        selection.Children.Add(Label(lblCategory.Text));
        Grid.SetRow(category, 1);
        selection.Children.Add(category);
        Grid.SetRow(list, 2);
        selection.Children.Add(list);

        var choice1 = Choice(lblChoice1, cboChoice1);
        var choice2 = Choice(lblChoice2, cboChoice2);
        var advantage = Label("");
        advantage.Name = "NativeMentorAdvantage";
        var disadvantage = Label("");
        disadvantage.Name = "NativeMentorDisadvantage";
        var source = new Button
        {
            Name = "NativeMentorSource", HorizontalAlignment = HorizontalAlignment.Left,
            ContentTemplate = new Avalonia.Controls.Templates.FuncDataTemplate<string>((text, _) => Label(text ?? ""))
        };
        source.Click += (_, _) => lblSource_Click(lblSource, EventArgs.Empty);
        void RefreshDetails(object? sender, EventArgs args)
        {
            bool selected = lstMentor.SelectedIndex >= 0;
            advantage.Text = selected ? lblAdvantage.Text : "";
            disadvantage.Text = selected ? lblDisadvantage.Text : "";
            choice1.IsVisible = selected && cboChoice1.Visible;
            choice2.IsVisible = selected && cboChoice2.Visible;
            source.Content = selected ? lblSource.Text : "";
            source.IsEnabled = selected && !string.IsNullOrWhiteSpace(lblSource.Text);
            AutomationProperties.SetName(source, lblSourceLabel.Text + " " + source.Content);
            ToolTip.SetTip(source, selected ? tipTooltip.GetToolTip(lblSource) : null);
        }
        lstMentor.SelectedIndexChanged += RefreshDetails;
        list.SelectionChanged += (_, _) => RefreshDetails(null, EventArgs.Empty);
        RefreshDetails(null, EventArgs.Empty);

        var details = new StackPanel
        {
            Spacing = 6, Children =
            {
                Label(lblAdvantageLabel.Text), advantage, choice1, choice2,
                Label(lblDisadvantageLabel.Text), disadvantage, Label(lblSourceLabel.Text), source
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
        var panes = NativePickerPanes.Create(selection, detailView);
        var actions = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Right, ItemSpacing = 8, LineSpacing = 4 };
        var accept = new Button { Name = "NativecmdOK", Content = Label(cmdOK.Text) };
        AutomationProperties.SetName(accept, cmdOK.Text);
        accept.Click += (_, _) => cmdOK.PerformClick();
        actions.Children.Add(accept);
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
        ClientSize = new System.Drawing.Size(620, 400);
        Disposed += (_, _) =>
        {
            lstMentor.SelectedIndexChanged -= RefreshDetails;
            host.NativeControl = null;
            foreach (var controller in controllers)
                controller.Dispose();
        };

        StackPanel Choice(Forms.Label caption, Forms.ComboBox controller)
        {
            var combo = NativeListSelection.Attach(this, controller, caption.Text, layoutManaged: true)!;
            return new StackPanel { Spacing = 4, Children = { Label(caption.Text), combo } };
        }

        static TextBlock Label(string text) => new() { Text = text, TextWrapping = TextWrapping.Wrap };
    }
}
