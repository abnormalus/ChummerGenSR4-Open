using System;
using System.Linq;
using Forms = Majorsilence.Forms;

namespace Chummer;

// Exercises the existing translated-specialization/meta policy through the
// native adapter; it does not duplicate the modifier calculation.
internal static class CreationActiveSkillMetaChecks
{
    internal static void Run(Character source, Action<bool, string> check)
    {
        var character = new Character(); character.LoadSnapshot(source.CaptureSnapshot());
        character.Created = false;
        var primaryModel = new Skill(character) { Name = "Pistols", Attribute = "AGI" };
        var metaModel = new Skill(character) { Name = "Synthetic meta", Attribute = "AGI",
            IsMeta = true, MetaBase = "Pistols", MetaSpec = "Canonical synthetic" };
        using var primary = new SkillControl { SkillObject = primaryModel };
        using var meta = new SkillControl { SkillObject = metaModel };
        primary.RatingChanged += _ => { }; primary.SpecializationChanged += _ => { };
        primary.MetaTalents.Add(meta);
        primary.AddSpec("Localized synthetic", "Canonical synthetic");
        meta.SkillIsMeta = true; primary.SkillRating = 2;
        using var panel = new Forms.Panel(); panel.Controls.Add(primary); panel.Controls.Add(meta);
        using var section = new CreationActiveSkillSection(null, panel, new[] { primary, meta });
        var primaryRow = section.Rows[primary]; var metaRow = section.Rows[meta];
        check(metaRow.Layout.IsVisible && !metaRow.Rating.IsVisible && !metaRow.Specialization.IsVisible,
            "Creation native meta row preserves its caption while hiding direct rating and specialization fields");
        metaRow.Rating.Value = 5; metaRow.Specialization.Text = "Queued meta edit";
        check(metaModel.Rating == 2 && metaModel.Specialization == "" && metaRow.Rating.Value == 2 && metaRow.Specialization.Text == "",
            "Creation native meta hidden fields reject queued edits and retain controller state");
        var choice = primary.CreationSpecializationInput.Items.Cast<object>().Single();
        primaryRow.Specialization.SelectedItem = choice;
        check(ReferenceEquals(primary.CreationSpecializationInput.SelectedItem, choice)
            && primaryModel.Specialization == "Localized synthetic" && metaModel.MetaRatingModifier == 2,
            "Creation native translated specialization selection preserves canonical meta mapping");
        primaryRow.Specialization.Text = "Unmapped literal";
        check(primaryModel.Specialization == "Unmapped literal" && metaModel.MetaRatingModifier == 0,
            "Creation native custom specialization clears the existing meta match through controller policy");
        primary.SkillRating = 3;
        check(metaModel.Rating == 3 && metaRow.Rating.Value == 3 && !metaRow.Rating.IsVisible,
            "Creation native meta row follows source rating propagation without exposing a direct edit");
    }
}
