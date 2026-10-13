namespace Chummer
{
	partial class frmSelectVehicle
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
            this.txtSearch = new Majorsilence.Forms.TextBox();
            this.lblSearchLabel = new Majorsilence.Forms.Label();
            this.lblVehiclePilot = new Majorsilence.Forms.Label();
            this.lblVehiclePilotLabel = new Majorsilence.Forms.Label();
            this.lblVehicleArmor = new Majorsilence.Forms.Label();
            this.lblVehicleArmorLabel = new Majorsilence.Forms.Label();
            this.lblVehicleBody = new Majorsilence.Forms.Label();
            this.lblVehicleBodyLabel = new Majorsilence.Forms.Label();
            this.lblVehicleSpeed = new Majorsilence.Forms.Label();
            this.lblVehicleSpeedLabel = new Majorsilence.Forms.Label();
            this.lblVehicleCost = new Majorsilence.Forms.Label();
            this.lblVehicleCostLabel = new Majorsilence.Forms.Label();
            this.lblVehicleAvail = new Majorsilence.Forms.Label();
            this.lblVehicleAvailLabel = new Majorsilence.Forms.Label();
            this.lblVehicleAccel = new Majorsilence.Forms.Label();
            this.lblVehicleAccelLabel = new Majorsilence.Forms.Label();
            this.lblVehicleHandling = new Majorsilence.Forms.Label();
            this.lblVehicleHandlingLabel = new Majorsilence.Forms.Label();
            this.cmdCancel = new Majorsilence.Forms.Button();
            this.cmdOK = new Majorsilence.Forms.Button();
            this.lstVehicle = new Majorsilence.Forms.ListBox();
            this.lblCategory = new Majorsilence.Forms.Label();
            this.cboCategory = new Majorsilence.Forms.ComboBox();
            this.lblVehicleSensor = new Majorsilence.Forms.Label();
            this.lblVehicleSensorLabel = new Majorsilence.Forms.Label();
            this.cmdOKAdd = new Majorsilence.Forms.Button();
            this.lblSource = new Majorsilence.Forms.Label();
            this.lblSourceLabel = new Majorsilence.Forms.Label();
            this.chkFreeItem = new Majorsilence.Forms.CheckBox();
            this.chkUsedVehicle = new Majorsilence.Forms.CheckBox();
            this.lblUsedVehicleDiscountLabel = new Majorsilence.Forms.Label();
            this.nudUsedVehicleDiscount = new Majorsilence.Forms.NumericUpDown();
            this.lblUsedVehicleDiscountPercentLabel = new Majorsilence.Forms.Label();
            this.nudMarkup = new Majorsilence.Forms.NumericUpDown();
            this.lblMarkupLabel = new Majorsilence.Forms.Label();
            this.lblMarkupPercentLabel = new Majorsilence.Forms.Label();
            this.lblTest = new Majorsilence.Forms.Label();
            this.lblTestLabel = new Majorsilence.Forms.Label();
            this.tipTooltip = new Majorsilence.Forms.ToolTip(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.nudUsedVehicleDiscount)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudMarkup)).BeginInit();
            this.SuspendLayout();
            // 
            // txtSearch
            // 
            this.txtSearch.Location = new System.Drawing.Point(553, 6);
            this.txtSearch.Name = "txtSearch";
            this.txtSearch.Size = new System.Drawing.Size(174, 20);
            this.txtSearch.TabIndex = 1;
            this.txtSearch.TextChanged += new System.EventHandler(this.txtSearch_TextChanged);
            this.txtSearch.KeyDown += new Majorsilence.Forms.KeyEventHandler(this.txtSearch_KeyDown);
            this.txtSearch.KeyUp += new Majorsilence.Forms.KeyEventHandler(this.txtSearch_KeyUp);
            // 
            // lblSearchLabel
            // 
            this.lblSearchLabel.AutoSize = true;
            this.lblSearchLabel.Location = new System.Drawing.Point(503, 9);
            this.lblSearchLabel.Name = "lblSearchLabel";
            this.lblSearchLabel.Size = new System.Drawing.Size(44, 13);
            this.lblSearchLabel.TabIndex = 0;
            this.lblSearchLabel.Tag = "Label_Search";
            this.lblSearchLabel.Text = "&Search:";
            // 
            // lblVehiclePilot
            // 
            this.lblVehiclePilot.AutoSize = true;
            this.lblVehiclePilot.Location = new System.Drawing.Point(629, 68);
            this.lblVehiclePilot.Name = "lblVehiclePilot";
            this.lblVehiclePilot.Size = new System.Drawing.Size(33, 13);
            this.lblVehiclePilot.TabIndex = 9;
            this.lblVehiclePilot.Text = "[Pilot]";
            // 
            // lblVehiclePilotLabel
            // 
            this.lblVehiclePilotLabel.AutoSize = true;
            this.lblVehiclePilotLabel.Location = new System.Drawing.Point(572, 68);
            this.lblVehiclePilotLabel.Name = "lblVehiclePilotLabel";
            this.lblVehiclePilotLabel.Size = new System.Drawing.Size(30, 13);
            this.lblVehiclePilotLabel.TabIndex = 8;
            this.lblVehiclePilotLabel.Tag = "Label_Pilot";
            this.lblVehiclePilotLabel.Text = "Pilot:";
            // 
            // lblVehicleArmor
            // 
            this.lblVehicleArmor.AutoSize = true;
            this.lblVehicleArmor.Location = new System.Drawing.Point(628, 91);
            this.lblVehicleArmor.Name = "lblVehicleArmor";
            this.lblVehicleArmor.Size = new System.Drawing.Size(40, 13);
            this.lblVehicleArmor.TabIndex = 13;
            this.lblVehicleArmor.Text = "[Armor]";
            // 
            // lblVehicleArmorLabel
            // 
            this.lblVehicleArmorLabel.AutoSize = true;
            this.lblVehicleArmorLabel.Location = new System.Drawing.Point(572, 91);
            this.lblVehicleArmorLabel.Name = "lblVehicleArmorLabel";
            this.lblVehicleArmorLabel.Size = new System.Drawing.Size(37, 13);
            this.lblVehicleArmorLabel.TabIndex = 12;
            this.lblVehicleArmorLabel.Tag = "Label_Armor";
            this.lblVehicleArmorLabel.Text = "Armor:";
            // 
            // lblVehicleBody
            // 
            this.lblVehicleBody.AutoSize = true;
            this.lblVehicleBody.Location = new System.Drawing.Point(478, 91);
            this.lblVehicleBody.Name = "lblVehicleBody";
            this.lblVehicleBody.Size = new System.Drawing.Size(37, 13);
            this.lblVehicleBody.TabIndex = 11;
            this.lblVehicleBody.Text = "[Body]";
            // 
            // lblVehicleBodyLabel
            // 
            this.lblVehicleBodyLabel.AutoSize = true;
            this.lblVehicleBodyLabel.Location = new System.Drawing.Point(421, 91);
            this.lblVehicleBodyLabel.Name = "lblVehicleBodyLabel";
            this.lblVehicleBodyLabel.Size = new System.Drawing.Size(34, 13);
            this.lblVehicleBodyLabel.TabIndex = 10;
            this.lblVehicleBodyLabel.Tag = "Label_Body";
            this.lblVehicleBodyLabel.Text = "Body:";
            // 
            // lblVehicleSpeed
            // 
            this.lblVehicleSpeed.AutoSize = true;
            this.lblVehicleSpeed.Location = new System.Drawing.Point(478, 68);
            this.lblVehicleSpeed.Name = "lblVehicleSpeed";
            this.lblVehicleSpeed.Size = new System.Drawing.Size(44, 13);
            this.lblVehicleSpeed.TabIndex = 7;
            this.lblVehicleSpeed.Text = "[Speed]";
            // 
            // lblVehicleSpeedLabel
            // 
            this.lblVehicleSpeedLabel.AutoSize = true;
            this.lblVehicleSpeedLabel.Location = new System.Drawing.Point(421, 68);
            this.lblVehicleSpeedLabel.Name = "lblVehicleSpeedLabel";
            this.lblVehicleSpeedLabel.Size = new System.Drawing.Size(41, 13);
            this.lblVehicleSpeedLabel.TabIndex = 6;
            this.lblVehicleSpeedLabel.Tag = "Label_Speed";
            this.lblVehicleSpeedLabel.Text = "Speed:";
            // 
            // lblVehicleCost
            // 
            this.lblVehicleCost.AutoSize = true;
            this.lblVehicleCost.Location = new System.Drawing.Point(478, 162);
            this.lblVehicleCost.Name = "lblVehicleCost";
            this.lblVehicleCost.Size = new System.Drawing.Size(34, 13);
            this.lblVehicleCost.TabIndex = 21;
            this.lblVehicleCost.Text = "[Cost]";
            // 
            // lblVehicleCostLabel
            // 
            this.lblVehicleCostLabel.AutoSize = true;
            this.lblVehicleCostLabel.Location = new System.Drawing.Point(421, 162);
            this.lblVehicleCostLabel.Name = "lblVehicleCostLabel";
            this.lblVehicleCostLabel.Size = new System.Drawing.Size(31, 13);
            this.lblVehicleCostLabel.TabIndex = 20;
            this.lblVehicleCostLabel.Tag = "Label_Cost";
            this.lblVehicleCostLabel.Text = "Cost:";
            // 
            // lblVehicleAvail
            // 
            this.lblVehicleAvail.AutoSize = true;
            this.lblVehicleAvail.Location = new System.Drawing.Point(478, 139);
            this.lblVehicleAvail.Name = "lblVehicleAvail";
            this.lblVehicleAvail.Size = new System.Drawing.Size(36, 13);
            this.lblVehicleAvail.TabIndex = 17;
            this.lblVehicleAvail.Text = "[Avail]";
            // 
            // lblVehicleAvailLabel
            // 
            this.lblVehicleAvailLabel.AutoSize = true;
            this.lblVehicleAvailLabel.Location = new System.Drawing.Point(421, 139);
            this.lblVehicleAvailLabel.Name = "lblVehicleAvailLabel";
            this.lblVehicleAvailLabel.Size = new System.Drawing.Size(33, 13);
            this.lblVehicleAvailLabel.TabIndex = 16;
            this.lblVehicleAvailLabel.Tag = "Label_Avail";
            this.lblVehicleAvailLabel.Text = "Avail:";
            // 
            // lblVehicleAccel
            // 
            this.lblVehicleAccel.AutoSize = true;
            this.lblVehicleAccel.Location = new System.Drawing.Point(629, 45);
            this.lblVehicleAccel.Name = "lblVehicleAccel";
            this.lblVehicleAccel.Size = new System.Drawing.Size(40, 13);
            this.lblVehicleAccel.TabIndex = 5;
            this.lblVehicleAccel.Text = "[Accel]";
            // 
            // lblVehicleAccelLabel
            // 
            this.lblVehicleAccelLabel.AutoSize = true;
            this.lblVehicleAccelLabel.Location = new System.Drawing.Point(572, 45);
            this.lblVehicleAccelLabel.Name = "lblVehicleAccelLabel";
            this.lblVehicleAccelLabel.Size = new System.Drawing.Size(37, 13);
            this.lblVehicleAccelLabel.TabIndex = 4;
            this.lblVehicleAccelLabel.Tag = "Label_Accel";
            this.lblVehicleAccelLabel.Text = "Accel:";
            // 
            // lblVehicleHandling
            // 
            this.lblVehicleHandling.AutoSize = true;
            this.lblVehicleHandling.Location = new System.Drawing.Point(478, 45);
            this.lblVehicleHandling.Name = "lblVehicleHandling";
            this.lblVehicleHandling.Size = new System.Drawing.Size(55, 13);
            this.lblVehicleHandling.TabIndex = 3;
            this.lblVehicleHandling.Text = "[Handling]";
            // 
            // lblVehicleHandlingLabel
            // 
            this.lblVehicleHandlingLabel.AutoSize = true;
            this.lblVehicleHandlingLabel.Location = new System.Drawing.Point(421, 45);
            this.lblVehicleHandlingLabel.Name = "lblVehicleHandlingLabel";
            this.lblVehicleHandlingLabel.Size = new System.Drawing.Size(52, 13);
            this.lblVehicleHandlingLabel.TabIndex = 2;
            this.lblVehicleHandlingLabel.Tag = "Label_Handling";
            this.lblVehicleHandlingLabel.Text = "Handling:";
            // 
            // cmdCancel
            // 
            this.cmdCancel.DialogResult = Majorsilence.Forms.DialogResult.Cancel;
            this.cmdCancel.Location = new System.Drawing.Point(567, 378);
            this.cmdCancel.Name = "cmdCancel";
            this.cmdCancel.Size = new System.Drawing.Size(75, 23);
            this.cmdCancel.TabIndex = 37;
            this.cmdCancel.Tag = "String_Cancel";
            this.cmdCancel.Text = "Cancel";
            this.cmdCancel.UseVisualStyleBackColor = true;
            this.cmdCancel.Click += new System.EventHandler(this.cmdCancel_Click);
            // 
            // cmdOK
            // 
            this.cmdOK.Location = new System.Drawing.Point(648, 378);
            this.cmdOK.Name = "cmdOK";
            this.cmdOK.Size = new System.Drawing.Size(75, 23);
            this.cmdOK.TabIndex = 35;
            this.cmdOK.Tag = "String_OK";
            this.cmdOK.Text = "OK";
            this.cmdOK.UseVisualStyleBackColor = true;
            this.cmdOK.Click += new System.EventHandler(this.cmdOK_Click);
            // 
            // lstVehicle
            // 
            this.lstVehicle.FormattingEnabled = true;
            this.lstVehicle.Location = new System.Drawing.Point(12, 33);
            this.lstVehicle.Name = "lstVehicle";
            this.lstVehicle.Size = new System.Drawing.Size(403, 368);
            this.lstVehicle.TabIndex = 32;
            this.lstVehicle.SelectedIndexChanged += new System.EventHandler(this.lstVehicle_SelectedIndexChanged);
            this.lstVehicle.DoubleClick += new System.EventHandler(this.lstVehicle_DoubleClick);
            // 
            // lblCategory
            // 
            this.lblCategory.AutoSize = true;
            this.lblCategory.Location = new System.Drawing.Point(12, 9);
            this.lblCategory.Name = "lblCategory";
            this.lblCategory.Size = new System.Drawing.Size(52, 13);
            this.lblCategory.TabIndex = 33;
            this.lblCategory.Tag = "Label_Category";
            this.lblCategory.Text = "Category:";
            // 
            // cboCategory
            // 
            this.cboCategory.DropDownStyle = Majorsilence.Forms.ComboBoxStyle.DropDownList;
            this.cboCategory.FormattingEnabled = true;
            this.cboCategory.Location = new System.Drawing.Point(70, 6);
            this.cboCategory.Name = "cboCategory";
            this.cboCategory.Size = new System.Drawing.Size(235, 21);
            this.cboCategory.TabIndex = 34;
            this.cboCategory.SelectedIndexChanged += new System.EventHandler(this.cboCategory_SelectedIndexChanged);
            // 
            // lblVehicleSensor
            // 
            this.lblVehicleSensor.AutoSize = true;
            this.lblVehicleSensor.Location = new System.Drawing.Point(478, 114);
            this.lblVehicleSensor.Name = "lblVehicleSensor";
            this.lblVehicleSensor.Size = new System.Drawing.Size(46, 13);
            this.lblVehicleSensor.TabIndex = 15;
            this.lblVehicleSensor.Text = "[Sensor]";
            // 
            // lblVehicleSensorLabel
            // 
            this.lblVehicleSensorLabel.AutoSize = true;
            this.lblVehicleSensorLabel.Location = new System.Drawing.Point(421, 114);
            this.lblVehicleSensorLabel.Name = "lblVehicleSensorLabel";
            this.lblVehicleSensorLabel.Size = new System.Drawing.Size(43, 13);
            this.lblVehicleSensorLabel.TabIndex = 14;
            this.lblVehicleSensorLabel.Tag = "Label_Sensor";
            this.lblVehicleSensorLabel.Text = "Sensor:";
            // 
            // cmdOKAdd
            // 
            this.cmdOKAdd.Location = new System.Drawing.Point(648, 349);
            this.cmdOKAdd.Name = "cmdOKAdd";
            this.cmdOKAdd.Size = new System.Drawing.Size(75, 23);
            this.cmdOKAdd.TabIndex = 36;
            this.cmdOKAdd.Tag = "String_AddMore";
            this.cmdOKAdd.Text = "&Add && More";
            this.cmdOKAdd.UseVisualStyleBackColor = true;
            this.cmdOKAdd.Click += new System.EventHandler(this.cmdOKAdd_Click);
            // 
            // lblSource
            // 
            this.lblSource.AutoSize = true;
            this.lblSource.Location = new System.Drawing.Point(472, 297);
            this.lblSource.Name = "lblSource";
            this.lblSource.Size = new System.Drawing.Size(47, 13);
            this.lblSource.TabIndex = 31;
            this.lblSource.Text = "[Source]";
            this.lblSource.Click += new System.EventHandler(this.lblSource_Click);
            // 
            // lblSourceLabel
            // 
            this.lblSourceLabel.AutoSize = true;
            this.lblSourceLabel.Location = new System.Drawing.Point(421, 297);
            this.lblSourceLabel.Name = "lblSourceLabel";
            this.lblSourceLabel.Size = new System.Drawing.Size(44, 13);
            this.lblSourceLabel.TabIndex = 30;
            this.lblSourceLabel.Tag = "Label_Source";
            this.lblSourceLabel.Text = "Source:";
            // 
            // chkFreeItem
            // 
            this.chkFreeItem.AutoSize = true;
            this.chkFreeItem.Location = new System.Drawing.Point(421, 234);
            this.chkFreeItem.Name = "chkFreeItem";
            this.chkFreeItem.Size = new System.Drawing.Size(50, 17);
            this.chkFreeItem.TabIndex = 26;
            this.chkFreeItem.Tag = "Checkbox_Free";
            this.chkFreeItem.Text = "Free!";
            this.chkFreeItem.UseVisualStyleBackColor = true;
            this.chkFreeItem.Visible = false;
            this.chkFreeItem.CheckedChanged += new System.EventHandler(this.chkFreeItem_CheckedChanged);
            // 
            // chkUsedVehicle
            // 
            this.chkUsedVehicle.AutoSize = true;
            this.chkUsedVehicle.Location = new System.Drawing.Point(421, 207);
            this.chkUsedVehicle.Name = "chkUsedVehicle";
            this.chkUsedVehicle.Size = new System.Drawing.Size(89, 17);
            this.chkUsedVehicle.TabIndex = 22;
            this.chkUsedVehicle.Tag = "Checkbox_SelectVehicle_UsedVehicle";
            this.chkUsedVehicle.Text = "Used Vehicle";
            this.chkUsedVehicle.UseVisualStyleBackColor = true;
            this.chkUsedVehicle.CheckedChanged += new System.EventHandler(this.chkUsedVehicle_CheckedChanged);
            // 
            // lblUsedVehicleDiscountLabel
            // 
            this.lblUsedVehicleDiscountLabel.AutoSize = true;
            this.lblUsedVehicleDiscountLabel.Location = new System.Drawing.Point(519, 208);
            this.lblUsedVehicleDiscountLabel.Name = "lblUsedVehicleDiscountLabel";
            this.lblUsedVehicleDiscountLabel.Size = new System.Drawing.Size(52, 13);
            this.lblUsedVehicleDiscountLabel.TabIndex = 23;
            this.lblUsedVehicleDiscountLabel.Tag = "Label_SelectVehicle_Discount";
            this.lblUsedVehicleDiscountLabel.Text = "Discount:";
            // 
            // nudUsedVehicleDiscount
            // 
            this.nudUsedVehicleDiscount.Location = new System.Drawing.Point(577, 206);
            this.nudUsedVehicleDiscount.Maximum = new decimal(new int[] {
            50,
            0,
            0,
            0});
            this.nudUsedVehicleDiscount.Minimum = new decimal(new int[] {
            20,
            0,
            0,
            0});
            this.nudUsedVehicleDiscount.Name = "nudUsedVehicleDiscount";
            this.nudUsedVehicleDiscount.Size = new System.Drawing.Size(41, 20);
            this.nudUsedVehicleDiscount.TabIndex = 24;
            this.nudUsedVehicleDiscount.Value = new decimal(new int[] {
            20,
            0,
            0,
            0});
            this.nudUsedVehicleDiscount.ValueChanged += new System.EventHandler(this.nudUsedVehicleDiscount_ValueChanged);
            // 
            // lblUsedVehicleDiscountPercentLabel
            // 
            this.lblUsedVehicleDiscountPercentLabel.AutoSize = true;
            this.lblUsedVehicleDiscountPercentLabel.Location = new System.Drawing.Point(617, 208);
            this.lblUsedVehicleDiscountPercentLabel.Name = "lblUsedVehicleDiscountPercentLabel";
            this.lblUsedVehicleDiscountPercentLabel.Size = new System.Drawing.Size(15, 13);
            this.lblUsedVehicleDiscountPercentLabel.TabIndex = 25;
            this.lblUsedVehicleDiscountPercentLabel.Text = "%";
            // 
            // nudMarkup
            // 
            this.nudMarkup.Location = new System.Drawing.Point(481, 257);
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
            this.nudMarkup.TabIndex = 28;
            this.nudMarkup.ValueChanged += new System.EventHandler(this.nudMarkup_ValueChanged);
            // 
            // lblMarkupLabel
            // 
            this.lblMarkupLabel.AutoSize = true;
            this.lblMarkupLabel.Location = new System.Drawing.Point(421, 259);
            this.lblMarkupLabel.Name = "lblMarkupLabel";
            this.lblMarkupLabel.Size = new System.Drawing.Size(46, 13);
            this.lblMarkupLabel.TabIndex = 27;
            this.lblMarkupLabel.Tag = "Label_SelectGear_Markup";
            this.lblMarkupLabel.Text = "Markup:";
            // 
            // lblMarkupPercentLabel
            // 
            this.lblMarkupPercentLabel.AutoSize = true;
            this.lblMarkupPercentLabel.Location = new System.Drawing.Point(536, 259);
            this.lblMarkupPercentLabel.Name = "lblMarkupPercentLabel";
            this.lblMarkupPercentLabel.Size = new System.Drawing.Size(15, 13);
            this.lblMarkupPercentLabel.TabIndex = 29;
            this.lblMarkupPercentLabel.Text = "%";
            // 
            // lblTest
            // 
            this.lblTest.AutoSize = true;
            this.lblTest.Location = new System.Drawing.Point(579, 139);
            this.lblTest.Name = "lblTest";
            this.lblTest.Size = new System.Drawing.Size(19, 13);
            this.lblTest.TabIndex = 19;
            this.lblTest.Text = "[0]";
            // 
            // lblTestLabel
            // 
            this.lblTestLabel.AutoSize = true;
            this.lblTestLabel.Location = new System.Drawing.Point(534, 139);
            this.lblTestLabel.Name = "lblTestLabel";
            this.lblTestLabel.Size = new System.Drawing.Size(31, 13);
            this.lblTestLabel.TabIndex = 18;
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
            // frmSelectVehicle
            // 
            this.AcceptButton = this.cmdOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = Majorsilence.Forms.AutoScaleMode.Font;
            this.CancelButton = this.cmdCancel;
            this.ClientSize = new System.Drawing.Size(735, 412);
            this.Controls.Add(this.lblTest);
            this.Controls.Add(this.lblTestLabel);
            this.Controls.Add(this.nudMarkup);
            this.Controls.Add(this.lblMarkupLabel);
            this.Controls.Add(this.lblMarkupPercentLabel);
            this.Controls.Add(this.nudUsedVehicleDiscount);
            this.Controls.Add(this.lblUsedVehicleDiscountLabel);
            this.Controls.Add(this.chkUsedVehicle);
            this.Controls.Add(this.chkFreeItem);
            this.Controls.Add(this.lblSource);
            this.Controls.Add(this.lblSourceLabel);
            this.Controls.Add(this.cmdOKAdd);
            this.Controls.Add(this.lblVehicleSensor);
            this.Controls.Add(this.lblVehicleSensorLabel);
            this.Controls.Add(this.txtSearch);
            this.Controls.Add(this.lblSearchLabel);
            this.Controls.Add(this.lblVehiclePilot);
            this.Controls.Add(this.lblVehiclePilotLabel);
            this.Controls.Add(this.lblVehicleArmor);
            this.Controls.Add(this.lblVehicleArmorLabel);
            this.Controls.Add(this.lblVehicleBody);
            this.Controls.Add(this.lblVehicleBodyLabel);
            this.Controls.Add(this.lblVehicleSpeed);
            this.Controls.Add(this.lblVehicleSpeedLabel);
            this.Controls.Add(this.lblVehicleCost);
            this.Controls.Add(this.lblVehicleCostLabel);
            this.Controls.Add(this.lblVehicleAvail);
            this.Controls.Add(this.lblVehicleAvailLabel);
            this.Controls.Add(this.lblVehicleAccel);
            this.Controls.Add(this.lblVehicleAccelLabel);
            this.Controls.Add(this.lblVehicleHandling);
            this.Controls.Add(this.lblVehicleHandlingLabel);
            this.Controls.Add(this.cmdCancel);
            this.Controls.Add(this.cmdOK);
            this.Controls.Add(this.lstVehicle);
            this.Controls.Add(this.lblCategory);
            this.Controls.Add(this.cboCategory);
            this.Controls.Add(this.lblUsedVehicleDiscountPercentLabel);
            this.FormBorderStyle = Majorsilence.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "frmSelectVehicle";
            this.ShowInTaskbar = false;
            this.StartPosition = Majorsilence.Forms.FormStartPosition.CenterScreen;
            this.Tag = "Title_SelectVehicle";
            this.Text = "Select a Vehicle";
            this.Load += new System.EventHandler(this.frmSelectVehicle_Load);
            ((System.ComponentModel.ISupportInitialize)(this.nudUsedVehicleDiscount)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudMarkup)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private Majorsilence.Forms.TextBox txtSearch;
		private Majorsilence.Forms.Label lblSearchLabel;
		private Majorsilence.Forms.Label lblVehiclePilot;
		private Majorsilence.Forms.Label lblVehiclePilotLabel;
		private Majorsilence.Forms.Label lblVehicleArmor;
		private Majorsilence.Forms.Label lblVehicleArmorLabel;
		private Majorsilence.Forms.Label lblVehicleBody;
		private Majorsilence.Forms.Label lblVehicleBodyLabel;
		private Majorsilence.Forms.Label lblVehicleSpeed;
		private Majorsilence.Forms.Label lblVehicleSpeedLabel;
		private Majorsilence.Forms.Label lblVehicleCost;
		private Majorsilence.Forms.Label lblVehicleCostLabel;
		private Majorsilence.Forms.Label lblVehicleAvail;
		private Majorsilence.Forms.Label lblVehicleAvailLabel;
		private Majorsilence.Forms.Label lblVehicleAccel;
		private Majorsilence.Forms.Label lblVehicleAccelLabel;
		private Majorsilence.Forms.Label lblVehicleHandling;
		private Majorsilence.Forms.Label lblVehicleHandlingLabel;
		private Majorsilence.Forms.Button cmdCancel;
		private Majorsilence.Forms.Button cmdOK;
		private Majorsilence.Forms.ListBox lstVehicle;
		private Majorsilence.Forms.Label lblCategory;
		private Majorsilence.Forms.ComboBox cboCategory;
		private Majorsilence.Forms.Label lblVehicleSensor;
		private Majorsilence.Forms.Label lblVehicleSensorLabel;
		private Majorsilence.Forms.Button cmdOKAdd;
		private Majorsilence.Forms.Label lblSource;
		private Majorsilence.Forms.Label lblSourceLabel;
		private Majorsilence.Forms.CheckBox chkFreeItem;
		private Majorsilence.Forms.CheckBox chkUsedVehicle;
		private Majorsilence.Forms.Label lblUsedVehicleDiscountLabel;
		private Majorsilence.Forms.NumericUpDown nudUsedVehicleDiscount;
		private Majorsilence.Forms.Label lblUsedVehicleDiscountPercentLabel;
		private Majorsilence.Forms.NumericUpDown nudMarkup;
		private Majorsilence.Forms.Label lblMarkupLabel;
		private Majorsilence.Forms.Label lblMarkupPercentLabel;
		private Majorsilence.Forms.Label lblTest;
		private Majorsilence.Forms.Label lblTestLabel;
		private Majorsilence.Forms.ToolTip tipTooltip;
	}
}