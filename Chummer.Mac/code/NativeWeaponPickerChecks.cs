#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.LogicalTree;
using Forms = Majorsilence.Forms;

namespace Chummer;

internal static class NativeWeaponPickerChecks
{
    private static T Find<T>(Window window, string name) where T : Control =>
        window.GetLogicalDescendants().OfType<T>().Single(control => control.Name == name);

    private static async Task WaitForAsync(Func<bool> condition)
    {
        DateTime deadline = DateTime.UtcNow.AddSeconds(15);
        while (!condition())
        {
            if (DateTime.UtcNow >= deadline) throw new TimeoutException("Native weapon picker did not reach the expected state.");
            await Task.Delay(20);
        }
    }
    internal static async Task RunAsync(Forms.Form main, Character character, string output, Action<bool, string> check)
    {
        byte[] original = character.CaptureSnapshot();
        using (var picker = new frmSelectWeapon(character, true))
        {
            var pending = picker.ShowDialogAsync(main);
            var window = (WeaponPickerWindow)NativeDialogHost.ActiveWindow!;
            window.SearchBox.Text = "Ares Predator";
            try { await WaitForAsync(() => window.WeaponList.SelectedItem is CatalogEntry && window.AcceptButton.IsEnabled); }
            catch
            {
                File.WriteAllText(Path.Combine(output, "native-picker-state.json"), System.Text.Json.JsonSerializer.Serialize(new {
                    modelItems = window.Model.Items.Count, listItems = window.WeaponList.ItemCount,
                    modelSelection = window.Model.SelectedEntry?.Name, listSelection = (window.WeaponList.SelectedItem as CatalogEntry)?.Name,
                    modelSearch = window.Model.SearchText, inputSearch = window.SearchBox.Text, window.Model.CanAccept, window.Model.Error
                }));
                using var failed = new RenderTargetBitmap(new PixelSize((int)window.ClientSize.Width, (int)window.ClientSize.Height));
                failed.Render(window); failed.Save(Path.Combine(output, "native-picker-failed.png"), PngBitmapEncoderOptions.Default);
                throw;
            }
            check(window.Model.SearchText == "Ares Predator" && Equals(window.Model.SelectedEntry, window.WeaponList.SelectedItem) &&
                !Forms.AvaloniaHostInterop.ToAvaloniaWindow(main).IsEnabled, "Native weapon list binding selects a real entry and disables its owner");
            var markup = Find<NumericUpDown>(window, "weaponMarkup");
            var free = Find<CheckBox>(window, "weaponFreeCost");
            markup.Value = 25; free.IsChecked = true;
            await WaitForAsync(() => window.Model.Markup == 25 && window.Model.FreeCost);
            check(Find<TextBlock>(window, "weaponCost").Text == window.Model.Details!.Cost,
                "Native career controls bind cost, markup and free purchase without manual label refresh");
            window.SearchBox.Text = "no \"matching\" weapon";
            await WaitForAsync(() => window.WeaponList.ItemCount == 0 && !window.AcceptButton.IsEnabled);
            check(window.Model.Details == null && string.IsNullOrEmpty(Find<TextBlock>(window, "weaponCost").Text),
                "Native empty search clears cost details and disables acceptance");
            window.Model.Category = window.Model.Categories.Single(category => category.Code == "Assault Rifles");
            await WaitForAsync(() => window.WeaponList.ItemCount > 0 && window.AcceptButton.IsEnabled);
            window.WeaponList.Focus();
            foreach (string letter in new[] { "a", "r", "e" })
                window.WeaponList.RaiseEvent(new TextInputEventArgs { RoutedEvent = InputElement.TextInputEvent, Text = letter });
            check(window.Model.SelectedEntry!.Name.StartsWith("Are", StringComparison.CurrentCultureIgnoreCase),
                "Native weapon list uses cumulative prefix typing instead of separate initial letters");
            window.Width = 760; window.Height = 480;
            await Task.Delay(120);
            using (var bitmap = new RenderTargetBitmap(new PixelSize((int)window.ClientSize.Width, (int)window.ClientSize.Height)))
            {
                bitmap.Render(window); bitmap.Save(Path.Combine(output, "native-weapon-picker.png"), PngBitmapEncoderOptions.Default);
            }
            check(window.WeaponList.Bounds.Width >= 200 && window.AcceptButton.IsEffectivelyVisible,
                "Native weapon picker remains usable at its smaller resizable layout");
            window.AddMoreButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            check(await pending == Forms.DialogResult.OK && picker.AddAgain && picker.FreeCost && picker.Markup == 25 &&
                picker.SelectedDefinition["name"]!.InnerText == picker.SelectedWeapon,
                "Native Add More returns the same catalog entry and purchase options to existing callers");
        }
        check(NativeDialogHost.Count == 0 && Forms.AvaloniaHostInterop.ToAvaloniaWindow(main).IsEnabled && character.CaptureSnapshot().SequenceEqual(original),
            "Closing the native picker restores its owner without changing character inventory");

        using (var cancelled = new frmSelectWeapon(character))
        {
            var pending = cancelled.ShowDialogAsync(main);
            NativeDialogHost.ActiveWindow!.Close();
            check(await pending == Forms.DialogResult.Cancel && cancelled.SelectedWeapon == "" && !cancelled.AddAgain,
                "Native title-bar close cancels without retaining a selected weapon");
        }
        using (var cancelledByQuit = new frmSelectWeapon(character))
        {
            var pending = cancelledByQuit.ShowDialogAsync(main);
            Forms.FormClosingEventHandler keepMain = (_, args) => args.Cancel = true;
            main.FormClosing += keepMain;
            try { await AppPlatform.QuitApplicationAsync(); }
            finally { main.FormClosing -= keepMain; }
            check(await pending == Forms.DialogResult.Cancel && NativeDialogHost.Count == 0 && Forms.AvaloniaHostInterop.ToAvaloniaWindow(main).IsEnabled,
                "Quit cancels native weapon selection before the main close sequence");
        }

        // Drive the existing synchronous creation handler through its real modal boundary.
        // This proves that the migrated dialog still feeds the established inventory logic.
        var testCharacter = new Character(); testCharacter.LoadSnapshot(original);
        using var editor = new frmCreate(testCharacter) { Loading = true, MdiParent = main };
        editor.Show();
        try
        {
            int count = testCharacter.Weapons.Count;
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                var window = (WeaponPickerWindow)NativeDialogHost.ActiveWindow!;
                window.SearchBox.Text = "Ares Predator";
                window.Accept(false);
            });
            typeof(frmCreate).GetMethod("cmdAddWeapon_Click", BindingFlags.NonPublic | BindingFlags.Instance)!
                .Invoke(editor, new object[] { editor, EventArgs.Empty });
            check(testCharacter.Weapons.Count == count + 1 && testCharacter.Weapons.Last().Name.Contains("Ares Predator"),
                "Existing creation handler adds the selected native-picker weapon through its established rule path");
        }
        finally
        {
            typeof(frmCreate).GetField("_blnIsDirty", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(editor, false);
            editor.Close();
        }
    }
}
