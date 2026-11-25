#nullable enable

namespace Chummer;

public partial class frmCareer
{
    private void InitializeCareerSpinners()
    {
        CommonMagicAllocation.Attach(this, lblMysticAdeptAssignment, lblMysticAdeptAssignmentMagician,
            nudMysticAdeptMAGMagician, lblMysticAdeptAssignmentAdept, lblMysticAdeptMAGAdept, tipTooltip, CareerMagicBounds, lblRatingLabel);
        CommonAiAttributes.Attach(this, tipTooltip,
            [(lblRatingLabel, lblRating), (lblSystemLabel, lblSystem), (lblFirewallLabel, lblFirewall)],
            [(lblResponseLabel, nudResponse), (lblSignalLabel, nudSignal)]);

    }
}
