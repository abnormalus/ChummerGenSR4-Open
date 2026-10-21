#nullable enable
using System;
using System.Collections.Generic;

namespace Chummer;

public partial class frmCreate
{
    // Knowledge rules and lifetime belong to the creation editor, independently
    // of the panel used to anchor its scrolling presentation.
    private readonly List<SkillControl> creationKnowledgeSkills = new();
    private CreationKnowledgeSkillSection? creationKnowledgeSkillSection;
    private bool creationKnowledgeSkillEventsAttached;
    private void InitializeCreationKnowledgeSkills()
    {
        // The native row has two readable 32px lines. Keep its complete
        // fields available even when the initial Skills split shrinks; the
        // existing splitter still permits users to give knowledge more space.
        splitSkills.Panel2MinSize = Math.Max(splitSkills.Panel2MinSize, panKnowledgeSkills.Top + 80);
        if (Avalonia.Application.Current == null || creationKnowledgeSkillEventsAttached) return;
        creationKnowledgeSkillEventsAttached = true;
        void ShowSkills(object? sender, EventArgs args)
        {
            if (IsDisposed || creationKnowledgeSkillSection != null || !tabSkills.Visible) return;
            creationKnowledgeSkillSection = new CreationKnowledgeSkillSection(this, panKnowledgeSkills, creationKnowledgeSkills);
        }
        tabSkills.VisibleChanged += ShowSkills; Shown += ShowSkills;
        Disposed += (_, _) => { tabSkills.VisibleChanged -= ShowSkills; Shown -= ShowSkills; creationKnowledgeSkillSection?.Dispose(); };
        ShowSkills(this, EventArgs.Empty);
    }
    private void AddCreationKnowledgeSkill(SkillControl controller)
    {
        creationKnowledgeSkills.Add(controller);
        if (creationKnowledgeSkillSection == null) panKnowledgeSkills.Controls.Add(controller);
        else creationKnowledgeSkillSection.Add(controller, reveal: true);
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
        creationKnowledgeSkillSection?.Remove(controller);
    }
}
