using System;
using System.Diagnostics;
using System.Threading;
using Majorsilence.Forms;

namespace Chummer
{
    public partial class frmUpdate : Form
    {
        private readonly CancellationTokenSource _request = new CancellationTokenSource();
        public bool SilentMode { get; set; }
        private bool _requestDisposed;
        private ReleaseInfo _cached;
        internal frmUpdate(ReleaseInfo release) : this() { _cached = release; }

        internal static async System.Threading.Tasks.Task CheckSilentlyAsync(Form owner)
        {
            using var request = new CancellationTokenSource();
            FormClosingEventHandler closing = (_, e) => { if (!e.Cancel) request.Cancel(); };
            owner.FormClosing += closing;
            try
            {
                var release = await ReleaseUpdates.CheckAsync(request.Token);
                if (release.Newer && !request.IsCancellationRequested && !owner.IsDisposed)
                {
                    using var dialog = new frmUpdate(release);
                    await dialog.ShowDialogAsync(owner);
                }
            }
            catch (OperationCanceledException) when (request.IsCancellationRequested) { }
            catch (Exception error) { AppDiagnostics.Record("automatic-update-check-failed", error); }
            finally { owner.FormClosing -= closing; }
        }

        public frmUpdate()
        {
            InitializeComponent();
            LanguageManager.Instance.Load(GlobalOptions.Instance.Language, this);
            FormClosing += (_, e) => { if (!e.Cancel && !_requestDisposed) _request.Cancel(); };
        }

        private async void frmUpdate_Load(object sender, EventArgs e)
        {
            cmdDownload.Enabled = false;
            lblCurrentVersion.Text = LanguageManager.Instance.GetString("Label_Update_CurrentVersion") + " " + Application.ProductVersion;
            lblLatestVersion.Text = "Checking Nightly releases…";
            try
            {
                var release = _cached ?? await ReleaseUpdates.CheckAsync(_request.Token);
                if (_request.IsCancellationRequested || IsDisposed) return;
                if (SilentMode && !release.Newer) { Close(); return; }
                lblLatestVersion.Text = release.Newer ? "A newer Nightly build is available." : "Latest Nightly release";
                txtReleaseNotes.Text = release.Notes;
                cmdDownload.Enabled = true;
                AppDiagnostics.Record("update-check-completed");
            }
            catch (OperationCanceledException) when (_request.IsCancellationRequested) { }
            catch (Exception error)
            {
                AppDiagnostics.Record("update-check-failed", error);
                if (!_request.IsCancellationRequested && !IsDisposed)
                {
                    if (SilentMode) Close();
                    else { lblLatestVersion.Text = "Unable to check for updates. You can still open the release page."; cmdDownload.Enabled = true; }
                }
            }
        }

        private void cmdDownload_Click(object sender, EventArgs e)
        {
            try { Process.Start(new ProcessStartInfo(ReleaseUpdates.ReleasePage) { UseShellExecute = true }); }
            catch (Exception error) { AppDiagnostics.Record("update-page-open-failed", error); AppErrors.Notify(this, "Unable to open the release page.", "Chummer updates"); }
        }
        private void cmdClose_Click(object sender, EventArgs e) => Close();
    }
}
