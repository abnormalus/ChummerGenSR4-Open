#nullable enable
using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Forms = Majorsilence.Forms;

namespace Chummer;

internal static class ArmorEquipmentLayout
{
    internal static ScrollViewer CreateDetails(Forms.Form owner, Forms.TabPage page, Forms.CheckBox equipped,
        Forms.Label heading, Forms.Label wornArmor, Forms.CheckBox included,
        Forms.Label sourceHeading, Forms.Label source, Forms.ToolTip tips, Action openSource,
        Forms.Button[] commands, Action<StackPanel> addDetails)
    {
        var actions = new WrapPanel { ItemSpacing = 8, LineSpacing = 4 };
        actions.Children.Add(GearReadoutsLayout.BindOption(owner, page, equipped));
        foreach (var command in commands)
            actions.Children.Add(BindCommand(owner, page, command));

        var list = GearReadoutsLayout.BindReadout(owner, page, heading, wornArmor);
        var sourceRow = new WrapPanel { ItemSpacing = 8, LineSpacing = 4 };
        sourceRow.Children.Add(GearReadoutsLayout.BindSource(owner, page, "NativeArmorSource",
            sourceHeading, source, tips, openSource));
        sourceRow.Children.Add(GearReadoutsLayout.BindOption(owner, page, included));
        sourceRow.Name = "NativeArmorSourceRow";
        actions.Name = "NativeArmorEquipmentActions";
        var rows = new StackPanel { Spacing = 8 };
        addDetails(rows);
        rows.Children.Add(sourceRow);
        rows.Children.Add(actions);
        rows.Children.Add(list);
        foreach (var row in rows.Children.OfType<WrapPanel>())
            NativeControlPresentation.ConstrainWrapItems(row);
        return new ScrollViewer
        {
            Name = "NativeArmorEquipment",
            Content = rows,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            BringIntoViewOnFocusChange = true
        };
    }

    internal static Button BindCommand(System.ComponentModel.Component owner, Forms.Control page, Forms.Button command, string? caption = null)
    {
        page.Controls.Remove(command);
        var text = new TextBlock { TextWrapping = TextWrapping.Wrap };
        var button = new Button { Name = "Native" + command.Name, Content = text };
        button.Click += (_, _) => command.PerformClick();
        void Refresh()
        {
            text.Text = caption ?? System.Text.RegularExpressions.Regex.Replace(command.Text, "&&|&", match => match.Length == 2 ? "&" : "");
            button.IsVisible = command.Visible;
            button.IsEnabled = command.Enabled;
        }
        void CaptionChanged(object? sender, EventArgs args) => Refresh();
        var observation = new NativeControlObservation(command, Refresh);
        command.TextChanged += CaptionChanged;
        Refresh();
        owner.Disposed += (_, _) =>
        {
            observation.Dispose();
            command.TextChanged -= CaptionChanged;
            command.Dispose();
        };
        return button;
    }
}
