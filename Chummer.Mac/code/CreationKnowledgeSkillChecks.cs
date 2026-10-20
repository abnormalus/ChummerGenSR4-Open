using System;
using System.Linq;
using System.Reflection;
using Forms = Majorsilence.Forms;

namespace Chummer;

internal static class CreationKnowledgeSkillChecks
{
    private static T Field<T>(object owner, string name) => (T)owner.GetType()
        .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(owner);
    internal static void Run(Character source, Action<bool, string> check)
    {
        var character = new Character(); character.LoadSnapshot(source.CaptureSnapshot());
        character.Created = false; character.IgnoreRules = true;
        using var editor = new frmCreate(character) { Loading = true };
        editor.Show();
        bool closed = false;
        bool confirmDelete = character.Options.ConfirmDelete;
        try
        {
            Field<Forms.TabControl>(editor, "tabCharacterTabs").SelectedTab = Field<Forms.TabPage>(editor, "tabSkills");
            editor.Loading = false;
            var panel = Field<Forms.Panel>(editor, "panKnowledgeSkills");
            int previous = panel.Controls.Count;
            Field<Forms.Button>(editor, "cmdAddKnowledgeSkill").PerformClick();
            var row = panel.Controls.OfType<SkillControl>().Last();
            check(panel.Controls.Count == previous + 1 && character.Skills.Contains(row.SkillObject)
                && row.KnowledgeSkill && row.SkillCategory == "Academic" && row.SkillObject.Attribute == "LOG",
                "Creation Add Knowledge Skill retains its canonical model and Academic LOG defaults");
            row.SkillRating = 2;
            typeof(frmCreate).GetField("_blnIsDirty", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(editor, false);
            Field<Forms.ComboBox>(row, "cboSkillName").Text = "Synthetic [literal] knowledge O'Brien";
            check(row.SkillObject.Name == "Synthetic [literal] knowledge O'Brien" && Field<bool>(editor, "_blnIsDirty"),
                "Creation editing a knowledge skill name updates the model and marks the editor dirty");
            var renamed = new Character(); renamed.LoadSnapshot(character.CaptureSnapshot());
            check(renamed.Skills.Any(skill => skill.KnowledgeSkill && skill.Name == "Synthetic [literal] knowledge O'Brien" && skill.Rating == 2),
                "Creation knowledge rename retains literal text and rating in the legacy save contract");
            typeof(frmCreate).GetField("_blnIsDirty", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(editor, false);
            Field<Forms.ComboBox>(row, "cboSkillName").Text = "Synthetic [literal] knowledge O'Brien";
            check(!Field<bool>(editor, "_blnIsDirty"), "Creation unchanged knowledge name does not dirty the editor");
            var notification = typeof(SkillControl).GetField("KnowledgeNameChanged", BindingFlags.Instance | BindingFlags.NonPublic);
            check(((Delegate)notification.GetValue(row)).GetInvocationList().Any(handler => ReferenceEquals(handler.Target, editor)),
                "Creation knowledge rename notification is owned by the editor while open");
            character.Options.ConfirmDelete = false;
            row.CreationDeleteButton.PerformClick();
            var deletedHandlers = notification.GetValue(row) as Delegate;
            check(!character.Skills.Contains(row.SkillObject) && !panel.Controls.Contains(row) && Field<bool>(editor, "_blnIsDirty")
                && (deletedHandlers == null || !deletedHandlers.GetInvocationList().Any(handler => ReferenceEquals(handler.Target, editor))),
                "Creation knowledge deletion removes the canonical row and releases its rename notification");
            character.Options.ConfirmDelete = confirmDelete;
            Field<Forms.Button>(editor, "cmdAddKnowledgeSkill").PerformClick();
            row = panel.Controls.OfType<SkillControl>().Last();
            typeof(frmCreate).GetField("_blnIsDirty", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(editor, false);
            editor.Close(); closed = true;
            var remaining = notification.GetValue(row) as Delegate;
            check(remaining == null || !remaining.GetInvocationList().Any(handler => ReferenceEquals(handler.Target, editor)),
                "Creation close releases the knowledge rename owner notification");
        }
        finally
        {
            typeof(frmCreate).GetField("_blnIsDirty", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(editor, false);
            character.Options.ConfirmDelete = confirmDelete;
            if (!closed) editor.Close();
        }
    }
}
