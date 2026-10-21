using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Forms = Majorsilence.Forms;

namespace Chummer;

// Save-state acceptance through the native knowledge name editor.
internal static class NativeCreationKnowledgeNameChecks
{
    private static T Field<T>(object owner, string name) => (T)owner.GetType()
        .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(owner);
    internal static async Task RunAsync(Forms.Form owner, Character source, Action<bool, string> check)
    {
        var character = new Character(); character.LoadSnapshot(source.CaptureSnapshot());
        character.Created = false; character.IgnoreRules = true;
        using var editor = new frmCreate(character) { MdiParent = owner, Loading = true };
        editor.Show(); await Task.Delay(30);
        try
        {
            Field<Forms.TabControl>(editor, "tabCharacterTabs").SelectedTab = Field<Forms.TabPage>(editor, "tabSkills");
            await Task.Delay(30); editor.Loading = false;
            Field<Forms.Button>(editor, "cmdAddKnowledgeSkill").PerformClick();
            var row = Field<System.Collections.Generic.List<SkillControl>>(editor, "creationKnowledgeSkills").Last();
            row.SkillRating = 2;
            var dirty = typeof(frmCreate).GetField("_blnIsDirty", BindingFlags.Instance | BindingFlags.NonPublic);
            dirty.SetValue(editor, false);
            var name = Field<CreationKnowledgeSkillSection>(editor, "creationKnowledgeSkillSection").Rows[row].Name;
            const string custom = "Synthetic [literal] knowledge O'Brien";
            name.Text = custom; await Task.Delay(20);
            check(row.SkillObject.Name == custom && (bool)dirty.GetValue(editor),
                "Native creation knowledge rename changes the real model and marks the editor dirty");
            dirty.SetValue(editor, false); name.Text = custom;
            check(!(bool)dirty.GetValue(editor), "Native creation unchanged knowledge name preserves clean state");
            var reopened = new Character(); reopened.LoadSnapshot(character.CaptureSnapshot());
            check(reopened.Skills.Any(skill => skill.KnowledgeSkill && skill.Name == custom && skill.Rating == 2),
                "Native creation knowledge rename preserves literal text and rating in legacy serialization");
        }
        finally
        {
            typeof(frmCreate).GetField("_blnIsDirty", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(editor, false);
            editor.Close();
        }
    }
}
