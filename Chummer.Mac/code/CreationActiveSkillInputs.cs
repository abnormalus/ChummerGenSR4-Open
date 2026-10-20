#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace Chummer;

public partial class frmCreate
{
    // Preserve creation order and controller identities independently of the
    // scrolling panel before the active-skill presentation is replaced.
    private readonly List<SkillControl> creationActiveSkills = new();
    private void AddCreationActiveSkill(SkillControl controller)
    {
        creationActiveSkills.Add(controller);
        if (creationActiveSkillSection == null) panActiveSkills.Controls.Add(controller);
        else creationActiveSkillSection.Add(controller, reveal: true);
    }
    private IEnumerable<SkillControl> SkillControllersFor(Majorsilence.Forms.Panel panel) =>
        ReferenceEquals(panel, panActiveSkills) ? creationActiveSkills :
        ReferenceEquals(panel, panKnowledgeSkills) ? creationKnowledgeSkills : panel.Controls.Cast<SkillControl>();
    private CreationActiveSkillSection? creationActiveSkillSection;
    private bool creationActiveSkillEventsAttached;
    private void InitializeCreationActiveSkills()
    {
        if (Avalonia.Application.Current == null || creationActiveSkillEventsAttached) return;
        creationActiveSkillEventsAttached = true;
        InitializeCreationSkillFilterBar();
        void ShowSkills(object? sender, EventArgs args)
        {
            if (IsDisposed || creationActiveSkillSection != null || !tabSkills.Visible) return;
            creationActiveSkillSection = new CreationActiveSkillSection(this, panActiveSkills, creationActiveSkills);
        }
        tabSkills.VisibleChanged += ShowSkills; Shown += ShowSkills;
        Disposed += (_, _) => { tabSkills.VisibleChanged -= ShowSkills; Shown -= ShowSkills; creationActiveSkillSection?.Dispose(); };
        ShowSkills(this, EventArgs.Empty);
    }
}
