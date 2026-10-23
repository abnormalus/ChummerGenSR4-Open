#nullable enable
using System;
using System.Threading.Tasks;
using Majorsilence.Forms;

namespace Chummer;

// Selection only: creation, improvements and BP/Karma charges remain with the caller.
public sealed class frmSelectQuality : IDisposable
{
    private readonly Character character;
    private QualityPickerWindow? window;
    private CatalogSelection? selection;
    public DialogResult DialogResult { get; private set; } = DialogResult.Cancel;
    public string SelectedQuality => selection?.Name ?? "";
    public bool AddAgain => selection?.AddAgain ?? false;
    public bool FreeCost => selection?.FreeCost ?? false;
    public string ForceCategory { private get; set; } = "";
    public string IgnoreQuality { private get; set; } = "";
    public frmSelectQuality(Character character) => this.character = character;
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
        window = new QualityPickerWindow(new QualityPickerModel(character, ForceCategory, IgnoreQuality), character);
        try
        {
            selection = await NativeDialogHost.ShowAsync<CatalogSelection>(window, owner);
            if (selection != null) DialogResult = DialogResult.OK;
            return DialogResult;
        }
        finally { window = null; }
    }
    public void Dispose() { window?.Close(); window = null; }
}
