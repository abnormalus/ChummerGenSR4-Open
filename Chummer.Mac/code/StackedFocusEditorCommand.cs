#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Forms = Majorsilence.Forms;

namespace Chummer;

// Thin legacy UI adapter: owns modal selection and tree projection, while the
// proposal owns validation and model transfer. The editor retains dirty/summary
// refresh and persistence. No selection changes owned gear before commit.
internal static class StackedFocusEditorCommand
{
    internal static bool Run(Forms.Form owner, Character character, Forms.TreeView tree, Func<bool> allowHigherForce)
    {
        var available = character.Gear.Where(g => !g.Bonded && (g.Category == "Foci" || g.Category == "Metamagic Foci")).ToList();
        if (available.Count < 2) { Notice("Message_CannotStackFoci"); return false; }
        var selected = new List<Gear>();
        using var picker = new frmSelectItem();
        do
        {
            picker.Gear = available; picker.AllowAutoSelect = false;
            picker.Description = LanguageManager.Instance.GetString("String_SelectItemFocus");
            picker.ShowDialog(owner);
            if (picker.DialogResult == Forms.DialogResult.OK)
            {
                var component = available.FirstOrDefault(g => g.InternalId == picker.SelectedItem);
                if (component != null) { selected.Add(component); available.Remove(component); }
            }
        } while (available.Count > 0 && picker.DialogResult != Forms.DialogResult.Cancel);

        // Cancel after two selections still completes the original command's
        // selection sequence. Fewer than two now rejects without bonding one.
        var proposal = new StackedFocusProposal(selected);
        bool allowHigher = allowHigherForce();
        var rejection = proposal.Validate(character, allowHigher);
        if (rejection != StackedFocusProposal.Rejection.None)
        {
            Notice(rejection == StackedFocusProposal.Rejection.Minimum ? "Message_StackedFocusMinimum" :
                rejection == StackedFocusProposal.Rejection.ForceLimit ? "Message_StackedFocusForce" : "Message_CannotStackFoci");
            return false;
        }
        var result = proposal.Commit(character, allowHigher);
        // Projection follows the complete model transfer; these controls do not
        // own inventory. UI failures do not roll model state back or save it.
        foreach (var component in result.Components)
        foreach (Forms.TreeNode root in tree.Nodes)
        foreach (Forms.TreeNode item in root.Nodes)
        {
            if (item.Tag!.ToString() == component.InternalId) { root.Nodes.Remove(item); break; }
        }
        tree.Nodes[0].Nodes.Add(new Forms.TreeNode { Text = result.Item.DisplayNameShort, Tag = result.Item.InternalId });
        return true;

        void Notice(string key) => Forms.MessageBox.Show(LanguageManager.Instance.GetString(key),
            LanguageManager.Instance.GetString("MessageTitle_CannotStackFoci"), Forms.MessageBoxButtons.OK, Forms.MessageBoxIcon.Information);
    }
}
