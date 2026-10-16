#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media.Imaging;
using Forms = Majorsilence.Forms;

namespace Chummer;

internal static class NativeAccessoryPickerChecks
{
    private static T Find<T>(Window window, string name) where T : Control =>
        window.GetLogicalDescendants().OfType<T>().Single(control => control.Name == name);
    private static async Task WaitForAsync(Func<bool> condition)
    {
        DateTime deadline = DateTime.UtcNow.AddSeconds(15);
        while (!condition())
        {
            if (DateTime.UtcNow >= deadline) throw new TimeoutException("Native accessory picker did not reach the expected state.");
            await Task.Delay(20);
        }
    }
    internal static async Task RunAsync(Forms.Form main, Character character, string output, Action<bool, string> check)
    {
        byte[] original = character.CaptureSnapshot();
        using (var picker = new frmSelectWeaponAccessory(character, true) { AllowedMounts = "Top/Under", WeaponCost = 1000 })
        {
            var pending = picker.ShowDialogAsync(main);
            var window = (AccessoryPickerWindow)NativeDialogHost.ActiveWindow!;
            window.SearchBox.Text = "Laser Sight";
            await WaitForAsync(() => window.AcceptButton.IsEnabled && window.ItemList.SelectedItem is CatalogEntry);
            check(window.Model.SelectedEntry?.Name == "Laser Sight" && !Forms.AvaloniaHostInterop.ToAvaloniaWindow(main).IsEnabled,
                "Native accessory picker binds filtered selection and disables its owner");
            Find<NumericUpDown>(window, "accessoryMarkup").Value = 25;
            Find<CheckBox>(window, "accessoryFreeCost").IsChecked = true;
            await WaitForAsync(() => window.Model.FreeCost && window.Model.Markup == 25);
            check(Find<TextBlock>(window, "accessoryCost").Text == window.Model.Details!.Cost,
                "Native accessory purchase controls update bound cost details");
            window.SearchBox.Text = "missing \"quoted\" accessory";
            await WaitForAsync(() => !window.AcceptButton.IsEnabled && window.ItemList.ItemCount == 0);
            check(window.Model.Details == null && string.IsNullOrEmpty(Find<TextBlock>(window, "accessoryMount").Text),
                "Native empty accessory search clears details and disables acceptance");
            window.SearchBox.Text = "Spare Clips";
            await WaitForAsync(() => window.Model.IsQuantityEnabled);
            var quantity = Find<NumericUpDown>(window, "accessoryQuantity");
            quantity.Value = 3;
            await WaitForAsync(() => window.Model.Quantity == 3);
            check(quantity.IsEnabled && window.Model.Accept(false)?.Quantity == 3,
                "Native accessory quantity control returns multiple ordinary units for spare clips");
            window.SearchBox.Text = "Laser Sight";
            await WaitForAsync(() => window.AcceptButton.IsEnabled);
            check(!quantity.IsEnabled && window.Model.Quantity == 1,
                "Native installed accessory selection resets to one unit and disables quantity");
            window.Width = 760; window.Height = 480; await Task.Delay(120);
            using (var bitmap = new RenderTargetBitmap(new PixelSize((int)window.ClientSize.Width * 2, (int)window.ClientSize.Height * 2), new Vector(192, 192)))
            {
                bitmap.Render(window); bitmap.Save(Path.Combine(output, "native-accessory-picker-2x.png"), PngBitmapEncoderOptions.Default);
            }
            check(window.ItemList.Bounds.Width >= 200 && window.AcceptButton.IsEffectivelyVisible,
                "Native accessory layout remains usable when resized and rendered at 2x");
            window.AddMoreButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            check(await pending == Forms.DialogResult.OK && picker.SelectedAccessory == "Laser Sight" && picker.SelectedMount == "Top/Under" &&
                picker.AddAgain && picker.FreeCost && picker.Markup == 25,
                "Native accessory Add More returns canonical identity, mount alternatives and purchase options");
        }
        using (var picker = new frmSelectWeaponAccessory(character))
        {
            var pending = picker.ShowDialogAsync(main); NativeDialogHost.ActiveWindow!.Close();
            check(await pending == Forms.DialogResult.Cancel && picker.SelectedAccessory == "" && picker.SelectedMount == "",
                "Native accessory title-bar close cancels without retaining purchase fields");
        }
        check(NativeDialogHost.Count == 0 && Forms.AvaloniaHostInterop.ToAvaloniaWindow(main).IsEnabled && character.CaptureSnapshot().SequenceEqual(original),
            "Native accessory browsing restores the owner and preserves character data");
    }
}
