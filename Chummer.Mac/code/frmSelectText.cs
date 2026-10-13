using System;
using Majorsilence.Forms;


namespace Chummer
{
    public partial class frmSelectText : Form
    {
        private string _strReturnValue = "";

		#region Control Events
		public frmSelectText()
        {
            InitializeComponent();
			LanguageManager.Instance.Load(GlobalOptions.Instance.Language, this);
        }

		private void cmdOK_Click(object sender, EventArgs e)
        {
            _strReturnValue = PromptDetails.Normalize(txtValue.Text);
            this.DialogResult = DialogResult.OK;
        }

        private void cmdCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
        }

		private void frmSelectText_Shown(object sender, EventArgs e)
		{
			// If the field is pre-populated, immediately click OK.
            if (AutoAcceptPrefilled && txtValue.Text != "")
				cmdOK_Click(sender, e);
		}		
		#endregion

		#region Properties
        public bool AutoAcceptPrefilled { get; set; } = true;
		/// <summary>
		/// Value that was entered in the dialogue.
		/// </summary>
		public string SelectedValue
        {
            get
            {
                return _strReturnValue;
            }
			set
			{
				txtValue.Text = value;
			}
        }

		/// <summary>
		/// Description to display in the dialogue.
		/// </summary>
        public string Description
        {
            set
            {
                lblDescription.Text = value;
            }
        }
		#endregion
    }
}
