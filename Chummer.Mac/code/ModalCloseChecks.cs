using System;
using Majorsilence.Forms;
namespace Chummer;

internal static class ModalCloseChecks
{
    internal static void Run(Action<bool, string> check)
    {
        using var owner = new Form(); owner.Show();
        using (var notes = new frmNotes())
        {
            notes.Notes = "Synthetic writer’s notes";
            int closing = 0; notes.FormClosing += (_, _) => closing++;
            var pending = notes.ShowDialogAsync(owner);
            notes.Close();
            check(pending.IsCompletedSuccessfully && pending.Result == DialogResult.OK && closing == 1 && owner.Enabled,
                "Notes close chooses OK once without recursive closing and restores its owner");
        }
        using (var dialog = new Form())
        {
            bool cancel = true; int calls = 0;
            dialog.FormClosing += (_, e) => { calls++; dialog.DialogResult = DialogResult.OK; e.Cancel = cancel; };
            var pending = dialog.ShowDialogAsync(owner);
            dialog.Close();
            check(calls == 1 && !pending.IsCompleted && !dialog.IsDisposed,
                "Modal close result chosen inside a canceled closing event leaves the dialog open");
            cancel = false; dialog.Close();
            check(calls == 2 && pending.IsCompletedSuccessfully && pending.Result == DialogResult.OK && owner.Enabled,
                "A subsequent accepted modal close completes once after an earlier cancellation");
        }
    }
}
