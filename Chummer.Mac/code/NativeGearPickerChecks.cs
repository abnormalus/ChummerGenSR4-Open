#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Forms = Majorsilence.Forms;

namespace Chummer;

internal static class NativeGearPickerChecks
{
    internal static async Task RunAsync(Forms.Form owner, Character original, string output, Action<bool,string> check)
    {
        async Task ExpectAsync(Func<bool> condition, string label)
        {
            var deadline = DateTime.UtcNow.AddSeconds(20);
            while (!condition())
            {
                if (DateTime.UtcNow >= deadline) { check(false, label); throw new TimeoutException("Native Gear assertion timed out."); }
                await Task.Delay(20);
            }
            check(true, label);
        }
        var nativeOwner = Forms.AvaloniaHostInterop.ToAvaloniaWindow(owner);
        foreach (bool career in new[] { false, true })
        {
            var character = new Character(); character.LoadSnapshot(original.CaptureSnapshot()); character.Created = career;
            byte[] snapshot = character.CaptureSnapshot(); string mode = career ? "career" : "creation";
            using (var dialog = new frmSelectGear(character, career) { InitialCategory = "Ammunition" })
            {
                var pending = dialog.ShowDialogAsync(owner); var window = (GearPickerWindow)NativeDialogHost.ActiveWindow!;
                try
                {
                    await ExpectAsync(() => window.IsVisible && window.ClientSize.Height > 0 && !nativeOwner.IsEnabled, "Native Gear " + mode + " opens standard owned modal");
                    T Input<T>(string name) where T : Control => window.GetVisualDescendants().OfType<T>().Single(control => control.Name == name);
                    var quantity = Input<NumericUpDown>("gearPickerQuantity");
                    window.ItemList.SelectedItem = window.Model.Items.Single(item => item.Name == "Ammo: Regular Ammo");
                    await ExpectAsync(() => quantity.Maximum == 100000 && quantity.Minimum == 1 && quantity.Increment == 10 && quantity.Value == 10 &&
                        quantity.GetVisualDescendants().OfType<Button>().Any(button => button.Name == "PART_IncreaseButton"),
                        "Native Gear " + mode + " binds full quantity range and pack increment");
                    quantity.Text = "30";
                    await ExpectAsync(() => window.Model.Quantity == 30 && window.Model.Details?.Cost == string.Format("{0:###,###,##0¥}",60),
                        "Native Gear " + mode + " typed pack quantity preserves pricing");
                    quantity.GetVisualDescendants().OfType<Button>().Single(button => button.Name == "PART_IncreaseButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    await ExpectAsync(() => window.Model.Quantity == 40, "Native Gear " + mode + " spinner steps by complete ammunition pack");
                    quantity.Text = "100001";
                    await ExpectAsync(() => quantity.Value == 100000 && window.Model.Quantity == 100000, "Native Gear " + mode + " quantity clips at original 100000 maximum");
                    window.SearchBox.Text = "Grenade: Fragmentation";
                    window.ItemList.SelectedItem = window.Model.Items.Single(item => item.Name == "Grenade: Fragmentation");
                    var aerodynamic = Input<CheckBox>("gearPickerAerodynamic"); aerodynamic.IsChecked = true;
                    await ExpectAsync(() => aerodynamic.IsVisible && window.Model.Aerodynamic,
                        "Native Gear " + mode + " grenade checkbox binds aerodynamic choice");
                    window.SearchBox.Text = "Medkit"; window.ItemList.SelectedItem = window.Model.Items.Single(item => item.Name == "Medkit");
                    await ExpectAsync(() => !aerodynamic.IsVisible && aerodynamic.IsChecked != true && !window.Model.Aerodynamic,
                        "Native Gear " + mode + " leaving grenade clears aerodynamic choice");
                    var rating = Input<NumericUpDown>("gearPickerRating"); rating.Text = "4"; quantity.Text = "3";
                    await ExpectAsync(() => window.Model.Rating == 4 && window.Model.Quantity == 3 && window.Model.Details?.Cost == string.Format("{0:###,###,##0¥}",1200),
                        "Native Gear " + mode + " rated inputs bind to original preview");
                    rating.GetVisualDescendants().OfType<TextBox>().Single().RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Up });
                    await ExpectAsync(() => window.Model.Rating == 5, "Native Gear " + mode + " keyboard increases whole rating");
                    window.SearchBox.Text = "Analyze"; window.ItemList.SelectedItem = window.Model.Items.Single(item => item.Name == "Analyze"); rating.Text = "7";
                    var hacked = Input<CheckBox>("gearPickerHacked"); hacked.IsChecked = true;
                    await ExpectAsync(() => hacked.IsVisible && window.Model.Hacked && window.Model.Details?.Cost == string.Format("{0:###,###,##0¥}",490),
                        "Native Gear " + mode + " hacked software checkbox retains catalog tier pricing");
                    hacked.IsChecked = false;
                    window.SearchBox.Text = "missing \"quoted\" item";
                    await ExpectAsync(() => window.Model.Items.Count == 0 && window.Model.Details == null && !window.AcceptButton.IsEnabled,
                        "Native Gear " + mode + " literal empty search clears stale purchase state");
                    window.SearchBox.Text = "Medkit"; window.ItemList.SelectedItem = window.Model.Items.Single(item => item.Name == "Medkit"); rating.Text = "4"; quantity.Text = "3";
                    await ExpectAsync(() => window.AcceptButton.IsEnabled && window.Model.Details?.Source.Length > 0,
                        "Native Gear " + mode + " search recovery restores selection and PDF source");
                    var previousTheme = nativeOwner.RequestedThemeVariant;
                    try
                    {
                        foreach (var theme in new[] { ThemeVariant.Dark, ThemeVariant.Light })
                        {
                            nativeOwner.RequestedThemeVariant = theme; window.Width = 820; window.Height = 560;
                            await ExpectAsync(() => window.ActualThemeVariant == theme && quantity.Bounds.Width >= 110 && rating.Bounds.Height > 0,
                                "Native Gear " + mode + " standard inputs follow " + (theme == ThemeVariant.Dark ? "dark" : "light") + " owner theme");
                            await Task.Delay(100);
                            using var bitmap = new RenderTargetBitmap(new PixelSize((int)window.ClientSize.Width * 2,(int)window.ClientSize.Height * 2),new Vector(192,192));
                            bitmap.Render(window); bitmap.Save(Path.Combine(output,"native-gear-"+mode+"-"+(theme==ThemeVariant.Dark?"dark":"light")+".png"),PngBitmapEncoderOptions.Default);
                        }
                    }
                    finally { nativeOwner.RequestedThemeVariant = previousTheme; }
                    if (career) Input<CheckBox>("gearPickerFreeCost").IsChecked = true;
                    await ExpectAsync(() => window.Model.FreeCost == career && window.Model.Details?.Cost == string.Format("{0:###,###,##0¥}",career?0:1200),
                        "Native Gear " + mode + " free purchase availability preserves mode");
                    window.AddMoreButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    await ExpectAsync(() => pending.IsCompleted, "Native Gear " + mode + " Add More completes modal result");
                    check(await pending == Forms.DialogResult.OK && dialog.SelectedGear == "Medkit" && dialog.SelectedCategory == "Biotech" && dialog.SelectedRating == 4 && dialog.SelectedQty == 3 && dialog.AddAgain && dialog.FreeCost == career && nativeOwner.IsEnabled,
                        "Native Gear " + mode + " accepted draft preserves identity options and owner availability");
                    check(character.CaptureSnapshot().SequenceEqual(snapshot), "Native Gear " + mode + " accepted draft leaves character unchanged");
                }
                finally { if (!pending.IsCompleted) window.Close(); }
            }
            using (var dialog = new frmSelectGear(character,career) { AllowedCategories = "Commlink Upgrade" })
            {
                var pending = dialog.ShowDialogAsync(owner); var window = (GearPickerWindow)NativeDialogHost.ActiveWindow!;
                try
                {
                    window.ItemList.SelectedItem = window.Model.Items.Single(item => item.Name == "Response 4");
                    var diy = window.GetVisualDescendants().OfType<CheckBox>().Single(control => control.Name == "gearPickerDoItYourself");
                    await ExpectAsync(() => diy.IsVisible && window.Model.IsCareer == career, "Native Gear " + mode + " context exposes DIY purchase choice");
                    diy.IsChecked = true;
                    await ExpectAsync(() => window.Model.DoItYourself && window.Model.Details?.Cost == string.Format("{0:###,###,##0¥}",1000),
                        "Native Gear " + mode + " native DIY choice updates preview");
                    window.Close();
                    check(await pending == Forms.DialogResult.Cancel && dialog.SelectedGear == "" && dialog.SelectedQty == 0 && !dialog.AddAgain && nativeOwner.IsEnabled && NativeDialogHost.Count == 0,
                        "Native Gear " + mode + " title-bar cancellation discards draft and restores owner");
                    check(character.CaptureSnapshot().SequenceEqual(snapshot), "Native Gear " + mode + " cancelled draft preserves complete snapshot");
                }
                finally { if (!pending.IsCompleted) window.Close(); }
            }
        }
        var ai = new Character(); ai.LoadSnapshot(original.CaptureSnapshot()); ai.Metatype = "A.I.";
        if (!ai.Options.Books.Contains("UN")) ai.Options.Books.Add("UN");
        byte[] aiSnapshot = ai.CaptureSnapshot();
        using var aiDialog = new frmSelectGear(ai, true) { InitialCategory = "Matrix Programs" };
        var aiPending = aiDialog.ShowDialogAsync(owner); var aiWindow = (GearPickerWindow)NativeDialogHost.ActiveWindow!;
        try
        {
            aiWindow.ItemList.SelectedItem = aiWindow.Model.Items.Single(item => item.Name == "Analyze");
            var inherent = aiWindow.GetVisualDescendants().OfType<CheckBox>().Single(control => control.Name == "gearPickerInherentProgram");
            var hacked = aiWindow.GetVisualDescendants().OfType<CheckBox>().Single(control => control.Name == "gearPickerHacked");
            await ExpectAsync(() => inherent.IsVisible && inherent.IsEnabled, "Native Gear AI exposes enabled inherent program choice");
            inherent.IsChecked = true;
            await ExpectAsync(() => aiWindow.Model.InherentProgram, "Native Gear AI inherent checkbox binds draft");
            hacked.IsChecked = true;
            await ExpectAsync(() => inherent.IsChecked != true && !inherent.IsEnabled && !aiWindow.Model.InherentProgram,
                "Native Gear AI hacked checkbox clears and disables incompatible inherent choice");
            hacked.IsChecked = false; inherent.IsChecked = true; aiWindow.Accept(false);
            check(await aiPending == Forms.DialogResult.OK && aiDialog.InherentProgram && !aiDialog.Hacked,
                "Native Gear AI accepted adapter preserves eligible inherent draft");
            check(ai.CaptureSnapshot().SequenceEqual(aiSnapshot) && nativeOwner.IsEnabled && NativeDialogHost.Count == 0,
                "Native Gear AI acceptance preserves character and restores owner");
        }
        finally { if (!aiPending.IsCompleted) aiWindow.Close(); }
    }
}
