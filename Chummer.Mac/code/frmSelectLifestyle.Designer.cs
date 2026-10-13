namespace Chummer
{
	partial class frmSelectLifestyle
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
            this.components = new System.ComponentModel.Container();
            this.cmdCancel = new Majorsilence.Forms.Button();
            this.lblCost = new Majorsilence.Forms.Label();
            this.lblCostLabel = new Majorsilence.Forms.Label();
            this.lstLifestyles = new Majorsilence.Forms.ListBox();
            this.cmdOK = new Majorsilence.Forms.Button();
            this.cmdOKAdd = new Majorsilence.Forms.Button();
            this.lblSource = new Majorsilence.Forms.Label();
            this.lblSourceLabel = new Majorsilence.Forms.Label();
            this.lblStartingNuyen = new Majorsilence.Forms.Label();
            this.lblStartingNuyenLabel = new Majorsilence.Forms.Label();
            this.lblPercentage = new Majorsilence.Forms.Label();
            this.nudPercentage = new Majorsilence.Forms.NumericUpDown();
            this.nudRoommates = new Majorsilence.Forms.NumericUpDown();
            this.lblRoommates = new Majorsilence.Forms.Label();
            this.tipTooltip = new Majorsilence.Forms.ToolTip(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.nudPercentage)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudRoommates)).BeginInit();
            this.SuspendLayout();
            // 
            // cmdCancel
            // 
            this.cmdCancel.DialogResult = Majorsilence.Forms.DialogResult.Cancel;
            this.cmdCancel.Location = new System.Drawing.Point(268, 164);
            this.cmdCancel.Name = "cmdCancel";
            this.cmdCancel.Size = new System.Drawing.Size(75, 23);
            this.cmdCancel.TabIndex = 13;
            this.cmdCancel.Tag = "String_Cancel";
            this.cmdCancel.Text = "Cancel";
            this.cmdCancel.UseVisualStyleBackColor = true;
            this.cmdCancel.Click += new System.EventHandler(this.cmdCancel_Click);
            // 
            // lblCost
            // 
            this.lblCost.AutoSize = true;
            this.lblCost.Location = new System.Drawing.Point(339, 12);
            this.lblCost.Name = "lblCost";
            this.lblCost.Size = new System.Drawing.Size(19, 13);
            this.lblCost.TabIndex = 2;
            this.lblCost.Text = "[0]";
            // 
            // lblCostLabel
            // 
            this.lblCostLabel.AutoSize = true;
            this.lblCostLabel.Location = new System.Drawing.Point(261, 12);
            this.lblCostLabel.Name = "lblCostLabel";
            this.lblCostLabel.Size = new System.Drawing.Size(66, 13);
            this.lblCostLabel.TabIndex = 1;
            this.lblCostLabel.Tag = "Label_SelectLifestyle_CostPerMonth";
            this.lblCostLabel.Text = "Cost/Month:";
            // 
            // lstLifestyles
            // 
            this.lstLifestyles.FormattingEnabled = true;
            this.lstLifestyles.Location = new System.Drawing.Point(12, 12);
            this.lstLifestyles.Name = "lstLifestyles";
            this.lstLifestyles.Size = new System.Drawing.Size(243, 173);
            this.lstLifestyles.TabIndex = 0;
            this.lstLifestyles.SelectedIndexChanged += new System.EventHandler(this.lstLifestyles_SelectedIndexChanged);
            this.lstLifestyles.DoubleClick += new System.EventHandler(this.lstLifestyles_DoubleClick);
            // 
            // cmdOK
            // 
            this.cmdOK.Location = new System.Drawing.Point(349, 164);
            this.cmdOK.Name = "cmdOK";
            this.cmdOK.Size = new System.Drawing.Size(75, 23);
            this.cmdOK.TabIndex = 11;
            this.cmdOK.Tag = "String_OK";
            this.cmdOK.Text = "OK";
            this.cmdOK.UseVisualStyleBackColor = true;
            this.cmdOK.Click += new System.EventHandler(this.cmdOK_Click);
            // 
            // cmdOKAdd
            // 
            this.cmdOKAdd.Location = new System.Drawing.Point(349, 135);
            this.cmdOKAdd.Name = "cmdOKAdd";
            this.cmdOKAdd.Size = new System.Drawing.Size(75, 23);
            this.cmdOKAdd.TabIndex = 12;
            this.cmdOKAdd.Tag = "String_AddMore";
            this.cmdOKAdd.Text = "&Add && More";
            this.cmdOKAdd.UseVisualStyleBackColor = true;
            this.cmdOKAdd.Click += new System.EventHandler(this.cmdOKAdd_Click);
            // 
            // lblSource
            // 
            this.lblSource.AutoSize = true;
            this.lblSource.Location = new System.Drawing.Point(340, 58);
            this.lblSource.Name = "lblSource";
            this.lblSource.Size = new System.Drawing.Size(47, 13);
            this.lblSource.TabIndex = 6;
            this.lblSource.Text = "[Source]";
            this.lblSource.Click += new System.EventHandler(this.lblSource_Click);
            // 
            // lblSourceLabel
            // 
            this.lblSourceLabel.AutoSize = true;
            this.lblSourceLabel.Location = new System.Drawing.Point(261, 58);
            this.lblSourceLabel.Name = "lblSourceLabel";
            this.lblSourceLabel.Size = new System.Drawing.Size(44, 13);
            this.lblSourceLabel.TabIndex = 5;
            this.lblSourceLabel.Tag = "Label_Source";
            this.lblSourceLabel.Text = "Source:";
            // 
            // lblStartingNuyen
            // 
            this.lblStartingNuyen.AutoSize = true;
            this.lblStartingNuyen.Location = new System.Drawing.Point(339, 35);
            this.lblStartingNuyen.Name = "lblStartingNuyen";
            this.lblStartingNuyen.Size = new System.Drawing.Size(19, 13);
            this.lblStartingNuyen.TabIndex = 4;
            this.lblStartingNuyen.Text = "[0]";
            // 
            // lblStartingNuyenLabel
            // 
            this.lblStartingNuyenLabel.AutoSize = true;
            this.lblStartingNuyenLabel.Location = new System.Drawing.Point(261, 35);
            this.lblStartingNuyenLabel.Name = "lblStartingNuyenLabel";
            this.lblStartingNuyenLabel.Size = new System.Drawing.Size(80, 13);
            this.lblStartingNuyenLabel.TabIndex = 3;
            this.lblStartingNuyenLabel.Tag = "Label_SelectLifestyle_StartingNuyen";
            this.lblStartingNuyenLabel.Text = "Starting Nuyen:";
            // 
            // lblPercentage
            // 
            this.lblPercentage.AutoSize = true;
            this.lblPercentage.Location = new System.Drawing.Point(261, 104);
            this.lblPercentage.Name = "lblPercentage";
            this.lblPercentage.Size = new System.Drawing.Size(51, 13);
            this.lblPercentage.TabIndex = 9;
            this.lblPercentage.Tag = "Label_SelectLifestyle_PercentToPay";
            this.lblPercentage.Text = "% to Pay:";
            // 
            // nudPercentage
            // 
            this.nudPercentage.Location = new System.Drawing.Point(342, 102);
            this.nudPercentage.Maximum = new decimal(new int[] {
            900,
            0,
            0,
            0});
            this.nudPercentage.Name = "nudPercentage";
            this.nudPercentage.Size = new System.Drawing.Size(45, 20);
            this.nudPercentage.TabIndex = 10;
            this.nudPercentage.Value = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this.nudPercentage.ValueChanged += new System.EventHandler(this.nudPercentage_ValueChanged);
            // 
            // nudRoommates
            // 
            this.nudRoommates.Location = new System.Drawing.Point(342, 79);
            this.nudRoommates.Maximum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.nudRoommates.Name = "nudRoommates";
            this.nudRoommates.Size = new System.Drawing.Size(45, 20);
            this.nudRoommates.TabIndex = 8;
            this.nudRoommates.ValueChanged += new System.EventHandler(this.nudRoommates_ValueChanged);
            // 
            // lblRoommates
            // 
            this.lblRoommates.AutoSize = true;
            this.lblRoommates.Location = new System.Drawing.Point(261, 81);
            this.lblRoommates.Name = "lblRoommates";
            this.lblRoommates.Size = new System.Drawing.Size(66, 13);
            this.lblRoommates.TabIndex = 7;
            this.lblRoommates.Tag = "Label_SelectLifestyle_Roommates";
            this.lblRoommates.Text = "Roommates:";
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
            // frmSelectLifestyle
            // 
            this.AcceptButton = this.cmdOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = Majorsilence.Forms.AutoScaleMode.Font;
            this.CancelButton = this.cmdCancel;
            this.ClientSize = new System.Drawing.Size(436, 196);
            this.Controls.Add(this.nudRoommates);
            this.Controls.Add(this.lblRoommates);
            this.Controls.Add(this.nudPercentage);
            this.Controls.Add(this.lblPercentage);
            this.Controls.Add(this.lblStartingNuyen);
            this.Controls.Add(this.lblStartingNuyenLabel);
            this.Controls.Add(this.lblSource);
            this.Controls.Add(this.lblSourceLabel);
            this.Controls.Add(this.cmdOKAdd);
            this.Controls.Add(this.cmdCancel);
            this.Controls.Add(this.lblCost);
            this.Controls.Add(this.lblCostLabel);
            this.Controls.Add(this.lstLifestyles);
            this.Controls.Add(this.cmdOK);
            this.FormBorderStyle = Majorsilence.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "frmSelectLifestyle";
            this.ShowInTaskbar = false;
            this.StartPosition = Majorsilence.Forms.FormStartPosition.CenterParent;
            this.Tag = "Title_SelectLifestyle";
            this.Text = "Select a Lifestyle";
            this.Load += new System.EventHandler(this.frmSelectLifestyle_Load);
            ((System.ComponentModel.ISupportInitialize)(this.nudPercentage)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudRoommates)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private Majorsilence.Forms.Button cmdCancel;
		private Majorsilence.Forms.Label lblCost;
		private Majorsilence.Forms.Label lblCostLabel;
		private Majorsilence.Forms.ListBox lstLifestyles;
		private Majorsilence.Forms.Button cmdOK;
		private Majorsilence.Forms.Button cmdOKAdd;
		private Majorsilence.Forms.Label lblSource;
		private Majorsilence.Forms.Label lblSourceLabel;
		private Majorsilence.Forms.Label lblStartingNuyen;
		private Majorsilence.Forms.Label lblStartingNuyenLabel;
		private Majorsilence.Forms.Label lblPercentage;
		private Majorsilence.Forms.NumericUpDown nudPercentage;
		private Majorsilence.Forms.NumericUpDown nudRoommates;
		private Majorsilence.Forms.Label lblRoommates;
		private Majorsilence.Forms.ToolTip tipTooltip;
	}
}