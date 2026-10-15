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
        Character Copy() { var copy = new Character(); copy.LoadSnapshot(original); return copy; }
        using var first = new frmCreate(Copy()) { Loading = true, MdiParent = main };
        var careerCharacter = Copy(); careerCharacter.Created = true;
        using var second = new frmCareer(careerCharacter) { Loading = true, MdiParent = main };
        var tabs = Field<TabControl>(main, "tabForms");
        var toolbar = Field<ToolStrip>(main, "toolStrip");
        var save = Field<ToolStripMenuItem>(main, "_mnuSaveCharacter");
        var close = Field<ToolStripMenuItem>(main, "_mnuCloseCharacter");
        var previousClipboardType = GlobalOptions.Instance.ClipboardContentType;
        first.Show(); second.Show();
        try
        {
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
                tabs.SelectedTab = (TabPage)second.Tag!;
                await Task.Delay(20);
            }
            // Closing an inactive editor must not undo the active editor's merge.
            typeof(frmCreate).GetField("_blnIsDirty", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(first, false);
            first.Close();
            check(main.ActiveMdiChild == second && tabs.SelectedTab == second.Tag &&
                toolbar.Items.Contains(Field<ToolStripButton>(second, "tsbSave")) && save.Enabled,
                "Native inactive character close retains the active save toolbar and menu");
            // Invoke the shared Close Character command, rather than closing the form directly.
            typeof(frmCareer).GetField("_blnIsDirty", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(second, false);
            close.PerformClick();
            check(second.IsDisposed && main.ActiveMdiChild == null && tabs.TabCount == 0 &&
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
