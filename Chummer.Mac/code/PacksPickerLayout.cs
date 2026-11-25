#nullable enable
using System.Linq;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Forms = Majorsilence.Forms;

namespace Chummer;

public partial class frmSelectPACKSKit
{
    private void InitializeNativeKitLayout()
    {
        if (Application.Current == null)
            return;

        var host = new Forms.NativeControlHost { Name = "NativePACKSKitPicker", Dock = Forms.DockStyle.Fill };
        var contents = NativeEditorTree.Attach(this, treContents, "NativetreContents", label1.Text, layoutHost: host)!;
        var controllers = Controls.Cast<Forms.Control>().ToArray();
        foreach (var controller in controllers)
            Controls.Remove(controller);
        var category = NativeListSelection.Attach(this, cboCategory, lblCategory.Text, layoutManaged: true)!;
        var kits = NativeListSelection.Attach(this, lstKits, Text, lstKits_DoubleClick, layoutManaged: true)!;
        kits.ClearValue(ListBox.FontSizeProperty);
        var selection = Pane(lblCategory.Text, category, kits);
        var detail = Pane(label1.Text, contents);
        var panes = NativePickerPanes.Create(selection, detail);

        var actions = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Right, ItemSpacing = 8, LineSpacing = 4 };
        foreach (var source in new[] { cmdDelete, cmdOKAdd, cmdCancel, cmdOK })
        {
            var button = new Button { Name = "Native" + source.Name, Content = new TextBlock { Text = source.Text, TextWrapping = TextWrapping.Wrap } };
            AutomationProperties.SetName(button, source.Text);
            void Refresh() { button.IsVisible = source.Visible; button.IsEnabled = source.Enabled; }
            var observer = new NativeControlObservation(source, Refresh);
            button.Click += (_, _) => source.PerformClick();
            actions.Children.Add(button);
            Disposed += (_, _) => observer.Dispose();
            Refresh();
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
        MinimumSize = new System.Drawing.Size(340, 380);
        Disposed += (_, _) =>
        {
            host.NativeControl = null;
            foreach (var controller in controllers)
                controller.Dispose();
        };
    }

    private static Grid Pane(string title, params Control[] controls)
    {
        var layout = new Grid { RowDefinitions = new RowDefinitions(controls.Length == 2 ? "Auto,Auto,*" : "Auto,*"), RowSpacing = 4 };
        layout.Children.Add(new TextBlock { Text = title, TextWrapping = TextWrapping.Wrap });
        for (int index = 0; index < controls.Length; index++)
        {
            Grid.SetRow(controls[index], index + 1);
            layout.Children.Add(controls[index]);
        }
        return layout;
    }
}
