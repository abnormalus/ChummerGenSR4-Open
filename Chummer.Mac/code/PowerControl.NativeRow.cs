#nullable enable
using System;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Layout;
using Forms = Majorsilence.Forms;

namespace Chummer;

public partial class PowerControl
{
    private void AttachNativePowerRow()
    {
        if (FindForm() == null)
            return;

        var name = BindLabel(lblPowerName, FontWeight.SemiBold);
        name.PointerEntered += (_, _) => ToolTip.SetTip(name, tipTooltip.GetToolTip(lblPowerName));
        var powerDetails = new Button { Name = "NativePowerDetails", Content = name };
        var detailsTitle = new TextBlock { TextWrapping = TextWrapping.Wrap, FontWeight = FontWeight.SemiBold };
        var detailsCost = new TextBlock { TextWrapping = TextWrapping.Wrap };
        var sourceCaption = new TextBlock { TextWrapping = TextWrapping.Wrap };
        var sourceLink = new Button { Name = "NativePowerDetailsSource", Content = sourceCaption, HorizontalAlignment = HorizontalAlignment.Left };
        var details = new StackPanel { MaxWidth = 290, Spacing = 6, Children = { detailsTitle, detailsCost, sourceLink } };
        var flyout = new Flyout { Content = details };
        powerDetails.Flyout = flyout;
        string citation = "";
        flyout.Opening += (_, _) =>
        {
            var power = PowerObject;
            var options = power.CharacterObject.Options;
            detailsTitle.Text = name.Text;
            detailsCost.Text = (lblRating.Text + " " + power.Rating + "   " + lblX.Text + " " + lblPowerPoints.Text).Trim();
            sourceLink.IsVisible = !string.IsNullOrWhiteSpace(power.Source) && !string.IsNullOrWhiteSpace(power.Page);
            if (sourceLink.IsVisible)
            {
                citation = options.LanguageBookShort(power.Source) + " " + power.Page;
                sourceCaption.Text = options.LanguageBookLong(power.Source) + " " +
                    LanguageManager.Instance.GetString("String_Page") + " " + power.Page;
            }
        };
        sourceLink.Click += (_, _) =>
        {
            if (sourceLink.IsVisible)
                new CommonFunctions(PowerObject.CharacterObject).OpenPDF(citation);
        };
        var ratingLabel = BindLabel(lblRating);
        Controls.Remove(nudRating);
        var rating = new NativeInlineNumericBinding(nudRating, tipTooltip, wholeNumbers: true);
        rating.Input.Width = 96;
        AutomationProperties.SetLabeledBy(rating.Input, ratingLabel);
        var cost = new WrapPanel { ItemSpacing = 5, LineSpacing = 3,
            Children = { ratingLabel, rating.Input, BindLabel(lblX), BindLabel(lblPowerPoints) } };
        var discounts = new WrapPanel { ItemSpacing = 8, LineSpacing = 3, Children =
            {
                BindLabel(lblDiscountLabel),
                GearReadoutsLayout.BindOption(this, this, chkDiscountedAdeptWay),
                GearReadoutsLayout.BindOption(this, this, chkDiscountedGeas)
            }
        };
        Controls.Remove(imgNotes);
        var notes = new Button { Name = "NativePowerNotes", Content = LanguageManager.Instance.GetString("Menu_Notes") };
        notes.Click += (_, _) => imgNotes_Click(imgNotes, EventArgs.Empty);
        notes.PointerEntered += (_, _) => ToolTip.SetTip(notes, tipTooltip.GetToolTip(imgNotes));
        var commands = new WrapPanel { ItemSpacing = 8, LineSpacing = 3,
            Children = { notes, ArmorEquipmentLayout.BindCommand(this, this, cmdDelete) } };
        powerDetails.HorizontalAlignment = HorizontalAlignment.Left;
        rating.Input.ClearValue(NumericUpDown.FontSizeProperty);
        var fields = new Grid { ColumnSpacing = 12, RowSpacing = 4, Children = { powerDetails, cost, discounts, commands } };
        var surface = new Border { Child = fields };
        NativeControlPresentation.AddTheme(surface.Styles);
        foreach (var panel in new Panel[] { fields, cost, discounts, commands })
            foreach (var field in panel.Children)
                field.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center;
        CreationRegionPresenter? presenter = null;
        Forms.Control? list = null;
        int columns = 0;

        void Resize(object? sender, EventArgs args)
        {
            if (list == null)
                return;
            Width = Math.Max(0, list.ClientSize.Width - 18);
            double fontSize = Avalonia.Controls.Documents.TextElement.GetFontSize(fields);
            int nextColumns = Width >= Math.Max(880, fontSize * 70) ? 4 :
                Width >= Math.Max(520, fontSize * 41) ? 2 : 1;
            if (columns != nextColumns)
            {
                columns = nextColumns;
                fields.ColumnDefinitions = new ColumnDefinitions(columns == 4 ? "2*,3*,3*,Auto" : columns == 2 ? "*,*" : "*");
                fields.RowDefinitions = new RowDefinitions(columns == 4 ? "Auto" : columns == 2 ? "Auto,Auto" : "Auto,Auto,Auto,Auto");
                for (int index = 0; index < fields.Children.Count; index++)
                {
                    Grid.SetColumn(fields.Children[index], index % columns);
                    Grid.SetRow(fields.Children[index], index / columns);
                }
            }
            fields.Measure(new Avalonia.Size(Width, double.PositiveInfinity));
            Height = Math.Max(32, (int)Math.Ceiling(fields.DesiredSize.Height)) + 6;
            presenter?.Refresh();
        }

        void Mount(object? sender, EventArgs args)
        {
            if (list != null)
                list.SizeChanged -= Resize;
            presenter?.Dispose();
            presenter = null;
            list = Parent;
            if (list == null || FindForm() is not { } editor)
                return;
            list.SizeChanged += Resize;
            presenter = new CreationRegionPresenter(editor, this, surface, "NativeAdeptPowerRow", clipToSource: true);
            Resize(null, EventArgs.Empty);
        }

        fields.PropertyChanged += (_, change) =>
        {
            if (change.Property == Avalonia.Controls.Documents.TextElement.FontSizeProperty)
                Resize(null, EventArgs.Empty);
        };
        ParentChanged += Mount;
        lblPowerName.TextChanged += Resize;
        lblPowerPoints.TextChanged += Resize;
        Disposed += (_, _) =>
        {
            ParentChanged -= Mount;
            lblPowerName.TextChanged -= Resize;
            lblPowerPoints.TextChanged -= Resize;
            if (list != null)
                list.SizeChanged -= Resize;
            flyout.Hide();
            presenter?.Dispose();
            rating.Dispose();
            nudRating.Dispose();
            imgNotes.Dispose();
        };
        Mount(null, EventArgs.Empty);

        TextBlock BindLabel(Forms.Label source, FontWeight? weight = null)
        {
            Controls.Remove(source);
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
            Disposed += (_, _) =>
            {
                observation.Dispose();
                source.TextChanged -= Changed;
                source.Dispose();
            };
            return text;
        }
    }
}
