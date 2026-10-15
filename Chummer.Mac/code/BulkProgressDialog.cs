#nullable enable
using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using Majorsilence.Forms;

namespace Chummer;

internal sealed class BulkProgressDialog : Form
{
    private readonly CancellationTokenSource cancellation = new();
    private readonly TaskCompletionSource finished = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly Label status;
    private readonly ProgressBar progress;
    private bool completed;
    private bool started;
    internal BulkProgressDialog()
    {
        Text = LanguageManager.Instance.GetString("Packs_ImportTitle");
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        ClientSize = new Size(440, 132);
        status = new Label { Left = 12, Top = 12, Width = 416, Height = 32,
            Text = LanguageManager.Instance.GetString("Packs_Preparing") };
        progress = new ProgressBar { Left = 12, Top = 48, Width = 416, Height = 22 };
        var cancel = new Button { Left = 328, Top = 86, Width = 100, Height = 28,
            Text = LanguageManager.Instance.GetString("String_Cancel") };
        cancel.Click += (_, _) => RequestCancellation();
        Controls.Add(status); Controls.Add(progress); Controls.Add(cancel);
        FormClosing += (_, args) => { if (!completed) { args.Cancel = true; RequestCancellation(); } };
        Disposed += (_, _) => { if (!completed) RequestCancellation(); };
    }
    internal Task CancelForQuitAsync()
    {
        RequestCancellation();
        return finished.Task;
    }
    private void RequestCancellation()
    {
        if (completed) return;
        cancellation.Cancel();
        // Item rules may currently be asking for a detail. Cancel only dialogs owned
        // by this operation, so Quit can unwind that prompt before awaiting cleanup.
        BulkOperationDialogs.CancelOwned(this);
    }
    internal async Task<T?> RunAsync<T>(Form owner, int total, Func<BulkWork, Task<T>> operation) where T : class
    {
        T? result = null;
        Exception? failure = null;
        EventHandler handler = async (_, _) => {
            if (started) return;
            started = true;
            AppDiagnostics.Record("packs.progress-shown");
            try
            {
                var work = new BulkWork(cancellation.Token, total, (current, count) => {
                    if (IsDisposed) return;
                    progress.Maximum = count; progress.Value = current;
                    status.Text = string.Format(LanguageManager.Instance.GetString("Packs_Progress"), current, count);
                });
                await work.CheckpointAsync();
                result = await operation(work);
                AppDiagnostics.Record("packs.prepared");
                cancellation.Token.ThrowIfCancellationRequested();
            }
            catch (OperationCanceledException) { result = null; AppDiagnostics.Record("packs.import-cancelled"); }
            catch (Exception error) { failure = error; AppDiagnostics.Record("packs.import-failed", error); }
            finally
            {
                completed = true;
                if (!IsDisposed) Close();
                finished.TrySetResult();
            }
        };
        Shown += handler;
        try
        {
            await ShowDialogAsync(owner);
            if (!started) { completed = true; finished.TrySetResult(); }
            await finished.Task;
        }
        finally
        {
            Shown -= handler;
            if (completed || !started) cancellation.Dispose();
        }
        if (failure != null) throw new InvalidOperationException("PACKS preparation failed.", failure);
        return result;
    }
}
