#nullable enable
using System.Collections.Generic;
using Majorsilence.Forms;

namespace Chummer;

public partial class frmCreate
{
    private CreationInputSection? creationInputSection;
    private void InitializeCreationInputSection()
    {
        if (Avalonia.Application.Current == null || creationInputSection != null) return;
        InitializeCreationCommonLayout();
        creationInputSection = new CreationInputSection(this, panAttributes, tabCommon, new[] {
            (nudBOD, lblBODLabel, lblBODMetatype, lblBODAug),
            (nudAGI, lblAGILabel, lblAGIMetatype, lblAGIAug),
            (nudREA, lblREALabel, lblREAMetatype, lblREAAug),
            (nudSTR, lblSTRLabel, lblSTRMetatype, lblSTRAug),
            (nudCHA, lblCHALabel, lblCHAMetatype, lblCHAAug),
            (nudINT, lblINTLabel, lblINTMetatype, lblINTAug),
            (nudLOG, lblLOGLabel, lblLOGMetatype, lblLOGAug),
            (nudWIL, lblWILLabel, lblWILMetatype, lblWILAug),
            (nudEDG, lblEDGLabel, lblEDGMetatype, lblEDGAug),
            (nudMAG, lblMAGLabel, lblMAGMetatype, lblMAGAug),
            (nudRES, lblRESLabel, lblRESMetatype, lblRESAug),
        }, nudNuyen, lblNuyen, lblNuyenTotal, tipTooltip, (lblAttributesBase, lblAttributesAug, lblAttributesMetatype));
        Disposed += (_, _) => creationInputSection?.Dispose();
    }

    // Rules enumerate this owned set, not the presentation panel's child tree.
    // Preserve the legacy panel order for BP/Karma tooltip construction while the
    // native section is introduced. These references remain caller-owned.
    private IEnumerable<NumericUpDown> CreationAttributeInputs
    {
        get
        {
            yield return nudRES; yield return nudMAG; yield return nudEDG;
            yield return nudWIL; yield return nudLOG; yield return nudINT;
            yield return nudCHA; yield return nudSTR; yield return nudREA;
            yield return nudAGI; yield return nudBOD;
        }
    }
}
