#nullable enable
using Forms = Majorsilence.Forms;

namespace Chummer;

public partial class frmCreate
{
    private void InitializeCreationSpecialistInputs()
    {
        if (_packsWorkspace)
            return;
        Attach(nudCyberwareRating, lblCyberwareRatingLabel);
        Attach(nudArmorRating, lblArmorRatingLabel);
        Attach(nudMartialArtsRating, lblMartialArtsRatingLabel);
        Attach(nudComplexFormRating, lblComplexFormRatingLabel);
        Attach(nudMysticAdeptMAGMagician, lblMysticAdeptAssignment);
        Attach(nudSignal, lblSignalLabel);
        Attach(nudResponse, lblResponseLabel);

        void Attach(Forms.NumericUpDown spinner, Forms.Label label) =>
            NativeCompactSpinner.Attach(this, spinner, tipTooltip, label.Text);
    }
}
