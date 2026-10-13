namespace Chummer
{
    partial class frmSelectText
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
			this.txtValue = new Majorsilence.Forms.TextBox();
			this.cmdCancel = new Majorsilence.Forms.Button();
			this.SuspendLayout();
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
			// txtValue
			// 
			this.txtValue.Location = new System.Drawing.Point(12, 43);
			this.txtValue.Name = "txtValue";
			this.txtValue.Size = new System.Drawing.Size(234, 20);
			this.txtValue.TabIndex = 1;
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
			// frmSelectText
			// 
			this.AcceptButton = this.cmdOK;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = Majorsilence.Forms.AutoScaleMode.Font;
			this.AutoSize = true;
			this.CancelButton = this.cmdCancel;
			this.ClientSize = new System.Drawing.Size(343, 103);
			this.ControlBox = false;
			this.Controls.Add(this.cmdCancel);
			this.Controls.Add(this.txtValue);
			this.Controls.Add(this.lblDescription);
			this.Controls.Add(this.cmdOK);
			this.FormBorderStyle = Majorsilence.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "frmSelectText";
			this.ShowInTaskbar = false;
			this.StartPosition = Majorsilence.Forms.FormStartPosition.CenterParent;
			this.Tag = "Title_SelectText";
			this.Text = "Enter a Value";
			this.Shown += new System.EventHandler(this.frmSelectText_Shown);
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

        private Majorsilence.Forms.Label lblDescription;
        private Majorsilence.Forms.Button cmdOK;
        private Majorsilence.Forms.TextBox txtValue;
        private Majorsilence.Forms.Button cmdCancel;
    }
}