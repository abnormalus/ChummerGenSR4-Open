using System;
using System.Linq;
using System.Drawing;
using Majorsilence.Forms.Drawing;
using Majorsilence.Forms;


namespace Chummer
{
    public partial class frmSelectText : Form
    {
        private string _strReturnValue = "";
        private ComboBox detailChoices;
        public void ConfigureChoices(Character character, string qualityName)
        {
            var choices = DetailChoices.ForQuality(character, qualityName);
            if (choices != null) ConfigureChoices(choices);
        }

        // The control only knows about labels and review flags; providers own rules/source filtering.
        internal void ConfigureChoices(System.Collections.Generic.IEnumerable<DetailChoice> availableChoices)
        {
            var choices = availableChoices.ToArray();
            detailChoices = new ComboBox { Name = "detailChoices", Location = txtValue.Location,
                Size = txtValue.Size, TabIndex = txtValue.TabIndex, DropDownStyle = ComboBoxStyle.DropDown,
                KeepInputFocusOnDropDown = true, DrawMode = DrawMode.OwnerDrawFixed, DropDownWidth = 440, MaxDropDownItems = 10 };
            detailChoices.DrawItem += (_, e) =>
            {
                e.DrawBackground();
                if (e.Index < 0 || e.Index >= detailChoices.Items.Count) return;
                var item = (DetailChoice)detailChoices.Items[e.Index];
                // Owner-draw bounds use framebuffer pixels, unlike the dialog's logical layout.
                using var font = new Font(e.Font.FamilyName, (float)(e.Font.SizeInPoints * 96 / 72 * detailChoices.Scaling),
                    item.Review ? FontStyle.Italic : FontStyle.Regular, GraphicsUnit.Pixel);
                using var brush = new SolidBrush(item.Review ? Color.Red : e.ForeColor);
                using var format = new StringFormat { LineAlignment = StringAlignment.Center };
                var bounds = e.Bounds;
                int padding = (int)(3 * detailChoices.Scaling);
                bounds.X += padding;
                bounds.Width -= padding;
                e.Graphics.DrawString(item.ToString(), font, brush, bounds, format);
                e.DrawFocusRectangle();
            };
            detailChoices.Items.AddRange(choices.Cast<object>().ToArray());
            detailChoices.Text = txtValue.Text;
            SearchableChoices.Attach(detailChoices, choices.Cast<object>(), item => ((DetailChoice)item).Name);
            txtValue.Visible = false;
            Controls.Add(detailChoices);
        }
        private string DetailText => detailChoices?.SelectedItem is DetailChoice selected && detailChoices.Text == selected.ToString() ? selected.Name : detailChoices?.Text ?? txtValue.Text;

		#region Control Events
		public frmSelectText()
        {
            InitializeComponent();
			LanguageManager.Instance.Load(GlobalOptions.Instance.Language, this);
        }

		private void cmdOK_Click(object sender, EventArgs e)
        {
            _strReturnValue = PromptDetails.Normalize(DetailText);
            this.DialogResult = DialogResult.OK;
        }

        private void cmdCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
        }

		private void frmSelectText_Shown(object sender, EventArgs e)
		{
			// If the field is pre-populated, immediately click OK.
            if (AutoAcceptPrefilled && DetailText != "")
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
                if (detailChoices != null) detailChoices.Text = value;
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
