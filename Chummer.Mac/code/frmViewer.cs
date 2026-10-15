using System;
using System.Collections.Generic;
using System.Text;
using Majorsilence.Forms;
using System.IO;
using System.Xml;
using System.Xml.Xsl;
using System.Threading.Tasks;
using System.Drawing;







namespace Chummer
{
	public partial class frmViewer : Form
	{
		private List<Character> _lstCharacters = new List<Character>();
		private XmlDocument _objCharacterXML = new XmlDocument();
		private string _strSelectedSheet = "Shadowrun 4";
		private bool _blnLoading = false;
        private int _generation;
        private bool _closed;
        private string _html = "";
        private Task<CharacterSheetServer> _sheetServerTask;
        private Uri _sheetUri;
        private readonly Button _openBrowser = new Button { Text = "Open in Browser", Enabled = false };
        private readonly Button _savePdf = new Button { Text = "Save PDF", Width = 120 };
        private readonly Button _saveJson = new Button { Text = "Foundry JSON", Enabled = false };
        private readonly Label _status = new Label { AutoSize = false };

		
		#region Control Events
		public frmViewer()
		{
			_strSelectedSheet = GlobalOptions.Instance.DefaultCharacterSheet;

			InitializeComponent();
			LanguageManager.Instance.Load(GlobalOptions.Instance.Language, this);
            MoveControls();
            cmdPrint.Image = null;
            cmdPrint.TextAlign = ContentAlignment.MiddleCenter;
            cmdPrint.SetBounds(12, 12, 110, 28);
            _savePdf.SetBounds(130, 12, 100, 28);
            cmdSaveHTML.AutoSize = cmdSaveXML.AutoSize = false;
            cmdSaveHTML.SetBounds(238, 12, 100, 28);
            cmdSaveXML.SetBounds(346, 12, 100, 28);
            label1.SetBounds(12, 53, 100, 24);
            cboXSLT.SetBounds(120, 49, ClientSize.Width - 132, 28);
            _status.SetBounds(12, 80, ClientSize.Width - 24, 24);
            _status.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            webBrowser1.SetBounds(12, 108, ClientSize.Width - 24, ClientSize.Height - 120);
            MinimumSize = new Size(795, 450);
            Controls.Add(_savePdf); Controls.Add(_status); Controls.Add(_openBrowser); Controls.Add(_saveJson);
            _saveJson.SetBounds(454, 12, 130, 28);
            _saveJson.Click += async (sender, e) => await SaveFoundryAsync();
            _openBrowser.SetBounds(592, 12, 175, 28);
            _openBrowser.Click += (sender, e) => {
                try { if (_sheetUri != null) System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(_sheetUri.AbsoluteUri) { UseShellExecute = true }); }
                catch (Exception error) { ReportFailure("open browser", error); }
            };
            if (System.OperatingSystem.IsMacOS())
            {
                _savePdf.Visible = false;
                cmdSaveHTML.Left = 130; cmdSaveXML.Left = 238; _saveJson.Left = 346; _openBrowser.Left = 484;
            }
            _savePdf.Click += async (sender, e) => await SaveOutputAsync("pdf");
            webBrowser1.DocumentCompleted += async (sender, e) => await PreviewReadyAsync();
            webBrowser1.NavigationFailed += (sender, e) => {
                AppDiagnostics.Record("character-sheet-navigation-failed");
                if (!_closed) _status.Text = "Preview could not load. Choose Open in Browser to view and print.";
            };
            cmdPrint.Enabled = _savePdf.Enabled = false;
		}

		private void frmViewer_Load(object sender, EventArgs e)
		{
			_blnLoading = true;
			List<ListItem> lstFiles = new List<ListItem>();
			// Populate the XSLT list with all of the XSL files found in the sheets directory.
			foreach (string strFile in Directory.GetFiles(Application.StartupPath + Path.DirectorySeparatorChar + "sheets"))
			{
				// Only show files that end in .xsl. Do not include files that end in .xslt since they are used as "hidden" reference sheets (hidden because they are partial templates that cannot be used on their own).
				if (!strFile.EndsWith(".xslt") && strFile.EndsWith(".xsl"))
				{
					string strFileName = strFile.Replace(Application.StartupPath + Path.DirectorySeparatorChar + "sheets" + Path.DirectorySeparatorChar, string.Empty).Replace(".xsl", string.Empty);
					ListItem objItem = new ListItem();
					objItem.Value = strFileName;
					objItem.Name = strFileName;
					lstFiles.Add(objItem);

					//cboXSLT.Items.Add(strFileName);
				}
			}

			try
			{
				// Populate the XSL list with all of the XSL files found in the sheets\[language] directory.
				if (GlobalOptions.Instance.Language != "en-us")
				{
					XmlDocument objLanguageDocument = LanguageManager.Instance.XmlDoc;
					string strLanguage = objLanguageDocument.SelectSingleNode("/chummer/name").InnerText;

					foreach (string strFile in Directory.GetFiles(Application.StartupPath + Path.DirectorySeparatorChar + "sheets" + Path.DirectorySeparatorChar + GlobalOptions.Instance.Language))
					{
						// Only show files that end in .xsl. Do not include files that end in .xslt since they are used as "hidden" reference sheets (hidden because they are partial templates that cannot be used on their own).
						if (!strFile.EndsWith(".xslt") && strFile.EndsWith(".xsl"))
						{
							string strFileName = strFile.Replace(Application.StartupPath + Path.DirectorySeparatorChar + "sheets" + Path.DirectorySeparatorChar + GlobalOptions.Instance.Language + Path.DirectorySeparatorChar, string.Empty).Replace(".xsl", string.Empty);
							ListItem objItem = new ListItem();
							objItem.Value = GlobalOptions.Instance.Language + Path.DirectorySeparatorChar + strFileName;
							objItem.Name = strLanguage + ": " + strFileName;
							lstFiles.Add(objItem);
						}
					}
				}
			}
			catch
			{
			}

			try
			{
				// Populate the XSLT list with all of the XSL files found in the sheets\omae directory.
				foreach (string strFile in Directory.GetFiles(Application.StartupPath + Path.DirectorySeparatorChar + "sheets" + Path.DirectorySeparatorChar + "omae"))
				{
					// Only show files that end in .xsl. Do not include files that end in .xslt since they are used as "hidden" reference sheets (hidden because they are partial templates that cannot be used on their own).
					if (!strFile.EndsWith(".xslt") && strFile.EndsWith(".xsl"))
					{
						string strFileName = strFile.Replace(Application.StartupPath + Path.DirectorySeparatorChar + "sheets" + Path.DirectorySeparatorChar + "omae" + Path.DirectorySeparatorChar, string.Empty).Replace(".xsl", string.Empty);
						ListItem objItem = new ListItem();
						objItem.Value = "omae" + Path.DirectorySeparatorChar + strFileName;
						objItem.Name = LanguageManager.Instance.GetString("Menu_Main_Omae") + ": " + strFileName;
						lstFiles.Add(objItem);
					}
				}
			}
			catch
			{
			}

			cboXSLT.ValueMember = "Value";
			cboXSLT.DisplayMember = "Name";
			cboXSLT.DataSource = lstFiles;

			cboXSLT.SelectedValue = _strSelectedSheet;
			// If the desired sheet was not found, fall back to the Shadowrun 4 sheet.
			if (cboXSLT.Text == "")
				cboXSLT.SelectedValue = "Shadowrun 4";
			GenerateOutput();
            this.Text = _lstCharacters.Count > 0 ? _lstCharacters[0].Alias : "Character sheet";
            if (this.Text == "" && _lstCharacters.Count > 0)
                this.Text = _lstCharacters[0].FileName;
			_blnLoading = false;
		}

		private void cboXSLT_SelectedIndexChanged(object sender, EventArgs e)
		{
			// Re-generate the output when a new sheet is selected.
			if (!_blnLoading && cboXSLT.SelectedValue != null)
			{
				_strSelectedSheet = cboXSLT.SelectedValue.ToString();
				GenerateOutput();
			}
		}

        private async void cmdPrint_Click(object sender, EventArgs e)
        {
            try { AppDiagnostics.Record("character-print-dialog-opening"); await webBrowser1.ShowPrintDialogAsync(); AppDiagnostics.Record("character-print-dialog-closed"); }
            catch (Exception error) { ReportFailure("print", error); }
        }

        protected override bool ProcessCmdKey(ref Message message, Keys keys)
        {
            if (keys == (Keys.Control | Keys.P) && cmdPrint.Enabled) { cmdPrint.PerformClick(); return true; }
            return base.ProcessCmdKey(ref message, keys);
        }

        private async void cmdSaveHTML_Click(object sender, EventArgs e) => await SaveOutputAsync("html");
        private async void cmdSaveXML_Click(object sender, EventArgs e) => await SaveOutputAsync("xml");

        private async Task SaveOutputAsync(string format)
        {
            try
            {
                using var picker = new SaveFileDialog { Filter = format.ToUpperInvariant() + " File|*." + format,
                    Title = "Save character sheet", FileName = "character-sheet." + format };
                if (await picker.ShowDialogAsync(this) != DialogResult.OK) return;
                if (_closed) return;
                if (format == "pdf" && !_savePdf.Enabled) return;
                if (format == "pdf")
                {
                    await ExportPdfAsync(picker.FileName);
                }
                else if (format == "xml") _objCharacterXML.Save(picker.FileName);
                else await File.WriteAllTextAsync(picker.FileName, _html, new UTF8Encoding(false));
                AppDiagnostics.Record("character-sheet-export-" + format);
            }
            catch (Exception error) { ReportFailure("export", error); }
        }

        private async Task SaveFoundryAsync()
        {
            try
            {
                if (_lstCharacters.Count == 0) return;
                using var options = new FoundryExportDialog(_lstCharacters);
                if (await options.ShowDialogAsync(this) != DialogResult.OK || _closed) return;
                using var picker = new SaveFileDialog { Filter = "Foundry Actor JSON|*.json", Title = "Export Foundry actor", FileName = "chummer-actor.json" };
                if (await picker.ShowDialogAsync(this) != DialogResult.OK || _closed) return;
                await ExportFoundryAsync(picker.FileName, options.CharacterIndex, options.Target);
                _status.Text = "Foundry actor exported. Check item notes for any manual adjustments.";
            }
            catch (Exception error) { ReportFailure("export Foundry JSON", error); }
        }

        internal async Task ExportFoundryAsync(string path, int characterIndex, FoundryActorExporter.Target target)
        {
            // Snapshot on the UI thread: model getters and temporary improvement measurements are synchronous.
            string json = FoundryActorExporter.Serialize(_lstCharacters[characterIndex], target);
            string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try { await File.WriteAllTextAsync(temporary, json, new UTF8Encoding(false)); File.Move(temporary, path, true); }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
            AppDiagnostics.Record("character-sheet-export-foundry");
        }

        internal async Task ExportPdfAsync(string path)
        {
            await OutputTask;
            await PreviewReadyAsync();
            if (_closed || !_savePdf.Enabled) throw new InvalidOperationException("The sheet is not ready for PDF output.");
            using var pdf = await webBrowser1.PrintToPdfStreamAsync();
            if (pdf.CanSeek) pdf.Position = 0;
            string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var output = File.Create(temporary)) await pdf.CopyToAsync(output);
                File.Move(temporary, path, true);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }

        private async Task PreviewReadyAsync()
        {
            int generation = _generation;
            if (!webBrowser1.IsWebViewFunctional) return;
            Exception lastError = null;
            var deadline = DateTime.UtcNow.AddSeconds(30);
            try
            {
                while (!_closed && generation == _generation && DateTime.UtcNow < deadline)
                {
                    string ready = null;
                    try
                    {
                        ready = await webBrowser1.ExecuteScriptAsync("window.chummerSheetGeneration === " + generation +
                            " && document.readyState === 'complete' && document.body.innerText.trim().length > 0 && Array.from(document.images).every(i => i.complete && i.naturalWidth > 0) && (!document.fonts || document.fonts.status === 'loaded')").WaitAsync(TimeSpan.FromSeconds(2));
                    }
                    catch (Exception error) { lastError = error; }
                    if (_closed || generation != _generation) return;
                    if (ready?.Trim().Trim('"') == "true")
                    {
                        cmdPrint.Enabled = _savePdf.Enabled = webBrowser1.CanPrint;
                        _status.Text = webBrowser1.CanPrint ? "Ready" : "Native printing unavailable. Choose Open in Browser to print.";
                        AppDiagnostics.Record("character-sheet-preview-ready");
                        return;
                    }
                    await Task.Delay(100);
                }
                if (!_closed && generation == _generation)
                {
                    _status.Text = "Preview did not finish loading. Choose Open in Browser to view and print.";
                    AppDiagnostics.Record("character-sheet-preview-timeout", lastError);
                }
            }
            catch (Exception error) { if (!_closed && generation == _generation) ReportFailure("load preview", error); }
        }

        private void ReportFailure(string action, Exception error)
        {
            AppDiagnostics.Record("character-sheet-" + action + "-failed", error);
            if (!_closed) { _status.Text = "Unable to " + action + " character sheet.";
                AppErrors.Notify(this, _status.Text + " Please try again or choose another sheet.", "Character output"); }
        }

        private void frmViewer_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.Cancel) return;
            CloseOutput();
            foreach (Character character in _lstCharacters) character.PrintWindow = null;
        }
		#endregion

		#region Methods
		/// <summary>
		/// Run the generated XML file through the XSL transformation engine to create the file output.
		/// </summary>
        internal static string TransformSheet(XmlDocument document, string sheet)
        {
            string root = Path.GetFullPath(Path.Combine(Application.StartupPath, "sheets"));
            string path = Path.GetFullPath(Path.Combine(root, sheet + ".xsl"));
            if (!path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                throw new InvalidOperationException("The sheet must be inside the bundled sheets folder.");
            var transform = new XslCompiledTransform();
            transform.Load(path, XsltSettings.Default, new BundledXmlResolver(root));
            using var stream = new MemoryStream();
            using (var writer = XmlWriter.Create(stream, transform.OutputSettings)) transform.Transform(document, null, writer);
            return Encoding.UTF8.GetString(stream.ToArray()).TrimStart('\uFEFF');
        }

        private void CloseOutput()
        {
            if (_closed) return;
            _closed = true; ++_generation;
            _ = StopSheetServerAsync();
        }

        private async Task StopSheetServerAsync()
        {
            try { if (_sheetServerTask != null) await (await _sheetServerTask.ConfigureAwait(false)).DisposeAsync().ConfigureAwait(false); }
            catch (Exception error) { AppDiagnostics.Record("character-sheet-server-close-failed", error); }
        }

        internal Task OutputTask { get; private set; } = Task.CompletedTask;

        private void GenerateOutput() => OutputTask = GenerateOutputAsync();

        private async Task GenerateOutputAsync()
        {
            int generation = ++_generation;
            cmdPrint.Enabled = _savePdf.Enabled = cmdSaveHTML.Enabled = cmdSaveXML.Enabled = _openBrowser.Enabled = false;
            _sheetUri = null;
            _status.Text = "Preparing character sheet…";
            try
            {
                var document = (XmlDocument)_objCharacterXML.CloneNode(true);
                string sheet = _strSelectedSheet;
                string html = await Task.Run(() => TransformSheet(document, sheet));
                if (_closed || generation != _generation) return;
                _html = html;
                cmdSaveHTML.Enabled = cmdSaveXML.Enabled = true;
                _sheetServerTask ??= CharacterSheetServer.CreateAsync();
                var server = await _sheetServerTask;
                if (_closed || generation != _generation) return;
                _sheetUri = server.SetSheet(html + "<script>window.chummerSheetGeneration=" + generation + ";</script>", generation);
                _openBrowser.Enabled = true;
                if (webBrowser1.IsWebViewFunctional)
                {
                    webBrowser1.Navigate(_sheetUri);
                    // Poll as well as listening for completion: initialization failures must not leave a blank, unexplained window.
                    _ = PreviewReadyAsync();
                }
                else _status.Text = "Preview unavailable. Choose Open in Browser or save HTML/XML.";
                AppDiagnostics.Record("character-sheet-rendered");
            }
            catch (Exception error) { if (generation == _generation) ReportFailure("prepare", error); }
        }

        public void RefreshView()
        {
            if (_closed) return;
            try
            {
                using var stream = new MemoryStream();
                using (var writer = new XmlTextWriter(stream, Encoding.UTF8))
                {
                    writer.WriteStartDocument(); writer.WriteStartElement("characters");
                    foreach (Character character in _lstCharacters) character.PrintToStream(stream, writer);
                    writer.WriteEndElement(); writer.WriteEndDocument(); writer.Flush();
                    stream.Position = 0;
                    var document = new XmlDocument(); document.Load(stream); _objCharacterXML = document;
                }
                GenerateOutput();
            }
            catch (Exception error) { ReportFailure("prepare", error); }
        }

		private void MoveControls()
		{
			int intWidth = cmdPrint.Width;
			cmdPrint.AutoSize = false;
			cmdPrint.Width = intWidth + 20;
		}
		#endregion

		#region Properties
		/// <summary>
		/// Character's XmlDocument.
		/// </summary>
		public XmlDocument CharacterXML
		{
			set
			{
				_objCharacterXML = value;
			}
		}

		/// <summary>
		/// Set the XSL sheet that will be selected by default.
		/// </summary>
		public string SelectedSheet
		{
			set
			{
				_strSelectedSheet = value;
			}
		}

		/// <summary>
		/// List of Characters to print.
		/// </summary>
		public List<Character> Characters
		{
			set
			{
				_lstCharacters = value;
                _saveJson.Enabled = value.Count > 0;
			}
		}
		#endregion
	}
}
