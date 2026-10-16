#nullable enable
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using Majorsilence.Forms;

namespace Chummer;

// Preserve the legacy purchase boundary while standard controls own the entire dialog.
// Context is captured at Show time because existing callers set it after construction.
public sealed class frmSelectArmorMod : IDisposable
{
    private readonly Character character;
    private readonly bool career;
    private ArmorModPickerWindow? window;
    private CatalogSelection? selection;
    public DialogResult DialogResult { get; private set; } = DialogResult.Cancel;
    public string SelectedArmorMod => selection?.Name ?? "";
    public int SelectedRating => selection?.Rating ?? 0;
    public bool AddAgain => selection?.AddAgain ?? false;
    public bool FreeCost => selection?.FreeCost ?? false;
    public int Markup => selection?.Markup ?? 0;
    public string AllowedCategories { get; set; } = "";
    public int ArmorCost { private get; set; }
    public CapacityStyle CapacityDisplayStyle { private get; set; } = CapacityStyle.Zero;
    internal XmlNode SelectedDefinition => XmlManager.Instance.Load("armor.xml").SelectNodes("/chummer/mods/mod")!
        .Cast<XmlNode>().First(node => node["name"]?.InnerText == selection?.Name);
    public frmSelectArmorMod(Character character, bool career = false)
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
        DialogResult = DialogResult.Cancel; selection = null;
        window = new ArmorModPickerWindow(new ArmorModPickerModel(character, career, AllowedCategories, ArmorCost, CapacityDisplayStyle), character);
        try
        {
            selection = await NativeDialogHost.ShowAsync<CatalogSelection>(window, owner);
            if (selection != null)
            {
                DialogResult = DialogResult.OK;
            }
            return DialogResult;
        }
        finally { window = null; }
    }
    public void Dispose() { window?.Close(); window = null; }
}
