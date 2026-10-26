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

// Actual owned windows only. Headless reference tests do not substitute for these checks.
internal static class NativeCyberwarePickerChecks
{
    internal static async Task RunAsync(Forms.Form owner, Character original, string output, Action<bool,string> check)
    {
        async Task ExpectAsync(Func<bool> condition, string label)
        {
            var deadline = DateTime.UtcNow.AddSeconds(20);
            while (!condition())
            {
                if (DateTime.UtcNow >= deadline) { check(false, label); throw new TimeoutException("Native Cyberware assertion timed out."); }
                await Task.Delay(20);
            }
            check(true, label);
        }
        var nativeOwner = Forms.AvaloniaHostInterop.ToAvaloniaWindow(owner);
        foreach (bool career in new[] { false, true })
        foreach (bool bio in new[] { false, true })
        {
            var character = new Character(); character.LoadSnapshot(original.CaptureSnapshot()); character.Created = career;
            if (!character.Options.Books.Contains("SR4")) character.Options.Books.Add("SR4");
            character.Options.AllowCyberwareESSDiscounts = true;
            string mode = (bio ? "bioware" : "cyberware") + "-" + (career ? "career" : "creation");
            string itemName = bio ? "Muscle Toner" : "Datajack", category = bio ? "Basic" : "Headware";
            var wareMode = bio ? frmSelectCyberware.Mode.Bioware : frmSelectCyberware.Mode.Cyberware;
            byte[] snapshot = character.CaptureSnapshot();
            using (var dialog = new frmSelectCyberware(character, career) { WindowMode = wareMode, SetGrade = "Standard" })
            {
                dialog.LockGrade();
                var pending = dialog.ShowDialogAsync(owner);
                var window = (CyberwarePickerWindow)NativeDialogHost.ActiveWindow!;
                try
                {
                    await ExpectAsync(() => window.IsVisible && window.ClientSize.Height > 0 && !nativeOwner.IsEnabled,
                        "Native " + mode + " opens standard owned modal");
                    T Input<T>(string name) where T : Control => window.GetVisualDescendants().OfType<T>().Single(control => control.Name == name);
                    window.SearchBox.Text = itemName;
                    window.ItemList.SelectedItem = window.Model.Items.Single(item => item.Name == itemName && item.Category == category);
                    var grade = Input<ComboBox>("cyberwarePickerGrade");
                    await ExpectAsync(() => !grade.IsEnabled && window.Model.SelectedGrade?.Name == "Standard",
                        "Native " + mode + " retains caller grade lock after selection");
                    var rating = Input<NumericUpDown>("cyberwarePickerRating");
                    if (bio) rating.Text = "2";
                    await ExpectAsync(() => window.Model.Rating == (bio ? 2 : 0) && rating.IsEnabled == bio,
                        "Native " + mode + " binds rated and unrated catalog entries");
                    if (bio)
                    {
                        rating.GetVisualDescendants().OfType<TextBox>().Single().RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Up });
                        await ExpectAsync(() => window.Model.Rating == 3, "Native " + mode + " keyboard increases whole rating");
                        rating.Text = "2";
                    }
                    var discount = Input<NumericUpDown>("cyberwarePickerEssenceDiscount");
                    discount.Text = "101";
                    await ExpectAsync(() => discount.Value == 100 && window.Model.EssenceDiscount == 100,
                        "Native " + mode + " clips typed positive essence discount bound");
                    discount.Text = "-101";
                    await ExpectAsync(() => discount.Value == -100 && window.Model.EssenceDiscount == -100,
                        "Native " + mode + " preserves signed negative essence discount bound");
                    discount.Text = "-10";
                    discount.GetVisualDescendants().OfType<TextBox>().Single().RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Up });
                    await ExpectAsync(() => window.Model.EssenceDiscount == -9, "Native " + mode + " keyboard increments signed essence discount");
                    discount.Text = "10";
                    discount.Text = "-";
                    await ExpectAsync(() => window.Model.EssenceDiscount == 10 && dialog.DialogResult == Forms.DialogResult.Cancel && dialog.SelectedCyberware == "",
                        "Native " + mode + " incomplete signed text retains last whole draft without publishing purchase");
                    discount.Text = "10";
                    if (bio)
                    {
                        rating.Text = "";
                        await ExpectAsync(() => window.Model.Rating == 2 && dialog.SelectedCyberware == "",
                            "Native " + mode + " incomplete rating text retains draft without publishing purchase");
                        rating.Text = "2";
                    }
                    window.SearchBox.Text = "missing \"Ware's\" entry";
                    await ExpectAsync(() => window.Model.Items.Count == 0 && window.Model.Details == null && !window.AcceptButton.IsEnabled,
                        "Native " + mode + " literal quoted search clears stale purchase");
                    window.SearchBox.Text = itemName;
                    window.ItemList.SelectedItem = window.Model.Items.Single(item => item.Name == itemName && item.Category == category);
                    await ExpectAsync(() => window.AcceptButton.IsEnabled && window.Model.Details?.Source.Length > 0,
                        "Native " + mode + " search recovery restores canonical source details");
                    // Exercise the real source button and its missing-link recovery without
                    // opening a file picker or relying on a runner's PDF configuration.
                    var sourcebook = GlobalOptions.Instance.SourcebookInfo.FirstOrDefault(book => book.Code == "SR4");
                    string? previousPath = sourcebook?.Path; var previousNotice = AppErrors.Notify;
                    int sourceNotices = 0;
                    try
                    {
                        if (sourcebook != null) sourcebook.Path = "";
                        AppErrors.Notify = (_, message, _) =>
                        {
                            if (message != LanguageManager.Instance.GetString("Message_PdfNotLinked"))
                                throw new InvalidOperationException("Unexpected Cyberware source notice.");
                            sourceNotices++;
                        };
                        window.GetVisualDescendants().OfType<Button>().Single(button => button.Content?.ToString() == window.Model.Details!.Source)
                            .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                        await ExpectAsync(() => sourceNotices == 1 && window.IsEnabled && !nativeOwner.IsEnabled && !pending.IsCompleted,
                            "Native " + mode + " source PDF missing-link notice restores picker and retains modal owner");
                    }
                    finally { AppErrors.Notify = previousNotice; if (sourcebook != null) sourcebook.Path = previousPath!; }
                    var previousTheme = nativeOwner.RequestedThemeVariant;
                    try
                    {
                        foreach (var theme in new[] { ThemeVariant.Dark, ThemeVariant.Light })
                        {
                            nativeOwner.RequestedThemeVariant = theme; window.Width = 820; window.Height = 650;
                            await ExpectAsync(() => window.ActualThemeVariant == theme && discount.Bounds.Width >= 110 && grade.Bounds.Height > 0,
                                "Native " + mode + " framework inputs follow " + (theme == ThemeVariant.Dark ? "dark" : "light") + " owner theme");
                            await Task.Delay(100);
                            using var bitmap = new RenderTargetBitmap(new PixelSize((int)window.ClientSize.Width * 2, (int)window.ClientSize.Height * 2), new Vector(192,192));
                            bitmap.Render(window); bitmap.Save(Path.Combine(output, "native-" + mode + "-" + (theme == ThemeVariant.Dark ? "dark" : "light") + ".png"), PngBitmapEncoderOptions.Default);
                        }
                    }
                    finally { nativeOwner.RequestedThemeVariant = previousTheme; }
                    if (career) Input<CheckBox>("cyberwarePickerFreeCost").IsChecked = true;
                    window.AddMoreButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    await ExpectAsync(() => pending.IsCompleted, "Native " + mode + " Add More completes owned modal");
                    check(await pending == Forms.DialogResult.OK && dialog.SelectedCyberware == itemName && dialog.SelectedCategory == category &&
                        dialog.SelectedGrade?.Name == "Standard" && dialog.SelectedRating == (bio ? 2 : 0) && dialog.AddAgain && dialog.FreeCost == career && nativeOwner.IsEnabled,
                        "Native " + mode + " publishes accepted canonical draft and restores owner");
                    check(character.CaptureSnapshot().SequenceEqual(snapshot), "Native " + mode + " draft acceptance preserves full character snapshot");
                }
                finally { if (!pending.IsCompleted) window.Close(); }
            }
            foreach (bool explicitCancel in new[] { false, true })
            using (var dialog = new frmSelectCyberware(character, career) { WindowMode = wareMode })
            {
                var pending = dialog.ShowDialogAsync(owner); var window = (CyberwarePickerWindow)NativeDialogHost.ActiveWindow!;
                try
                {
                    await ExpectAsync(() => window.IsVisible && !nativeOwner.IsEnabled, "Native " + mode + " cancellation opens owned window explicit=" + explicitCancel);
                    if (explicitCancel) window.GetVisualDescendants().OfType<Button>().Single(button => button.IsCancel).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    else window.Close();
                    check(await pending == Forms.DialogResult.Cancel && dialog.SelectedCyberware == "" && dialog.SelectedCategory == "" &&
                        dialog.SelectedGrade == null && !dialog.AddAgain && nativeOwner.IsEnabled && character.CaptureSnapshot().SequenceEqual(snapshot),
                        "Native " + mode + " cancellation discards draft and restores owner snapshot explicit=" + explicitCancel);
                }
                finally { if (!pending.IsCompleted) window.Close(); }
            }
        }
    }
}
