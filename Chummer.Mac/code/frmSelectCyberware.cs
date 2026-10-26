#nullable enable
using System;
using System.Threading.Tasks;
using Majorsilence.Forms;

namespace Chummer;

// Existing purchase contract over an owned standard catalog window. Configuration
// is captured at Show time; only an accepted draft is published to the caller.
public sealed class frmSelectCyberware : IDisposable
{
    public enum Mode { Cyberware = 0, Bioware = 1 }
    private readonly Character character;
    private readonly bool career;
    private CyberwarePickerWindow? window;
    private CyberwarePickerSelection? selection;
    private bool showing, disposed, lockGrade, subsystemsOnly, allowModular;
    private int maximumCapacity = -1;
    private string grade = "", subsystems = "";
    private double characterEssence = 1, basicBiowareEssence = 1, genetechCost = 1, transgenicCost = 1;
    public DialogResult DialogResult { get; private set; } = DialogResult.Cancel;
    public Mode WindowMode { get; set; }
    internal bool PluginSelection { get; set; }
    public string SelectedCyberware => selection?.Item.Name ?? "";
    public string SelectedCategory => selection?.Item.Category ?? "";
    public Grade? SelectedGrade => selection?.Grade;
    public int SelectedRating => selection?.Item.Rating ?? 0;
    public int SelectedESSDiscount => selection?.EssenceDiscount ?? 0;
    public bool FreeCost => selection?.Item.FreeCost ?? false;
    public bool AddAgain => selection?.Item.AddAgain ?? false;
    public bool ForceTransgenic => selection?.Transgenic ?? false;
    public double CharacterESSMultiplier { set => characterEssence = value; }
    public double BasicBiowareESSMultiplier { set => basicBiowareEssence = value; }
    public double GenetechCostMultiplier { set => genetechCost = value; }
    public double TransgenicsBiowareCostMultiplier { set => transgenicCost = value; }
    public int MaximumCapacity { set => maximumCapacity = value; }
    public bool ShowOnlySubsystems { set => subsystemsOnly = value; }
    public string Subsystems { set => subsystems = value ?? ""; }
    public string SetGrade { set => grade = value ?? ""; }
    public bool AllowModularPlugins { set => allowModular = value; }
    public void LockGrade() => lockGrade = true;

    public frmSelectCyberware(Character character, bool blnCareer = false) { this.character = character; career = blnCareer; }
    internal CyberwarePickerModel CreateModel()
    {
        if (WindowMode is not (Mode.Cyberware or Mode.Bioware)) throw new ArgumentOutOfRangeException(nameof(WindowMode));
        return new(character, career, new(Bioware: WindowMode == Mode.Bioware,
            Context: new(allowModular, PluginSelection, subsystemsOnly, subsystems), InitialGrade: grade,
            LockGrade: lockGrade, MaximumCapacity: maximumCapacity, CharacterEssenceMultiplier: characterEssence,
            BasicBiowareEssenceMultiplier: basicBiowareEssence, GenetechCostMultiplier: genetechCost,
            TransgenicCostMultiplier: transgenicCost));
    }
    public DialogResult ShowDialog(Form owner)
    {
        var pending = ShowDialogAsync(owner);
        Majorsilence.Forms.Backends.Platform.Backend.RunModalLoop(pending);
        return pending.GetAwaiter().GetResult();
    }
    public async Task<DialogResult> ShowDialogAsync(Form owner)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (showing) throw new InvalidOperationException("This picker is already open.");
        showing = true; DialogResult = DialogResult.Cancel; selection = null;
        try
        {
            var model = CreateModel();
            // Another allowed category may have choices even if the initial one is empty.
            if (PluginSelection && !model.HasCompatibleEntries)
            {
                AppErrors.Inform(owner, "Message_NoCompatibleCyberwareAddins");
                return DialogResult;
            }
            window = new CyberwarePickerWindow(model, character);
            var accepted = await NativeDialogHost.ShowAsync<CatalogSelection>(window, owner);
            if (!disposed && accepted != null) { selection = model.PurchaseDraft(accepted); DialogResult = DialogResult.OK; }
            return DialogResult;
        }
        finally { window = null; showing = false; }
    }
    public void Dispose() { disposed = true; window?.Close(); window = null; }
}
