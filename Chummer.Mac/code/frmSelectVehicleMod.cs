#nullable enable
using System;
using System.Threading.Tasks;
using Forms = Majorsilence.Forms;

namespace Chummer;

// Legacy purchase callers keep their context API; only an explicitly accepted
// immutable selection escapes the native modal. No inventory is created here.
public sealed class frmSelectVehicleMod : IDisposable
{
    private readonly Character character;
    private readonly bool career;
    private VehicleModPickerWindow? window;
    private CatalogSelection? selection;
    private int body;
    private int? deviceRating;
    public Forms.DialogResult DialogResult { get; private set; } = Forms.DialogResult.Cancel;
    public string SelectedMod => selection?.Name ?? "";
    public int SelectedRating => selection?.Rating ?? 0;
    public bool AddAgain => selection?.AddAgain ?? false;
    public bool FreeCost => selection?.FreeCost ?? false;
    public int Markup => selection?.Markup ?? 0;
    public int VehicleCost { private get; set; }
    public int WeaponCost { private get; set; }
    public int TotalWeaponCost { private get; set; }
    public int ModMultiplier { private get; set; } = 1;
    public int Body { set => body = value > 0 ? value : 2; }
    public int Speed { private get; set; }
    public int AccelRunning { private get; set; }
    public int AccelWalking { private get; set; }
    public string WeaponType { get; set; } = "";
    public string InputFile { private get; set; } = "vehicles";
    // Kept for API compatibility; the old picker did not filter by this property.
    public string AllowedCategories { get; set; } = "";
    public bool HasModularElectronics { private get; set; }
    public int DeviceRating { set => deviceRating = value; }
    public frmSelectVehicleMod(Character objCharacter, bool blnCareer = false) { character = objCharacter; career = blnCareer; }
    public Forms.DialogResult ShowDialog(Forms.Form owner)
    {
        var pending = ShowDialogAsync(owner); Forms.Backends.Platform.Backend.RunModalLoop(pending); return pending.GetAwaiter().GetResult();
    }
    internal VehicleModPickerModel CreateModel() => new(character, career, InputFile, WeaponType,
        new VehicleModPurchaseContext(VehicleCost, WeaponCost, TotalWeaponCost, body, Speed, AccelRunning, AccelWalking, ModMultiplier),
        deviceRating, HasModularElectronics);
    internal async Task<Forms.DialogResult> ShowDialogAsync(Forms.Form owner)
    {
        if (window != null) throw new InvalidOperationException("Modification picker is already open.");
        DialogResult = Forms.DialogResult.Cancel; selection = null;
        window = new VehicleModPickerWindow(CreateModel(), character, InputFile == "weapons");
        try
        {
            selection = await NativeDialogHost.ShowAsync<CatalogSelection>(window, owner);
            if (selection != null) DialogResult = Forms.DialogResult.OK;
            return DialogResult;
        }
        finally { window = null; }
    }
    public void Dispose() { window?.Close(); window = null; }
}
