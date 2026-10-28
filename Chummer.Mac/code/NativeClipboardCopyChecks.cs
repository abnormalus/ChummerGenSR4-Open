#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Forms = Majorsilence.Forms;

namespace Chummer;

// Drive successful Copy/Paste through real shown editors, complementing the
// independent headless serialization/failure baseline. Restore all shared state.
internal static class NativeClipboardCopyChecks
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    private static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, Flags)!.GetValue(owner)!;
    private static void GearTab(Forms.Form editor)
    {
        Field<Forms.TabControl>(editor, "tabCharacterTabs").SelectedTab = Field<Forms.TabPage>(editor, "tabStreetGear");
        Field<Forms.TabControl>(editor, "tabStreetGearTabs").SelectedTab = Field<Forms.TabPage>(editor, "tabGear");
    }
    private static void CloseClean(Forms.Form editor)
    {
        editor.GetType().GetField("_blnIsDirty", Flags)!.SetValue(editor, false); editor.Close();
    }
    internal static async Task RunAsync(Forms.Form main, Character original, Action<bool, string> check)
    {
        var oldDocument = GlobalOptions.Instance.Clipboard; var oldType = GlobalOptions.Instance.ClipboardContentType;
        var app = Avalonia.Application.Current!; var oldTheme = app.RequestedThemeVariant;
        try
        {
            foreach (bool career in new[] { false, true })
            foreach (var theme in new[] { Avalonia.Styling.ThemeVariant.Light, Avalonia.Styling.ThemeVariant.Dark })
            {
                var source = new Character(); source.LoadSnapshot(original.CaptureSnapshot()); source.Created = career;
                var target = new Character(); target.LoadSnapshot(original.CaptureSnapshot()); target.Created = false;
                using Forms.Form from = career ? new frmCareer(source) { Loading = true, MdiParent = main } : new frmCreate(source) { Loading = true, MdiParent = main };
                // Career owns Copy but no Paste command; both supported source
                // modes transfer into a separate creation editor.
                using Forms.Form to = new frmCreate(target) { Loading = true, MdiParent = main };
                from.Show(); to.Show();
                try
                {
                    app.RequestedThemeVariant = theme;
                    var gear = new Gear(source); var node = new Forms.TreeNode();
                    gear.Create(XmlManager.Instance.Load("gear.xml").SelectSingleNode("/chummer/gears/gear[name='Medkit']")!, source, node, 1,
                        new List<Weapon>(), new List<Forms.TreeNode>());
                    const string notes = "Synthetic native copy <雪> & 😀"; gear.Notes = notes; source.Gear.Add(gear);
                    Field<Forms.TreeView>(from, "treGear").Nodes[0].Nodes.Add(node);
                    GearTab(from); from.Select(); Field<Forms.TreeView>(from, "treGear").SelectedNode = node;
                    await Task.Delay(100);
                    byte[] before = source.CaptureSnapshot();
                    var copy = Field<Forms.ToolStripMenuItem>(from, "mnuEditCopy"); bool copyEnabled = copy.Enabled; copy.PerformClick();
                    string context = " sourceCareer=" + career + " theme=" + theme.Key;
                    check(copyEnabled && GlobalOptions.Instance.ClipboardContentType == ClipboardContentType.Gear &&
                        GlobalOptions.Instance.Clipboard.SelectSingleNode("/character/gear/notes")?.InnerText == notes,
                        "Native actual Copy command publishes Unicode Gear XML" + context);
                    string copied = GlobalOptions.Instance.Clipboard.OuterXml;
                    GearTab(to); to.Select(); await Task.Delay(100);
                    var ids = target.Gear.Select(g => g.InternalId).ToHashSet(); int count = target.Gear.Count, money = target.Nuyen, expenses = target.ExpenseEntries.Count;
                    var paste = Field<Forms.ToolStripMenuItem>(to, "mnuEditPaste"); bool pasteEnabled = paste.Enabled; paste.PerformClick();
                    var added = target.Gear.FirstOrDefault(g => !ids.Contains(g.InternalId));
                    check(pasteEnabled && target.Gear.Count == count + 1 && added != null && added.InternalId != gear.InternalId && added.Name == "Medkit" && added.Notes == notes,
                        "Native cross-editor Paste retains data and assigns fresh identity" + context);
                    check(target.Nuyen == money && target.ExpenseEntries.Count == expenses && Field<bool>(to, "_blnIsDirty"),
                        "Native cross-editor Paste preserves established funds ledger and dirty behavior" + context);
                    check(before.AsSpan().SequenceEqual(source.CaptureSnapshot()) && GlobalOptions.Instance.Clipboard.OuterXml == copied,
                        "Native Copy and Paste preserve source character and clipboard document" + context);
                }
                finally { CloseClean(to); CloseClean(from); }
            }
        }
        finally { GlobalOptions.Instance.Clipboard = oldDocument; GlobalOptions.Instance.ClipboardContentType = oldType; app.RequestedThemeVariant = oldTheme; }
    }
}
