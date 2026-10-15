using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Majorsilence.Forms;
using System.Xml;





namespace Chummer
{
	public partial class frmPrintMultiple : Form
	{
        private CancellationTokenSource batchCancellation;
        private TaskCompletionSource preparationFinished;
        internal Task CancelPreparationForQuitAsync()
        {
            CancelPreparation();
            return preparationFinished?.Task ?? Task.CompletedTask;
        }
        private void CancelPreparation()
        {
            batchCancellation?.Cancel();
            BulkOperationDialogs.CancelOwned(this);
        }
        private bool closeAfterCancellation;
        private Label batchStatus;

		#region Control Events
		public frmPrintMultiple()
		{
			InitializeComponent();
			LanguageManager.Instance.Load(GlobalOptions.Instance.Language, this);
			MoveControls();
            batchStatus = new Label { Left = prgProgress.Left, Top = prgProgress.Bottom + 6,
                Width = prgProgress.Width, Height = 36, Text = "" };
            Controls.Add(batchStatus);
            ClientSize = new System.Drawing.Size(ClientSize.Width, batchStatus.Bottom + 8);
            Disposed += (_, _) => CancelPreparation();
            FormClosing += (_, args) => {
                if (batchCancellation == null) return;
                args.Cancel = true;
                closeAfterCancellation = true;
                CancelPreparation();
            };
		}

		private void cmdSelectCharacter_Click(object sender, EventArgs e)
		{
			if (batchCancellation != null) return;
			// Add the selected Files to the list of characters to print.
			if (dlgOpenFile.ShowDialog(this) == DialogResult.OK)
			{
				foreach (string strFileName in dlgOpenFile.FileNames)
				{
					string[] strLongName = strFileName.Split(Path.DirectorySeparatorChar);
					TreeNode objNode = new TreeNode();
					objNode.Text = strLongName[strLongName.Length - 1];
					objNode.Tag = strFileName;
					treCharacters.Nodes.Add(objNode);
				}
			}
		}

		private void cmdDelete_Click(object sender, EventArgs e)
		{
            if (batchCancellation != null) return;
			try
			{
				treCharacters.SelectedNode.Remove();
			}
			catch
			{
			}
		}

        private async void cmdPrint_Click(object sender, EventArgs e)
        {
            if (batchCancellation != null) { CancelPreparation(); return; }
            if (AppPlatform.QuitInProgress) return;
            string[] files = treCharacters.Nodes.Cast<TreeNode>().Select(n => n.Tag.ToString()).ToArray();
            if (files.Length == 0) return;
            using var cancellation = new CancellationTokenSource();
            preparationFinished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            batchCancellation = cancellation;
            cmdSelectCharacter.Enabled = cmdDelete.Enabled = treCharacters.Enabled = false;
            string printCaption = cmdPrint.Text;
            cmdPrint.Text = LanguageManager.Instance.GetString("String_Cancel");
            XmlDocument document = null;
            AppDiagnostics.Record("batch-output-start");
            try
            {
                document = await BatchCharacterOutput.BuildAsync(files, cancellation.Token, (done, total) => {
                    prgProgress.Maximum = total;
                    prgProgress.Value = done;
                    batchStatus.Text = string.Format(LanguageManager.Instance.GetString("BatchOutput_Progress"), done, total);
                });
                batchStatus.Text = "";
                AppDiagnostics.Record("batch-output-completed");
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
            {
                if (!IsDisposed) batchStatus.Text = LanguageManager.Instance.GetString("BatchOutput_Cancelled");
                AppDiagnostics.Record("batch-output-cancelled");
            }
            catch (Exception error)
            {
                AppDiagnostics.Record("batch-output-failed", error);
                if (!IsDisposed) batchStatus.Text = LanguageManager.Instance.GetString("BatchOutput_Failed");
            }
            finally
            {
                batchCancellation = null;
                if (!IsDisposed)
                {
                    cmdPrint.Text = printCaption;
                    cmdSelectCharacter.Enabled = cmdDelete.Enabled = treCharacters.Enabled = true;
                    prgProgress.Value = 0;
                }
                preparationFinished.TrySetResult();
            }
            if (IsDisposed) return;
            if (closeAfterCancellation) { Close(); return; }
            if (document == null || IsDisposed) return;
            try
            {
                using var preview = new frmViewer { CharacterXML = document, SelectedSheet = "Game Master Summary" };
                await preview.ShowDialogAsync(this);
            }
            catch (Exception error)
            {
                AppDiagnostics.Record("batch-preview-failed", error);
                if (!IsDisposed) batchStatus.Text = LanguageManager.Instance.GetString("BatchOutput_Failed");
            }
        }

		#endregion

		#region Methods
		private void MoveControls()
		{
			int intWidth = Math.Max(cmdSelectCharacter.Width, cmdPrint.Width);
			intWidth = Math.Max(intWidth, cmdDelete.Width);
			cmdSelectCharacter.AutoSize = false;
			cmdPrint.AutoSize = false;
			cmdDelete.AutoSize = false;

			cmdSelectCharacter.Width = intWidth;
			cmdPrint.Width = intWidth;
			cmdDelete.Width = intWidth;
			this.Width = cmdPrint.Left + cmdPrint.Width + 19;

			prgProgress.Width = this.Width - prgProgress.Left - 19;
		}
		#endregion
	}
}