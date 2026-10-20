using System;
using System.Linq;
using System.Reflection;
using Majorsilence.Forms;

namespace Chummer;

// Characterize the existing controller/event boundary before replacing the
// scrolling skill sections. These checks deliberately use the shipping controls.
internal static class CreationSkillInputChecks
{
    private static NumericUpDown Input(object control) => (NumericUpDown)control.GetType()
        .GetField("nudSkill", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(control)!;

    internal static void Run(Character source, Action<bool, string> check)
    {
        var character = new Character();
        character.LoadSnapshot(source.CaptureSnapshot());
        character.Created = false;
        var group = new SkillGroup { Name = "Athletics", RatingMaximum = 4 };
        using var groupControl = new SkillGroupControl(character.Options) { SkillGroupObject = group };
        int groupEvents = 0; int observedGroup = -1;
        groupControl.GroupRatingChanged += _ => { groupEvents++; observedGroup = group.Rating; };
        groupControl.GroupRatingMaximum = 4;
        var groupInput = Input(groupControl);
        groupInput.Value = 2;
        check(group.Rating == 2 && observedGroup == 2 && groupEvents == 1,
            "Legacy creation group edit updates the model before one owner event");
        bool rejected = false;
        try { groupInput.Value = 9; }
        catch (ArgumentOutOfRangeException) { rejected = true; }
        check(rejected && groupInput.Value == 2 && group.Rating == 2 && groupEvents == 1,
            "Legacy creation group rejects out-of-range controller writes without mutation");
        groupControl.Broken = true;
        check(group.Rating == 0 && group.Broken && !groupInput.Enabled,
            "Legacy broken creation group clears its rating and disables input");
        groupControl.Broken = false;
        groupInput.Value = 1;
        check(!group.Broken && groupInput.Enabled && group.Rating == 1,
            "Legacy restored creation group accepts a subsequent rating");

        var skill = character.Skills.First(item => !item.KnowledgeSkill && !item.IsMeta);
        using var skillControl = new SkillControl { SkillObject = skill };
        int skillEvents = 0; int observedSkill = -1;
        skillControl.RatingChanged += _ => { skillEvents++; observedSkill = skill.Rating; };
        skillControl.SkillRatingMaximum = 6;
        var skillInput = Input(skillControl);
        skillInput.Value = 0;
        skillEvents = 0;
        skillInput.Value = 2;
        check(skill.Rating == 2 && observedSkill == 2 && skillEvents == 1,
            "Legacy creation skill edit updates the model before one owner event");
        skillControl.SkillRatingMaximum = 1;
        check(skill.Rating == 1 && skillInput.Value == 1 && observedSkill == 1,
            "Legacy creation skill maximum refresh clips the existing model rating");
        CheckEditorCleanup(source, check);
    }

    private static void CheckEditorCleanup(Character source, Action<bool, string> check)
    {
        var character = new Character(); character.LoadSnapshot(source.CaptureSnapshot()); character.Created = false;
        using var editor = new frmCreate(character) { Loading = true };
        editor.Show();
        Panel Panel(string name) => (Panel)typeof(frmCreate).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(editor)!;
        var active = Panel("panActiveSkills").Controls.OfType<SkillControl>().First();
        var groups = Panel("panSkillGroups").Controls.OfType<SkillGroupControl>().ToArray();
        bool HasEditorHandler(object row, string name) =>
            (row.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(row) as Delegate)?
            .GetInvocationList().Any(handler => ReferenceEquals(handler.Target, editor)) == true;
        check(HasEditorHandler(active, "RatingChanged") && groups.Any(row => HasEditorHandler(row, "GroupRatingChanged")),
            "Creation skill cleanup baseline exercises real editor-owned rating subscriptions");
        typeof(frmCreate).GetField("_blnIsDirty", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(editor, false);
        editor.Close();
        check(!HasEditorHandler(active, "RatingChanged") && !HasEditorHandler(active, "SpecializationChanged") &&
            !HasEditorHandler(active, "BreakGroupClicked") && groups.All(row => !HasEditorHandler(row, "GroupRatingChanged")),
            "Closed creation editor releases retained skill and group row subscriptions");
    }
}
