#nullable enable
using System;
using System.Linq;
using System.Reflection;
using Majorsilence.Forms;

namespace Chummer;

// Grouped characterization of actual creation-owner behavior before changing
// the active-skill viewport or editable specialization presentation.
internal static class CreationActiveSkillChecks
{
    private static T Field<T>(object owner, string name) => (T)owner.GetType()
        .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(owner)!;
    internal static void Run(Character source, Action<bool, string> check, bool useNativeViewport = false)
    {
        var originalCheck = check;
        check = (passed, description) => originalCheck(passed, (useNativeViewport ? "Native-adapted " : "") + description);
        var character = new Character(); character.LoadSnapshot(source.CaptureSnapshot());
        character.Created = false; character.IgnoreRules = true; character.BuildMethod = CharacterBuildMethod.BP;
        foreach (var group in character.SkillGroups) { group.Rating = 0; group.Broken = false; }
        using var editor = new frmCreate(character) { Loading = true };
        editor.Show();
        CreationActiveSkillSection? nativeView = null;
        try
        {
            byte[] beforeShowing = character.CaptureSnapshot();
            bool dirtyBefore = Field<bool>(editor, "_blnIsDirty");
            var tabs = Field<TabControl>(editor, "tabCharacterTabs");
            tabs.SelectedTab = Field<TabPage>(editor, "tabSkills");
            check(character.CaptureSnapshot().SequenceEqual(beforeShowing) && Field<bool>(editor, "_blnIsDirty") == dirtyBefore,
                "Creation first Skills show preserves model and editor dirty state");
            var rows = Field<Panel>(editor, "panActiveSkills").Controls.OfType<SkillControl>().ToArray();
            var groups = Field<Panel>(editor, "panSkillGroups").Controls.OfType<SkillGroupControl>().ToArray();
            check(Field<System.Collections.Generic.List<SkillControl>>(editor, "creationActiveSkills").SequenceEqual(rows),
                "Creation active-skill ownership preserves original row identities and order");
            if (useNativeViewport) nativeView = new CreationActiveSkillSection(null, Field<Panel>(editor, "panActiveSkills"), rows);
            editor.Loading = true;
            foreach (var row in rows.Where(row => !row.SkillIsMeta)) row.SkillRating = 0;
            editor.Loading = false;
            SkillControl Row(string name) => rows.Single(row => row.SkillName == name && !row.SkillIsMeta && !row.SkillObject.ExoticSkill);
            var pistols = Row("Pistols"); var automatics = Row("Automatics"); var longarms = Row("Longarms");
            NumericUpDown Input(SkillControl row) => Field<NumericUpDown>(row, "nudSkill");
            void Set(SkillControl row, decimal value)
            {
                if (nativeView == null) Input(row).Value = value;
                else nativeView.Rows[row].Rating.Value = value;
            }
            var filterMethod = typeof(frmCreate).GetMethod("SelectCreationSkillFilter", BindingFlags.Instance | BindingFlags.NonPublic)!;
            var filterSnapshot = character.CaptureSnapshot();
            bool filterDirty = Field<bool>(editor, "_blnIsDirty");
            foreach (string value in new[] { "0", "BOD", "AGI", "REA", "STR", "CHA", "INT", "LOG", "WIL", "EDG", "MAG", "RES", "1", "GROUP:Firearms" })
            {
                filterMethod.Invoke(editor, new object[] { value });
                check(rows.All(row => row.Visible == (value == "0" ||
                    (value == "1" ? row.SkillObject.Rating > 0 : value.StartsWith("GROUP:") ?
                    row.SkillObject.SkillGroup == value.Substring(6) : row.SkillObject.Attribute == value))) &&
                    character.CaptureSnapshot().SequenceEqual(filterSnapshot) && Field<bool>(editor, "_blnIsDirty") == filterDirty,
                    "Creation quick filter preserves character and shows exactly matching skills: " + value);
            }
            filterMethod.Invoke(editor, new object[] { "0" });
            Set(pistols, 1);
            filterMethod.Invoke(editor, new object[] { "1" });
            Set(pistols, 0);
            var beforeRankedRefresh = character.CaptureSnapshot();
            bool dirtyBeforeRankedRefresh = Field<bool>(editor, "_blnIsDirty");
            filterMethod.Invoke(editor, new object[] { "1" });
            check(!pistols.Visible && character.CaptureSnapshot().SequenceEqual(beforeRankedRefresh) &&
                Field<bool>(editor, "_blnIsDirty") == dirtyBeforeRankedRefresh,
                "Creation clicking an already selected Ranked filter refreshes edited ranks without changing character or dirty state");
            filterMethod.Invoke(editor, new object[] { "0" });
            character.IgnoreRules = false;
            Set(pistols, 6);
            check(pistols.SkillRating == 6, "Creation owner permits one ordinary active skill at six");
            Set(automatics, 5);
            check(automatics.SkillRating == 4 && Input(automatics).Value == 4,
                "Creation owner rejects another high skill while one skill is at six");
            Set(pistols, 5); Set(automatics, 5);
            check(pistols.SkillRating == 5 && automatics.SkillRating == 5,
                "Creation owner permits two ordinary active skills at five");
            Set(longarms, 5);
            check(longarms.SkillRating == 4 && Input(longarms).Value == 4,
                "Creation owner rejects a third ordinary active skill at five");
            Set(pistols, 6);
            check(pistols.SkillRating == 5 && Input(pistols).Value == 5,
                "Creation owner retains two-at-five correction when attempting six");
            character.IgnoreRules = true; Set(longarms, 6);
            check(longarms.SkillRating == 6, "Creation Ignore Rules retains its active-skill high-rating override");
            typeof(frmCreate).GetField("_blnIsDirty", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(editor, false);
            string specialization = "Synthetic [literal] O'Brien";
            var spec = Field<ComboBox>(pistols, "cboSpec");
            if (nativeView == null) spec.Text = specialization; else nativeView.Rows[pistols].Specialization.Text = specialization;
            check(pistols.SkillObject.Specialization == specialization && Field<bool>(editor, "_blnIsDirty"),
                "Creation editable specialization retains literal custom text and marks the editor dirty");
            var reopened = new Character(); reopened.LoadSnapshot(character.CaptureSnapshot());
            check(reopened.Skills.Single(skill => skill.Name == "Pistols" && !skill.IsMeta && !skill.ExoticSkill).Specialization == specialization,
                "Creation custom active specialization retains the legacy serialization contract");
            if (nativeView != null)
            {
                var nativeRow = nativeView.Rows[pistols];
                check(nativeRow.Layout.Children.OfType<Avalonia.Controls.TextBlock>().Last().FontWeight == Avalonia.Media.FontWeight.Bold
                    && nativeRow.Layout.Classes.Contains("trained"),
                    "Creation native trained skill preserves the source bold pool and row emphasis");
                pistols.Visible = false;
                var filteredSnapshot = character.CaptureSnapshot();
                bool filteredDirty = Field<bool>(editor, "_blnIsDirty");
                nativeRow.Specialization.Text = "Queued hidden edit";
                check(!nativeRow.Layout.IsVisible && character.CaptureSnapshot().SequenceEqual(filteredSnapshot)
                    && Field<bool>(editor, "_blnIsDirty") == filteredDirty && nativeRow.Specialization.Text == specialization,
                    "Creation filtered native skill rejects queued specialization edits without changing the character");
                pistols.Visible = true;
                nativeRow.Specialization.Text = "   ";
                check(pistols.SkillObject.Specialization == "" && spec.Text == "",
                    "Creation native whitespace specialization follows the existing clear policy");
                nativeRow.Specialization.Text = specialization;
                var integerSnapshot = character.CaptureSnapshot();
                nativeRow.Rating.Value = 1.5m;
                check(character.CaptureSnapshot().SequenceEqual(integerSnapshot) && nativeRow.Rating.Value == pistols.SkillRating,
                    "Creation native fractional skill rating restores the approved value without changing the character");
            }
            groups.Single(group => group.GroupName == "Firearms").GroupRating = 1;
            check(pistols.IsGrouped && !Input(pistols).Enabled && !spec.Enabled,
                "Creation group membership disables ordinary rating and specialization inputs");
            if (nativeView != null)
            {
                var groupedSnapshot = character.CaptureSnapshot();
                nativeView.Rows[pistols].Specialization.Text = "Queued grouped edit";
                nativeView.Rows[pistols].Rating.Value = 3;
                check(character.CaptureSnapshot().SequenceEqual(groupedSnapshot)
                    && nativeView.Rows[pistols].Specialization.Text == spec.Text
                    && nativeView.Rows[pistols].Rating.Value == pistols.SkillRating,
                    "Creation grouped native fields reject queued rating and specialization edits");
            }
            groups.Single(group => group.GroupName == "Firearms").GroupRating = 0;
            check(!pistols.IsGrouped && Input(pistols).Enabled && !spec.Enabled && spec.Text == "" && pistols.SkillObject.Specialization == "",
                "Creation zero group rating restores rating editing while preserving zero-rating specialization policy");
            if (nativeView != null)
                check(nativeView.Rows[pistols].Layout.Children.OfType<Avalonia.Controls.TextBlock>().Last().FontWeight == Avalonia.Media.FontWeight.Normal
                    && !nativeView.Rows[pistols].Layout.Classes.Contains("trained"),
                    "Creation native zero-rating skill clears source pool and row emphasis");
            Set(pistols, 1);
            check(spec.Enabled, "Creation positive ungrouped rating restores specialization editing");
        }
        finally
        {
            typeof(frmCreate).GetField("_blnIsDirty", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(editor, false);
            editor.Close(); nativeView?.Dispose();
            if (nativeView != null && nativeView.Rows.Count > 0)
            {
                var snapshot = character.CaptureSnapshot();
                var retained = nativeView.Rows.Values.First();
                retained.Rating.Value = 2;
                retained.Specialization.Text = "Queued disposed edit";
                check(character.CaptureSnapshot().SequenceEqual(snapshot),
                    "Disposed native active-skill fields disconnect queued callbacks from the character");
            }
        }
    }
}
