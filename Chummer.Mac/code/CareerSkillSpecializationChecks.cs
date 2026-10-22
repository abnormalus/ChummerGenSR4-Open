using System;
using System.Linq;
using System.Reflection;
using Forms = Majorsilence.Forms;

namespace Chummer;

internal static class CareerSkillSpecializationChecks
{
    private const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;
    private static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, Flags).GetValue(owner);
    private static void Clean(frmCareer editor) => typeof(frmCareer).GetField("_blnIsDirty", Flags).SetValue(editor, false);
    internal static void Run(Character source, Action<bool, string> check)
    {
        foreach (bool knowledge in new[] { false, true })
        foreach (bool saved in new[] { false, true })
        {
            var character = new Character(); character.LoadSnapshot(source.CaptureSnapshot());
            character.Created = true; character.Karma = 100;
            var skill = knowledge ? new Skill(character) { Name = "Synthetic career knowledge", KnowledgeSkill = true,
                SkillCategory = "Academic", Attribute = "LOG", RatingMaximum = 6 }
                : character.Skills.First(s => !s.KnowledgeSkill && !s.IsMeta && !s.ExoticSkill);
            if (knowledge) character.Skills.Add(skill);
            skill.Rating = 2; skill.Specialization = saved ? "Synthetic retained specialty" : ""; skill.SkillGroup = ""; skill.IsGrouped = false;
            bool confirm = character.Options.ConfirmKarmaExpense;
            using var editor = new frmCareer(character) { Loading = true };
            editor.Show();
            Field<Forms.TabControl>(editor, "tabCharacterTabs").SelectedTab = Field<Forms.TabPage>(editor, "tabSkills");
            editor.Loading = false; Clean(editor);
            try
            {
                character.Options.ConfirmKarmaExpense = false;
                var panel = Field<Forms.Panel>(editor, knowledge ? "panKnowledgeSkills" : "panActiveSkills");
                var row = panel.Controls.OfType<SkillControl>().Single(c => ReferenceEquals(c.SkillObject, skill));
                var input = Field<Forms.ComboBox>(row, "cboSpec");
                byte[] before = character.CaptureSnapshot(); int karma = character.Karma, expenses = character.ExpenseEntries.Count;
                string label = " knowledge=" + knowledge + " saved=" + saved;
                check(!input.Visible && skill.Specialization == (saved ? "Synthetic retained specialty" : ""),
                    "Career specialty editor starts hidden while retaining saved text" + label);
                if (knowledge)
                {
                    Forms.Backends.Platform.Backend.Post(() =>
                    {
                        var prompt = Forms.Application.OpenForms.OfType<frmSelectText>().Last();
                        prompt.DialogResult = Forms.DialogResult.Cancel;
                    });
                    row.CreationKnowledgeSpecialtyButton.PerformClick();
                }
                else Field<Forms.Button>(row, "cmdChangeSpec").PerformClick();
                check(input.Visible && character.CaptureSnapshot().SequenceEqual(before) && !Field<bool>(editor, "_blnIsDirty"),
                    "Career right-hand plus reveals only its row without editing the character" + label);
                Field<Forms.Button>(row, "cmdImproveSkill").Focus();
                check(character.Karma == karma && character.ExpenseEntries.Count == expenses && character.CaptureSnapshot().SequenceEqual(before)
                    && !Field<bool>(editor, "_blnIsDirty"),
                    "Career unchanged revealed specialty leaves without Karma expense or dirty state" + label);
                row.RefreshControl();
                check(input.Visible && skill.Specialization == (saved ? "Synthetic retained specialty" : ""),
                    "Career row refresh retains explicit specialty disclosure and saved text" + label);
                const string changed = "Synthetic changed [O'Brien]";
                if (knowledge)
                {
                    Forms.Backends.Platform.Backend.Post(() =>
                    {
                        var prompt = Forms.Application.OpenForms.OfType<frmSelectText>().Last();
                        Field<Forms.TextBox>(prompt, "txtValue").Text = changed;
                        Field<Forms.Button>(prompt, "cmdOK").PerformClick();
                    });
                    row.CreationKnowledgeSpecialtyButton.PerformClick();
                }
                else
                {
                    Field<Forms.Button>(row, "cmdChangeSpec").PerformClick();
                    input.Text = changed;
                    Field<Forms.Button>(row, "cmdImproveSkill").Focus();
                }
                check(character.Karma == karma - character.Options.KarmaSpecialization && character.ExpenseEntries.Count == expenses + 1
                    && skill.Specialization == changed && Field<bool>(editor, "_blnIsDirty"),
                    "Career explicit specialty edit retains the original single Karma expense" + label
                    + " [karma=" + character.Karma + "/" + karma + "; expenses=" + character.ExpenseEntries.Count + "/" + expenses
                    + "; textMatch=" + (skill.Specialization == changed) + "; dirty=" + Field<bool>(editor, "_blnIsDirty") + "]");
                var reopened = new Character(); reopened.LoadSnapshot(character.CaptureSnapshot());
                check(reopened.Skills.Any(s => s.Name == skill.Name && s.KnowledgeSkill == knowledge && s.Specialization == changed),
                    "Career specialty accepted after plus survives the unchanged save format" + label);
            }
            finally { character.Options.ConfirmKarmaExpense = confirm; Clean(editor); editor.Close(); }
        }
    }
}
