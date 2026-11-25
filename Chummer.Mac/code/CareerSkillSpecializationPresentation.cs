namespace Chummer;

public partial class SkillControl
{
    // Keep the saved specialty out of the
    // visible layout until the user explicitly opens that row's plus command.
    private bool careerSpecializationExpanded;
    private Avalonia.Controls.ComboBox nativeCareerSpecialization;

    private void RevealCareerSpecialization()
    {
        if (_objSkill == null || !_objSkill.CharacterObject.Created || SkillIsMeta) return;
        careerSpecializationExpanded = true;
        RefreshCareerSpecializationPresentation();
        if (FindForm() is frmCareer editor)
            MountCareerSpecialization(editor);
    }

    private void MountCareerSpecialization(frmCareer editor)
    {
        if (nativeCareerSpecialization == null)
        {
            if (careerNativeRow != null)
                Controls.Remove(cboSpec);
            nativeCareerSpecialization = NativeListSelection.Attach(editor, cboSpec, "Skill specialization",
                row: this, layoutManaged: careerNativeRow != null);
            if (careerNativeRow != null && nativeCareerSpecialization != null)
                careerNativeRow.Children.Add(nativeCareerSpecialization);
            if (nativeCareerSpecialization != null && !KnowledgeSkill)
            {
                nativeCareerSpecialization.LostFocus += (_, _) => Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    if (!IsDisposed && cboSpec.Enabled && nativeCareerSpecialization.IsEffectivelyVisible &&
                        !nativeCareerSpecialization.IsKeyboardFocusWithin && !nativeCareerSpecialization.IsDropDownOpen)
                        cboSpec_Leave(cboSpec, System.EventArgs.Empty);
                }, Avalonia.Threading.DispatcherPriority.Input);
            }
        }
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
