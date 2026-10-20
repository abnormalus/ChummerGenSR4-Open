using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using Majorsilence.Forms;

namespace Chummer;

internal static class CreationSkillGroupSectionChecks
{
    internal static void Run(Character source, Action<bool, string> check)
    {
        var character = new Character(); character.LoadSnapshot(source.CaptureSnapshot());
        using var panel = new Panel { AutoScroll = true };
        var model = new SkillGroup { Name = "Athletics", RatingMaximum = 4 };
        var controller = new SkillGroupControl(character.Options) { SkillGroupObject = model };
        int events = 0; controller.GroupRatingChanged += _ => events++;
        controller.GroupName = model.Name; controller.GroupRatingMaximum = 4;
        panel.Controls.Add(controller);
        using var section = new CreationSkillGroupSection(null, panel, new[] { controller });
        var input = section.Inputs[controller];
        check(controller.Parent == null && panel.Controls.Count == 0 && !panel.AutoScroll && input.Maximum == 4,
            "Creation native skill-group viewport owns scrolling while retaining its detached controller");
        input.Value = 2;
        check(model.Rating == 2 && events == 1 && input.Value == 2,
            "Creation native group input routes one accepted edit through the existing model and owner event");
        input.Value = 2.5m;
        check(model.Rating == 2 && events == 1 && input.Value == 2,
            "Creation native group input rejects fractional ratings without an owner event");
        input.Value = null;
        check(model.Rating == 2 && events == 1,
            "Creation native group incomplete edit does not commit a fallback");
        controller.GroupRatingMaximum = 1;
        check(input.Value == 1 && model.Rating == 1 && input.Maximum == 1,
            "Creation native group follows controller maximum clipping and model correction");
        controller.Broken = true;
        check(!input.IsEnabled && model.Broken && model.Rating == 0 && input.Value == 0,
            "Creation native broken group clears and disables its rating through existing policy");
        input.Value = 1;
        check(model.Rating == 0 && input.Value == 0,
            "Creation native broken group rejects queued input changes");
        controller.Broken = false; input.Value = 1;
        check(input.IsEnabled && !model.Broken && model.Rating == 1,
            "Creation native group restoration permits a valid edit");
        section.Dispose(); int before = events;
        input.Value = 0;
        check(model.Rating == 1 && events == before,
            "Disposed native group viewport rejects queued callbacks");
        CheckOwnerRules(source, check);
    }
    private static void CheckOwnerRules(Character source, Action<bool, string> check)
    {
        var character = new Character(); character.LoadSnapshot(source.CaptureSnapshot());
        character.Created = false; character.IgnoreRules = true; character.BuildMethod = CharacterBuildMethod.BP;
        foreach (var group in character.SkillGroups) { group.Rating = 0; group.Broken = false; }
        using var editor = new frmCreate(character) { Loading = true };
        editor.Show();
        object Field(string name) => typeof(frmCreate).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(editor)!;
        var controllers = (List<SkillGroupControl>)Field("creationSkillGroups");
        using var section = new CreationSkillGroupSection(null, (Panel)Field("panSkillGroups"), controllers);
        try
        {
            editor.Loading = false;
            var athletics = controllers.Single(group => group.GroupName == "Athletics");
            section.Inputs[athletics].Value = 2;
            var members = character.Skills.Where(skill => skill.SkillGroup == athletics.GroupName).ToArray();
            check(members.Length > 0 && members.All(skill => skill.Rating == 2 && skill.IsGrouped) && athletics.GroupRating == 2,
                "Detached native group controllers retain actual creation owner member-grouping rules");
            int points = controllers.Sum(group => group.GroupRating * character.Options.BPSkillGroup);
            check(((Label)Field("lblSkillGroupsBP")).Text.StartsWith(points.ToString() + " ", StringComparison.Ordinal),
                "Detached native group controllers retain actual creation BP summary accounting");
            section.Inputs[athletics].Value = 0;
            check(members.All(skill => skill.Rating == 0 && !skill.IsGrouped),
                "Detached native group zero rating releases actual owner member skills");
            character.BuildMethod = CharacterBuildMethod.Karma;
            section.Inputs[athletics].Value = 2;
            int karma = character.Options.KarmaNewSkillGroup + 2 * character.Options.KarmaImproveSkillGroup;
            check(((Label)Field("lblSkillGroupsBP")).Text.StartsWith(karma.ToString() + " ", StringComparison.Ordinal),
                "Detached native group controllers retain actual creation incremental Karma accounting");
        }
        finally
        {
            typeof(frmCreate).GetField("_blnIsDirty", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(editor, false);
            editor.Close();
        }
    }
}
