using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Forms = Majorsilence.Forms;

namespace Chummer;

internal static class CreationKnowledgeSkillChecks
{
    private static T Field<T>(object owner, string name) => (T)owner.GetType()
        .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(owner);
    internal static void Run(Character source, Action<bool, string> check, bool useNativeViewport = false)
    {
        var originalCheck = check;
        check = (valid, label) => originalCheck(valid, (useNativeViewport ? "Native-adapted " : "") + label);
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
            var owned = Field<List<SkillControl>>(editor, "creationKnowledgeSkills");
            if (useNativeViewport)
                typeof(frmCreate).GetField("creationKnowledgeSkillSection", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(editor,
                    new CreationKnowledgeSkillSection(null, panel, owned));
            var native = Field<CreationKnowledgeSkillSection>(editor, "creationKnowledgeSkillSection");
            if (useNativeViewport)
            {
                var originalSize = editor.Size;
                byte[] beforeResize = character.CaptureSnapshot(); bool resizeDirty = Field<bool>(editor, "_blnIsDirty");
                foreach (var size in new[] { new System.Drawing.Size(932, 545), new System.Drawing.Size(1200, 800) })
                {
                    editor.Size = size;
                    check(panel.Height >= 73 && character.CaptureSnapshot().SequenceEqual(beforeResize) && Field<bool>(editor, "_blnIsDirty") == resizeDirty,
                        "Creation knowledge resizing reserves a complete two-line native row without changing the character at " + size);
                }
                editor.Size = originalSize;
            }
            int previous = owned.Count;
            Field<Forms.Button>(editor, "cmdAddKnowledgeSkill").PerformClick();
            var row = owned.Last();
            check(owned.Count == previous + 1 && character.Skills.Contains(row.SkillObject)
                && row.KnowledgeSkill && row.SkillCategory == "Academic" && row.SkillObject.Attribute == "LOG",
                "Creation Add Knowledge Skill retains its canonical model and Academic LOG defaults");
            check((useNativeViewport ? owned.SequenceEqual(native.Rows.Keys) && panel.Controls.Count == 0 : owned.SequenceEqual(panel.Controls.OfType<SkillControl>())) && owned.Select(item => item.SkillObject).SequenceEqual(character.Skills.Where(skill => skill.KnowledgeSkill)),
                "Creation knowledge ownership preserves canonical row identities and order");
            var category = Field<Forms.ComboBox>(row, "cboKnowledgeSkillCategory");
            var nameInput = Field<Forms.ComboBox>(row, "cboSkillName");
            var specialization = Field<Forms.ComboBox>(row, "cboSpec");
            var model = row.SkillObject;
            void SelectType(string value)
            {
                if (native == null) category.SelectedValue = value;
                else native.Rows[row].Type.SelectedItem = category.Items.Cast<ListItem>().Single(item => item.Value == value);
            }
            void SetName(string value) { if (native == null) nameInput.Text = value; else native.Rows[row].Name.Text = value; }
            void SetSpecialization(string value) { if (native == null) specialization.Text = value; else native.Rows[row].Specialization.Text = value; }
            foreach (var type in new[] { "Academic", "Professional", "Street", "Interest", "Language" })
            {
                typeof(frmCreate).GetField("_blnIsDirty", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(editor, false);
                SelectType(type == "Academic" ? "Street" : "Academic");
                typeof(frmCreate).GetField("_blnIsDirty", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(editor, false);
                SelectType(type);
                string expectedAttribute = type == "Academic" || type == "Professional" ? "LOG" : "INT";
                check(ReferenceEquals(row.SkillObject, model) && model.SkillCategory == type && model.Attribute == expectedAttribute
                    && Field<bool>(editor, "_blnIsDirty") && character.Skills.Contains(model),
                    "Creation knowledge type " + type + " retains canonical ownership and attribute accounting notification");
                SetName("Synthetic literal " + type + " [O'Brien]");
                check(model.Name == nameInput.Text && model.SkillCategory == type && model.Attribute == expectedAttribute,
                    "Creation custom knowledge name preserves chosen type and linked attribute for " + type);
            }
            SelectType("Academic");
            SetSpecialization("Synthetic specialty [literal] O'Brien");
            check(model.Specialization == specialization.Text && Field<bool>(editor, "_blnIsDirty"),
                "Creation knowledge specialization uses the existing literal edit and dirty transition");
            var specialized = new Character(); specialized.LoadSnapshot(character.CaptureSnapshot());
            check(specialized.Skills.Any(skill => skill.KnowledgeSkill && skill.Name == model.Name &&
                skill.SkillCategory == "Academic" && skill.Attribute == "LOG" && skill.Specialization == specialization.Text),
                "Creation knowledge type name and specialization round trip together without changing the save contract");
            if (native == null) row.SkillRating = 2; else native.Rows[row].Rating.Value = 2;
            panel.Controls.Remove(row);
            row.SkillObject.RatingMaximum = 4;
            editor.UpdateCharacterInfo();
            check(row.SkillRatingMaximum == 4 && owned.Contains(row) && character.Skills.Contains(row.SkillObject),
                "Creation knowledge refresh retains owned detached rows and their model-defined rating limits");
            row.SkillObject.RatingMaximum = 6;
            editor.UpdateCharacterInfo();
            typeof(frmCreate).GetField("_blnIsDirty", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(editor, false);
            SetName("Synthetic [literal] knowledge O'Brien");
            check(row.SkillObject.Name == "Synthetic [literal] knowledge O'Brien" && Field<bool>(editor, "_blnIsDirty"),
                "Creation editing a knowledge skill name updates the model and marks the editor dirty");
            var renamed = new Character(); renamed.LoadSnapshot(character.CaptureSnapshot());
            check(renamed.Skills.Any(skill => skill.KnowledgeSkill && skill.Name == "Synthetic [literal] knowledge O'Brien" && skill.Rating == 2),
                "Creation knowledge rename retains literal text and rating in the legacy save contract");
            typeof(frmCreate).GetField("_blnIsDirty", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(editor, false);
            SetName("Synthetic [literal] knowledge O'Brien");
            check(!Field<bool>(editor, "_blnIsDirty"), "Creation unchanged knowledge name does not dirty the editor");
            var notification = typeof(SkillControl).GetField("KnowledgeNameChanged", BindingFlags.Instance | BindingFlags.NonPublic);
            check(((Delegate)notification.GetValue(row)).GetInvocationList().Any(handler => ReferenceEquals(handler.Target, editor)),
                "Creation knowledge rename notification is owned by the editor while open");
            character.Options.ConfirmDelete = false;
            if (native == null) row.CreationDeleteButton.PerformClick();
            else native.Rows[row].Delete.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Avalonia.Controls.Button.ClickEvent));
            var deletedHandlers = notification.GetValue(row) as Delegate;
            check(!character.Skills.Contains(row.SkillObject) && !panel.Controls.Contains(row) && !owned.Contains(row) && Field<bool>(editor, "_blnIsDirty")
                && (deletedHandlers == null || !deletedHandlers.GetInvocationList().Any(handler => ReferenceEquals(handler.Target, editor))),
                "Creation knowledge deletion removes the canonical row and releases its rename notification");
            var ratingHandlers = typeof(SkillControl).GetField("RatingChanged", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(row) as Delegate;
            var specializationHandlers = typeof(SkillControl).GetField("SpecializationChanged", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(row) as Delegate;
            check((ratingHandlers == null || !ratingHandlers.GetInvocationList().Any(handler => ReferenceEquals(handler.Target, editor)))
                && (specializationHandlers == null || !specializationHandlers.GetInvocationList().Any(handler => ReferenceEquals(handler.Target, editor))),
                "Creation accepted knowledge deletion releases rating and specialization owner notifications");
            character.Options.ConfirmDelete = confirmDelete;
            // Exercise the actual owner's high-rating policy with detached rows,
            // rather than recreating the rule inside the viewport.
            foreach (var item in owned) item.SkillRating = 0;
            for (int index = 0; index < 3; index++) Field<Forms.Button>(editor, "cmdAddKnowledgeSkill").PerformClick();
            var capRows = owned.TakeLast(3).ToArray();
            for (int index = 0; index < capRows.Length; index++)
            {
                string uniqueName = "Synthetic capped knowledge " + index;
                if (native == null) capRows[index].CreationKnowledgeNameInput.Text = uniqueName;
                else native.Rows[capRows[index]].Name.Text = uniqueName;
            }
            void SetRating(SkillControl item, int value)
            { if (native == null) item.CreationRatingInput.Value = value; else native.Rows[item].Rating.Value = value; }
            character.IgnoreRules = false;
            SetRating(capRows[0], 6); SetRating(capRows[1], 5);
            check(capRows[0].SkillRating == 6 && capRows[1].SkillRating == 4,
                "Creation knowledge owner permits one six and rejects a second high rating" +
                $" [ratings={string.Join(",", capRows.Select(item => item.SkillRating))}; max={string.Join(",", capRows.Select(item => item.SkillRatingMaximum))}; distinct={capRows.Select(item => item.SkillName).Distinct().Count()}]");
            SetRating(capRows[0], 5); SetRating(capRows[1], 5); SetRating(capRows[2], 5);
            check(capRows[0].SkillRating == 5 && capRows[1].SkillRating == 5 && capRows[2].SkillRating == 4,
                "Creation knowledge owner permits two fives and rejects a third high rating");
            character.IgnoreRules = true;
            foreach (var method in new[] { CharacterBuildMethod.BP, CharacterBuildMethod.Karma })
            {
                character.BuildMethod = method; editor.UpdateCharacterInfo();
                var comparison = new Character(); comparison.LoadSnapshot(character.CaptureSnapshot());
                using var baselineEditor = new frmCreate(comparison) { Loading = true };
                baselineEditor.Show(); baselineEditor.UpdateCharacterInfo();
                check(Field<Forms.Label>(editor, "lblKnowledgeSkillsBP").Text == Field<Forms.Label>(baselineEditor, "lblKnowledgeSkillsBP").Text
                    && Field<Forms.Label>(editor, "lblKnowledgeSkillPoints").Text == Field<Forms.Label>(baselineEditor, "lblKnowledgeSkillPoints").Text,
                    "Creation knowledge owned-row accounting matches the legacy loaded editor in " + method);
                typeof(frmCreate).GetField("_blnIsDirty", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(baselineEditor, false);
                baselineEditor.Close();
            }
            foreach (var item in owned) { item.SkillRating = 0; item.SkillCategory = "Academic"; }
            capRows[0].SkillCategory = "Language"; capRows[1].SkillCategory = "Language";
            check(capRows[0].SkillObject.IsNativeLanguage && capRows[1].SkillObject.IsNativeLanguage == character.Qualities.Any(quality => quality.Name == "Bilingual")
                && owned.IndexOf(capRows[0]) < owned.IndexOf(capRows[1]),
                "Creation knowledge native-language eligibility retains canonical order and Bilingual policy");
            Field<Forms.Button>(editor, "cmdAddKnowledgeSkill").PerformClick();
            row = owned.Last();
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
