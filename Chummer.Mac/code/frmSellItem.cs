#nullable enable
using System;
using System.Threading.Tasks;
using Forms = Majorsilence.Forms;

namespace Chummer;

// Sale rules retain their existing fractional-price boundary. The shared prompt
// owns editing, modality and theme; no inventory changes occur before acceptance.
public sealed class frmSellItem : IDisposable
{
    private readonly frmSelectNumber percentage = new()
    {
        Minimum = 0,
        Maximum = 9999,
        SelectedValue = 50,
        Text = LanguageManager.Instance.GetString("Title_SellItem"),
        Description = LanguageManager.Instance.GetString("Label_SellItem_SellItemFor") + " (%)"
    };

    public Forms.DialogResult DialogResult => percentage.DialogResult;
    public double SellPercent => percentage.SelectedValue / 100.0;
    public Forms.DialogResult ShowDialog(Forms.Form? owner = null) => percentage.ShowDialog(owner);
    internal Task<Forms.DialogResult> ShowDialogAsync(Forms.Form owner) => percentage.ShowDialogAsync(owner);
    public void Dispose() => percentage.Dispose();
}
