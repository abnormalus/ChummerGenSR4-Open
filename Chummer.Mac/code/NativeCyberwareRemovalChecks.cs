#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml;
using Majorsilence.Forms;

namespace Chummer;

internal static class NativeCyberwareRemovalChecks
{
    private static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(owner)!;
    internal static Task RunAsync(Form main, Character source, Action<bool, string> check)
    {
        foreach (bool career in new[] { false, true })
        {
            var character = new Character(); character.LoadSnapshot(source.CaptureSnapshot());
            character.Created = career; character.IgnoreRules = true;
            var catalog = XmlManager.Instance.Load("cyberware.xml");
            var grades = new GradeList(); grades.LoadList(Improvement.ImprovementSource.Cyberware);
            var weapons = new List<Weapon>(); var weaponNodes = new List<TreeNode>();
            var arm = new Cyberware(character); var armNode = new TreeNode();
            arm.Create(catalog.SelectSingleNode("/chummer/cyberwares/cyberware[name='Obvious Lower Arm']")!, character,
                grades.GetGrade("Standard"), Improvement.ImprovementSource.Cyberware, 0, armNode, weapons, weaponNodes, false, false, "Left");
            var spur = new Cyberware(character); var spurNode = new TreeNode();
            spur.Create(catalog.SelectSingleNode("/chummer/cyberwares/cyberware[name='Spur']")!, character,
                grades.GetGrade("Standard"), Improvement.ImprovementSource.Cyberware, 0, spurNode, weapons, weaponNodes, false, false);
            spur.Parent = arm; arm.Children.Add(spur); character.Cyberware.Add(arm);
            character.Weapons.AddRange(weapons);
            using Form editor = career ? new frmCareer(character) { Loading = true } : new frmCreate(character) { Loading = true };
            editor.MdiParent = main; editor.Show();
            bool originalConfirm = character.Options.ConfirmDelete;
            try
            {
                var tree = Field<TreeView>(editor, "treCyberware");
                var parentNode = tree.Nodes[0].Nodes.Cast<TreeNode>().Single(node => node.Tag?.ToString() == arm.InternalId);
                var childNode = parentNode.Nodes.Cast<TreeNode>().Single(node => node.Tag?.ToString() == spur.InternalId);
                tree.SelectedNode = childNode; character.Options.ConfirmDelete = false;
                editor.GetType().GetMethod("cmdDeleteCyberware_Click", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(editor, new object[] { editor, EventArgs.Empty });
                check(arm.Children.Count == 0 && childNode.Parent == null && !parentNode.Nodes.Cast<TreeNode>().Any(node => node.Tag?.ToString() == spur.InternalId),
                    "Native " + (career ? "career" : "creation") + " deleting a spur removes its nested tree row and canonical model record");
                // Reproduce an already-queued click against a retired projection.
                parentNode.Nodes.Add(childNode); tree.SelectedNode = childNode;
                check(tree.SelectedNode == null && childNode.Parent == null && Field<Label>(editor, "lblCyberwareName").Text == "" && character.Cyberware.Contains(arm),
                    "Native " + (career ? "career" : "creation") + " stale cyberware selections clear safely without altering the parent limb");
                tree.SelectedNode = parentNode;
                check(Field<Label>(editor, "lblCyberwareName").Text == arm.DisplayNameShort,
                    "Native " + (career ? "career" : "creation") + " parent cyberlimb remains selectable after child removal");
            }
            finally
            {
                character.Options.ConfirmDelete = originalConfirm;
                editor.GetType().GetField("_blnIsDirty", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(editor, false);
                editor.Close();
            }
        }
        return Task.CompletedTask;
    }
}
