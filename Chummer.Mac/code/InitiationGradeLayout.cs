#nullable enable
using System;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Forms = Majorsilence.Forms;

namespace Chummer;

internal static class InitiationGradeLayout
{
    internal static void AttachPage(Forms.Form owner, Forms.TabPage page, string name,
        WrapPanel grade, StackPanel group, Forms.Label historyHeading, Forms.ListBox historySource,
        Forms.TreeView source, Forms.Label referenceHeading, Forms.Label reference, Forms.ToolTip tips,
        Action delete, Action openSource, params Forms.Button[] commands)
    {
        var container = new Grid { Margin = new Avalonia.Thickness(6) };
        NativeControlPresentation.AddTheme(container.Styles);
        var presenter = InventoryTreeLayout.PresentPage(owner, page, container, name);
        var tree = NativeEditorTree.Attach(owner, source, name, "Metamagic", delete: delete, layoutHost: presenter.Host)!;
        var choices = InventoryTreeLayout.ComposeMetamagic(owner, page, tree, source, referenceHeading,
            reference, tips, openSource, commands);
        var title = TechnomancerPersonaLayout.BindLabel(owner, page, historyHeading);
        var history = NativeListSelection.Attach(owner, historySource, historyHeading.Text, layoutManaged: true)!;
        page.Controls.Remove(historySource);
        history.ClearValue(ListBox.FontSizeProperty);
        void SizeHistory()
        {
            history.MinHeight = history.FontSize * 6;
            history.MaxHeight = history.FontSize * 12;
        }
        history.PropertyChanged += (_, args) =>
        {
            if (args.Property == ListBox.FontSizeProperty) SizeHistory();
        };
        SizeHistory();
        AutomationProperties.SetLabeledBy(history, title);
        var rows = new StackPanel { Spacing = 6, Children = { grade, title, history, group } };
        var details = new ScrollViewer
        {
            Content = rows,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            BringIntoViewOnFocusChange = true
        };
        container.Children.Add(NativePickerPanes.Create(choices, details));
        owner.Disposed += (_, _) =>
        {
            presenter.Dispose();
            historySource.Dispose();
        };
    }

    internal static StackPanel CreateGroup(Forms.Form owner, Forms.TabPage page, Forms.CheckBox membership,
        Forms.Label nameHeading, Forms.TextBox name, Forms.Label notesHeading, Forms.TextBox notes)
    {
        var fields = new StackPanel { Spacing = 6 };
        fields.Children.Add(GearReadoutsLayout.BindOption(owner, page, membership));
        AddField(nameHeading, name);
        AddField(notesHeading, notes);
        return fields;

        void AddField(Forms.Label heading, Forms.TextBox source)
        {
            var caption = TechnomancerPersonaLayout.BindLabel(owner, page, heading);
            page.Controls.Remove(source);
            var binding = new NativeInlineTextBinding(source, () => true);
            var input = binding.Input;
            input.Name = "Native" + source.Name;
            input.ClearValue(TextBox.FontSizeProperty);
            input.AcceptsReturn = source.Multiline;
            input.TextWrapping = source.Multiline ? Avalonia.Media.TextWrapping.Wrap : Avalonia.Media.TextWrapping.NoWrap;
            if (source.Multiline)
            {
                input.MinLines = 7;
                input.MaxLines = 12;
            }
            AutomationProperties.SetLabeledBy(input, caption);
            fields.Children.Add(caption);
            fields.Children.Add(input);
            owner.Disposed += (_, _) =>
            {
                binding.Dispose();
                source.Dispose();
            };
        }
    }

    internal static WrapPanel CreateGrade(Forms.Form owner, Forms.TabPage page, Forms.Label heading, Forms.Label grade,
        Forms.CheckBox group, Forms.CheckBox ordeal, Forms.ToolTip tips,
        Forms.Button improve, Forms.Button? reduce = null)
    {
        var rows = new WrapPanel { ItemSpacing = 10, LineSpacing = 4 };
        rows.Children.Add(GearReadoutsLayout.BindReadout(owner, page, heading, grade));
        AddCommand(improve, "+");
        if (reduce != null)
            AddCommand(reduce, "−");
        rows.Children.Add(GearReadoutsLayout.BindOption(owner, page, group));
        rows.Children.Add(GearReadoutsLayout.BindOption(owner, page, ordeal));
        NativeControlPresentation.ConstrainWrapItems(rows);
        return rows;

        void AddCommand(Forms.Button source, string symbol)
        {
            var button = ArmorEquipmentLayout.BindCommand(owner, page, source, symbol);
            AutomationProperties.SetName(button, heading.Text + " " + symbol);
            button.PointerEntered += (_, _) => ToolTip.SetTip(button, tips.GetToolTip(source));
            rows.Children.Add(button);
        }
    }
}
