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

public partial class frmSelectCyberwareSuite
{
    private void InitializeNativeSuitePicker()
    {
        if (Application.Current == null)
            return;

        var controllers = Controls.Cast<Forms.Control>().ToArray();
        foreach (var controller in controllers)
            Controls.Remove(controller);

        var list = NativeListSelection.Attach(this, lstCyberware, Text, lstCyberware_DoubleClick, layoutManaged: true)!;
        list.ClearValue(ListBox.FontSizeProperty);
        var details = new StackPanel { Spacing = 8 };
        AddDetail(lblGradeLabel, lblGrade);
        AddDetail(lblEssenceLabel, lblEssence);
        AddDetail(lblCostLabel, lblCost);
        details.Children.Add(new TextBlock { Text = lblCyberwareLabel.Text, TextWrapping = TextWrapping.Wrap });
        details.Children.Add(BindText(lblCyberware));
        var scroll = new ScrollViewer
        {
            Content = details, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        var panes = NativePickerPanes.Create(list, scroll);

        var actions = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Right, ItemSpacing = 8, LineSpacing = 4 };
        foreach (var source in new[] { cmdCancel, cmdOK })
        {
            var button = new Button
            {
                Name = "Native" + source.Name,
                Content = new TextBlock { Text = source.Text, TextWrapping = TextWrapping.Wrap }
            };
            AutomationProperties.SetName(button, source.Text);
            button.Click += (_, _) => source.PerformClick();
            actions.Children.Add(button);
        }
        NativeControlPresentation.ConstrainWrapItems(actions);
        var layout = new Grid { Margin = new Thickness(12), RowDefinitions = new RowDefinitions("*,Auto"), RowSpacing = 10 };
        layout.Children.Add(panes);
        Grid.SetRow(actions, 1);
        layout.Children.Add(actions);
        var surface = new Border { Child = layout };
        NativeControlPresentation.AddTheme(surface.Styles);
        var host = new Forms.NativeControlHost { Name = "NativeSuitePicker", Dock = Forms.DockStyle.Fill, NativeControl = surface };
        Controls.Add(host);
        host.SizeChanged += (_, _) => host.SyncNativeControl();
        FormBorderStyle = Forms.FormBorderStyle.Sizable;
        MinimumSize = new System.Drawing.Size(300, 290);
        Disposed += (_, _) =>
        {
            host.NativeControl = null;
            foreach (var controller in controllers)
                controller.Dispose();
        };

        void AddDetail(Forms.Label caption, Forms.Label value)
        {
            var row = new WrapPanel { ItemSpacing = 6, LineSpacing = 2 };
            row.Children.Add(new TextBlock { Text = caption.Text, TextWrapping = TextWrapping.Wrap });
            row.Children.Add(BindText(value));
            details.Children.Add(row);
        }

        TextBlock BindText(Forms.Label source)
        {
            var text = new TextBlock { Name = "Native" + source.Name, Text = source.Text, TextWrapping = TextWrapping.Wrap };
            void Refresh(object? sender, EventArgs args) => text.Text = source.Text;
            source.TextChanged += Refresh;
            Disposed += (_, _) => source.TextChanged -= Refresh;
            return text;
        }
    }
}
