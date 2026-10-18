#nullable enable
using System;
using System.Threading.Tasks;
using Forms = Majorsilence.Forms;

namespace Chummer;

// Hit selection uses the shared accepted-integer boundary. Conversion rules stay
// in the creation caller, which already rejects a canceled selection.
public sealed class frmDiceHits : IDisposable
{
    private readonly frmSelectNumber prompt = new();
    public int Dice { get; set; }
    public string Text { get; set; } = LanguageManager.Instance.GetString("Title_DiceHits");
    public string Title { set => Text = value; }
    public string Description { get; set; } = "";
    public int Result => prompt.SelectedValue;
    public Forms.DialogResult DialogResult => prompt.DialogResult;
    private void Prepare()
    {
        if (Dice < 0) throw new ArgumentOutOfRangeException(nameof(Dice));
        prompt.Minimum = 0; prompt.Maximum = Dice; prompt.SelectedValue = 0;
        prompt.Text = Text;
        prompt.Description = Description + Environment.NewLine + LanguageManager.Instance.GetString("String_DiceHits_HitsOn") + " " + Dice + "D6:";
    }
    public Forms.DialogResult ShowDialog(Forms.Form owner) { Prepare(); return prompt.ShowDialog(owner); }
    internal Task<Forms.DialogResult> ShowDialogAsync(Forms.Form owner) { Prepare(); return prompt.ShowDialogAsync(owner); }
    public void Dispose() => prompt.Dispose();
}
