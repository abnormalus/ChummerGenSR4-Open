namespace Chummer
{
	partial class frmAbout
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            Majorsilence.Forms.ComponentResourceManager resources = new Majorsilence.Forms.ComponentResourceManager(typeof(frmAbout));
            this.okButton = new Majorsilence.Forms.Button();
            this.textBoxDescription = new Majorsilence.Forms.TextBox();
            this.labelCompanyName = new Majorsilence.Forms.Label();
            this.labelCopyright = new Majorsilence.Forms.Label();
            this.labelVersion = new Majorsilence.Forms.Label();
            this.labelProductName = new Majorsilence.Forms.Label();
            this.tableLayoutPanel = new Majorsilence.Forms.TableLayoutPanel();
            this.logoPictureBox = new Majorsilence.Forms.PictureBox();
            this.txtDisclaimer = new Majorsilence.Forms.TextBox();
            this.cmdDonate = new Majorsilence.Forms.Button();
            this.tableLayoutPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.logoPictureBox)).BeginInit();
            this.SuspendLayout();
            // 
            // okButton
            // 
            this.okButton.Anchor = ((Majorsilence.Forms.AnchorStyles)((Majorsilence.Forms.AnchorStyles.Bottom | Majorsilence.Forms.AnchorStyles.Right)));
            this.okButton.DialogResult = Majorsilence.Forms.DialogResult.Cancel;
            this.okButton.Location = new System.Drawing.Point(496, 356);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(75, 23);
            this.okButton.TabIndex = 24;
            this.okButton.Text = "&OK";
            // 
            // textBoxDescription
            // 
            this.textBoxDescription.Dock = Majorsilence.Forms.DockStyle.Fill;
            this.textBoxDescription.Location = new System.Drawing.Point(195, 143);
            this.textBoxDescription.Margin = new Majorsilence.Forms.Padding(6, 3, 3, 3);
            this.textBoxDescription.Multiline = true;
            this.textBoxDescription.Name = "textBoxDescription";
            this.textBoxDescription.ReadOnly = true;
            this.textBoxDescription.ScrollBars = Majorsilence.Forms.ScrollBars.Both;
            this.textBoxDescription.Size = new System.Drawing.Size(376, 64);
            this.textBoxDescription.TabIndex = 23;
            this.textBoxDescription.TabStop = false;
            this.textBoxDescription.Text = "Description";
            // 
            // labelCompanyName
            // 
            this.labelCompanyName.Dock = Majorsilence.Forms.DockStyle.Fill;
            this.labelCompanyName.Location = new System.Drawing.Point(195, 105);
            this.labelCompanyName.Margin = new Majorsilence.Forms.Padding(6, 0, 3, 0);
            this.labelCompanyName.MaximumSize = new System.Drawing.Size(0, 17);
            this.labelCompanyName.Name = "labelCompanyName";
            this.labelCompanyName.Size = new System.Drawing.Size(376, 17);
            this.labelCompanyName.TabIndex = 22;
            this.labelCompanyName.Text = "Company Name";
            this.labelCompanyName.TextAlign = Majorsilence.Forms.ContentAlignment.MiddleLeft;
            // 
            // labelCopyright
            // 
            this.labelCopyright.Dock = Majorsilence.Forms.DockStyle.Fill;
            this.labelCopyright.Location = new System.Drawing.Point(195, 70);
            this.labelCopyright.Margin = new Majorsilence.Forms.Padding(6, 0, 3, 0);
            this.labelCopyright.MaximumSize = new System.Drawing.Size(0, 17);
            this.labelCopyright.Name = "labelCopyright";
            this.labelCopyright.Size = new System.Drawing.Size(376, 17);
            this.labelCopyright.TabIndex = 21;
            this.labelCopyright.Text = "Copyright";
            this.labelCopyright.TextAlign = Majorsilence.Forms.ContentAlignment.MiddleLeft;
            // 
            // labelVersion
            // 
            this.labelVersion.Dock = Majorsilence.Forms.DockStyle.Fill;
            this.labelVersion.Location = new System.Drawing.Point(195, 35);
            this.labelVersion.Margin = new Majorsilence.Forms.Padding(6, 0, 3, 0);
            this.labelVersion.MaximumSize = new System.Drawing.Size(0, 17);
            this.labelVersion.Name = "labelVersion";
            this.labelVersion.Size = new System.Drawing.Size(376, 17);
            this.labelVersion.TabIndex = 0;
            this.labelVersion.Text = "Version";
            this.labelVersion.TextAlign = Majorsilence.Forms.ContentAlignment.MiddleLeft;
            // 
            // labelProductName
            // 
            this.labelProductName.Dock = Majorsilence.Forms.DockStyle.Fill;
            this.labelProductName.Location = new System.Drawing.Point(195, 0);
            this.labelProductName.Margin = new Majorsilence.Forms.Padding(6, 0, 3, 0);
            this.labelProductName.MaximumSize = new System.Drawing.Size(0, 17);
            this.labelProductName.Name = "labelProductName";
            this.labelProductName.Size = new System.Drawing.Size(376, 17);
            this.labelProductName.TabIndex = 19;
            this.labelProductName.Text = "Product Name";
            this.labelProductName.TextAlign = Majorsilence.Forms.ContentAlignment.MiddleLeft;
            // 
            // tableLayoutPanel
            // 
            this.tableLayoutPanel.ColumnCount = 2;
            this.tableLayoutPanel.ColumnStyles.Add(new Majorsilence.Forms.ColumnStyle(Majorsilence.Forms.SizeType.Percent, 33F));
            this.tableLayoutPanel.ColumnStyles.Add(new Majorsilence.Forms.ColumnStyle(Majorsilence.Forms.SizeType.Percent, 67F));
            this.tableLayoutPanel.Controls.Add(this.logoPictureBox, 0, 0);
            this.tableLayoutPanel.Controls.Add(this.labelProductName, 1, 0);
            this.tableLayoutPanel.Controls.Add(this.labelVersion, 1, 1);
            this.tableLayoutPanel.Controls.Add(this.labelCopyright, 1, 2);
            this.tableLayoutPanel.Controls.Add(this.labelCompanyName, 1, 3);
            this.tableLayoutPanel.Controls.Add(this.textBoxDescription, 1, 4);
            this.tableLayoutPanel.Controls.Add(this.txtDisclaimer, 1, 5);
            this.tableLayoutPanel.Controls.Add(this.okButton, 1, 7);
            this.tableLayoutPanel.Controls.Add(this.cmdDonate, 1, 6);
            this.tableLayoutPanel.Dock = Majorsilence.Forms.DockStyle.Fill;
            this.tableLayoutPanel.Location = new System.Drawing.Point(9, 9);
            this.tableLayoutPanel.Name = "tableLayoutPanel";
            this.tableLayoutPanel.RowCount = 8;
            this.tableLayoutPanel.RowStyles.Add(new Majorsilence.Forms.RowStyle(Majorsilence.Forms.SizeType.Percent, 10F));
            this.tableLayoutPanel.RowStyles.Add(new Majorsilence.Forms.RowStyle(Majorsilence.Forms.SizeType.Percent, 10F));
            this.tableLayoutPanel.RowStyles.Add(new Majorsilence.Forms.RowStyle(Majorsilence.Forms.SizeType.Percent, 10F));
            this.tableLayoutPanel.RowStyles.Add(new Majorsilence.Forms.RowStyle(Majorsilence.Forms.SizeType.Percent, 10F));
            this.tableLayoutPanel.RowStyles.Add(new Majorsilence.Forms.RowStyle(Majorsilence.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel.RowStyles.Add(new Majorsilence.Forms.RowStyle(Majorsilence.Forms.SizeType.Percent, 30F));
            this.tableLayoutPanel.RowStyles.Add(new Majorsilence.Forms.RowStyle(Majorsilence.Forms.SizeType.Percent, 10F));
            this.tableLayoutPanel.RowStyles.Add(new Majorsilence.Forms.RowStyle(Majorsilence.Forms.SizeType.Absolute, 29F));
            this.tableLayoutPanel.Size = new System.Drawing.Size(574, 382);
            this.tableLayoutPanel.TabIndex = 0;
            // 
            // logoPictureBox
            // 
            this.logoPictureBox.Dock = Majorsilence.Forms.DockStyle.Fill;
            this.logoPictureBox.Image = ((Majorsilence.Forms.Drawing.Image)(resources.GetObject("logoPictureBox.Image")));
            this.logoPictureBox.Location = new System.Drawing.Point(3, 3);
            this.logoPictureBox.Name = "logoPictureBox";
            this.tableLayoutPanel.SetRowSpan(this.logoPictureBox, 7);
            this.logoPictureBox.Size = new System.Drawing.Size(183, 344);
            this.logoPictureBox.SizeMode = Majorsilence.Forms.PictureBoxSizeMode.Zoom;
            this.logoPictureBox.TabIndex = 12;
            this.logoPictureBox.TabStop = false;
            // 
            // txtDisclaimer
            // 
            this.txtDisclaimer.Anchor = ((Majorsilence.Forms.AnchorStyles)((((Majorsilence.Forms.AnchorStyles.Top | Majorsilence.Forms.AnchorStyles.Bottom) 
            | Majorsilence.Forms.AnchorStyles.Left) 
            | Majorsilence.Forms.AnchorStyles.Right)));
            this.txtDisclaimer.Location = new System.Drawing.Point(192, 213);
            this.txtDisclaimer.Multiline = true;
            this.txtDisclaimer.Name = "txtDisclaimer";
            this.txtDisclaimer.ReadOnly = true;
            this.txtDisclaimer.ScrollBars = Majorsilence.Forms.ScrollBars.Vertical;
            this.txtDisclaimer.Size = new System.Drawing.Size(379, 99);
            this.txtDisclaimer.TabIndex = 25;
            this.txtDisclaimer.Text = "Disclaimer";
            // 
            // cmdDonate
            // 
            this.cmdDonate.Anchor = ((Majorsilence.Forms.AnchorStyles)((Majorsilence.Forms.AnchorStyles.Bottom | Majorsilence.Forms.AnchorStyles.Right)));
            this.cmdDonate.DialogResult = Majorsilence.Forms.DialogResult.Cancel;
            this.cmdDonate.Location = new System.Drawing.Point(496, 324);
            this.cmdDonate.Name = "cmdDonate";
            this.cmdDonate.Size = new System.Drawing.Size(75, 23);
            this.cmdDonate.TabIndex = 26;
            this.cmdDonate.Text = "Donate";
            this.cmdDonate.Click += new System.EventHandler(this.cmdDonate_Click);
            // 
            // frmAbout
            // 
            this.AcceptButton = this.okButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = Majorsilence.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.CancelButton = this.okButton;
            this.ClientSize = new System.Drawing.Size(592, 400);
            this.Controls.Add(this.tableLayoutPanel);
            this.FormBorderStyle = Majorsilence.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "frmAbout";
            this.Padding = new Majorsilence.Forms.Padding(9);
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = Majorsilence.Forms.FormStartPosition.CenterParent;
            this.Text = "frmAbout";
            this.tableLayoutPanel.ResumeLayout(false);
            this.tableLayoutPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.logoPictureBox)).EndInit();
            this.ResumeLayout(false);

		}

		#endregion

		private Majorsilence.Forms.Button okButton;
		private Majorsilence.Forms.TextBox textBoxDescription;
		private Majorsilence.Forms.Label labelCompanyName;
		private Majorsilence.Forms.Label labelCopyright;
		private Majorsilence.Forms.Label labelVersion;
		private Majorsilence.Forms.Label labelProductName;
		private Majorsilence.Forms.TableLayoutPanel tableLayoutPanel;
		private Majorsilence.Forms.PictureBox logoPictureBox;
		private Majorsilence.Forms.TextBox txtDisclaimer;
		private Majorsilence.Forms.Button cmdDonate;

	}
}
