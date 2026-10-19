#nullable enable
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Majorsilence.Forms;

namespace Chummer;

// Exercise real editors: generic MDI forms cannot expose toolbar ownership or
// character command availability when activation and close events interleave.
internal static class NativeCharacterSwitchChecks
{
    private static T Field<T>(object owner, string name) => (T)owner.GetType()
        .GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(owner)!;

    internal static async Task RunAsync(frmMain main, Character character, Action<bool, string> check)
    {
        byte[] original = character.CaptureSnapshot();
        if (System.OperatingSystem.IsMacOS())
        {
            var nativeMain = main.ToAvaloniaWindow();
            nativeMain.WindowState = Avalonia.Controls.WindowState.FullScreen; await Task.Delay(100);
            check(Field<MenuStrip>(main, "menuStrip").Visible && Field<ToolStripMenuItem>(main, "optionsToolStripMenuItem").Visible &&
                Field<ToolStripMenuItem>(main, "exitToolStripMenuItem").Visible,
                "Mac full-screen fallback includes Preferences and Quit");
            nativeMain.WindowState = Avalonia.Controls.WindowState.Normal; await Task.Delay(100);
            check(!Field<MenuStrip>(main, "menuStrip").Visible && !Field<ToolStripMenuItem>(main, "optionsToolStripMenuItem").Visible &&
                !Field<ToolStripMenuItem>(main, "exitToolStripMenuItem").Visible,
                "Mac leaving full screen restores application-menu command placement");
        }

        Character Copy() { var copy = new Character(); copy.LoadSnapshot(original); return copy; }
        using var first = new frmCreate(Copy()) { Loading = true, MdiParent = main };
        var careerCharacter = Copy(); careerCharacter.Created = true;
        using var second = new frmCareer(careerCharacter) { Loading = true, MdiParent = main };
        var tabs = Field<TabControl>(main, "tabForms");
        var toolbar = Field<ToolStrip>(main, "toolStrip");
        var menus = Field<MenuStrip>(main, "menuStrip");
        var save = Field<ToolStripMenuItem>(main, "_mnuSaveCharacter");
        var close = Field<ToolStripMenuItem>(main, "_mnuCloseCharacter");
        var previousClipboardType = GlobalOptions.Instance.ClipboardContentType;
        first.Show(); second.Show();
        try
        {
            var packsTab = Field<TabControl>(first, "tabStreetGearTabs").TabPages.Cast<TabPage>().Single(page => page.Name == "tabPacks");
            check(packsTab.Text == "PACKS" && packsTab.Controls.OfType<Button>().Count() == 2,
                "Native Street Gear exposes PACKS browsing/import and creation commands");
            // Clipboard contents are shared across editors, unlike selection state.
            // Returning to an unchanged tab must re-evaluate its edit commands.
            GlobalOptions.Instance.ClipboardContentType = ClipboardContentType.Gear;
            Field<TabControl>(first, "tabCharacterTabs").SelectedTab = Field<TabPage>(first, "tabStreetGear");
            Field<TabControl>(first, "tabStreetGearTabs").SelectedTab = Field<TabPage>(first, "tabGear");
            first.Select(); second.Select();
            GlobalOptions.Instance.ClipboardContentType = ClipboardContentType.Lifestyle;
            first.Select();
            check(!Field<ToolStripMenuItem>(first, "mnuEditPaste").Enabled && !Field<ToolStripButton>(first, "tsbPaste").Enabled,
                "Returning to a character disables stale Paste after incompatible clipboard changes");
            second.Select();
            GlobalOptions.Instance.ClipboardContentType = ClipboardContentType.Gear;
            first.Select();
            check(Field<ToolStripMenuItem>(first, "mnuEditPaste").Enabled && Field<ToolStripButton>(first, "tsbPaste").Enabled,
                "Returning to a character enables compatible Paste in both menu and toolbar");
            for (int iteration = 0; iteration < 5; iteration++)
            {
                first.Select(); await Task.Delay(20);
                check(main.ActiveMdiChild == first && tabs.SelectedTab == first.Tag &&
                    toolbar.Items.Contains(Field<ToolStripButton>(first, "tsbSave")) &&
                    !toolbar.Items.Contains(Field<ToolStripButton>(second, "tsbSave")) && save.Enabled && close.Enabled,
                    "Native repeated character switch keeps tab, toolbar and commands aligned " + iteration);
                check(menus.Items.Contains(Field<ToolStripMenuItem>(first, "mnuCreateSpecial")) &&
                    menus.Items.Contains(Field<ToolStripMenuItem>(first, "mnuCreateEdit")) &&
                    !menus.Items.Contains(Field<ToolStripMenuItem>(second, "mnuCreateSpecial")) &&
                    menus.Items.IndexOf(Field<ToolStripMenuItem>(main, "fileMenu")) <
                        menus.Items.IndexOf(Field<ToolStripMenuItem>(first, "mnuCreateEdit")),
                    "Native creation menu ownership follows active character " + iteration);
                if (System.OperatingSystem.IsMacOS())
                {
                    var nativeMenu = Avalonia.Controls.NativeMenu.GetMenu(main.ToAvaloniaWindow())!;
                    check(nativeMenu.Items.OfType<Avalonia.Controls.NativeMenuItem>().Take(2).Select(item => item.Header)
                        .SequenceEqual(new[] { "File", "Edit" }) &&
                        nativeMenu.Items.OfType<Avalonia.Controls.NativeMenuItem>().Any(item => item.Header == "Special" &&
                        item.Menu!.Items.OfType<Avalonia.Controls.NativeMenuItem>().Any(command => command.Header == "Add PACKS Kit")),
                        "Mac native Special menu exposes PACKS after character switch " + iteration);
                }
                tabs.SelectedTab = (TabPage)second.Tag!;
                await Task.Delay(20);
            }
            // Closing an inactive editor must not undo the active editor's merge.
            typeof(frmCreate).GetField("_blnIsDirty", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(first, false);
            first.Close();
            check(main.ActiveMdiChild == second && tabs.SelectedTab == second.Tag &&
                toolbar.Items.Contains(Field<ToolStripButton>(second, "tsbSave")) && save.Enabled &&
                menus.Items.IndexOf(Field<ToolStripMenuItem>(main, "fileMenu")) <
                    menus.Items.IndexOf(Field<ToolStripMenuItem>(second, "mnuCreateEdit")),
                "Native inactive character close retains the active save toolbar and menu");
            // Invoke the shared Close Character command, rather than closing the form directly.
            typeof(frmCareer).GetField("_blnIsDirty", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(second, false);
            close.PerformClick();
            check(second.IsDisposed && main.ActiveMdiChild == null && tabs.TabCount == 0 &&
                !menus.Items.OfType<ToolStripMenuItem>().Any(item => item.Name == "mnuCreateSpecial" || item.Name == "mnuCreateEdit") &&
                !save.Enabled && !close.Enabled && !toolbar.Items.Contains(Field<ToolStripButton>(second, "tsbSave")) &&
                !toolbar.Items.OfType<ToolStripButton>().Any(item => item.Name == "tsbSave" && item.Visible),
                "Native Close Character clears final tab, toolbar and document command availability");
        }
        finally
        {
            GlobalOptions.Instance.ClipboardContentType = previousClipboardType;
            foreach (var editor in new Form[] { first, second })
                if (!editor.IsDisposed)
                {
                    editor.GetType().GetField("_blnIsDirty", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(editor, false);
                    editor.Close();
                }
        }
        check(character.CaptureSnapshot().SequenceEqual(original), "Native character switching leaves the source character unchanged");
    }
}
