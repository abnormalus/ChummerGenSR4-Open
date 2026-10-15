#nullable enable
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using Majorsilence.Forms;

namespace Chummer;

// Compatibility at the existing call boundary only: no hidden Form or legacy controls.
// Callers retain their purchase/creation logic and receive the selected catalog identity.
public sealed class frmSelectWeapon : IDisposable
{
    private readonly Character character;
    private readonly WeaponPickerModel model;
    private WeaponPickerWindow? window;
    private WeaponSelection? selection;
    public DialogResult DialogResult { get; private set; } = DialogResult.Cancel;
    public string SelectedWeapon => selection?.Name ?? "";
    public bool AddAgain => selection?.AddAgain ?? false;
    public bool FreeCost => selection?.FreeCost ?? false;
    public int Markup => selection?.Markup ?? 0;
    internal XmlNode SelectedDefinition => XmlManager.Instance.Load("weapons.xml").SelectNodes("/chummer/weapons/weapon")!
        .Cast<XmlNode>().First(node => node["name"]?.InnerText == selection?.Name && node["category"]?.InnerText == selection?.Category);

    public frmSelectWeapon(Character character, bool career = false)
    {
        this.character = character;
        model = new WeaponPickerModel(character, career);
    }
    public DialogResult ShowDialog(Form owner)
    {
        Task<DialogResult> pending = ShowDialogAsync(owner);
        // Existing rule handlers require a result synchronously. Use the established
        // backend modal loop, never Task.Wait/Result or manual Application.DoEvents.
        Majorsilence.Forms.Backends.Platform.Backend.RunModalLoop(pending);
        return pending.GetAwaiter().GetResult();
    }
    public async Task<DialogResult> ShowDialogAsync(Form owner)
    {
        DialogResult = DialogResult.Cancel; selection = null;
        if (window != null) throw new InvalidOperationException("This picker is already open.");
        window = new WeaponPickerWindow(model, character);
        try
        {
            selection = await NativeDialogHost.ShowAsync<WeaponSelection>(window, owner);
            if (selection != null) DialogResult = DialogResult.OK;
            return DialogResult;
        }
        finally { window = null; }
    }
    public void Dispose() { window?.Close(); window = null; }
}
