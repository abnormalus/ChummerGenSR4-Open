#nullable enable
using System;
using System.Drawing;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Forms = Majorsilence.Forms;

namespace Chummer;

internal static class MagicInventoryLayout
{
    internal static void AttachPage(Forms.Form owner, Forms.TabPage page, string name, string detailsName, StackPanel rows,
        Forms.Label treeHeading, Forms.Label entitiesHeading, Forms.Button addEntity, Forms.Panel entities,
        Forms.ToolTip tips, Forms.TreeView source, Action delete, params Forms.Button[] commands)
    {
        const int inset = 6;
        var workspace = owner.MdiParent ?? owner;
        entities.Anchor = Forms.AnchorStyles.Top | Forms.AnchorStyles.Left;
        void ResizeEntities(object? sender, EventArgs args) =>
            entities.Bounds = EntityPanelBounds(InventoryTreeLayout.VisiblePageSize(workspace, page));
        page.SizeChanged += ResizeEntities;
        page.VisibleChanged += ResizeEntities;
        workspace.SizeChanged += ResizeEntities;
        ResizeEntities(null, EventArgs.Empty);
        var container = new Grid { RowDefinitions = new RowDefinitions("*,Auto"), RowSpacing = 6 };
        NativeControlPresentation.AddTheme(container.Styles);
        var presenter = new CreationRegionPresenter(owner, page, container, name,
            () => new Rectangle(inset, inset, entities.Width,
                Math.Max(0, entities.Top - 2 * inset)), clipToSource: true);
        var tree = NativeEditorTree.Attach(owner, source, name, treeHeading.Text, delete: delete, layoutHost: presenter.Host)!;
        var choices = (Grid)InventoryTreeLayout.Compose(owner, tree, source, commands);
        var heading = TechnomancerPersonaLayout.BindLabel(owner, page, treeHeading, FontWeight.SemiBold);
        choices.RowDefinitions = new RowDefinitions("Auto,Auto,*");
        Grid.SetRow(choices.Children[0], 1);
        Grid.SetRow(tree, 2);
        choices.Children.Insert(0, heading);

        var details = new ScrollViewer
        {
            Name = detailsName, Content = rows,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            BringIntoViewOnFocusChange = true
        };
        container.Children.Add(NativePickerPanes.Create(choices, details));
        var entityCaption = TechnomancerPersonaLayout.BindLabel(owner, page, entitiesHeading, FontWeight.SemiBold);
        ToolTip.SetTip(entityCaption, tips.GetToolTip(entitiesHeading));
        var entityHeader = new WrapPanel { ItemSpacing = 8, LineSpacing = 4,
            Children = { entityCaption, ArmorEquipmentLayout.BindCommand(owner, page, addEntity) } };
        Grid.SetRow(entityHeader, 1);
        container.Children.Add(entityHeader);
        owner.Disposed += (_, _) =>
        {
            page.SizeChanged -= ResizeEntities;
            page.VisibleChanged -= ResizeEntities;
            workspace.SizeChanged -= ResizeEntities;
            presenter.Dispose();
        };
    }

    internal static Rectangle EntityPanelBounds(Size page)
    {
        const int inset = 6;
        int available = Math.Max(0, page.Height - 2 * inset);
        int height = Math.Min(available * 45 / 100, Math.Max(96, available * 27 / 100));
        return new Rectangle(inset, inset + available - height, Math.Max(0, page.Width - 2 * inset), height);
    }

}
