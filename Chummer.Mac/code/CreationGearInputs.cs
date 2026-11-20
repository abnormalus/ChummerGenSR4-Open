#nullable enable
using System;
using System.Drawing;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Styling;
using Forms = Majorsilence.Forms;

namespace Chummer;

public partial class frmCreate
{
    private const int GearRatingInputWidth = 76;
    private NativeInlineNumericBinding? gearRatingInput;
    private NativeInlineNumericBinding? gearQuantityInput;

    public void RefreshSelectedGear()
    {
        RefreshSelectedGearDetails();
        gearRatingInput?.Refresh();
        gearQuantityInput?.Refresh();
    }

    private void InitializeCreationGearInputs()
    {
        if (Avalonia.Application.Current == null || IsDisposed) return;

        gearRatingInput = Attach(nudGearRating, lblGearRatingLabel, GearRatingInputWidth);
        gearQuantityInput = Attach(nudGearQty, lblGearQtyLabel, 96);

        NativeInlineNumericBinding Attach(Forms.NumericUpDown source, Forms.Label label, int width)
        {
            const int height = 24;
            var binding = new NativeInlineNumericBinding(source, tipTooltip, wholeNumbers: true);
            var input = binding.Input;
            input.Width = width;
            input.Height = height;
            input.MinHeight = height;
            input.Padding = new Avalonia.Thickness(2, 0);
            var region = new Border { Child = input, ClipToBounds = true };
            NativeControlPresentation.AddTheme(region.Styles);
            region.Styles.Add(new Style(selector => selector.OfType<NumericUpDown>().Template().OfType<TextBox>())
            {
                Setters =
                {
                    new Setter(TextBox.MinHeightProperty, 0d),
                    new Setter(TextBox.HeightProperty, (double)height),
                    new Setter(TextBox.PaddingProperty, new Avalonia.Thickness(2, 0)),
                    new Setter(TextBox.VerticalContentAlignmentProperty, VerticalAlignment.Center)
                }
            });

            AutomationProperties.SetName(input, label.Text);
            tabGear.Controls.Remove(source);
            binding.Refresh();
            var presenter = new CreationRegionPresenter(this, tabGear, region, input.Name!,
                () => new Rectangle(source.Left, source.Top, width, height));
            Disposed += (_, _) =>
            {
                presenter.Dispose();
                binding.Dispose();
                source.Dispose();
            };
            return binding;
        }
    }
}
