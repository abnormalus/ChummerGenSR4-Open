namespace Chummer
{
	partial class frmSelectExoticSkill
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
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
			this.lblDescription = new Majorsilence.Forms.Label();
			this.cmdOK = new Majorsilence.Forms.Button();
			this.cboCategory = new Majorsilence.Forms.ComboBox();
			this.cmdCancel = new Majorsilence.Forms.Button();
			this.SuspendLayout();
			// 
			// lblDescription
			// 
			this.lblDescription.Location = new System.Drawing.Point(12, 9);
			this.lblDescription.Name = "lblDescription";
			this.lblDescription.Size = new System.Drawing.Size(234, 33);
			this.lblDescription.TabIndex = 0;
			this.lblDescription.Tag = "Label_SelectExoticSkill_Description";
			this.lblDescription.Text = "Select an Exotic Active Skill to add to your character.";
			// 
			// cmdOK
			// 
			this.cmdOK.Location = new System.Drawing.Point(171, 77);
			this.cmdOK.Name = "cmdOK";
			this.cmdOK.Size = new System.Drawing.Size(75, 23);
			this.cmdOK.TabIndex = 2;
			this.cmdOK.Tag = "String_OK";
			this.cmdOK.Text = "OK";
			this.cmdOK.UseVisualStyleBackColor = true;
			this.cmdOK.Click += new System.EventHandler(this.cmdOK_Click);
			// 
			// cboCategory
			// 
			this.cboCategory.DropDownStyle = Majorsilence.Forms.ComboBoxStyle.DropDownList;
			this.cboCategory.FormattingEnabled = true;
			this.cboCategory.Location = new System.Drawing.Point(12, 45);
			this.cboCategory.Name = "cboCategory";
			this.cboCategory.Size = new System.Drawing.Size(234, 21);
			this.cboCategory.TabIndex = 1;
			// 
			// cmdCancel
			// 
			this.cmdCancel.DialogResult = Majorsilence.Forms.DialogResult.Cancel;
			this.cmdCancel.Location = new System.Drawing.Point(90, 77);
			this.cmdCancel.Name = "cmdCancel";
			this.cmdCancel.Size = new System.Drawing.Size(75, 23);
			this.cmdCancel.TabIndex = 3;
			this.cmdCancel.Tag = "String_Cancel";
			this.cmdCancel.Text = "Cancel";
			this.cmdCancel.UseVisualStyleBackColor = true;
			this.cmdCancel.Click += new System.EventHandler(this.cmdCancel_Click);
			// 
			// frmSelectExoticSkill
			// 
			this.AcceptButton = this.cmdOK;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = Majorsilence.Forms.AutoScaleMode.Font;
			this.AutoSize = true;
			this.CancelButton = this.cmdCancel;
			this.ClientSize = new System.Drawing.Size(260, 111);
			this.ControlBox = false;
			this.Controls.Add(this.cmdCancel);
			this.Controls.Add(this.lblDescription);
			this.Controls.Add(this.cmdOK);
			this.Controls.Add(this.cboCategory);
			this.FormBorderStyle = Majorsilence.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "frmSelectExoticSkill";
			this.ShowInTaskbar = false;
			this.StartPosition = Majorsilence.Forms.FormStartPosition.CenterParent;
			this.Tag = "Title_SelectExoticSkill";
			this.Text = "Select an Exotic Active Skill";
			this.Load += new System.EventHandler(this.frmSelectExoticSkill_Load);
			this.ResumeLayout(false);

		}

		#endregion

		private Majorsilence.Forms.Label lblDescription;
		private Majorsilence.Forms.Button cmdOK;
		private Majorsilence.Forms.ComboBox cboCategory;
		private Majorsilence.Forms.Button cmdCancel;
	}
}