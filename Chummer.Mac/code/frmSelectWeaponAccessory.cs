#nullable enable
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using Majorsilence.Forms;

namespace Chummer;

// Preserve the legacy purchase boundary while standard controls own the entire dialog.
// Context is captured at Show time because existing callers set it after construction.
public sealed class frmSelectWeaponAccessory : IDisposable
{
    private readonly Character character;
    private readonly bool career;
    private AccessoryPickerWindow? window;
    private CatalogSelection? selection;
    private string selectedMount = "";
    public DialogResult DialogResult { get; private set; } = DialogResult.Cancel;
    public string SelectedAccessory => selection?.Name ?? "";
    public string SelectedMount => selectedMount;
    public bool AddAgain => selection?.AddAgain ?? false;
    public bool FreeCost => selection?.FreeCost ?? false;
    public int SelectedQuantity => selection?.Quantity ?? 1;
    public int Markup => selection?.Markup ?? 0;
    public string AllowedMounts { private get; set; } = "";
    public int WeaponCost { private get; set; }
    // Retained caller contract. As before, core/purchase rules own this multiplier.
    public int AccessoryMultiplier { private get; set; } = 1;
    internal XmlNode SelectedDefinition => XmlManager.Instance.Load("weapons.xml").SelectNodes("/chummer/accessories/accessory")!
        .Cast<XmlNode>().First(node => node["name"]?.InnerText == selection?.Name);
    public frmSelectWeaponAccessory(Character character, bool career = false)
    { this.character = character; this.career = career; }
    public DialogResult ShowDialog(Form owner)
    {
        Task<DialogResult> pending = ShowDialogAsync(owner);
        Majorsilence.Forms.Backends.Platform.Backend.RunModalLoop(pending);
        return pending.GetAwaiter().GetResult();
    }
    public async Task<DialogResult> ShowDialogAsync(Form owner)
    {
        if (window != null) throw new InvalidOperationException("This picker is already open.");
        DialogResult = DialogResult.Cancel; selection = null; selectedMount = "";
        window = new AccessoryPickerWindow(new AccessoryPickerModel(character, career, AllowedMounts, WeaponCost), character);
        try
        {
            selection = await NativeDialogHost.ShowAsync<CatalogSelection>(window, owner);
            if (selection != null)
            {
                selectedMount = SelectedDefinition["mount"]!.InnerText;
                DialogResult = DialogResult.OK;
            }
            return DialogResult;
        }
        finally { window = null; }
    }
    public void Dispose() { window?.Close(); window = null; }
}
