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

public partial class frmSelectProgram
{
    private void InitializeNativeLayout()
    {
        if (Application.Current == null)
            return;

        var host = new Forms.NativeControlHost { Name = "NativeProgramPicker", Dock = Forms.DockStyle.Fill };
        var tree = NativeEditorTree.Attach(this, trePrograms, "NativetrePrograms", Text,
            editDetail: () => trePrograms_DoubleClick(trePrograms, EventArgs.Empty), layoutHost: host)!;
        var controllers = Controls.Cast<Forms.Control>().ToArray();
        foreach (var controller in controllers)
            Controls.Remove(controller);

        var source = new Button
        {
            Name = "NativeProgramSource", HorizontalAlignment = HorizontalAlignment.Left,
            ContentTemplate = new Avalonia.Controls.Templates.FuncDataTemplate<string>((text, _) => Label(text ?? ""))
        };
        source.Click += (_, _) => lblSource_Click(lblSource, EventArgs.Empty);
        var skill = Label("");
        skill.Name = "NativeProgramSkill";
        void RefreshDetails()
        {
            bool selected = trePrograms.SelectedNode is { Level: > 0 };
            skill.Text = selected ? lblCommonSkill.Text : "";
            source.Content = selected ? lblSource.Text : "";
            source.IsEnabled = selected && !string.IsNullOrWhiteSpace(lblSource.Text);
            AutomationProperties.SetName(source, lblSourceLabel.Text + " " + source.Content);
            ToolTip.SetTip(source, selected ? tipTooltip.GetToolTip(lblSource) : null);
        }
        void AfterSelect(object? sender, Forms.TreeViewEventArgs args) => RefreshDetails();
        trePrograms.AfterSelect += AfterSelect;
        tree.SelectionChanged += (_, _) => RefreshDetails();
        RefreshDetails();

        var search = new TextBox { Name = "NativeProgramSearch", Text = txtSearch.Text };
        AutomationProperties.SetName(search, lblSearchLabel.Text);
        search.TextChanged += (_, _) =>
        {
            txtSearch.Text = search.Text ?? "";
            RefreshDetails();
        };
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
                Label(lblCommonSkillLabel.Text),
                skill,
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
        var panes = NativePickerPanes.Create(tree, detailView);
        var actions = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Right, ItemSpacing = 8, LineSpacing = 4 };
        foreach (var command in new[] { cmdOKAdd, cmdCancel, cmdOK })
        {
            var button = new Button { Name = "Native" + command.Name, Content = Label(command.Text) };
            AutomationProperties.SetName(button, command.Text);
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
        host.NativeControl = surface;
        Controls.Add(host);
        host.SizeChanged += (_, _) => host.SyncNativeControl();
        FormBorderStyle = Forms.FormBorderStyle.Sizable;
        MinimumSize = new System.Drawing.Size(300, 300);
        ClientSize = new System.Drawing.Size(620, 400);
        Disposed += (_, _) =>
        {
            trePrograms.AfterSelect -= AfterSelect;
            host.NativeControl = null;
            foreach (var controller in controllers)
                controller.Dispose();
        };

        static TextBlock Label(string text) => new() { Text = text, TextWrapping = TextWrapping.Wrap };
    }
}
