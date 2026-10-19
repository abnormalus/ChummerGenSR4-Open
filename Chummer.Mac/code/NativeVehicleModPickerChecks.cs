#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Avalonia.LogicalTree;
using Avalonia.Styling;
using Avalonia.Media.Imaging;
using Forms = Majorsilence.Forms;
namespace Chummer;

internal static class NativeVehicleModPickerChecks
{
    internal static async Task RunAsync(Forms.Form owner, Character character, string output, Action<bool, string> check)
    {
        var books = character.Options.Books.ToArray(); var before = character.CaptureSnapshot();
        try
        {
            character.Options.Books.Clear(); character.Options.Books.Add("AR"); character.Options.Books.Add("SR4");
            foreach (bool weapons in new[] { false, true })
            {
                using var prompt = new frmSelectVehicleMod(character, true) { InputFile = weapons ? "weapons" : "vehicles", WeaponType = "Melee", VehicleCost = 13579, Body = 0 };
                var pending = prompt.ShowDialogAsync(owner); var window = (VehicleModPickerWindow)NativeDialogHost.ActiveWindow!;
                window.SearchBox.Text = weapons ? "Weapon Focus" : "Armor, Normal";
                await Task.Delay(100);
                var rating = window.GetLogicalDescendants().OfType<NumericUpDown>().Single(item => item.Name == "vehicleModRating");
                rating.Text = "3"; await Task.Delay(30);
                check(window.Model.Rating == 3 && rating.IsEnabled && rating.Maximum == (weapons ? 18 : 20), "Native " + (weapons ? "weapon" : "vehicle") + " modification accepts typed rating with catalog bounds");
                if (!weapons)
                {
                    window.SearchBox.Text = "Off-Road Tires"; await Task.Delay(30);
                    check(window.Model.Rating == 3 && rating.Maximum == 20 && window.Model.RatingLabel == LanguageManager.Instance.GetString("Label_Qty"), "Native vehicle modification changes rated input to tire quantity without losing selection");
                }
                rating.GetVisualDescendants().OfType<Button>().Single(button => button.Name == "PART_IncreaseButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                await Task.Delay(30);
                check(window.Model.Rating == 4, "Native modification spinner increases the bound catalog rating");
                var textEditor = rating.GetVisualDescendants().OfType<TextBox>().Single(); textEditor.Focus();
                textEditor.CaretIndex = textEditor.Text?.Length ?? 0; textEditor.SelectionStart = textEditor.CaretIndex; textEditor.SelectionEnd = textEditor.CaretIndex;
                textEditor.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Down }); await Task.Delay(30);
                check(window.Model.Rating == 3, "Native modification keyboard stepping follows the standard numeric editor");
                rating.Text = "999"; await Task.Delay(30);
                check(window.Model.Rating == (weapons ? 18 : 20), "Native modification typed value clips at the selected catalog limit");
                window.SearchBox.Text = "";
                window.Model.SelectedEntry = window.Model.Items.First(item => item.Definition["rating"]!.InnerText == "0"); await Task.Delay(30);
                check(!rating.IsEnabled && rating.Minimum == 0 && rating.Maximum == 0 && window.Model.Rating == 0,
                    "Native modification fixed definition disables rating and clears the previous value");
                window.SearchBox.Text = weapons ? "Weapon Focus" : "Off-Road Tires"; window.Model.Rating = 3; await Task.Delay(30);
                character.Options.Books.Clear(); window.Accept(false);
                check(!pending.IsCompleted && !window.Model.CanAccept && window.Model.HasError,
                    "Native modification rejects acceptance after sources change while the modal is open");
                character.Options.Books.Add("AR"); character.Options.Books.Add("SR4"); window.Model.Refresh();
                window.SearchBox.Text = weapons ? "Weapon Focus" : "Off-Road Tires"; window.Model.Rating = 3; await Task.Delay(30);
                var nativeOwner = Forms.AvaloniaHostInterop.ToAvaloniaWindow(owner); var previousTheme = nativeOwner.RequestedThemeVariant;
                try
                {
                    foreach (var theme in new[] { ThemeVariant.Dark, ThemeVariant.Light })
                    {
                        nativeOwner.RequestedThemeVariant = theme; window.Width = 800; window.Height = 560; await Task.Delay(100);
                        check(window.ActualThemeVariant == theme && rating.Bounds.Width >= 110 && window.Model.CanAccept, "Native " + (weapons ? "weapon" : "vehicle") + " modification follows " + theme + " owner theme with usable numeric layout");
                        using var image = new RenderTargetBitmap(new PixelSize((int)window.ClientSize.Width * 2, (int)window.ClientSize.Height * 2), new Vector(192, 192));
                        image.Render(window); image.Save(Path.Combine(output, "native-" + (weapons ? "weapon" : "vehicle") + "-mod-" + (theme == ThemeVariant.Dark ? "dark" : "light") + ".png"), PngBitmapEncoderOptions.Default);
                    }
                }
                finally { nativeOwner.RequestedThemeVariant = previousTheme; }
                window.Model.Markup = 25; window.Model.FreeCost = true; window.Accept(true);
                check(await pending == Forms.DialogResult.OK && prompt.SelectedRating == 3 && prompt.AddAgain && prompt.FreeCost && prompt.Markup == 25,
                    "Native modification returns only explicit accepted rating and purchase choices");
            }
            using (var cancelled = new frmSelectVehicleMod(character))
            {
                var pending = cancelled.ShowDialogAsync(owner); var window = (VehicleModPickerWindow)NativeDialogHost.ActiveWindow!;
                window.SearchBox.Text = "no \"quoted\" modification"; await Task.Delay(30);
                check(!window.AcceptButton.IsEnabled && !window.AddMoreButton.IsEnabled && window.Model.Details == null, "Native modification empty results disable both acceptance actions");
                window.Close();
                check(await pending == Forms.DialogResult.Cancel && cancelled.SelectedMod == "" && cancelled.SelectedRating == 0 && !cancelled.AddAgain,
                    "Native modification title close discards draft identity and numeric choices");
            }
        }
        finally { character.Options.Books.Clear(); character.Options.Books.AddRange(books); }
        check(character.CaptureSnapshot().AsSpan().SequenceEqual(before), "Native modification browsing accepted and canceled drafts never purchase inventory");
    }
}
