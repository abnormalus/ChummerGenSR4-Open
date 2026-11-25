#nullable enable
using System;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Media;
using Forms = Majorsilence.Forms;

namespace Chummer;

internal static class TechnomancerPersonaLayout
{
    internal static StackPanel CreateComplexFormSummary(Forms.Form owner, Forms.TabPage page,
        Forms.Label skillHeading, Forms.Label skill, Forms.Label sourceHeading, Forms.Label source,
        Forms.ToolTip tips, Action openSource, Func<WrapPanel> createRating, Action<StackPanel> addRows)
    {
        var rows = new StackPanel { Spacing = 3 };
        rows.Children.Add(createRating());
        rows.Children.Add(GearReadoutsLayout.BindReadout(owner, page, skillHeading, skill));
        rows.Children.Add(GearReadoutsLayout.BindSource(owner, page, "NativeComplexFormSource",
            sourceHeading, source, tips, openSource));
        addRows(rows);
        return rows;
    }

    internal static void AddPersona(Forms.Form owner, Forms.TabPage page, StackPanel rows,
        Forms.Label heading, Forms.Label paragonHeading, Forms.Label paragon, Forms.Label description,
        params (Forms.Label Title, Forms.Label Value)[] persona)
    {
        var stats = new WrapPanel { ItemSpacing = 12, LineSpacing = 4 };
        foreach (var (title, value) in persona)
            stats.Children.Add(GearReadoutsLayout.BindReadout(owner, page, title, value));
        NativeControlPresentation.ConstrainWrapItems(stats);
        rows.Children.Add(new StackPanel
        {
            Name = "NativeTechnomancerPersona", Spacing = 7,
            Children =
            {
                BindLabel(owner, page, heading, FontWeight.SemiBold), stats,
                GearReadoutsLayout.BindReadout(owner, page, paragonHeading, paragon),
                BindLabel(owner, page, description)
            }
        });
    }

    internal static WrapPanel CreateStreamRow(Forms.Form owner, Forms.TabPage page, Forms.Label title, Forms.ComboBox stream)
    {
        var caption = BindLabel(owner, page, title);
        var input = NativeSelectionInput.Bind(owner, page, stream);
        AutomationProperties.SetLabeledBy(input, caption);
        var row = new WrapPanel { ItemSpacing = 8, LineSpacing = 4 };
        row.Children.Add(NativeControlPresentation.CreateLabeledField(row, caption, input));
        NativeControlPresentation.ConstrainWrapItems(row);
        return row;
    }

    internal static WrapPanel CreateFadingRow(Forms.Form owner, Forms.TabPage page,
        Forms.Label title, Forms.Label attributes, Forms.Label value)
    {
        var row = new WrapPanel
        {
            ItemSpacing = 8, LineSpacing = 4,
            Children =
            {
                GearReadoutsLayout.BindReadout(owner, page, title, attributes),
                BindLabel(owner, page, value)
            }
        };
        NativeControlPresentation.ConstrainWrapItems(row);
        return row;
    }

    internal static WrapPanel CreateRatingRow(Forms.Form owner, Forms.TabPage page, Forms.Label title,
        Forms.NumericUpDown rating, Forms.ToolTip tips, Forms.Label? currentRating = null)
    {
        var caption = currentRating == null ? BindLabel(owner, page, title) :
            GearReadoutsLayout.BindReadout(owner, page, title, currentRating);
        page.Controls.Remove(rating);
        var binding = new NativeInlineNumericBinding(rating, tips, wholeNumbers: true);
        binding.Input.ClearValue(NumericUpDown.FontSizeProperty);
        AutomationProperties.SetLabeledBy(binding.Input, caption);
        owner.Disposed += (_, _) =>
        {
            binding.Dispose();
            rating.Dispose();
        };
        var row = new WrapPanel { ItemSpacing = 8, LineSpacing = 4 };
        row.Children.Add(NativeControlPresentation.CreateLabeledField(row, caption, binding.Input));
        NativeControlPresentation.ConstrainWrapItems(row);
        return row;
    }

    internal static TextBlock BindLabel(Forms.Form owner, Forms.Control page, Forms.Label source, FontWeight? weight = null)
    {
        page.Controls.Remove(source);
        var text = new TextBlock { TextWrapping = TextWrapping.Wrap, FontWeight = weight ?? FontWeight.Normal };
        void Refresh()
        {
            text.Text = source.Text;
            text.IsVisible = source.Visible;
        }
        void Changed(object? sender, EventArgs args) => Refresh();
        var observation = new NativeControlObservation(source, Refresh);
        source.TextChanged += Changed;
        Refresh();
        owner.Disposed += (_, _) =>
        {
            observation.Dispose();
            source.TextChanged -= Changed;
            source.Dispose();
        };
        return text;
    }
}
