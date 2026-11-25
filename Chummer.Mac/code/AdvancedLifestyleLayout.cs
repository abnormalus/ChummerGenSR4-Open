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

public partial class frmSelectAdvancedLifestyle
{
    private void InitializeNativeLayout()
    {
        if (Application.Current == null)
            return;

        var host = new Forms.NativeControlHost { Name = "NativeAdvancedLifestyle", Dock = Forms.DockStyle.Fill };
        var positive = NativeEditorTree.Attach(this, trePositiveQualities, "NativetrePositiveQualities", label1.Text, layoutHost: host)!;
        var negative = NativeEditorTree.Attach(this, treNegativeQualities, "NativetreNegativeQualities", label2.Text, layoutHost: host)!;
        var controllers = Controls.Cast<Forms.Control>().ToArray();
        foreach (var controller in controllers)
            Controls.Remove(controller);

        var qualities = new Grid { RowDefinitions = new RowDefinitions("Auto,*,Auto,*"), RowSpacing = 4 };
        AddRow(qualities, Label(label1.Text), 0);
        AddRow(qualities, positive, 1);
        AddRow(qualities, Label(label2.Text), 2);
        AddRow(qualities, negative, 3);

        var fields = new StackPanel { Spacing = 5 };
        var name = new NativeInlineTextBinding(txtLifestyleName, () => true);
        name.Input.ClearValue(TextBox.FontSizeProperty);
        name.Input.Name = "NativetxtLifestyleName";
        AutomationProperties.SetName(name.Input, lblLifestyleNameLabel.Text);
        AddField(lblLifestyleNameLabel, name.Input);
        foreach (var (label, controller) in new[]
        {
            (lblComforts, cboComforts), (lblEntertainment, cboEntertainment), (lblNecessities, cboNecessities),
            (lblNeighborhood, cboNeighborhood), (lblSecurity, cboSecurity)
        })
            AddField(label, NativeListSelection.Attach(this, controller, label.Text, layoutManaged: true)!);

        AddTotal(lblTotalLPLabel, lblTotalLP);
        var roommates = new NativeInlineNumericBinding(nudRoommates, tipTooltip, wholeNumbers: true);
        var percentage = new NativeInlineNumericBinding(nudPercentage, tipTooltip, wholeNumbers: nudPercentage.DecimalPlaces == 0);
        AddNumber(lblRoommates, roommates);
        AddNumber(lblPercentage, percentage);
        AddTotal(lblCostLabel, lblCost);

        var citation = Label("");
        var source = new Button { Name = "NativeLifestyleSource", Content = citation, HorizontalAlignment = HorizontalAlignment.Left };
        source.Click += (_, _) => lblSource_Click(lblSource, EventArgs.Empty);
        void RefreshSource(object? sender, EventArgs args)
        {
            citation.Text = lblSource.Text;
            source.IsEnabled = !string.IsNullOrWhiteSpace(lblSource.Text);
            AutomationProperties.SetName(source, lblSourceLabel.Text + " " + lblSource.Text);
            ToolTip.SetTip(source, tipTooltip.GetToolTip(lblSource));
        }
        void QualitySelected(object? sender, Forms.TreeViewEventArgs args) => RefreshSource(sender, args);
        trePositiveQualities.AfterSelect += QualitySelected;
        treNegativeQualities.AfterSelect += QualitySelected;
        RefreshSource(null, EventArgs.Empty);
        AddField(lblSourceLabel, source);

        var panes = NativePickerPanes.Create(qualities, new ScrollViewer
        {
            Content = fields,
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled
        });
        var actions = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Right, ItemSpacing = 8, LineSpacing = 4 };
        foreach (var command in new[] { cmdOKAdd, cmdCancel, cmdOK })
        {
            var button = new Button { Name = "Native" + command.Name, Content = Label(command.Text) };
            AutomationProperties.SetName(button, command.Text);
            button.Click += (_, _) => command.PerformClick();
            actions.Children.Add(button);
        }

        var layout = new Grid { Margin = new Thickness(12), RowDefinitions = new RowDefinitions("*,Auto"), RowSpacing = 10 };
        AddRow(layout, panes, 0);
        AddRow(layout, actions, 1);
        layout.SizeChanged += (_, args) =>
        {
            if (args.NewSize.Width <= 0) return;
            foreach (var action in actions.Children)
                action.MaxWidth = args.NewSize.Width;
        };
        var surface = new Border { Child = layout };
        NativeControlPresentation.AddTheme(surface.Styles);
        host.NativeControl = surface;
        Controls.Add(host);
        host.SizeChanged += (_, _) => host.SyncNativeControl();
        FormBorderStyle = Forms.FormBorderStyle.Sizable;
        MinimumSize = new System.Drawing.Size(320, 420);
        ClientSize = new System.Drawing.Size(750, 590);
        Disposed += (_, _) =>
        {
            trePositiveQualities.AfterSelect -= QualitySelected;
            treNegativeQualities.AfterSelect -= QualitySelected;
            name.Dispose();
            roommates.Dispose();
            percentage.Dispose();
            host.NativeControl = null;
            foreach (var controller in controllers)
                controller.Dispose();
        };

        void AddField(Forms.Label caption, Control input)
        {
            var text = Label(caption.Text);
            AutomationProperties.SetLabeledBy(input, text);
            void Refresh(object? sender, EventArgs args) => text.Text = caption.Text;
            caption.TextChanged += Refresh;
            Disposed += (_, _) => caption.TextChanged -= Refresh;
            fields.Children.Add(new StackPanel { Spacing = 3, Children = { text, input } });
        }

        void AddNumber(Forms.Label caption, NativeInlineNumericBinding binding)
        {
            binding.Input.ClearValue(Layoutable.WidthProperty);
            binding.Input.ClearValue(NumericUpDown.FontSizeProperty);
            AutomationProperties.SetName(binding.Input, caption.Text);
            AddField(caption, binding.Input);
        }

        void AddTotal(Forms.Label caption, Forms.Label controller)
        {
            var total = Label(caption.Text + " " + controller.Text);
            total.Name = "Native" + controller.Name;
            void Refresh(object? sender, EventArgs args) => total.Text = caption.Text + " " + controller.Text;
            controller.TextChanged += Refresh;
            Disposed += (_, _) => controller.TextChanged -= Refresh;
            fields.Children.Add(total);
        }

        static TextBlock Label(string text) => new() { Text = text, TextWrapping = TextWrapping.Wrap };
        static void AddRow(Grid grid, Control child, int row)
        {
            Grid.SetRow(child, row);
            grid.Children.Add(child);
        }
    }
}
