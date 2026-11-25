#nullable enable
using System;
using System.Linq;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Forms = Majorsilence.Forms;

namespace Chummer;

public partial class frmSelectCritterPower
{
    private void InitializeNativeLayout()
    {
        if (Application.Current == null)
            return;

        var host = new Forms.NativeControlHost { Name = "NativeCritterPowerPicker", Dock = Forms.DockStyle.Fill };
        var tree = NativeEditorTree.Attach(this, trePowers, "NativetrePowers", Text, layoutHost: host)!;
        var controllers = Controls.Cast<Forms.Control>().ToArray();
        foreach (var controller in controllers)
            Controls.Remove(controller);

        tree.DoubleTapped += (_, args) =>
        {
            if (args.Source is Visual visual &&
                (visual as TreeViewItem ?? visual.FindAncestorOfType<TreeViewItem>()) is { } row)
            {
                tree.SelectedItem = row;
                trePowers_DoubleClick(trePowers, EventArgs.Empty);
            }
        };

        var category = NativeListSelection.Attach(this, cboCategory, lblCategory.Text, layoutManaged: true)!;
        var selection = new Grid { RowDefinitions = new RowDefinitions("Auto,Auto,*"), RowSpacing = 4 };
        selection.Children.Add(Label(lblCategory.Text));
        Grid.SetRow(category, 1);
        selection.Children.Add(category);
        Grid.SetRow(tree, 2);
        selection.Children.Add(tree);

        var rating = new NativeInlineNumericBinding(nudCritterPowerRating, tipTooltip, wholeNumbers: true);
        rating.Input.ClearValue(Layoutable.WidthProperty);
        rating.Input.HorizontalAlignment = HorizontalAlignment.Stretch;
        AutomationProperties.SetName(rating.Input, lblCritterPowerRatingLabel.Text);

        var source = new Button
        {
            Name = "NativeCritterPowerSource", HorizontalAlignment = HorizontalAlignment.Left,
            ContentTemplate = new Avalonia.Controls.Templates.FuncDataTemplate<string>((text, _) => Label(text ?? ""))
        };
        source.Click += (_, _) => lblCritterPowerSource_Click(lblCritterPowerSource, EventArgs.Empty);
        void RefreshSource()
        {
            source.Content = lblCritterPowerSource.Text;
            source.IsEnabled = !string.IsNullOrWhiteSpace(lblCritterPowerSource.Text);
            AutomationProperties.SetName(source, lblCritterPowerSourceLabel.Text + " " + lblCritterPowerSource.Text);
            ToolTip.SetTip(source, tipTooltip.GetToolTip(lblCritterPowerSource));
        }
        var details = new StackPanel
        {
            Spacing = 6, Children =
            {
                Detail(lblCritterPowerCategoryLabel, lblCritterPowerCategory),
                Detail(lblCritterPowerTypeLabel, lblCritterPowerType),
                Detail(lblCritterPowerActionLabel, lblCritterPowerAction),
                Detail(lblCritterPowerRangeLabel, lblCritterPowerRange),
                Detail(lblCritterPowerDurationLabel, lblCritterPowerDuration),
                Label(lblCritterPowerRatingLabel.Text),
                rating.Input,
                Label(lblCritterPowerSourceLabel.Text),
                source,
                Detail(lblPowerPointsLabel, lblPowerPoints)
            }
        };
        void RefreshSelection(object? sender, EventArgs args)
        {
            details.IsVisible = trePowers.SelectedNode != null;
            RefreshSource();
        }
        tree.SelectionChanged += (_, _) => RefreshSelection(null, EventArgs.Empty);
        void AfterSelect(object? sender, Forms.TreeViewEventArgs args) => RefreshSelection(sender, args);
        trePowers.AfterSelect += AfterSelect;
        cboCategory.SelectedIndexChanged += RefreshSelection;
        RefreshSelection(null, EventArgs.Empty);

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
        host.NativeControl = surface;
        Controls.Add(host);
        host.SizeChanged += (_, _) => host.SyncNativeControl();
        FormBorderStyle = Forms.FormBorderStyle.Sizable;
        MinimumSize = new System.Drawing.Size(310, 340);
        ClientSize = new System.Drawing.Size(640, 460);
        Disposed += (_, _) =>
        {
            cboCategory.SelectedIndexChanged -= RefreshSelection;
            trePowers.AfterSelect -= AfterSelect;
            rating.Dispose();
            host.NativeControl = null;
            foreach (var controller in controllers)
                controller.Dispose();
        };

        TextBlock Detail(Forms.Label caption, Forms.Label value)
        {
            var label = Label("");
            label.Name = "Native" + value.Name;
            void Refresh()
            {
                label.Text = caption.Text + " " + value.Text;
                label.IsVisible = value.Visible && caption.Visible;
            }
            var observation = new NativeControlObservation(value, Refresh);
            value.TextChanged += Changed;
            void Changed(object? sender, EventArgs args) => Refresh();
            Disposed += (_, _) =>
            {
                value.TextChanged -= Changed;
                observation.Dispose();
            };
            Refresh();
            return label;
        }

        static TextBlock Label(string text) => new() { Text = text, TextWrapping = TextWrapping.Wrap };
    }
}
