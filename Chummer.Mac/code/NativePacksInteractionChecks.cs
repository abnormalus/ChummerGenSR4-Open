#nullable enable
using System;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml;
using Majorsilence.Forms;

namespace Chummer;

// PACKS replaces the editor rather than mutating it in place. Exercise the actual
// replacement and button handlers so generic modal tests cannot miss stale ownership.
internal static class NativePacksInteractionChecks
{
    private static T Field<T>(object instance, string name) => (T)instance.GetType()
        .GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(instance)!;

    private static async Task WaitForAsync(Func<bool> ready)
    {
        var deadline = DateTime.UtcNow.AddSeconds(15);
        while (!ready())
        {
            if (DateTime.UtcNow >= deadline) throw new TimeoutException("PACKS interaction did not reach its expected state.");
            await Task.Delay(20);
        }
    }

    internal static async Task RunAsync(frmMain main, Character source, string output, Action<bool, string> check)
    {
        var workspace = main.Controls.OfType<MdiClient>().Single();
        check(main.MdiChildren.Length == 0 && workspace.BackgroundImage != null && workspace.BackgroundImageLayout == ImageLayout.Zoom,
            "Native empty workspace displays aspect-preserving artwork before character selection");
        using (var artwork = SkiaSharp.SKBitmap.Decode(Path.Combine(AppContext.BaseDirectory, "branding", "workspace-emblem.png")))
            check(artwork != null && artwork.GetPixel(0, 0).Alpha == 0 &&
                artwork.GetPixel(artwork.Width - 1, artwork.Height - 1).Alpha == 0,
                "Native workspace artwork retains transparent corners instead of an opaque rectangle");
        var app = Avalonia.Application.Current!;
        var originalTheme = app.RequestedThemeVariant;
        try
        {
            app.RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Dark;
            await Task.Delay(120);
            var darkBackground = workspace.Style.GetBackgroundColor();
            app.RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Light;
            await Task.Delay(120);
            check(darkBackground != workspace.Style.GetBackgroundColor(),
                "Native legacy workspace follows live application light and dark theme changes");
        }
        finally { app.RequestedThemeVariant = originalTheme; }
        var nativeMain = main.ToAvaloniaWindow();
        double originalWidth = nativeMain.Width, originalHeight = nativeMain.Height;
        var originalState = nativeMain.WindowState;
        try
        {
            nativeMain.WindowState = Avalonia.Controls.WindowState.Normal;
            foreach (var theme in new[] { Avalonia.Styling.ThemeVariant.Light, Avalonia.Styling.ThemeVariant.Dark })
            foreach (var size in new[] { (Width: 900, Height: 650), (Width: 1200, Height: 850) })
            {
                app.RequestedThemeVariant = theme;
                nativeMain.Width = size.Width; nativeMain.Height = size.Height; await Task.Delay(120);
                // Capture only the empty workspace: no character fields are visible.
                using var image = new Avalonia.Media.Imaging.RenderTargetBitmap(
                    new Avalonia.PixelSize((int)nativeMain.ClientSize.Width, (int)nativeMain.ClientSize.Height));
                image.Render(nativeMain);
                image.Save(Path.Combine(output, "native-empty-workspace-" + theme.Key.ToString()!.ToLowerInvariant() + "-" + size.Width + ".png"),
                    Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
            }
        }
        finally {
            app.RequestedThemeVariant = originalTheme;
            nativeMain.Width = originalWidth; nativeMain.Height = originalHeight; nativeMain.WindowState = originalState;
        }
        var copy = new Character(); copy.LoadSnapshot(source.CaptureSnapshot());
        var editor = new frmCreate(copy) { Loading = true, MdiParent = main };
        frmCreate? replacement = null;
        int priorToolbarFailures = main.ToolbarRefreshFailureCount;
        editor.Show();
        check(workspace.BackgroundImage == null, "Native workspace artwork hides while a character is open");
        try
        {
            var kit = new XmlDocument();
            kit.LoadXml("<pack><name>Check</name><category>Custom</category><gears><gear><name>Backpack</name></gear></gears></pack>");
            replacement = await editor.ImportPacksKitAsync(kit.DocumentElement!, false);
            check(replacement != null && editor.IsDisposed && main.ActiveMdiChild == replacement &&
                main.Enabled && main.ToAvaloniaWindow().IsEnabled && replacement.Enabled,
                "Native PACKS replacement restores an enabled active character owner");
            var armor = Field<Button>(replacement!, "cmdAddArmor");
            var gear = Field<Button>(replacement!, "cmdAddGear");
            var mainTabs = Field<TabControl>(replacement!, "tabCharacterTabs");
            mainTabs.SelectedTab = Field<TabPage>(replacement!, "tabStreetGear");
            var streetTabs = Field<TabControl>(replacement!, "tabStreetGearTabs");
            streetTabs.SelectedTab = Field<TabPage>(replacement!, "tabArmor");
            var armorObserved = new TaskCompletionSource<bool>();
            Avalonia.Threading.Dispatcher.UIThread.Post(async () => {
                try {
                    await WaitForAsync(() => NativeDialogHost.ActiveWindow is ArmorPickerWindow);
                    check(!main.ToAvaloniaWindow().IsEnabled, "Native post-PACKS armor picker disables the correct owner");
                    NativeDialogHost.ActiveWindow!.Close(); armorObserved.TrySetResult(true);
                } catch (Exception error) { NativeDialogHost.CancelAll(); armorObserved.TrySetException(error); }
            });
            armor.PerformClick();
            await armorObserved.Task;
            check(main.ToAvaloniaWindow().IsEnabled && main.ActiveMdiChild == replacement,
                "Native post-PACKS Add Armor opens and cancels back to the replacement editor");
            streetTabs.SelectedTab = Field<TabPage>(replacement!, "tabGear");
            var gearObserved = new TaskCompletionSource<bool>();
            Avalonia.Threading.Dispatcher.UIThread.Post(async () => {
                try {
                    await WaitForAsync(() => Application.OpenForms.OfType<frmSelectGear>().Any());
                    Application.OpenForms.OfType<frmSelectGear>().Last().Close();
                    gearObserved.TrySetResult(true);
                } catch (Exception error) { gearObserved.TrySetException(error); }
            });
            gear.PerformClick();
            await gearObserved.Task;
            check(main.Enabled && main.ToAvaloniaWindow().IsEnabled && replacement!.Enabled,
                "Native post-PACKS Add Gear opens and cancels without leaving the editor blocked");
            check(main.ToolbarRefreshFailureCount == priorToolbarFailures,
                "Native PACKS replacement and purchase cancellation preserve toolbar roots without merge exceptions");
        }
        finally
        {
            NativeDialogHost.CancelAll();
            foreach (var characterEditor in new[] { replacement, editor }.Where(item => item != null && !item.IsDisposed))
            {
                characterEditor!.GetType().GetField("_blnIsDirty", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(characterEditor, false);
                characterEditor.Close();
            }
        }
        check(main.MdiChildren.Length == 0 && workspace.BackgroundImage != null,
            "Native workspace artwork returns after the final character closes");
    }
}
