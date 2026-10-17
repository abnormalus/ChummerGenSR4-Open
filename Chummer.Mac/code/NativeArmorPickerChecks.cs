#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Forms = Majorsilence.Forms;

namespace Chummer;

internal static class NativeArmorPickerChecks
{
    private static T Find<T>(Window window, string name) where T : Control =>
        window.GetLogicalDescendants().OfType<T>().Single(control => control.Name == name);
    private static async Task WaitForAsync(Func<bool> condition)
    {
        DateTime deadline = DateTime.UtcNow.AddSeconds(15);
        while (!condition())
        {
            if (DateTime.UtcNow >= deadline) throw new TimeoutException("Native armor picker did not reach the expected state.");
            await Task.Delay(20);
        }
    }
    internal static async Task RunAsync(Forms.Form main, Character character, string output, Action<bool, string> check)
    {
        byte[] original = character.CaptureSnapshot();
        var nativeOwner = Forms.AvaloniaHostInterop.ToAvaloniaWindow(main);
        var app = Avalonia.Application.Current!;
        var originalApplicationTheme = app.RequestedThemeVariant;
        var originalOwnerTheme = nativeOwner.RequestedThemeVariant;
        try
        {
            // A native child is a separate theme root. An explicit owner override
            // must win over an opposite application default, including live changes.
            app.RequestedThemeVariant = ThemeVariant.Light;
            nativeOwner.RequestedThemeVariant = ThemeVariant.Dark;
            await Task.Delay(80);
            using var themedPicker = new frmSelectArmor(character);
            var shown = themedPicker.ShowDialogAsync(main);
            var themedWindow = (ArmorPickerWindow)NativeDialogHost.ActiveWindow!;
            await Task.Delay(80);
            bool followsOwner = themedWindow.ActualThemeVariant == nativeOwner.ActualThemeVariant;
            themedWindow.SearchBox.Text = "Armor Jacket";
            await WaitForAsync(() => themedWindow.AcceptButton.IsEnabled &&
                themedWindow.ItemList.ContainerFromIndex(0) is { IsEffectivelyVisible: true } row && row.Bounds.Height > 0);
            await Task.Delay(100);
            void CaptureTheme(string name)
            {
                using var bitmap = new RenderTargetBitmap(new PixelSize((int)themedWindow.ClientSize.Width * 2, (int)themedWindow.ClientSize.Height * 2), new Vector(192, 192));
                bitmap.Render(themedWindow); bitmap.Save(Path.Combine(output, name), PngBitmapEncoderOptions.Default);
            }
            CaptureTheme("native-armor-owner-dark.png");
            nativeOwner.RequestedThemeVariant = ThemeVariant.Light;
            await Task.Delay(80);
            bool followsLight = themedWindow.ActualThemeVariant == ThemeVariant.Light;
            CaptureTheme("native-armor-owner-light.png");
            nativeOwner.RequestedThemeVariant = ThemeVariant.Dark;
            await Task.Delay(80);
            bool followsDarkAgain = themedWindow.ActualThemeVariant == ThemeVariant.Dark;
            themedWindow.Close(); await shown;
            var closedTheme = themedWindow.RequestedThemeVariant;
            nativeOwner.RequestedThemeVariant = ThemeVariant.Light;
            await Task.Delay(40);
            check(followsOwner, "Native armor dialog follows explicit dark owner over light application default");
            check(followsLight && followsDarkAgain, "Native armor dialog follows live owner theme changes in both directions");
            check(themedWindow.RequestedThemeVariant == closedTheme && nativeOwner.IsEnabled,
                "Native dialog closure detaches theme subscription and restores owner input");
        }
        finally
        {
            nativeOwner.RequestedThemeVariant = originalOwnerTheme;
            app.RequestedThemeVariant = originalApplicationTheme;
        }

        using (var picker = new frmSelectArmor(character, true))
        {
            var pending = picker.ShowDialogAsync(main);
            var window = (ArmorPickerWindow)NativeDialogHost.ActiveWindow!;
            window.SearchBox.Text = "Armor Jacket";
            await WaitForAsync(() => window.ItemList.SelectedItem is CatalogEntry && window.AcceptButton.IsEnabled);
            check(Equals(window.Model.SelectedEntry, window.ItemList.SelectedItem) &&
                !Forms.AvaloniaHostInterop.ToAvaloniaWindow(main).IsEnabled,
                "Native armor picker binds a selectable entry and respects owner modality");
            Find<NumericUpDown>(window, "armorMarkup").Value = 25;
            Find<CheckBox>(window, "armorFreeCost").IsChecked = true;
            await WaitForAsync(() => window.Model.Markup == 25 && window.Model.FreeCost);
            check(Find<TextBlock>(window, "armorCost").Text == window.Model.Details!.Cost && window.Model.Details.Cost.Contains("0"),
                "Native armor purchase controls bind markup and free cost");
            window.SearchBox.Text = "unmatched \"quoted\" armor";
            await WaitForAsync(() => window.ItemList.ItemCount == 0 && !window.AcceptButton.IsEnabled);
            check(window.Model.Details == null && string.IsNullOrEmpty(Find<TextBlock>(window, "armorBallistic").Text),
                "Native armor empty search clears protection details and acceptance");
            window.SearchBox.Text = "Armor Jacket";
            await WaitForAsync(() => window.AcceptButton.IsEnabled);
            window.Model.SelectedEntry = window.Model.Items.Single(entry => entry.Name == "Armor Jacket");
            window.Width = 760; window.Height = 480; await Task.Delay(120);
            using (var bitmap = new RenderTargetBitmap(new PixelSize((int)window.ClientSize.Width, (int)window.ClientSize.Height)))
            {
                bitmap.Render(window); bitmap.Save(Path.Combine(output, "native-armor-picker.png"), PngBitmapEncoderOptions.Default);
            }
            // Record the alternative theme at 2x resolution for manual visual QA;
            // logical row sizes and wrapping must remain independent of pixel density.
            var previousTheme = window.RequestedThemeVariant;
            window.RequestedThemeVariant = ThemeVariant.Light;
            await Task.Delay(60);
            using (var bitmap = new RenderTargetBitmap(new PixelSize((int)window.ClientSize.Width * 2, (int)window.ClientSize.Height * 2), new Vector(192, 192)))
            {
                bitmap.Render(window); bitmap.Save(Path.Combine(output, "native-armor-picker-light-2x.png"), PngBitmapEncoderOptions.Default);
            }
            window.RequestedThemeVariant = previousTheme;
            check(window.ItemList.Bounds.Width >= 200 && window.AcceptButton.IsEffectivelyVisible,
                "Native armor picker remains usable at its smaller resizable layout");
            window.AddMoreButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            check(await pending == Forms.DialogResult.OK && picker.AddAgain && picker.FreeCost && picker.Markup == 25 &&
                picker.SelectedArmor == "Armor Jacket" && picker.SelectedDefinition["name"]!.InnerText == picker.SelectedArmor,
                "Native armor Add More returns the exact definition and purchase options");
        }
        check(NativeDialogHost.Count == 0 && Forms.AvaloniaHostInterop.ToAvaloniaWindow(main).IsEnabled && character.CaptureSnapshot().SequenceEqual(original),
            "Native armor browsing restores owner state and leaves character data untouched");
        using (var cancelled = new frmSelectArmor(character))
        {
            var pending = cancelled.ShowDialogAsync(main); NativeDialogHost.ActiveWindow!.Close();
            check(await pending == Forms.DialogResult.Cancel && cancelled.SelectedArmor == "",
                "Closing the native armor dialog cancels without retaining a purchase");
        }
        // Preserve the established core creation path, including its included-item rules.
        var testCharacter = new Character(); testCharacter.LoadSnapshot(original);
        using var editor = new frmCreate(testCharacter) { Loading = true, MdiParent = main };
        editor.Show();
        try
        {
            int count = testCharacter.Armor.Count;
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                var window = (ArmorPickerWindow)NativeDialogHost.ActiveWindow!;
                window.SearchBox.Text = "Armor Jacket";
                window.Model.SelectedEntry = window.Model.Items.Single(entry => entry.Name == "Armor Jacket");
                window.Accept(false);
            });
            typeof(frmCreate).GetMethod("cmdAddArmor_Click", BindingFlags.NonPublic | BindingFlags.Instance)!
                .Invoke(editor, new object[] { editor, EventArgs.Empty });
            check(testCharacter.Armor.Count == count + 1 && testCharacter.Armor.Last().Name == "Armor Jacket",
                "Existing creation handler buys armor selected through the native dialog");
        }
        finally
        {
            typeof(frmCreate).GetField("_blnIsDirty", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(editor, false);
            editor.Close();
        }
    }
}
