namespace Chummer
{
	partial class frmSelectArmor
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
            this.lstArmor = new Majorsilence.Forms.ListBox();
            this.cmdOK = new Majorsilence.Forms.Button();
            this.cmdCancel = new Majorsilence.Forms.Button();
            this.lblBallisticLabel = new Majorsilence.Forms.Label();
            this.lblBallistic = new Majorsilence.Forms.Label();
            this.lblImpactLabel = new Majorsilence.Forms.Label();
            this.lblImpact = new Majorsilence.Forms.Label();
            this.lblCost = new Majorsilence.Forms.Label();
            this.lblCostLabel = new Majorsilence.Forms.Label();
            this.lblAvail = new Majorsilence.Forms.Label();
            this.lblAvailLabel = new Majorsilence.Forms.Label();
            this.lblCategory = new Majorsilence.Forms.Label();
            this.cboCategory = new Majorsilence.Forms.ComboBox();
            this.cmdOKAdd = new Majorsilence.Forms.Button();
            this.lblSourceLabel = new Majorsilence.Forms.Label();
            this.lblSource = new Majorsilence.Forms.Label();
            this.txtSearch = new Majorsilence.Forms.TextBox();
            this.lblSearchLabel = new Majorsilence.Forms.Label();
            this.chkFreeItem = new Majorsilence.Forms.CheckBox();
            this.lblCapacity = new Majorsilence.Forms.Label();
            this.lblCapacityLabel = new Majorsilence.Forms.Label();
            this.nudMarkup = new Majorsilence.Forms.NumericUpDown();
            this.lblMarkupLabel = new Majorsilence.Forms.Label();
            this.lblMarkupPercentLabel = new Majorsilence.Forms.Label();
            this.lblTest = new Majorsilence.Forms.Label();
            this.lblTestLabel = new Majorsilence.Forms.Label();
            this.tipTooltip = new Majorsilence.Forms.ToolTip(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.nudMarkup)).BeginInit();
            this.SuspendLayout();
            // 
            // lstArmor
            // 
            this.lstArmor.FormattingEnabled = true;
            this.lstArmor.Location = new System.Drawing.Point(12, 38);
            this.lstArmor.Name = "lstArmor";
            this.lstArmor.Size = new System.Drawing.Size(306, 329);
            this.lstArmor.TabIndex = 22;
            this.lstArmor.SelectedIndexChanged += new System.EventHandler(this.lstArmor_SelectedIndexChanged);
            this.lstArmor.DoubleClick += new System.EventHandler(this.lstArmor_DoubleClick);
            // 
            // cmdOK
            // 
            this.cmdOK.Location = new System.Drawing.Point(497, 343);
            this.cmdOK.Name = "cmdOK";
            this.cmdOK.Size = new System.Drawing.Size(75, 23);
            this.cmdOK.TabIndex = 23;
            this.cmdOK.Tag = "String_OK";
            this.cmdOK.Text = "OK";
            this.cmdOK.UseVisualStyleBackColor = true;
            this.cmdOK.Click += new System.EventHandler(this.cmdOK_Click);
            // 
            // cmdCancel
            // 
            this.cmdCancel.DialogResult = Majorsilence.Forms.DialogResult.Cancel;
            this.cmdCancel.Location = new System.Drawing.Point(416, 342);
            this.cmdCancel.Name = "cmdCancel";
            this.cmdCancel.Size = new System.Drawing.Size(75, 23);
            this.cmdCancel.TabIndex = 25;
            this.cmdCancel.Tag = "String_Cancel";
            this.cmdCancel.Text = "Cancel";
            this.cmdCancel.UseVisualStyleBackColor = true;
            this.cmdCancel.Click += new System.EventHandler(this.cmdCancel_Click);
            // 
            // lblBallisticLabel
            // 
            this.lblBallisticLabel.AutoSize = true;
            this.lblBallisticLabel.Location = new System.Drawing.Point(324, 37);
            this.lblBallisticLabel.Name = "lblBallisticLabel";
            this.lblBallisticLabel.Size = new System.Drawing.Size(45, 13);
            this.lblBallisticLabel.TabIndex = 2;
            this.lblBallisticLabel.Tag = "Label_BallisticArmorShort";
            this.lblBallisticLabel.Text = "Ballistic:";
            // 
            // lblBallistic
            // 
            this.lblBallistic.AutoSize = true;
            this.lblBallistic.Location = new System.Drawing.Point(381, 37);
            this.lblBallistic.Name = "lblBallistic";
            this.lblBallistic.Size = new System.Drawing.Size(20, 13);
            this.lblBallistic.TabIndex = 3;
            this.lblBallistic.Text = "[B]";
            // 
            // lblImpactLabel
            // 
            this.lblImpactLabel.AutoSize = true;
            this.lblImpactLabel.Location = new System.Drawing.Point(324, 60);
            this.lblImpactLabel.Name = "lblImpactLabel";
            this.lblImpactLabel.Size = new System.Drawing.Size(42, 13);
            this.lblImpactLabel.TabIndex = 4;
            this.lblImpactLabel.Tag = "Label_ImpactArmorShort";
            this.lblImpactLabel.Text = "Impact:";
            // 
            // lblImpact
            // 
            this.lblImpact.AutoSize = true;
            this.lblImpact.Location = new System.Drawing.Point(381, 60);
            this.lblImpact.Name = "lblImpact";
            this.lblImpact.Size = new System.Drawing.Size(16, 13);
            this.lblImpact.TabIndex = 5;
            this.lblImpact.Text = "[I]";
            // 
            // lblCost
            // 
            this.lblCost.AutoSize = true;
            this.lblCost.Location = new System.Drawing.Point(381, 129);
            this.lblCost.Name = "lblCost";
            this.lblCost.Size = new System.Drawing.Size(34, 13);
            this.lblCost.TabIndex = 13;
            this.lblCost.Text = "[Cost]";
            // 
            // lblCostLabel
            // 
            this.lblCostLabel.AutoSize = true;
            this.lblCostLabel.Location = new System.Drawing.Point(324, 129);
            this.lblCostLabel.Name = "lblCostLabel";
            this.lblCostLabel.Size = new System.Drawing.Size(31, 13);
            this.lblCostLabel.TabIndex = 12;
            this.lblCostLabel.Tag = "Label_Cost";
            this.lblCostLabel.Text = "Cost:";
            // 
            // lblAvail
            // 
            this.lblAvail.AutoSize = true;
            this.lblAvail.Location = new System.Drawing.Point(381, 106);
            this.lblAvail.Name = "lblAvail";
            this.lblAvail.Size = new System.Drawing.Size(36, 13);
            this.lblAvail.TabIndex = 9;
            this.lblAvail.Text = "[Avail]";
            // 
            // lblAvailLabel
            // 
            this.lblAvailLabel.AutoSize = true;
            this.lblAvailLabel.Location = new System.Drawing.Point(324, 106);
            this.lblAvailLabel.Name = "lblAvailLabel";
            this.lblAvailLabel.Size = new System.Drawing.Size(33, 13);
            this.lblAvailLabel.TabIndex = 8;
            this.lblAvailLabel.Tag = "Label_Avail";
            this.lblAvailLabel.Text = "Avail:";
            // 
            // lblCategory
            // 
            this.lblCategory.AutoSize = true;
            this.lblCategory.Location = new System.Drawing.Point(9, 9);
            this.lblCategory.Name = "lblCategory";
            this.lblCategory.Size = new System.Drawing.Size(52, 13);
            this.lblCategory.TabIndex = 20;
            this.lblCategory.Tag = "Label_Category";
            this.lblCategory.Text = "Category:";
            // 
            // cboCategory
            // 
            this.cboCategory.DropDownStyle = Majorsilence.Forms.ComboBoxStyle.DropDownList;
            this.cboCategory.FormattingEnabled = true;
            this.cboCategory.Location = new System.Drawing.Point(67, 6);
            this.cboCategory.Name = "cboCategory";
            this.cboCategory.Size = new System.Drawing.Size(251, 21);
            this.cboCategory.TabIndex = 21;
            this.cboCategory.SelectedIndexChanged += new System.EventHandler(this.cboCategory_SelectedIndexChanged);
            // 
            // cmdOKAdd
            // 
            this.cmdOKAdd.Location = new System.Drawing.Point(497, 314);
            this.cmdOKAdd.Name = "cmdOKAdd";
            this.cmdOKAdd.Size = new System.Drawing.Size(75, 23);
            this.cmdOKAdd.TabIndex = 24;
            this.cmdOKAdd.Tag = "String_AddMore";
            this.cmdOKAdd.Text = "&Add && More";
            this.cmdOKAdd.UseVisualStyleBackColor = true;
            this.cmdOKAdd.Click += new System.EventHandler(this.cmdOKAdd_Click);
            // 
            // lblSourceLabel
            // 
            this.lblSourceLabel.AutoSize = true;
            this.lblSourceLabel.Location = new System.Drawing.Point(324, 213);
            this.lblSourceLabel.Name = "lblSourceLabel";
            this.lblSourceLabel.Size = new System.Drawing.Size(44, 13);
            this.lblSourceLabel.TabIndex = 18;
            this.lblSourceLabel.Tag = "Label_Source";
            this.lblSourceLabel.Text = "Source:";
            // 
            // lblSource
            // 
            this.lblSource.AutoSize = true;
            this.lblSource.Location = new System.Drawing.Point(381, 213);
            this.lblSource.Name = "lblSource";
            this.lblSource.Size = new System.Drawing.Size(47, 13);
            this.lblSource.TabIndex = 19;
            this.lblSource.Text = "[Source]";
            this.lblSource.Click += new System.EventHandler(this.lblSource_Click);
            // 
            // txtSearch
            // 
            this.txtSearch.Location = new System.Drawing.Point(390, 6);
            this.txtSearch.Name = "txtSearch";
            this.txtSearch.Size = new System.Drawing.Size(182, 20);
            this.txtSearch.TabIndex = 1;
            this.txtSearch.TextChanged += new System.EventHandler(this.txtSearch_TextChanged);
            this.txtSearch.KeyDown += new Majorsilence.Forms.KeyEventHandler(this.txtSearch_KeyDown);
            this.txtSearch.KeyUp += new Majorsilence.Forms.KeyEventHandler(this.txtSearch_KeyUp);
            // 
            // lblSearchLabel
            // 
            this.lblSearchLabel.AutoSize = true;
            this.lblSearchLabel.Location = new System.Drawing.Point(340, 9);
            this.lblSearchLabel.Name = "lblSearchLabel";
            this.lblSearchLabel.Size = new System.Drawing.Size(44, 13);
            this.lblSearchLabel.TabIndex = 0;
            this.lblSearchLabel.Tag = "Label_Search";
            this.lblSearchLabel.Text = "&Search:";
            // 
            // chkFreeItem
            // 
            this.chkFreeItem.AutoSize = true;
            this.chkFreeItem.Location = new System.Drawing.Point(327, 155);
            this.chkFreeItem.Name = "chkFreeItem";
            this.chkFreeItem.Size = new System.Drawing.Size(50, 17);
            this.chkFreeItem.TabIndex = 14;
            this.chkFreeItem.Tag = "Checkbox_Free";
            this.chkFreeItem.Text = "Free!";
            this.chkFreeItem.UseVisualStyleBackColor = true;
            this.chkFreeItem.Visible = false;
            this.chkFreeItem.CheckedChanged += new System.EventHandler(this.chkFreeItem_CheckedChanged);
            // 
            // lblCapacity
            // 
            this.lblCapacity.AutoSize = true;
            this.lblCapacity.Location = new System.Drawing.Point(381, 83);
            this.lblCapacity.Name = "lblCapacity";
            this.lblCapacity.Size = new System.Drawing.Size(54, 13);
            this.lblCapacity.TabIndex = 7;
            this.lblCapacity.Text = "[Capacity]";
            // 
            // lblCapacityLabel
            // 
            this.lblCapacityLabel.AutoSize = true;
            this.lblCapacityLabel.Location = new System.Drawing.Point(324, 83);
            this.lblCapacityLabel.Name = "lblCapacityLabel";
            this.lblCapacityLabel.Size = new System.Drawing.Size(51, 13);
            this.lblCapacityLabel.TabIndex = 6;
            this.lblCapacityLabel.Tag = "Label_Capacity";
            this.lblCapacityLabel.Text = "Capacity:";
            // 
            // nudMarkup
            // 
            this.nudMarkup.Location = new System.Drawing.Point(384, 178);
            this.nudMarkup.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.nudMarkup.Minimum = new decimal(new int[] {
            99,
            0,
            0,
            -2147483648});
            this.nudMarkup.Name = "nudMarkup";
            this.nudMarkup.Size = new System.Drawing.Size(56, 20);
            this.nudMarkup.TabIndex = 16;
            this.nudMarkup.ValueChanged += new System.EventHandler(this.nudMarkup_ValueChanged);
            // 
            // lblMarkupLabel
            // 
            this.lblMarkupLabel.AutoSize = true;
            this.lblMarkupLabel.Location = new System.Drawing.Point(324, 180);
            this.lblMarkupLabel.Name = "lblMarkupLabel";
            this.lblMarkupLabel.Size = new System.Drawing.Size(46, 13);
            this.lblMarkupLabel.TabIndex = 15;
            this.lblMarkupLabel.Tag = "Label_SelectGear_Markup";
            this.lblMarkupLabel.Text = "Markup:";
            // 
            // lblMarkupPercentLabel
            // 
            this.lblMarkupPercentLabel.AutoSize = true;
            this.lblMarkupPercentLabel.Location = new System.Drawing.Point(439, 180);
            this.lblMarkupPercentLabel.Name = "lblMarkupPercentLabel";
            this.lblMarkupPercentLabel.Size = new System.Drawing.Size(15, 13);
            this.lblMarkupPercentLabel.TabIndex = 17;
            this.lblMarkupPercentLabel.Text = "%";
            // 
            // lblTest
            // 
            this.lblTest.AutoSize = true;
            this.lblTest.Location = new System.Drawing.Point(490, 106);
            this.lblTest.Name = "lblTest";
            this.lblTest.Size = new System.Drawing.Size(19, 13);
            this.lblTest.TabIndex = 11;
            this.lblTest.Text = "[0]";
            // 
            // lblTestLabel
            // 
            this.lblTestLabel.AutoSize = true;
            this.lblTestLabel.Location = new System.Drawing.Point(439, 106);
            this.lblTestLabel.Name = "lblTestLabel";
            this.lblTestLabel.Size = new System.Drawing.Size(31, 13);
            this.lblTestLabel.TabIndex = 10;
            this.lblTestLabel.Tag = "Label_Test";
            this.lblTestLabel.Text = "Test:";
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
            // frmSelectArmor
            // 
            this.AcceptButton = this.cmdOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = Majorsilence.Forms.AutoScaleMode.Font;
            this.CancelButton = this.cmdCancel;
            this.ClientSize = new System.Drawing.Size(584, 378);
            this.Controls.Add(this.lblTest);
            this.Controls.Add(this.lblTestLabel);
            this.Controls.Add(this.nudMarkup);
            this.Controls.Add(this.lblMarkupLabel);
            this.Controls.Add(this.lblMarkupPercentLabel);
            this.Controls.Add(this.lblCapacity);
            this.Controls.Add(this.lblCapacityLabel);
            this.Controls.Add(this.chkFreeItem);
            this.Controls.Add(this.txtSearch);
            this.Controls.Add(this.lblSearchLabel);
            this.Controls.Add(this.lblSource);
            this.Controls.Add(this.lblSourceLabel);
            this.Controls.Add(this.cmdOKAdd);
            this.Controls.Add(this.lblCategory);
            this.Controls.Add(this.cboCategory);
            this.Controls.Add(this.lblCost);
            this.Controls.Add(this.lblCostLabel);
            this.Controls.Add(this.lblAvail);
            this.Controls.Add(this.lblAvailLabel);
            this.Controls.Add(this.lblImpact);
            this.Controls.Add(this.lblImpactLabel);
            this.Controls.Add(this.lblBallistic);
            this.Controls.Add(this.lblBallisticLabel);
            this.Controls.Add(this.cmdCancel);
            this.Controls.Add(this.cmdOK);
            this.Controls.Add(this.lstArmor);
            this.FormBorderStyle = Majorsilence.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "frmSelectArmor";
            this.ShowInTaskbar = false;
            this.StartPosition = Majorsilence.Forms.FormStartPosition.CenterParent;
            this.Tag = "Title_SelectArmor";
            this.Text = "Select Armor";
            this.Load += new System.EventHandler(this.frmSelectArmor_Load);
            ((System.ComponentModel.ISupportInitialize)(this.nudMarkup)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private Majorsilence.Forms.ListBox lstArmor;
		private Majorsilence.Forms.Button cmdOK;
		private Majorsilence.Forms.Button cmdCancel;
		private Majorsilence.Forms.Label lblBallisticLabel;
		private Majorsilence.Forms.Label lblBallistic;
		private Majorsilence.Forms.Label lblImpactLabel;
		private Majorsilence.Forms.Label lblImpact;
		private Majorsilence.Forms.Label lblCost;
		private Majorsilence.Forms.Label lblCostLabel;
		private Majorsilence.Forms.Label lblAvail;
		private Majorsilence.Forms.Label lblAvailLabel;
		private Majorsilence.Forms.Label lblCategory;
		private Majorsilence.Forms.ComboBox cboCategory;
		private Majorsilence.Forms.Button cmdOKAdd;
		private Majorsilence.Forms.Label lblSourceLabel;
		private Majorsilence.Forms.Label lblSource;
		private Majorsilence.Forms.TextBox txtSearch;
		private Majorsilence.Forms.Label lblSearchLabel;
		private Majorsilence.Forms.CheckBox chkFreeItem;
		private Majorsilence.Forms.Label lblCapacity;
		private Majorsilence.Forms.Label lblCapacityLabel;
		private Majorsilence.Forms.NumericUpDown nudMarkup;
		private Majorsilence.Forms.Label lblMarkupLabel;
		private Majorsilence.Forms.Label lblMarkupPercentLabel;
		private Majorsilence.Forms.Label lblTest;
		private Majorsilence.Forms.Label lblTestLabel;
		private Majorsilence.Forms.ToolTip tipTooltip;
	}
}