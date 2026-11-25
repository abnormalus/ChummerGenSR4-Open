#nullable enable
using System;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Media;
using Forms = Majorsilence.Forms;

namespace Chummer;

internal static class InventoryTreeLayout
{
    internal static void AttachPage(Forms.Form owner, Forms.TabPage page, string name, string accessibleName, ScrollViewer details,
        Forms.TreeView source, Action delete, Action? editDetail, NativeTreeDrag? drag, params Forms.Button[] commands)
        => AttachPage(owner, page, name, accessibleName, details, source, delete, editDetail, drag,
            tree => Compose(owner, tree, source, commands));

    internal static void AttachPage(Forms.Form owner, Forms.TabPage page, string name, string accessibleName, ScrollViewer details,
        Forms.TreeView source, Action delete, Action? editDetail, NativeTreeDrag? drag, Func<TreeView, Control> compose)
        => AttachResponsivePage(owner, page, name, accessibleName, source, delete, editDetail, drag,
            tree => NativePickerPanes.Create(compose(tree), details));

    internal static void AttachResponsivePage(Forms.Form owner, Forms.TabPage page, string name, string accessibleName,
        Forms.TreeView source, Action delete, Action? editDetail, NativeTreeDrag? drag, Func<TreeView, Control> compose)
    {
        var container = new Grid { Margin = new Avalonia.Thickness(6) };
        NativeControlPresentation.AddTheme(container.Styles);
        var presenter = PresentPage(owner, page, container, name);
        var tree = NativeEditorTree.Attach(owner, source, name, accessibleName, delete: delete, editDetail: editDetail,
            drag: drag, layoutHost: presenter.Host)!;
        container.Children.Add(compose(tree));
        owner.Disposed += (_, _) => presenter.Dispose();
    }

    internal static CreationRegionPresenter PresentPage(Forms.Form owner, Forms.TabPage page, Control content, string name)
    {
        var workspace = owner.MdiParent ?? owner;
        return new CreationRegionPresenter(owner, page, content, name,
            () => new System.Drawing.Rectangle(System.Drawing.Point.Empty, VisiblePageSize(workspace, page)),
            clipToSource: true);
    }

    internal static System.Drawing.Size VisiblePageSize(Forms.Form workspace, Forms.Control page)
    {
        var origin = workspace.PointToClient(page.PointToScreen(System.Drawing.Point.Empty));
        return new System.Drawing.Size(
            Math.Clamp(workspace.ClientSize.Width - origin.X, 0, Math.Max(0, page.ClientSize.Width)),
            Math.Clamp(workspace.ClientSize.Height - origin.Y, 0, Math.Max(0, page.ClientSize.Height)));
    }

    internal static Control Compose(Forms.Form owner, TreeView tree, Forms.TreeView controller, params Forms.Button[] commands)
    {
        var layout = new Grid { RowDefinitions = new RowDefinitions("Auto,*"), RowSpacing = 4 };
        layout.Children.Add(CreateCommands(owner, controller, commands));
        Grid.SetRow(tree, 1);
        layout.Children.Add(tree);
        NativeControlPresentation.AddTheme(layout.Styles);
        return layout;
    }

    internal static Control ComposeMetamagic(Forms.Form owner, Forms.TabPage page, TreeView tree,
        Forms.TreeView controller, Forms.Label sourceHeading, Forms.Label source, Forms.ToolTip tips,
        Action openSource, params Forms.Button[] commands)
    {
        var layout = (Grid)Compose(owner, tree, controller, commands);
        var reference = GearReadoutsLayout.BindSource(owner, page, "NativeMetamagicSource",
            sourceHeading, source, tips, openSource);
        layout.RowDefinitions = new RowDefinitions("Auto,*,Auto");
        Grid.SetRow(reference, 2);
        layout.Children.Add(reference);
        return layout;
    }

    internal static Control ComposeFoci(Forms.Form owner, TreeView tree, Forms.TreeView controller,
        Forms.Label label, Forms.Button createStack)
    {
        var region = (Grid)Compose(owner, tree, controller, createStack);
        int bottom = controller.Bottom;
        controller.Top = label.Top;
        controller.Height = Math.Max(0, bottom - controller.Top);
        label.Parent?.Controls.Remove(label);
        var heading = new TextBlock { Text = label.Text, TextWrapping = TextWrapping.Wrap };
        AutomationProperties.SetLabeledBy(tree, heading);
        void CaptionChanged(object? sender, EventArgs args) => heading.Text = label.Text;
        label.TextChanged += CaptionChanged;
        owner.Disposed += (_, _) =>
        {
            label.TextChanged -= CaptionChanged;
            label.Dispose();
        };
        region.RowDefinitions = new RowDefinitions("Auto,Auto,*");
        Grid.SetRow(region.Children[0], 1);
        Grid.SetRow(tree, 2);
        region.Children.Insert(0, heading);
        return region;
    }

    internal static WrapPanel CreateCommands(Forms.Form owner, Forms.TreeView sourceTree, params Forms.Button[] commands)
    {
        int bottom = sourceTree.Bottom;
        sourceTree.Top = commands[0].Top;
        sourceTree.Height = Math.Max(0, bottom - sourceTree.Top);
        var actions = new WrapPanel { ItemSpacing = 5, LineSpacing = 4 };
        ContextMenu? openMenu = null;
        foreach (var command in commands)
        {
            command.Parent?.Controls.Remove(command);
            var caption = new TextBlock { Text = command.Text.Replace("&", ""), TextWrapping = TextWrapping.Wrap };
            var button = new Button { Name = "Native" + command.Name, Content = caption };
            button.Click += (_, _) => command.PerformClick();
            var group = new DockPanel();
            Button? menu = null;
            if (command is SplitButton split)
            {
                menu = new Button { Name = button.Name + "Menu", Content = "▾" };
                menu.Click += (_, _) =>
                {
                    openMenu?.Close();
                    if (split.SplitMenuStrip is { } source)
                        openMenu = Forms.AvaloniaContextCommands.Show(menu, new Point(0, menu.Bounds.Height),
                            source, split, () => menu.IsEffectivelyVisible && menu.IsEnabled, NativeControlPresentation.AddTheme);
                };
                DockPanel.SetDock(menu, Avalonia.Controls.Dock.Right);
                group.Children.Add(menu);
            }
            group.Children.Add(button);
            void RefreshCommand()
            {
                button.IsEnabled = group.IsEnabled = command.Enabled;
                group.IsVisible = command.Visible;
                caption.Text = command.Text.Replace("&", "");
                if (menu != null) AutomationProperties.SetName(menu, caption.Text);
            }
            var observer = new NativeControlObservation(command, RefreshCommand);
            void CaptionChanged(object? sender, EventArgs args) => RefreshCommand();
            command.TextChanged += CaptionChanged;
            RefreshCommand();
            actions.Children.Add(group);
            owner.Disposed += (_, _) =>
            {
                command.TextChanged -= CaptionChanged;
                observer.Dispose();
                command.Dispose();
            };
        }
        NativeControlPresentation.ConstrainWrapItems(actions);
        owner.Disposed += (_, _) => openMenu?.Close();
        return actions;
    }

}
