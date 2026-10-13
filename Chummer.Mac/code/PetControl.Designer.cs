namespace Chummer
{
	partial class PetControl
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

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this.tipTooltip = new Majorsilence.Forms.ToolTip(this.components);
			this.txtContactName = new Majorsilence.Forms.TextBox();
			this.cmdDelete = new Majorsilence.Forms.Button();
			this.cmsContact = new Majorsilence.Forms.ContextMenuStrip(this.components);
			this.imgLink = new Majorsilence.Forms.PictureBox();
			this.imgNotes = new Majorsilence.Forms.PictureBox();
			this.tsAttachCharacter = new Majorsilence.Forms.ToolStripMenuItem();
			this.tsRemoveCharacter = new Majorsilence.Forms.ToolStripMenuItem();
			this.tsContactOpen = new Majorsilence.Forms.ToolStripMenuItem();
			this.lblName = new Majorsilence.Forms.Label();
			this.lblMetatypeLabel = new Majorsilence.Forms.Label();
			this.lblMetatype = new Majorsilence.Forms.Label();
			this.cmsContact.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.imgLink)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.imgNotes)).BeginInit();
			this.SuspendLayout();
			// 
			// tipTooltip
			// 
			this.tipTooltip.AutoPopDelay = 10000;
			this.tipTooltip.InitialDelay = 250;
			this.tipTooltip.IsBalloon = true;
			this.tipTooltip.ReshowDelay = 100;
			this.tipTooltip.ToolTipIcon = Majorsilence.Forms.ToolTipIcon.Info;
			this.tipTooltip.ToolTipTitle = "Chummer Help";
			// 
			// txtContactName
			// 
			this.txtContactName.BackColor = Majorsilence.Forms.SystemColors.Window;
			this.txtContactName.Cursor = Majorsilence.Forms.Cursors.IBeam;
			this.txtContactName.Location = new System.Drawing.Point(47, 2);
			this.txtContactName.Name = "txtContactName";
			this.txtContactName.Size = new System.Drawing.Size(172, 20);
			this.txtContactName.TabIndex = 11;
			this.txtContactName.TextChanged += new System.EventHandler(this.txtName_TextChanged);
			// 
			// cmdDelete
			// 
			this.cmdDelete.Location = new System.Drawing.Point(682, 0);
			this.cmdDelete.Name = "cmdDelete";
			this.cmdDelete.Size = new System.Drawing.Size(75, 23);
			this.cmdDelete.TabIndex = 18;
			this.cmdDelete.Tag = "String_Delete";
			this.cmdDelete.Text = "Delete";
			this.cmdDelete.UseVisualStyleBackColor = true;
			this.cmdDelete.Click += new System.EventHandler(this.cmdDelete_Click);
			// 
			// cmsContact
			// 
			this.cmsContact.Items.AddRange(new Majorsilence.Forms.ToolStripItem[] {
            this.tsContactOpen,
            this.tsRemoveCharacter,
            this.tsAttachCharacter});
			this.cmsContact.Name = "cmsContact";
			this.cmsContact.Size = new System.Drawing.Size(172, 70);
			this.cmsContact.Opening += new System.ComponentModel.CancelEventHandler(this.cmsContact_Opening);
			// 
			// imgLink
			// 
			this.imgLink.Image = global::Chummer.Properties.Resources.link;
			this.imgLink.Location = new System.Drawing.Point(641, 4);
			this.imgLink.Name = "imgLink";
			this.imgLink.Size = new System.Drawing.Size(16, 16);
			this.imgLink.TabIndex = 17;
			this.imgLink.TabStop = false;
			this.imgLink.Click += new System.EventHandler(this.imgLink_Click);
			// 
			// imgNotes
			// 
			this.imgNotes.Image = global::Chummer.Properties.Resources.note_edit;
			this.imgNotes.Location = new System.Drawing.Point(662, 4);
			this.imgNotes.Name = "imgNotes";
			this.imgNotes.Size = new System.Drawing.Size(16, 16);
			this.imgNotes.TabIndex = 21;
			this.imgNotes.TabStop = false;
			this.imgNotes.Click += new System.EventHandler(this.imgNotes_Click);
			// 
			// tsAttachCharacter
			// 
			this.tsAttachCharacter.Image = global::Chummer.Properties.Resources.link_add;
			this.tsAttachCharacter.Name = "tsAttachCharacter";
			this.tsAttachCharacter.Size = new System.Drawing.Size(171, 22);
			this.tsAttachCharacter.Tag = "MenuItem_AttachCharacter";
			this.tsAttachCharacter.Text = "Attach Character";
			this.tsAttachCharacter.Click += new System.EventHandler(this.tsAttachCharacter_Click);
			// 
			// tsRemoveCharacter
			// 
			this.tsRemoveCharacter.Image = global::Chummer.Properties.Resources.link_delete;
			this.tsRemoveCharacter.Name = "tsRemoveCharacter";
			this.tsRemoveCharacter.Size = new System.Drawing.Size(171, 22);
			this.tsRemoveCharacter.Tag = "MenuItem_RemoveCharacter";
			this.tsRemoveCharacter.Text = "Remove Character";
			this.tsRemoveCharacter.Click += new System.EventHandler(this.tsRemoveCharacter_Click);
			// 
			// tsContactOpen
			// 
			this.tsContactOpen.Image = global::Chummer.Properties.Resources.link_go;
			this.tsContactOpen.Name = "tsContactOpen";
			this.tsContactOpen.Size = new System.Drawing.Size(171, 22);
			this.tsContactOpen.Tag = "MenuItem_OpenCharacter";
			this.tsContactOpen.Text = "Open Character";
			this.tsContactOpen.Click += new System.EventHandler(this.tsContactOpen_Click);
			// 
			// lblName
			// 
			this.lblName.AutoSize = true;
			this.lblName.Location = new System.Drawing.Point(3, 5);
			this.lblName.Name = "lblName";
			this.lblName.Size = new System.Drawing.Size(38, 13);
			this.lblName.TabIndex = 22;
			this.lblName.Tag = "Label_CharacterName";
			this.lblName.Text = "Name:";
			// 
			// lblMetatypeLabel
			// 
			this.lblMetatypeLabel.AutoSize = true;
			this.lblMetatypeLabel.Location = new System.Drawing.Point(225, 5);
			this.lblMetatypeLabel.Name = "lblMetatypeLabel";
			this.lblMetatypeLabel.Size = new System.Drawing.Size(54, 13);
			this.lblMetatypeLabel.TabIndex = 23;
			this.lblMetatypeLabel.Tag = "Label_Metatype";
			this.lblMetatypeLabel.Text = "Metatype:";
			// 
			// lblMetatype
			// 
			this.lblMetatype.AutoSize = true;
			this.lblMetatype.Location = new System.Drawing.Point(285, 5);
			this.lblMetatype.Name = "lblMetatype";
			this.lblMetatype.Size = new System.Drawing.Size(57, 13);
			this.lblMetatype.TabIndex = 24;
			this.lblMetatype.Text = "[Metatype]";
			// 
			// PetControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = Majorsilence.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.lblMetatype);
			this.Controls.Add(this.lblMetatypeLabel);
			this.Controls.Add(this.lblName);
			this.Controls.Add(this.imgLink);
			this.Controls.Add(this.txtContactName);
			this.Controls.Add(this.cmdDelete);
			this.Controls.Add(this.imgNotes);
			this.Name = "PetControl";
			this.Size = new System.Drawing.Size(760, 23);
			this.Load += new System.EventHandler(this.PetControl_Load);
			this.cmsContact.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.imgLink)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.imgNotes)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private Majorsilence.Forms.ToolStripMenuItem tsAttachCharacter;
		private Majorsilence.Forms.ToolTip tipTooltip;
		private Majorsilence.Forms.PictureBox imgLink;
		private Majorsilence.Forms.ToolStripMenuItem tsRemoveCharacter;
		private Majorsilence.Forms.TextBox txtContactName;
		private Majorsilence.Forms.PictureBox imgNotes;
		private Majorsilence.Forms.Button cmdDelete;
		private Majorsilence.Forms.ToolStripMenuItem tsContactOpen;
		private Majorsilence.Forms.ContextMenuStrip cmsContact;
		private Majorsilence.Forms.Label lblName;
		private Majorsilence.Forms.Label lblMetatypeLabel;
		private Majorsilence.Forms.Label lblMetatype;
	}
}
