namespace Chummer
{
	partial class frmTest
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
			this.cboTest = new Majorsilence.Forms.ComboBox();
			this.cmdTest = new Majorsilence.Forms.Button();
			this.txtOutput = new Majorsilence.Forms.TextBox();
			this.pgbProgress = new Majorsilence.Forms.ProgressBar();
			this.SuspendLayout();
			// 
			// cboTest
			// 
			this.cboTest.DropDownStyle = Majorsilence.Forms.ComboBoxStyle.DropDownList;
			this.cboTest.FormattingEnabled = true;
			this.cboTest.Items.AddRange(new object[] {
            "armor.xml",
            "bioware.xml",
            "critters.xml",
            "cyberware.xml",
            "gear.xml",
            "metatypes.xml",
            "qualities.xml",
            "vehicles.xml",
            "weapons.xml"});
			this.cboTest.Location = new System.Drawing.Point(12, 12);
			this.cboTest.Name = "cboTest";
			this.cboTest.Size = new System.Drawing.Size(187, 21);
			this.cboTest.Sorted = true;
			this.cboTest.TabIndex = 0;
			// 
			// cmdTest
			// 
			this.cmdTest.Location = new System.Drawing.Point(205, 10);
			this.cmdTest.Name = "cmdTest";
			this.cmdTest.Size = new System.Drawing.Size(75, 23);
			this.cmdTest.TabIndex = 1;
			this.cmdTest.Text = "&Test";
			this.cmdTest.UseVisualStyleBackColor = true;
			this.cmdTest.Click += new System.EventHandler(this.cmdTest_Click);
			// 
			// txtOutput
			// 
			this.txtOutput.Location = new System.Drawing.Point(12, 68);
			this.txtOutput.Multiline = true;
			this.txtOutput.Name = "txtOutput";
			this.txtOutput.ScrollBars = Majorsilence.Forms.ScrollBars.Vertical;
			this.txtOutput.Size = new System.Drawing.Size(927, 524);
			this.txtOutput.TabIndex = 2;
			// 
			// pgbProgress
			// 
			this.pgbProgress.Location = new System.Drawing.Point(12, 39);
			this.pgbProgress.Name = "pgbProgress";
			this.pgbProgress.Size = new System.Drawing.Size(927, 23);
			this.pgbProgress.TabIndex = 3;
			// 
			// frmTest
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = Majorsilence.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(951, 604);
			this.Controls.Add(this.pgbProgress);
			this.Controls.Add(this.txtOutput);
			this.Controls.Add(this.cmdTest);
			this.Controls.Add(this.cboTest);
			this.Name = "frmTest";
			this.Text = "XML Test";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private Majorsilence.Forms.ComboBox cboTest;
		private Majorsilence.Forms.Button cmdTest;
		private Majorsilence.Forms.TextBox txtOutput;
		private Majorsilence.Forms.ProgressBar pgbProgress;
	}
}