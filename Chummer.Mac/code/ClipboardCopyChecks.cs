#nullable enable
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml;
using Forms = Majorsilence.Forms;

namespace Chummer;

// Exercise actual Copy commands before extracting their repeated XML wrapper.
// The dangling-weapon case records existing non-atomic type publication, not a desired fix.
internal static class ClipboardCopyChecks
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    private static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, Flags)!.GetValue(owner)!;
    internal static void Run(Character source, Action<bool, string> check)
    {
        var priorDocument = GlobalOptions.Instance.Clipboard;
        var priorType = GlobalOptions.Instance.ClipboardContentType;
        try
        {
            foreach (bool career in new[] { false, true })
            {
                var character = new Character(); character.LoadSnapshot(source.CaptureSnapshot()); character.Created = career;
                var gear = new Gear(character); var node = new Forms.TreeNode();
                gear.Create(XmlManager.Instance.Load("gear.xml").SelectSingleNode("/chummer/gears/gear[name='Medkit']")!, character, node, 1,
                    new List<Weapon>(), new List<Forms.TreeNode>());
                const string notes = "Synthetic copy <雪> & '😀'"; gear.Notes = notes;
                character.Gear.Add(gear);
                using Forms.Form editor = career ? new frmCareer(character) { Loading = true } : new frmCreate(character) { Loading = true };
                Field<Forms.TabControl>(editor, "tabCharacterTabs").SelectedTab = Field<Forms.TabPage>(editor, "tabStreetGear");
                Field<Forms.TabControl>(editor, "tabStreetGearTabs").SelectedTab = Field<Forms.TabPage>(editor, "tabGear");
                var tree = Field<Forms.TreeView>(editor, "treGear");
                tree.Nodes.Clear(); var root = new Forms.TreeNode("Synthetic root"); root.Nodes.Add(node); tree.Nodes.Add(root); tree.SelectedNode = node;
                void Copy() => editor.GetType().GetMethod("mnuEditCopy_Click", Flags)!.Invoke(editor, new object[] { editor, EventArgs.Empty });
                XmlDocument Sentinel()
                {
                    var doc = new XmlDocument(); doc.LoadXml("<character><sentinel /></character>");
                    GlobalOptions.Instance.Clipboard = doc; GlobalOptions.Instance.ClipboardContentType = ClipboardContentType.Lifestyle; return doc;
                }
                gear.WeaponID = Guid.NewGuid().ToString();
                var sentinel = Sentinel(); byte[] before = character.CaptureSnapshot(); Copy();
                check(ReferenceEquals(GlobalOptions.Instance.Clipboard, sentinel) && GlobalOptions.Instance.ClipboardContentType == ClipboardContentType.Gear,
                    "Legacy Gear Copy failure retains prior document after publishing Gear type career=" + career + " actualType=" + GlobalOptions.Instance.ClipboardContentType);
                check(before.AsSpan().SequenceEqual(character.CaptureSnapshot()),
                    "Legacy failed Gear Copy preserves character snapshot career=" + career);
                gear.WeaponID = Guid.Empty.ToString(); Sentinel(); before = character.CaptureSnapshot(); Copy();
                var copied = GlobalOptions.Instance.Clipboard;
                check(GlobalOptions.Instance.ClipboardContentType == ClipboardContentType.Gear && copied.DocumentElement?.Name == "character" &&
                    copied.FirstChild is XmlDeclaration declaration && declaration.Encoding == "utf-16" && copied.SelectSingleNode("/character/gear/notes")?.InnerText == notes,
                    "Legacy Gear Copy preserves character-root Unicode declaration and escaped notes career=" + career);
                check(before.AsSpan().SequenceEqual(character.CaptureSnapshot()),
                    "Legacy successful Gear Copy preserves character snapshot career=" + career);
                // Invalidate only the selected identity: raising AfterSelect on a
                // deliberately missing model would exercise a separate refresh path.
                node.Tag = Guid.NewGuid().ToString();
                sentinel = Sentinel(); Copy();
                check(ReferenceEquals(GlobalOptions.Instance.Clipboard, sentinel) && GlobalOptions.Instance.ClipboardContentType == ClipboardContentType.Lifestyle,
                    "Legacy missing Gear selection preserves clipboard document and type career=" + career);
            }
        }
        finally { GlobalOptions.Instance.Clipboard = priorDocument; GlobalOptions.Instance.ClipboardContentType = priorType; }
    }
}
