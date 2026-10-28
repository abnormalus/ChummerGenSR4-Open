#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Forms = Majorsilence.Forms;

namespace Chummer;

// Shared command fixture: actual shown forms and fixed expectations on either
// backend. Native callers provide the UI settling delay; headless callers stay synchronous.
internal static class ClipboardTransferChecks
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
    internal static async Task RunAsync(Forms.Form? main, Character original, Action<bool, string> check, Func<Task> settle, string scope)
    {
        var oldDocument = GlobalOptions.Instance.Clipboard; var oldType = GlobalOptions.Instance.ClipboardContentType;
        try
        {
            foreach (bool career in new[] { false, true })
            {
                var source = new Character(); source.LoadSnapshot(original.CaptureSnapshot()); source.Created = career;
                var target = new Character(); target.LoadSnapshot(original.CaptureSnapshot()); target.Created = false;
                using Forms.Form from = career ? new frmCareer(source) { Loading = true } : new frmCreate(source) { Loading = true };
                // Career owns Copy but no Paste command; both supported source
                // modes transfer into a separate creation editor.
                using Forms.Form to = new frmCreate(target) { Loading = true };
                if (main != null) { from.MdiParent = main; to.MdiParent = main; }
                from.Show(); to.Show();
                try
                {
                    var gear = new Gear(source); var node = new Forms.TreeNode();
                    gear.Create(XmlManager.Instance.Load("gear.xml").SelectSingleNode("/chummer/gears/gear[name='Medkit']")!, source, node, 1,
                        new List<Weapon>(), new List<Forms.TreeNode>());
                    const string notes = "Synthetic native copy <雪> & 😀"; gear.Notes = notes; source.Gear.Add(gear);
                    Field<Forms.TreeView>(from, "treGear").Nodes[0].Nodes.Add(node);
                    GearTab(from); from.Select(); Field<Forms.TreeView>(from, "treGear").SelectedNode = node;
                    await settle();
                    byte[] before = source.CaptureSnapshot();
                    var copy = Field<Forms.ToolStripMenuItem>(from, "mnuEditCopy"); bool copyEnabled = copy.Enabled; copy.PerformClick();
                    string context = " sourceCareer=" + career + " host=" + scope;
                    check(copyEnabled && GlobalOptions.Instance.ClipboardContentType == ClipboardContentType.Gear &&
                        GlobalOptions.Instance.Clipboard.SelectSingleNode("/character/gear/notes")?.InnerText == notes,
                        "Shown editor actual Copy command publishes Unicode Gear XML" + context);
                    string copied = GlobalOptions.Instance.Clipboard.OuterXml;
                    GearTab(to); to.Select(); await settle();
                    var ids = target.Gear.Select(g => g.InternalId).ToHashSet(); int count = target.Gear.Count, money = target.Nuyen, expenses = target.ExpenseEntries.Count;
                    var paste = Field<Forms.ToolStripMenuItem>(to, "mnuEditPaste"); bool pasteEnabled = paste.Enabled; paste.PerformClick();
                    var added = target.Gear.FirstOrDefault(g => !ids.Contains(g.InternalId));
                    check(pasteEnabled && target.Gear.Count == count + 1 && added != null && added.InternalId != gear.InternalId && added.Name == "Medkit" && added.Notes == notes,
                        "Shown cross-editor Paste retains data and assigns fresh identity" + context);
                    check(target.Nuyen == money && target.ExpenseEntries.Count == expenses && Field<bool>(to, "_blnIsDirty"),
                        "Shown cross-editor Paste preserves established funds ledger and dirty behavior" + context);
                    check(before.AsSpan().SequenceEqual(source.CaptureSnapshot()) && GlobalOptions.Instance.Clipboard.OuterXml == copied,
                        "Shown Copy and Paste preserve source character and clipboard document" + context);
                }
                finally { CloseClean(to); CloseClean(from); }
            }
        }
        finally { GlobalOptions.Instance.Clipboard = oldDocument; GlobalOptions.Instance.ClipboardContentType = oldType; }
    }
}
