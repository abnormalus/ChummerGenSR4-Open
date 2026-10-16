#nullable enable
using System;
using System.Linq;
using System.Threading.Tasks;
using Majorsilence.Forms;
using NativeMenu = Avalonia.Controls.NativeMenu;
using NativeMenuItem = Avalonia.Controls.NativeMenuItem;

namespace Chummer;

internal static class NativeControlChecks
{
    // Use the exporter's click bridge to exercise even a queued stale selection.
    // Disabling a menu visually alone does not protect that invocation path.
    internal static async Task RunAsync(Action<bool, string> check)
    {
        var application = Avalonia.Application.Current!;
        var previousAppMenu = NativeMenu.GetMenu(application);
        var previousItems = previousAppMenu?.Items.ToArray();
        using var owner = new Form { Text = "Native menu checks" };
        using var dialog = new Form { Text = "Modal menu checks" };
        using var source = new MenuStrip();
        int commands = 0, preferences = 0, quits = 0;
        var action = new ToolStripMenuItem("Check command"); action.Click += (_, _) => commands++;
        var file = new ToolStripMenuItem("Check menu"); file.DropDownItems.Add(action); source.Items.Add(file);
        var settings = new ToolStripMenuItem("Check preferences"); settings.Click += (_, _) => preferences++;
        var about = new ToolStripMenuItem("Check about");
        var quit = new ToolStripMenuItem("Check quit"); quit.Click += (_, _) => quits++;
        try
        {
            owner.Controls.Add(source);
            owner.Show();
            DesktopMenus.Install(owner, source, settings, about, quit);
            var nativeOwner = owner.ToAvaloniaWindow();
            var root = NativeMenu.GetMenu(nativeOwner)!;
            NativeMenuItem Command() => (NativeMenuItem)((NativeMenuItem)root.Items.Single()).Menu!.Items.Single();
            static void Click(NativeMenuItem item) => typeof(NativeMenuItem).GetInterfaces().Single(i => i.Name == "INativeMenuItemExporterEventsImplBridge")
                .GetMethod("RaiseClicked")!.Invoke(item, null);
            var staleCommand = Command();
            Click(staleCommand);
            check(commands == 1, "Native menu invokes enabled commands");
            var app = NativeMenu.GetMenu(application)!;
            var nativeSettings = app.Items.OfType<NativeMenuItem>().Single(i => i.Header == "Check preferences");
            var nativeQuit = app.Items.OfType<NativeMenuItem>().Single(i => i.Header == "Check quit");
            var pending = dialog.ShowDialogAsync(owner);
            try
            {
                check(!nativeOwner.IsEnabled && !Command().IsEnabled && !nativeSettings.IsEnabled,
                    "Native menus disable owner commands and preferences during a modal dialog");
                Click(staleCommand); Click(nativeSettings);
                check(commands == 1 && preferences == 0, "Queued native menu clicks cannot bypass a modal dialog");
                Click(nativeQuit);
                check(nativeQuit.IsEnabled && quits == 1, "Native Quit remains available during modal work");
            }
            finally { dialog.Close(); await pending; }
            Click(Command()); Click(nativeSettings);
            check(nativeOwner.IsEnabled && commands == 2 && preferences == 1,
                "Native commands and preferences recover after modal cleanup");
            action.Enabled = false;
            Click(staleCommand);
            check(commands == 2, "A queued native click respects an item disabled after menu creation");
            action.Enabled = true; file.DropDownItems.Remove(action); DesktopMenus.Refresh(owner);
            Click(staleCommand);
            check(commands == 2, "A queued native click cannot invoke a detached character command");
            nativeOwner.WindowState = Avalonia.Controls.WindowState.FullScreen;
            await Task.Delay(100);
            check(source.Visible, "Native full-screen windows retain an in-window menu fallback");
            nativeOwner.WindowState = Avalonia.Controls.WindowState.Normal;
            await Task.Delay(100);
            check(!source.Visible, "Native menu fallback hides again after leaving full screen");
        }
        finally
        {
            owner.Close();
            if (previousAppMenu != null && previousItems != null)
            {
                previousAppMenu.Items.Clear();
                foreach (var item in previousItems) previousAppMenu.Items.Add(item);
            }
            NativeMenu.SetMenu(application, previousAppMenu);
        }
    }
}
