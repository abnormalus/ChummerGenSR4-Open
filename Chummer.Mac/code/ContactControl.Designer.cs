namespace Chummer
{
    partial class ContactControl
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
			this.txtContactName = new Majorsilence.Forms.TextBox();
			this.nudConnection = new Majorsilence.Forms.NumericUpDown();
			this.nudLoyalty = new Majorsilence.Forms.NumericUpDown();
			this.lblConnection = new Majorsilence.Forms.Label();
			this.lblLoyalty = new Majorsilence.Forms.Label();
			this.cmdDelete = new Majorsilence.Forms.Button();
			this.cmsContact = new Majorsilence.Forms.ContextMenuStrip(this.components);
			this.tsContactOpen = new Majorsilence.Forms.ToolStripMenuItem();
			this.tsRemoveCharacter = new Majorsilence.Forms.ToolStripMenuItem();
			this.tsAttachCharacter = new Majorsilence.Forms.ToolStripMenuItem();
			this.imgLink = new Majorsilence.Forms.PictureBox();
			this.lblGroupLabel = new Majorsilence.Forms.Label();
			this.lblGroup = new Majorsilence.Forms.Label();
			this.cmdGroup = new Majorsilence.Forms.Button();
			this.tipTooltip = new Majorsilence.Forms.ToolTip(this.components);
			this.imgNotes = new Majorsilence.Forms.PictureBox();
			((System.ComponentModel.ISupportInitialize)(this.nudConnection)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.nudLoyalty)).BeginInit();
			this.cmsContact.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.imgLink)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.imgNotes)).BeginInit();
			this.SuspendLayout();
			// 
			// txtContactName
			// 
			this.txtContactName.BackColor = Majorsilence.Forms.SystemColors.Window;
			this.txtContactName.Cursor = Majorsilence.Forms.Cursors.IBeam;
			this.txtContactName.Location = new System.Drawing.Point(2, 1);
			this.txtContactName.Name = "txtContactName";
			this.txtContactName.Size = new System.Drawing.Size(136, 20);
			this.txtContactName.TabIndex = 0;
			this.txtContactName.TextChanged += new System.EventHandler(this.txtContactName_TextChanged);
			// 
			// nudConnection
			// 
			this.nudConnection.Location = new System.Drawing.Point(204, 2);
			this.nudConnection.Maximum = new decimal(new int[] {
            6,
            0,
            0,
            0});
			this.nudConnection.Name = "nudConnection";
			this.nudConnection.Size = new System.Drawing.Size(40, 20);
			this.nudConnection.TabIndex = 2;
			this.nudConnection.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
			this.nudConnection.ValueChanged += new System.EventHandler(this.nudConnection_ValueChanged);
			// 
			// nudLoyalty
			// 
			this.nudLoyalty.Location = new System.Drawing.Point(310, 2);
			this.nudLoyalty.Maximum = new decimal(new int[] {
            6,
            0,
            0,
            0});
			this.nudLoyalty.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
			this.nudLoyalty.Name = "nudLoyalty";
			this.nudLoyalty.Size = new System.Drawing.Size(40, 20);
			this.nudLoyalty.TabIndex = 4;
			this.nudLoyalty.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
			this.nudLoyalty.ValueChanged += new System.EventHandler(this.nudLoyalty_ValueChanged);
			// 
			// lblConnection
			// 
			this.lblConnection.AutoSize = true;
			this.lblConnection.Location = new System.Drawing.Point(139, 4);
			this.lblConnection.Name = "lblConnection";
			this.lblConnection.Size = new System.Drawing.Size(64, 13);
			this.lblConnection.TabIndex = 1;
			this.lblConnection.Tag = "Label_Contact_Connection";
			this.lblConnection.Text = "Connection:";
			// 
			// lblLoyalty
			// 
			this.lblLoyalty.AutoSize = true;
			this.lblLoyalty.Location = new System.Drawing.Point(252, 4);
			this.lblLoyalty.Name = "lblLoyalty";
			this.lblLoyalty.Size = new System.Drawing.Size(43, 13);
			this.lblLoyalty.TabIndex = 3;
			this.lblLoyalty.Tag = "Label_Contact_Loyalty";
			this.lblLoyalty.Text = "Loyalty:";
			// 
			// cmdDelete
			// 
			this.cmdDelete.Location = new System.Drawing.Point(484, 0);
			this.cmdDelete.Name = "cmdDelete";
			this.cmdDelete.Size = new System.Drawing.Size(49, 23);
			this.cmdDelete.TabIndex = 7;
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
			// tsContactOpen
			// 
			this.tsContactOpen.Image = global::Chummer.Properties.Resources.link_go;
			this.tsContactOpen.Name = "tsContactOpen";
			this.tsContactOpen.Size = new System.Drawing.Size(171, 22);
			this.tsContactOpen.Tag = "MenuItem_OpenCharacter";
			this.tsContactOpen.Text = "Open Character";
			this.tsContactOpen.Click += new System.EventHandler(this.tsContactOpen_Click);
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
			// tsAttachCharacter
			// 
			this.tsAttachCharacter.Image = global::Chummer.Properties.Resources.link_add;
			this.tsAttachCharacter.Name = "tsAttachCharacter";
			this.tsAttachCharacter.Size = new System.Drawing.Size(171, 22);
			this.tsAttachCharacter.Tag = "MenuItem_AttachCharacter";
			this.tsAttachCharacter.Text = "Attach Character";
			this.tsAttachCharacter.Click += new System.EventHandler(this.tsAttachCharacter_Click);
			// 
			// imgLink
			// 
			this.imgLink.Image = global::Chummer.Properties.Resources.link;
			this.imgLink.Location = new System.Drawing.Point(443, 4);
			this.imgLink.Name = "imgLink";
			this.imgLink.Size = new System.Drawing.Size(16, 16);
			this.imgLink.TabIndex = 6;
			this.imgLink.TabStop = false;
			this.imgLink.Click += new System.EventHandler(this.imgLink_Click);
			// 
			// lblGroupLabel
			// 
			this.lblGroupLabel.AutoSize = true;
			this.lblGroupLabel.Location = new System.Drawing.Point(356, 4);
			this.lblGroupLabel.Name = "lblGroupLabel";
			this.lblGroupLabel.Size = new System.Drawing.Size(39, 13);
			this.lblGroupLabel.TabIndex = 5;
			this.lblGroupLabel.Tag = "Label_Contact_Group";
			this.lblGroupLabel.Text = "Group:";
			// 
			// lblGroup
			// 
			this.lblGroup.AutoSize = true;
			this.lblGroup.Location = new System.Drawing.Point(395, 4);
			this.lblGroup.Name = "lblGroup";
			this.lblGroup.Size = new System.Drawing.Size(13, 13);
			this.lblGroup.TabIndex = 8;
			this.lblGroup.Text = "0";
			// 
			// cmdGroup
			// 
			this.cmdGroup.Location = new System.Drawing.Point(415, 0);
			this.cmdGroup.Name = "cmdGroup";
			this.cmdGroup.Size = new System.Drawing.Size(25, 23);
			this.cmdGroup.TabIndex = 9;
			this.cmdGroup.Text = "...";
			this.tipTooltip.SetToolTip(this.cmdGroup, "Advanced Contact options");
			this.cmdGroup.UseVisualStyleBackColor = true;
			this.cmdGroup.Click += new System.EventHandler(this.cmdGroup_Click);
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
			// imgNotes
			// 
			this.imgNotes.Image = global::Chummer.Properties.Resources.note_edit;
			this.imgNotes.Location = new System.Drawing.Point(464, 4);
			this.imgNotes.Name = "imgNotes";
			this.imgNotes.Size = new System.Drawing.Size(16, 16);
			this.imgNotes.TabIndex = 10;
			this.imgNotes.TabStop = false;
			this.imgNotes.Click += new System.EventHandler(this.imgNotes_Click);
			// 
			// ContactControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = Majorsilence.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.imgNotes);
			this.Controls.Add(this.cmdGroup);
			this.Controls.Add(this.lblGroup);
			this.Controls.Add(this.lblGroupLabel);
			this.Controls.Add(this.lblConnection);
			this.Controls.Add(this.imgLink);
			this.Controls.Add(this.cmdDelete);
			this.Controls.Add(this.txtContactName);
			this.Controls.Add(this.lblLoyalty);
			this.Controls.Add(this.nudConnection);
			this.Controls.Add(this.nudLoyalty);
			this.Name = "ContactControl";
			this.Size = new System.Drawing.Size(536, 23);
			this.Load += new System.EventHandler(this.ContactControl_Load);
			((System.ComponentModel.ISupportInitialize)(this.nudConnection)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.nudLoyalty)).EndInit();
			this.cmsContact.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.imgLink)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.imgNotes)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

        private Majorsilence.Forms.TextBox txtContactName;
        private Majorsilence.Forms.NumericUpDown nudConnection;
        private Majorsilence.Forms.NumericUpDown nudLoyalty;
        private Majorsilence.Forms.Label lblConnection;
        private Majorsilence.Forms.Label lblLoyalty;
        private Majorsilence.Forms.Button cmdDelete;
		private Majorsilence.Forms.PictureBox imgLink;
		private Majorsilence.Forms.ContextMenuStrip cmsContact;
		private Majorsilence.Forms.ToolStripMenuItem tsContactOpen;
		private Majorsilence.Forms.ToolStripMenuItem tsRemoveCharacter;
		private Majorsilence.Forms.ToolStripMenuItem tsAttachCharacter;
		private Majorsilence.Forms.Label lblGroupLabel;
		private Majorsilence.Forms.Label lblGroup;
		private Majorsilence.Forms.Button cmdGroup;
		private Majorsilence.Forms.ToolTip tipTooltip;
		private Majorsilence.Forms.PictureBox imgNotes;
    }
}
