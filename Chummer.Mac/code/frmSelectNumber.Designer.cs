namespace Chummer
{
	partial class frmSelectNumber
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
			this.cmdCancel = new Majorsilence.Forms.Button();
			this.lblDescription = new Majorsilence.Forms.Label();
			this.cmdOK = new Majorsilence.Forms.Button();
			this.nudNumber = new Majorsilence.Forms.NumericUpDown();
			((System.ComponentModel.ISupportInitialize)(this.nudNumber)).BeginInit();
			this.SuspendLayout();
			// 
			// cmdCancel
			// 
			this.cmdCancel.DialogResult = Majorsilence.Forms.DialogResult.Cancel;
			this.cmdCancel.Location = new System.Drawing.Point(256, 72);
			this.cmdCancel.Name = "cmdCancel";
			this.cmdCancel.Size = new System.Drawing.Size(75, 23);
			this.cmdCancel.TabIndex = 3;
			this.cmdCancel.Tag = "String_Cancel";
			this.cmdCancel.Text = "Cancel";
			this.cmdCancel.UseVisualStyleBackColor = true;
			this.cmdCancel.Click += new System.EventHandler(this.cmdCancel_Click);
			// 
			// lblDescription
			// 
			this.lblDescription.Location = new System.Drawing.Point(12, 9);
			this.lblDescription.Name = "lblDescription";
			this.lblDescription.Size = new System.Drawing.Size(319, 33);
			this.lblDescription.TabIndex = 0;
			this.lblDescription.Text = "Description goes here.";
			// 
			// cmdOK
			// 
			this.cmdOK.Location = new System.Drawing.Point(256, 43);
			this.cmdOK.Name = "cmdOK";
			this.cmdOK.Size = new System.Drawing.Size(75, 23);
			this.cmdOK.TabIndex = 2;
			this.cmdOK.Tag = "String_OK";
			this.cmdOK.Text = "OK";
			this.cmdOK.UseVisualStyleBackColor = true;
			this.cmdOK.Click += new System.EventHandler(this.cmdOK_Click);
			// 
			// nudNumber
			// 
			this.nudNumber.Location = new System.Drawing.Point(84, 46);
			this.nudNumber.Maximum = new decimal(new int[] {
            10000000,
            0,
            0,
            0});
			this.nudNumber.Name = "nudNumber";
			this.nudNumber.Size = new System.Drawing.Size(99, 20);
			this.nudNumber.TabIndex = 1;
			// 
			// frmSelectNumber
			// 
			this.AcceptButton = this.cmdOK;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = Majorsilence.Forms.AutoScaleMode.Font;
			this.AutoSize = true;
			this.CancelButton = this.cmdCancel;
			this.ClientSize = new System.Drawing.Size(343, 103);
			this.Controls.Add(this.nudNumber);
			this.Controls.Add(this.cmdCancel);
			this.Controls.Add(this.lblDescription);
			this.Controls.Add(this.cmdOK);
			this.FormBorderStyle = Majorsilence.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "frmSelectNumber";
			this.ShowInTaskbar = false;
			this.StartPosition = Majorsilence.Forms.FormStartPosition.CenterParent;
			this.Tag = "Title_SelectNumber";
			this.Text = "Select a Number";
			this.Shown += new System.EventHandler(this.frmSelectNumber_Shown);
			((System.ComponentModel.ISupportInitialize)(this.nudNumber)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private Majorsilence.Forms.Button cmdCancel;
		private Majorsilence.Forms.Label lblDescription;
		private Majorsilence.Forms.Button cmdOK;
		private Majorsilence.Forms.NumericUpDown nudNumber;
	}
}