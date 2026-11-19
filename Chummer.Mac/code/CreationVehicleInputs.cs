#nullable enable
using System;
using System.Drawing;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Styling;

namespace Chummer;

public partial class frmCreate
{
    private NativeInlineNumericBinding? vehicleRatingInput;
    private NativeInlineNumericBinding? vehicleQuantityInput;

    private void RefreshSelectedVehicle()
    {
        RefreshSelectedVehicleDetails();
        vehicleRatingInput?.Refresh();
        vehicleQuantityInput?.Refresh();
    }

    private void InitializeCreationVehicleInputs()
    {
        if (Avalonia.Application.Current == null || IsDisposed) return;

        var rating = vehicleRatingInput = new NativeInlineNumericBinding(nudVehicleRating, tipTooltip, wholeNumbers: true);
        var quantity = vehicleQuantityInput = new NativeInlineNumericBinding(nudVehicleGearQty, tipTooltip, wholeNumbers: true);
        int width = Math.Clamp(chkVehicleIncludedInWeapon.Left - nudVehicleRating.Left - 4,
            nudVehicleRating.Width, 80);
        var inputs = new StackPanel { ClipToBounds = true, Spacing = 1 };
        NativeControlPresentation.AddTheme(inputs.Styles);
        inputs.Styles.Add(new Style(selector => selector.OfType<NumericUpDown>().Template().OfType<TextBox>())
        {
            Setters =
            {
                new Setter(TextBox.MinHeightProperty, 0d),
                new Setter(TextBox.HeightProperty, 25d),
                new Setter(TextBox.PaddingProperty, new Avalonia.Thickness(2, 0)),
                new Setter(TextBox.VerticalContentAlignmentProperty, VerticalAlignment.Center)
            }
        });

        foreach (var binding in new[] { rating, quantity })
        {
            binding.Input.Width = width;
            binding.Input.MinHeight = 25;
            binding.Input.Height = 25;
            binding.Input.Padding = new Avalonia.Thickness(2, 0);
            inputs.Children.Add(binding.Input);
        }

        void SyncRatingLabel(object? sender, EventArgs args) => AutomationProperties.SetName(rating.Input, lblVehicleRatingLabel.Text);
        lblVehicleRatingLabel.TextChanged += SyncRatingLabel;
        SyncRatingLabel(null, EventArgs.Empty);
        AutomationProperties.SetName(quantity.Input, lblVehicleGearQtyLabel.Text);
        tabVehicles.Controls.Remove(nudVehicleRating);
        tabVehicles.Controls.Remove(nudVehicleGearQty);
        rating.Refresh();
        quantity.Refresh();

        var presenter = new CreationRegionPresenter(this, tabVehicles, inputs, "nativeCreationVehicleInputs",
            () => new Rectangle(nudVehicleRating.Left, nudVehicleRating.Top, width, 51));
        Disposed += (_, _) =>
        {
            presenter.Dispose();
            rating.Dispose();
            quantity.Dispose();
            lblVehicleRatingLabel.TextChanged -= SyncRatingLabel;
            nudVehicleRating.Dispose();
            nudVehicleGearQty.Dispose();
        };
    }
}
