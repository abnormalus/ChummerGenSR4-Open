#nullable enable
using System;
using System.Threading.Tasks;
using Forms = Majorsilence.Forms;
namespace Chummer;

// The source lifestyle is read-only. Only a confirmed immutable draft is applied
// to the caller's unattached replacement object; controllers own model insertion.
public sealed class frmSelectLifestyle : IDisposable
{
    private readonly Lifestyle target;
    private readonly Character character;
    private Lifestyle? source;
    private BasicLifestylePickerWindow? window;
    public Forms.DialogResult DialogResult { get; private set; } = Forms.DialogResult.Cancel;
    public bool AddAgain { get; private set; }
    public Lifestyle SelectedLifestyle => target;
    public frmSelectLifestyle(Lifestyle objLifestyle, Character objCharacter) { target = objLifestyle; character = objCharacter; }
    public void SetLifestyle(Lifestyle objLifestyle) => source = objLifestyle;
    public Forms.DialogResult ShowDialog(Forms.Form owner)
    {
        var pending = ShowDialogAsync(owner); Forms.Backends.Platform.Backend.RunModalLoop(pending); return pending.GetAwaiter().GetResult();
    }
    internal async Task<Forms.DialogResult> ShowDialogAsync(Forms.Form owner)
    {
        if (window != null) throw new InvalidOperationException("Lifestyle picker is already open.");
        DialogResult = Forms.DialogResult.Cancel; AddAgain = false;
        var model = new BasicLifestylePickerModel(character, source, target); window = new BasicLifestylePickerWindow(model, character);
        try
        {
            var accepted = await NativeDialogHost.ShowAsync<CatalogSelection>(window, owner);
            if (accepted != null) { model.PrepareAccepted(accepted).ApplyTo(target); AddAgain = accepted.AddAgain; DialogResult = Forms.DialogResult.OK; }
            return DialogResult;
        }
        finally { window = null; }
    }
    public void Dispose() { window?.Close(); window = null; }
}
