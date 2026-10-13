namespace Chummer
{
	partial class frmNotes
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
			this.txtNotes = new Majorsilence.Forms.TextBox();
			this.SuspendLayout();
			// 
			// txtNotes
			// 
			this.txtNotes.Anchor = ((Majorsilence.Forms.AnchorStyles)((((Majorsilence.Forms.AnchorStyles.Top | Majorsilence.Forms.AnchorStyles.Bottom) 
            | Majorsilence.Forms.AnchorStyles.Left) 
            | Majorsilence.Forms.AnchorStyles.Right)));
			this.txtNotes.Location = new System.Drawing.Point(3, 3);
			this.txtNotes.Multiline = true;
			this.txtNotes.Name = "txtNotes";
			this.txtNotes.ScrollBars = Majorsilence.Forms.ScrollBars.Vertical;
			this.txtNotes.Size = new System.Drawing.Size(512, 237);
			this.txtNotes.TabIndex = 0;
			this.txtNotes.KeyDown += new Majorsilence.Forms.KeyEventHandler(this.txtNotes_KeyDown);
			// 
			// frmNotes
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = Majorsilence.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(518, 244);
			this.Controls.Add(this.txtNotes);
			this.FormBorderStyle = Majorsilence.Forms.FormBorderStyle.SizableToolWindow;
			this.MinimumSize = new System.Drawing.Size(534, 278);
			this.Name = "frmNotes";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.StartPosition = Majorsilence.Forms.FormStartPosition.CenterParent;
			this.Tag = "Title_Notes";
			this.Text = "Notes";
			this.FormClosing += new Majorsilence.Forms.FormClosingEventHandler(this.frmNotes_FormClosing);
			this.Resize += new System.EventHandler(this.frmNotes_Resize);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private Majorsilence.Forms.TextBox txtNotes;
	}
}