#nullable enable
using System.Collections.Generic;

namespace Chummer;

public partial class frmCreate
{
    // Knowledge rules and lifetime belong to the creation editor, independently
    // of the scrolling panel that currently presents the legacy row controls.
    private readonly List<SkillControl> creationKnowledgeSkills = new();
    private void AddCreationKnowledgeSkill(SkillControl controller)
    {
        creationKnowledgeSkills.Add(controller);
        panKnowledgeSkills.Controls.Add(controller);
    }
    private void RemoveCreationKnowledgeSkill(SkillControl controller)
    {
        controller.RatingChanged -= objKnowledgeSkill_RatingChanged;
        controller.SpecializationChanged -= objSkill_SpecializationChanged;
        controller.DeleteSkill -= objKnowledgeSkill_DeleteSkill;
        controller.KnowledgeNameChanged -= objKnowledgeSkill_NameChanged;
        controller.BreakGroupClicked -= objSkill_BreakGroupClicked;
        creationKnowledgeSkills.Remove(controller);
        panKnowledgeSkills.Controls.Remove(controller);
    }
}
