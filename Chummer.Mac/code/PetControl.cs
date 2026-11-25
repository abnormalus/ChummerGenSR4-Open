using Majorsilence.Forms.Drawing;
using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using Majorsilence.Forms;





namespace Chummer
{
	public partial class PetControl : UserControl
	{
		private Contact _objContact;
        private bool metatypeInitialized;

		// Events.
		public event DeleteContactHandler DeleteContact;
		public event FileNameChangedHandler FileNameChanged;
        internal event Action<object> DetailsChanged;

		#region Control Events
		public PetControl()
		{
			InitializeComponent();
			LanguageManager.Instance.Load(GlobalOptions.Instance.Language, this);
			MoveControls();
		}

		private void PetControl_Load(object sender, EventArgs e)
		{
			this.Width = cmdDelete.Left + cmdDelete.Width;
            InitializePetMetatype();
        }

        internal void InitializePetMetatype()
        {
            if (metatypeInitialized)
                return;
            metatypeInitialized = true;
            lblMetatype.Text = "";
            string path = ResolveLinkedCharacter();
            if (path == null)
                return;

            try
            {
                var pet = new Character { FileName = path };
                if (pet.Load(false))
                    lblMetatype.Text = FormatMetatype(pet);
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.Xml.XmlException)
            {
                AppDiagnostics.Record("pet-metatype-load-failed", error);
            }
        }

		private void cmdDelete_Click(object sender, EventArgs e)
		{
			// Raise the DeleteContact Event when the user has confirmed their desire to delete the Contact.
			// The entire ContactControl is passed as an argument so the handling event can evaluate its contents.
			DeleteContact(this);
		}

		private void txtName_TextChanged(object sender, EventArgs e)
		{
            if (_objContact == null || _objContact.Name == txtContactName.Text)
                return;

            _objContact.Name = txtContactName.Text;
            if (Parent != null)
                DetailsChanged?.Invoke(this);
		}

		private void imgLink_Click(object sender, EventArgs e)
		{
			// Determine which options should be shown based on the FileName value.
			if (_objContact.FileName != "")
			{
				tsAttachCharacter.Visible = false;
				tsContactOpen.Visible = true;
				tsRemoveCharacter.Visible = true;
			}
			else
			{
				tsAttachCharacter.Visible = true;
				tsContactOpen.Visible = false;
				tsRemoveCharacter.Visible = false;
			}
			cmsContact.Show(imgLink, imgLink.Left - 700, imgLink.Top);
		}

        private void tsContactOpen_Click(object sender, EventArgs e)
        {
            string path = ResolveLinkedCharacter();
            if (path == null)
            {
                var language = LanguageManager.Instance;
                MessageBox.Show(FindForm(), language.GetString("Message_FileNotFound").Replace("{0}", _objContact.FileName),
                    language.GetString("MessageTitle_FileNotFound"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            GlobalOptions.Instance.MainForm.LoadCharacter(path, false);
        }

        internal string ResolveLinkedCharacter()
        {
            if (File.Exists(_objContact.FileName))
                return _objContact.FileName;

            string relative = _objContact.RelativeFileName;
            if (string.IsNullOrWhiteSpace(relative))
                return null;

            try
            {
                string installedPath = Path.GetFullPath(Uri.UnescapeDataString(relative), Application.StartupPath);
                if (File.Exists(installedPath))
                    return installedPath;

                string legacyPath = Path.GetFullPath(relative);
                return File.Exists(legacyPath) ? legacyPath : null;
            }
            catch (Exception error) when (error is ArgumentException or IOException or NotSupportedException)
            {
                return null;
            }
        }

        private void tsAttachCharacter_Click(object sender, EventArgs e)
        {
            using var picker = new OpenFileDialog { Filter = "Chummer Files (*.chum)|*.chum|All Files (*.*)|*.*" };
            var owner = FindForm();
            if (picker.ShowDialog(owner) != DialogResult.OK)
                return;

            try
            {
                AttachSelectedCharacter(picker.FileName);
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.Xml.XmlException or ArgumentException)
            {
                AppDiagnostics.Record("pet-link-failed", error);
                AppErrors.Inform(owner, "Message_CommandFailed");
            }
        }

        internal bool AttachSelectedCharacter(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            string fullPath = Path.GetFullPath(path);
            var pet = new Character { FileName = fullPath };
            if (!pet.Load(false))
                throw new InvalidDataException("Unable to read the selected character.");

            var relativePath = new Uri(Application.StartupPath).MakeRelativeUri(new Uri(fullPath));
            _objContact.FileName = fullPath;
            _objContact.RelativeFileName = "../" + relativePath;
            lblMetatype.Text = FormatMetatype(pet);
            tipTooltip.SetToolTip(imgLink, LanguageManager.Instance.GetString("Tip_Contact_OpenFile"));
            FileNameChanged?.Invoke(this);
            return true;
        }

        private static string FormatMetatype(Character pet) =>
            string.IsNullOrEmpty(pet.Metavariant) ? pet.Metatype : pet.Metatype + " (" + pet.Metavariant + ")";

		private void tsRemoveCharacter_Click(object sender, EventArgs e)
		{
			// Remove the file association from the Contact.
			if (MessageBox.Show(FindForm(), LanguageManager.Instance.GetString("Message_RemoveCharacterAssociation"), LanguageManager.Instance.GetString("MessageTitle_RemoveCharacterAssociation"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
			{
				_objContact.FileName = "";
				_objContact.RelativeFileName = "";
				tipTooltip.SetToolTip(imgLink, LanguageManager.Instance.GetString("Tip_Contact_LinkFile"));
				lblMetatype.Text = "";
				FileNameChanged(this);
			}
		}

		private void imgNotes_Click(object sender, EventArgs e)
		{
			using var frmContactNotes = new frmNotes();
			frmContactNotes.Notes = _objContact.Notes;
			frmContactNotes.ShowDialog(this);

			if (frmContactNotes.DialogResult == DialogResult.OK && _objContact.Notes != frmContactNotes.Notes)
            {
                _objContact.Notes = frmContactNotes.Notes;
                DetailsChanged?.Invoke(this);
            }

			string strTooltip = "";
			strTooltip = LanguageManager.Instance.GetString("Tip_Contact_EditNotes");
			if (_objContact.Notes != string.Empty)
				strTooltip += "\n\n" + _objContact.Notes;
			tipTooltip.SetToolTip(imgNotes, strTooltip);
		}

		private void cmsContact_Opening(object sender, CancelEventArgs e)
		{
			foreach (ToolStripItem objItem in ((ContextMenuStrip)sender).Items)
			{
				if (objItem.Tag != null)
				{
					objItem.Text = LanguageManager.Instance.GetString(objItem.Tag.ToString());
				}
			}
		}
		#endregion

		#region Methods
		private void MoveControls()
		{
			txtContactName.Left = lblName.Left + lblName.Width + 6;
			lblMetatypeLabel.Left = txtContactName.Left + txtContactName.Width + 16;
			lblMetatype.Left = lblMetatypeLabel.Left + lblMetatypeLabel.Width + 6;
		}
		#endregion

		#region Properties
		/// <summary>
		/// Contact object this is linked to.
		/// </summary>
		public Contact ContactObject
		{
			get
			{
				return _objContact;
			}
			set
			{
				_objContact = value;
			}
		}

		/// <summary>
		/// Contact name.
		/// </summary>
		public string ContactName
		{
			get
			{
				return _objContact.Name;
			}
			set
			{
				txtContactName.Text = value;
				_objContact.Name = value;
			}
		}
		#endregion
	}
}