namespace Chummer;

public partial class SkillControl
{
    // Career still uses the legacy row. Keep its saved specialty out of the
    // visible layout until the user explicitly opens that row's plus command.
    private bool careerSpecializationExpanded;

    private void RevealCareerSpecialization()
    {
        if (_objSkill == null || !_objSkill.CharacterObject.Created || SkillIsMeta) return;
        careerSpecializationExpanded = true;
        RefreshCareerSpecializationPresentation();
    }

    private void RefreshCareerSpecializationPresentation()
    {
        if (_objSkill == null || !_objSkill.CharacterObject.Created) return;
        cboSpec.Visible = careerSpecializationExpanded && !SkillIsMeta;
        if (!KnowledgeSkill)
        {
            cmdChangeSpec.Visible = !SkillIsMeta;
            return;
        }
        // The original knowledge-specialty prompt and Karma approval remain the
        // edit path. Its plus is the final command in the compact default row.
        cmdChangeSpec.Visible = false;
        if (addKnowledgeSpecialty == null) return;
        cmdDelete.Left = careerSpecializationExpanded ? 758 : 481;
        addKnowledgeSpecialty.Left = cmdDelete.Right + 6;
        addKnowledgeSpecialty.Width = 24;
        addKnowledgeSpecialty.Text = "+";
        tipTooltip.SetToolTip(addKnowledgeSpecialty, LanguageManager.Instance.GetString("Button_AddSpecialty"));
        addKnowledgeSpecialty.Visible = !SkillIsMeta;
        Width = addKnowledgeSpecialty.Right + 3;
    }
}
