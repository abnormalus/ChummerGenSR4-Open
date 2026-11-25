#nullable enable
using System;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Forms = Majorsilence.Forms;

namespace Chummer;

internal static class GearReadoutsLayout
{
    internal static void AttachPage(Forms.Form owner, Forms.TabPage page, string prefix,
        StackPanel rows, Forms.TreeView gear, Forms.TreeView foci, Forms.Label fociHeading, Forms.Button createStack,
        Forms.CheckBox commlinks, Action delete, Action editDetail, NativeTreeDrag drag, params Forms.Button[] commands)
    {
        var columns = new Grid { Margin = new Avalonia.Thickness(6) };
        NativeControlPresentation.AddTheme(columns.Styles);
        var presenter = InventoryTreeLayout.PresentPage(owner, page, columns, prefix + "Gear");
        var inventory = NativeEditorTree.Attach(owner, gear, prefix + "Gear", "Gear",
            delete: delete, editDetail: editDetail, drag: drag, layoutHost: presenter.Host)!;
        var choices = (Grid)InventoryTreeLayout.Compose(owner, inventory, gear, commands);
        ((WrapPanel)choices.Children[0]).Children.Add(BindOption(owner, page, commlinks));

        var scroll = new ScrollViewer
        {
            Name = prefix + "GearSummary", Content = rows,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            BringIntoViewOnFocusChange = true
        };
        var fociTree = NativeEditorTree.Attach(owner, foci, prefix + "Foci", "Foci", layoutHost: presenter.Host)!;
        var bonded = InventoryTreeLayout.ComposeFoci(owner, fociTree, foci, fociHeading, createStack);
        bonded.Name = prefix + "FociPane";
        var detail = new Grid { RowSpacing = 6, Children = { scroll, bonded } };
        Grid.SetRow(bonded, 1);
        void RefreshFoci()
        {
            bonded.IsVisible = foci.Visible;
            detail.RowDefinitions = new RowDefinitions(foci.Visible ? "3*,2*" : "*,Auto");
        }
        var observation = new NativeControlObservation(foci, RefreshFoci);
        RefreshFoci();
        columns.Children.Add(NativePickerPanes.Create(choices, detail));
        owner.Disposed += (_, _) =>
        {
            observation.Dispose();
            presenter.Dispose();
        };
    }

    internal static void ShowForSelection(Forms.Form owner, Control details, Forms.TreeView tree)
    {
        void Refresh() => details.IsVisible = tree.SelectedNode is { Level: > 0 };
        void Selected(object? sender, Forms.TreeViewEventArgs args) => Refresh();
        Forms.InvalidateEventHandler invalidated = (_, _) => Refresh();
        tree.AfterSelect += Selected;
        tree.Invalidated += invalidated;
        Refresh();
        owner.Disposed += (_, _) =>
        {
            tree.AfterSelect -= Selected;
            tree.Invalidated -= invalidated;
        };
    }

    internal static void AddOptions(Forms.Form owner, Forms.TabPage page, StackPanel rows,
        params Forms.CheckBox[] sources)
    {
        var options = new WrapPanel { Name = "NativeGearOptions", ItemSpacing = 12, LineSpacing = 4 };
        foreach (var source in sources)
            options.Children.Add(BindOption(owner, page, source));
        NativeControlPresentation.ConstrainWrapItems(options);
        rows.Children.Add(options);
    }

    internal static CheckBox BindOption(System.ComponentModel.Component owner, Forms.Control page, Forms.CheckBox source)
    {
        page.Controls.Remove(source);
        var caption = new TextBlock { TextWrapping = TextWrapping.Wrap };
        var toggle = new CheckBox { Name = "Native" + source.Name, Content = caption };
        void Refresh()
        {
            caption.Text = source.Text;
            toggle.IsVisible = source.Visible;
            toggle.IsEnabled = source.Enabled;
            toggle.IsChecked = source.Checked;
        }
        void Changed(object? sender, EventArgs args) => Refresh();
        var observation = new NativeControlObservation(source, Refresh);
        source.CheckedChanged += Changed;
        source.TextChanged += Changed;
        toggle.IsCheckedChanged += (_, _) =>
        {
            if (source.Enabled && source.Visible)
                source.Checked = toggle.IsChecked == true;
        };
        Refresh();
        owner.Disposed += (_, _) =>
        {
            observation.Dispose();
            source.CheckedChanged -= Changed;
            source.TextChanged -= Changed;
            source.Dispose();
        };
        return toggle;
    }

    internal static void AddReadouts(Forms.Form owner, Forms.TabPage page, StackPanel rows,
        Forms.Label sourceHeading, Forms.Label source, Forms.ToolTip tips, Action openSource,
        (Forms.Label Title, Forms.Label Value)[] matrix, (Forms.Label Title, Forms.Label Value)[] weapon)
    {
        AddRow(matrix);
        AddRow(weapon);
        rows.Children.Add(BindSource(owner, page, "NativeGearSource", sourceHeading, source, tips, openSource));

        void AddRow((Forms.Label Title, Forms.Label Value)[] fields)
        {
            var row = new WrapPanel { ItemSpacing = 12, LineSpacing = 3 };
            foreach (var (title, value) in fields)
                row.Children.Add(BindReadout(owner, page, title, value));
            NativeControlPresentation.ConstrainWrapItems(row);
            rows.Children.Add(row);
        }
    }

    internal static Button BindSource(Forms.Form owner, Forms.TabPage page, string name,
        Forms.Label sourceHeading, Forms.Label source, Forms.ToolTip tips, Action openSource)
    {
        page.Controls.Remove(sourceHeading);
        page.Controls.Remove(source);
        var citation = new TextBlock { TextWrapping = TextWrapping.Wrap };
        var reference = new Button
        {
            Name = name,
            Content = citation,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        reference.Click += (_, _) => openSource();
        void Refresh()
        {
            citation.Text = (sourceHeading.Text + " " + source.Text).Trim();
            reference.IsVisible = sourceHeading.Visible || source.Visible;
            reference.IsEnabled = source.Visible && !string.IsNullOrWhiteSpace(source.Text);
        }
        void Changed(object? sender, EventArgs args) => Refresh();
        reference.PointerEntered += (_, _) => ToolTip.SetTip(reference,
            tips.GetToolTip(source) is { Length: > 0 } tip ? tip : citation.Text);
        var headingState = new NativeControlObservation(sourceHeading, Refresh);
        var sourceState = new NativeControlObservation(source, Refresh);
        sourceHeading.TextChanged += Changed;
        source.TextChanged += Changed;
        Refresh();
        owner.Disposed += (_, _) =>
        {
            headingState.Dispose();
            sourceState.Dispose();
            sourceHeading.TextChanged -= Changed;
            source.TextChanged -= Changed;
            sourceHeading.Dispose();
            source.Dispose();
        };
        return reference;
    }

    internal static StackPanel CreateCareerSummary(Forms.Form owner, Forms.TabPage page,
        (Forms.Label Title, Forms.Label Value) name, (Forms.Label Title, Forms.Label Value) category,
        (Forms.Label Title, Forms.Label Value)[] ratings,
        (Forms.Label Title, Forms.Label Value) capacity,
        (Forms.Label Title, Forms.Label Value) quantity,
        params (Forms.Button Controller, string Caption)[] commands)
    {
        var fields = new StackPanel { Spacing = 3 };
        fields.Children.Add(BindReadout(owner, page, name.Title, name.Value));
        fields.Children.Add(BindReadout(owner, page, category.Title, category.Value));
        var ratingRow = new WrapPanel { ItemSpacing = 12, LineSpacing = 3 };
        foreach (var (title, value) in ratings)
            ratingRow.Children.Add(BindReadout(owner, page, title, value));
        NativeControlPresentation.ConstrainWrapItems(ratingRow);
        fields.Children.Add(ratingRow);
        fields.Children.Add(BindReadout(owner, page, capacity.Title, capacity.Value));
        var quantityRow = new WrapPanel { ItemSpacing = 8, LineSpacing = 4 };
        quantityRow.Children.Add(BindReadout(owner, page, quantity.Title, quantity.Value));
        foreach (var (controller, caption) in commands)
        {
            page.Controls.Remove(controller);
            var button = new Button
            {
                Name = "Native" + controller.Name,
                Content = new TextBlock { Text = caption, TextWrapping = TextWrapping.Wrap }
            };
            button.Click += (_, _) => controller.PerformClick();
            void Refresh()
            {
                button.IsEnabled = controller.Enabled;
                button.IsVisible = controller.Visible;
            }
            var observation = new NativeControlObservation(controller, Refresh);
            Refresh();
            quantityRow.Children.Add(button);
            owner.Disposed += (_, _) =>
            {
                observation.Dispose();
                controller.Dispose();
            };
        }
        NativeControlPresentation.ConstrainWrapItems(quantityRow);
        fields.Children.Add(quantityRow);
        return fields;
    }

    internal static TextBlock BindReadout(Forms.Form owner, Forms.TabPage page, Forms.Label title, Forms.Label value)
    {
        page.Controls.Remove(title);
        page.Controls.Remove(value);
        var text = new TextBlock { Name = "Native" + value.Name, TextWrapping = TextWrapping.Wrap };
        void Refresh()
        {
            text.IsVisible = title.Visible || value.Visible;
            text.Text = ((title.Visible ? title.Text : "") + " " + (value.Visible ? value.Text : "")).Trim();
        }
        void TextChanged(object? sender, EventArgs args) => Refresh();
        var titleState = new NativeControlObservation(title, Refresh);
        var valueState = new NativeControlObservation(value, Refresh);
        title.TextChanged += TextChanged;
        value.TextChanged += TextChanged;
        Refresh();
        owner.Disposed += (_, _) =>
        {
            titleState.Dispose();
            valueState.Dispose();
            title.TextChanged -= TextChanged;
            value.TextChanged -= TextChanged;
            title.Dispose();
            value.Dispose();
        };
        return text;
    }
}
