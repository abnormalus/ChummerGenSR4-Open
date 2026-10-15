#nullable enable
using System.Linq;
using Majorsilence.Forms;

namespace Chummer;

internal static class BulkOperationDialogs
{
    internal static Form[] OwnedPrompts(Form operation) => Application.OpenForms.OfType<Form>()
        .Where(dialog => dialog != operation && dialog.Modal && !dialog.IsDisposed && IsOwned(dialog, operation)).Reverse().ToArray();
    private static bool IsOwned(Form dialog, Form operation)
    {
        for (Form? owner = dialog.Owner; owner != null; owner = owner.Owner)
            if (owner == operation) return true;
        return false;
    }
    internal static void CancelOwned(Form operation)
    {
        foreach (var dialog in OwnedPrompts(operation)) dialog.DialogResult = DialogResult.Cancel;
    }
}
