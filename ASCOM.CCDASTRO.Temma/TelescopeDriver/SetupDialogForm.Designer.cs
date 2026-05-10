namespace ASCOM.CCDASTROTemma.Telescope
{
    partial class SetupDialogForm
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.GroupBox grpMountSetup;
        private System.Windows.Forms.GroupBox grpParkSettings;
        private System.Windows.Forms.GroupBox grpSiteInfo;
        private System.Windows.Forms.GroupBox grpOrientation;
        private System.Windows.Forms.GroupBox grpTelescopeSetup;

        private System.Windows.Forms.ComboBox cboComPort;
        private System.Windows.Forms.ComboBox cboTrackingRate;
        private System.Windows.Forms.ComboBox cboMountVoltage;
        private System.Windows.Forms.ComboBox cboMountModel;
        private System.Windows.Forms.ComboBox cboLatitudeSign;
        private System.Windows.Forms.ComboBox cboLongitudeSign;

        private System.Windows.Forms.NumericUpDown nudLatitudeDeg;
        private System.Windows.Forms.NumericUpDown nudLatitudeMin;
        private System.Windows.Forms.NumericUpDown nudLatitudeSec;
        private System.Windows.Forms.NumericUpDown nudLongitudeDeg;
        private System.Windows.Forms.NumericUpDown nudLongitudeMin;
        private System.Windows.Forms.NumericUpDown nudLongitudeSec;
        private System.Windows.Forms.NumericUpDown nudSiteElevation;
        private System.Windows.Forms.NumericUpDown nudParkAltitude;
        private System.Windows.Forms.NumericUpDown nudParkAzimuth;
        private System.Windows.Forms.NumericUpDown nudGuideRateRA;
        private System.Windows.Forms.NumericUpDown nudGuideRateDec;
        private System.Windows.Forms.NumericUpDown nudAperture;
        private System.Windows.Forms.NumericUpDown nudCentralObstruction;
        private System.Windows.Forms.NumericUpDown nudFocalLength;

        private System.Windows.Forms.CheckBox chkTrace;
        private System.Windows.Forms.CheckBox chkUnparkOnReconnect;
        private System.Windows.Forms.CheckBox chkKeepLastSync;
        private System.Windows.Forms.CheckBox chkAskAtStart;
        private System.Windows.Forms.CheckBox chkSendRate;
        private System.Windows.Forms.CheckBox chkHighPrecisionGoto;
        private System.Windows.Forms.CheckBox chkTrackingOffOnConnect;
        private System.Windows.Forms.CheckBox chkWarnBeforeMeridianFlip;

        private System.Windows.Forms.RadioButton optParkCurrentPosition;
        private System.Windows.Forms.RadioButton optParkSlewToPosition;
        private System.Windows.Forms.RadioButton optOtaEast;
        private System.Windows.Forms.RadioButton optOtaWest;
        private System.Windows.Forms.RadioButton optCounterweightDown;
        private System.Windows.Forms.RadioButton optCounterweightWest;

        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.grpMountSetup = new System.Windows.Forms.GroupBox();
            this.cboComPort = new System.Windows.Forms.ComboBox();
            this.cboTrackingRate = new System.Windows.Forms.ComboBox();
            this.cboMountVoltage = new System.Windows.Forms.ComboBox();
            this.cboMountModel = new System.Windows.Forms.ComboBox();
            this.chkTrace = new System.Windows.Forms.CheckBox();
            this.grpParkSettings = new System.Windows.Forms.GroupBox();
            this.nudParkAltitude = new System.Windows.Forms.NumericUpDown();
            this.nudParkAzimuth = new System.Windows.Forms.NumericUpDown();
            this.optParkCurrentPosition = new System.Windows.Forms.RadioButton();
            this.optParkSlewToPosition = new System.Windows.Forms.RadioButton();
            this.chkUnparkOnReconnect = new System.Windows.Forms.CheckBox();
            this.nudGuideRateRA = new System.Windows.Forms.NumericUpDown();
            this.nudGuideRateDec = new System.Windows.Forms.NumericUpDown();
            this.chkSendRate = new System.Windows.Forms.CheckBox();
            this.grpSiteInfo = new System.Windows.Forms.GroupBox();
            this.cboLatitudeSign = new System.Windows.Forms.ComboBox();
            this.nudLatitudeDeg = new System.Windows.Forms.NumericUpDown();
            this.nudLatitudeMin = new System.Windows.Forms.NumericUpDown();
            this.nudLatitudeSec = new System.Windows.Forms.NumericUpDown();
            this.cboLongitudeSign = new System.Windows.Forms.ComboBox();
            this.nudLongitudeDeg = new System.Windows.Forms.NumericUpDown();
            this.nudLongitudeMin = new System.Windows.Forms.NumericUpDown();
            this.nudLongitudeSec = new System.Windows.Forms.NumericUpDown();
            this.nudSiteElevation = new System.Windows.Forms.NumericUpDown();
            this.grpOrientation = new System.Windows.Forms.GroupBox();
            this.optOtaEast = new System.Windows.Forms.RadioButton();
            this.optOtaWest = new System.Windows.Forms.RadioButton();
            this.optCounterweightDown = new System.Windows.Forms.RadioButton();
            this.optCounterweightWest = new System.Windows.Forms.RadioButton();
            this.chkKeepLastSync = new System.Windows.Forms.CheckBox();
            this.chkAskAtStart = new System.Windows.Forms.CheckBox();
            this.grpTelescopeSetup = new System.Windows.Forms.GroupBox();
            this.nudAperture = new System.Windows.Forms.NumericUpDown();
            this.nudCentralObstruction = new System.Windows.Forms.NumericUpDown();
            this.nudFocalLength = new System.Windows.Forms.NumericUpDown();
            this.chkHighPrecisionGoto = new System.Windows.Forms.CheckBox();
            this.chkTrackingOffOnConnect = new System.Windows.Forms.CheckBox();
            this.chkWarnBeforeMeridianFlip = new System.Windows.Forms.CheckBox();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.label12 = new System.Windows.Forms.Label();
            this.label13 = new System.Windows.Forms.Label();
            this.label14 = new System.Windows.Forms.Label();
            this.grpMountSetup.SuspendLayout();
            this.grpParkSettings.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudParkAltitude)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudParkAzimuth)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudGuideRateRA)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudGuideRateDec)).BeginInit();
            this.grpSiteInfo.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudLatitudeDeg)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudLatitudeMin)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudLatitudeSec)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudLongitudeDeg)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudLongitudeMin)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudLongitudeSec)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudSiteElevation)).BeginInit();
            this.grpOrientation.SuspendLayout();
            this.grpTelescopeSetup.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudAperture)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudCentralObstruction)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudFocalLength)).BeginInit();
            this.SuspendLayout();
            // 
            // grpMountSetup
            // 
            this.grpMountSetup.Controls.Add(this.label4);
            this.grpMountSetup.Controls.Add(this.label3);
            this.grpMountSetup.Controls.Add(this.label2);
            this.grpMountSetup.Controls.Add(this.label1);
            this.grpMountSetup.Controls.Add(this.cboComPort);
            this.grpMountSetup.Controls.Add(this.cboTrackingRate);
            this.grpMountSetup.Controls.Add(this.cboMountVoltage);
            this.grpMountSetup.Controls.Add(this.cboMountModel);
            this.grpMountSetup.Controls.Add(this.chkTrace);
            this.grpMountSetup.Location = new System.Drawing.Point(12, 12);
            this.grpMountSetup.Name = "grpMountSetup";
            this.grpMountSetup.Size = new System.Drawing.Size(211, 181);
            this.grpMountSetup.TabIndex = 0;
            this.grpMountSetup.TabStop = false;
            this.grpMountSetup.Text = "Mount Setup";
            // 
            // cboComPort
            // 
            this.cboComPort.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboComPort.Location = new System.Drawing.Point(87, 24);
            this.cboComPort.Name = "cboComPort";
            this.cboComPort.Size = new System.Drawing.Size(80, 21);
            this.cboComPort.TabIndex = 0;
            // 
            // cboTrackingRate
            // 
            this.cboTrackingRate.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboTrackingRate.Location = new System.Drawing.Point(87, 50);
            this.cboTrackingRate.Name = "cboTrackingRate";
            this.cboTrackingRate.Size = new System.Drawing.Size(80, 21);
            this.cboTrackingRate.TabIndex = 1;
            // 
            // cboMountVoltage
            // 
            this.cboMountVoltage.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboMountVoltage.Location = new System.Drawing.Point(87, 77);
            this.cboMountVoltage.Name = "cboMountVoltage";
            this.cboMountVoltage.Size = new System.Drawing.Size(80, 21);
            this.cboMountVoltage.TabIndex = 2;
            // 
            // cboMountModel
            // 
            this.cboMountModel.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboMountModel.Location = new System.Drawing.Point(87, 104);
            this.cboMountModel.Name = "cboMountModel";
            this.cboMountModel.Size = new System.Drawing.Size(80, 21);
            this.cboMountModel.TabIndex = 3;
            // 
            // chkTrace
            // 
            this.chkTrace.Location = new System.Drawing.Point(31, 140);
            this.chkTrace.Name = "chkTrace";
            this.chkTrace.Size = new System.Drawing.Size(136, 20);
            this.chkTrace.TabIndex = 4;
            this.chkTrace.Text = "Enable Trace Logging";
            // 
            // grpParkSettings
            // 
            this.grpParkSettings.Controls.Add(this.label11);
            this.grpParkSettings.Controls.Add(this.label10);
            this.grpParkSettings.Controls.Add(this.label9);
            this.grpParkSettings.Controls.Add(this.label8);
            this.grpParkSettings.Controls.Add(this.nudParkAltitude);
            this.grpParkSettings.Controls.Add(this.nudParkAzimuth);
            this.grpParkSettings.Controls.Add(this.optParkCurrentPosition);
            this.grpParkSettings.Controls.Add(this.optParkSlewToPosition);
            this.grpParkSettings.Controls.Add(this.chkUnparkOnReconnect);
            this.grpParkSettings.Controls.Add(this.nudGuideRateRA);
            this.grpParkSettings.Controls.Add(this.nudGuideRateDec);
            this.grpParkSettings.Controls.Add(this.chkSendRate);
            this.grpParkSettings.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.grpParkSettings.Location = new System.Drawing.Point(12, 208);
            this.grpParkSettings.Name = "grpParkSettings";
            this.grpParkSettings.Size = new System.Drawing.Size(211, 214);
            this.grpParkSettings.TabIndex = 1;
            this.grpParkSettings.TabStop = false;
            this.grpParkSettings.Text = "Park/Rate Settings";
            // 
            // nudParkAltitude
            // 
            this.nudParkAltitude.Location = new System.Drawing.Point(22, 53);
            this.nudParkAltitude.Name = "nudParkAltitude";
            this.nudParkAltitude.Size = new System.Drawing.Size(46, 20);
            this.nudParkAltitude.TabIndex = 0;
            // 
            // nudParkAzimuth
            // 
            this.nudParkAzimuth.Location = new System.Drawing.Point(132, 53);
            this.nudParkAzimuth.Name = "nudParkAzimuth";
            this.nudParkAzimuth.Size = new System.Drawing.Size(46, 20);
            this.nudParkAzimuth.TabIndex = 1;
            this.nudParkAzimuth.ValueChanged += new System.EventHandler(this.nudParkAzimuth_ValueChanged);
            // 
            // optParkCurrentPosition
            // 
            this.optParkCurrentPosition.Location = new System.Drawing.Point(22, 79);
            this.optParkCurrentPosition.Name = "optParkCurrentPosition";
            this.optParkCurrentPosition.Size = new System.Drawing.Size(100, 20);
            this.optParkCurrentPosition.TabIndex = 2;
            this.optParkCurrentPosition.Text = "Current Position";
            // 
            // optParkSlewToPosition
            // 
            this.optParkSlewToPosition.Location = new System.Drawing.Point(132, 79);
            this.optParkSlewToPosition.Name = "optParkSlewToPosition";
            this.optParkSlewToPosition.Size = new System.Drawing.Size(75, 20);
            this.optParkSlewToPosition.TabIndex = 3;
            this.optParkSlewToPosition.Text = "Slew To Position";
            // 
            // chkUnparkOnReconnect
            // 
            this.chkUnparkOnReconnect.Location = new System.Drawing.Point(22, 104);
            this.chkUnparkOnReconnect.Name = "chkUnparkOnReconnect";
            this.chkUnparkOnReconnect.Size = new System.Drawing.Size(160, 20);
            this.chkUnparkOnReconnect.TabIndex = 4;
            this.chkUnparkOnReconnect.Text = "Unpark On Reconnect";
            // 
            // nudGuideRateRA
            // 
            this.nudGuideRateRA.DecimalPlaces = 2;
            this.nudGuideRateRA.Location = new System.Drawing.Point(22, 158);
            this.nudGuideRateRA.Name = "nudGuideRateRA";
            this.nudGuideRateRA.Size = new System.Drawing.Size(46, 20);
            this.nudGuideRateRA.TabIndex = 5;
            // 
            // nudGuideRateDec
            // 
            this.nudGuideRateDec.DecimalPlaces = 2;
            this.nudGuideRateDec.Location = new System.Drawing.Point(132, 158);
            this.nudGuideRateDec.Name = "nudGuideRateDec";
            this.nudGuideRateDec.Size = new System.Drawing.Size(46, 20);
            this.nudGuideRateDec.TabIndex = 6;
            this.nudGuideRateDec.ValueChanged += new System.EventHandler(this.nudGuideRateDec_ValueChanged);
            // 
            // chkSendRate
            // 
            this.chkSendRate.Location = new System.Drawing.Point(22, 188);
            this.chkSendRate.Name = "chkSendRate";
            this.chkSendRate.Size = new System.Drawing.Size(100, 20);
            this.chkSendRate.TabIndex = 7;
            this.chkSendRate.Text = "Send Rate";
            // 
            // grpSiteInfo
            // 
            this.grpSiteInfo.Controls.Add(this.label7);
            this.grpSiteInfo.Controls.Add(this.label6);
            this.grpSiteInfo.Controls.Add(this.label5);
            this.grpSiteInfo.Controls.Add(this.cboLatitudeSign);
            this.grpSiteInfo.Controls.Add(this.nudLatitudeDeg);
            this.grpSiteInfo.Controls.Add(this.nudLatitudeMin);
            this.grpSiteInfo.Controls.Add(this.nudLatitudeSec);
            this.grpSiteInfo.Controls.Add(this.cboLongitudeSign);
            this.grpSiteInfo.Controls.Add(this.nudLongitudeDeg);
            this.grpSiteInfo.Controls.Add(this.nudLongitudeMin);
            this.grpSiteInfo.Controls.Add(this.nudLongitudeSec);
            this.grpSiteInfo.Controls.Add(this.nudSiteElevation);
            this.grpSiteInfo.Location = new System.Drawing.Point(229, 15);
            this.grpSiteInfo.Name = "grpSiteInfo";
            this.grpSiteInfo.Size = new System.Drawing.Size(285, 110);
            this.grpSiteInfo.TabIndex = 2;
            this.grpSiteInfo.TabStop = false;
            this.grpSiteInfo.Text = "Site Information Setup";
            // 
            // cboLatitudeSign
            // 
            this.cboLatitudeSign.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboLatitudeSign.Location = new System.Drawing.Point(67, 23);
            this.cboLatitudeSign.Name = "cboLatitudeSign";
            this.cboLatitudeSign.Size = new System.Drawing.Size(47, 21);
            this.cboLatitudeSign.TabIndex = 0;
            // 
            // nudLatitudeDeg
            // 
            this.nudLatitudeDeg.Location = new System.Drawing.Point(119, 24);
            this.nudLatitudeDeg.Name = "nudLatitudeDeg";
            this.nudLatitudeDeg.Size = new System.Drawing.Size(47, 20);
            this.nudLatitudeDeg.TabIndex = 1;
            // 
            // nudLatitudeMin
            // 
            this.nudLatitudeMin.Location = new System.Drawing.Point(172, 23);
            this.nudLatitudeMin.Name = "nudLatitudeMin";
            this.nudLatitudeMin.Size = new System.Drawing.Size(47, 20);
            this.nudLatitudeMin.TabIndex = 2;
            // 
            // nudLatitudeSec
            // 
            this.nudLatitudeSec.Location = new System.Drawing.Point(225, 24);
            this.nudLatitudeSec.Name = "nudLatitudeSec";
            this.nudLatitudeSec.Size = new System.Drawing.Size(47, 20);
            this.nudLatitudeSec.TabIndex = 3;
            // 
            // cboLongitudeSign
            // 
            this.cboLongitudeSign.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboLongitudeSign.Location = new System.Drawing.Point(67, 49);
            this.cboLongitudeSign.Name = "cboLongitudeSign";
            this.cboLongitudeSign.Size = new System.Drawing.Size(47, 21);
            this.cboLongitudeSign.TabIndex = 4;
            // 
            // nudLongitudeDeg
            // 
            this.nudLongitudeDeg.Location = new System.Drawing.Point(119, 50);
            this.nudLongitudeDeg.Name = "nudLongitudeDeg";
            this.nudLongitudeDeg.Size = new System.Drawing.Size(47, 20);
            this.nudLongitudeDeg.TabIndex = 5;
            // 
            // nudLongitudeMin
            // 
            this.nudLongitudeMin.Location = new System.Drawing.Point(172, 49);
            this.nudLongitudeMin.Name = "nudLongitudeMin";
            this.nudLongitudeMin.Size = new System.Drawing.Size(47, 20);
            this.nudLongitudeMin.TabIndex = 6;
            // 
            // nudLongitudeSec
            // 
            this.nudLongitudeSec.Location = new System.Drawing.Point(225, 50);
            this.nudLongitudeSec.Name = "nudLongitudeSec";
            this.nudLongitudeSec.Size = new System.Drawing.Size(47, 20);
            this.nudLongitudeSec.TabIndex = 7;
            // 
            // nudSiteElevation
            // 
            this.nudSiteElevation.Location = new System.Drawing.Point(67, 77);
            this.nudSiteElevation.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.nudSiteElevation.Minimum = new decimal(new int[] {
            500,
            0,
            0,
            -2147483648});
            this.nudSiteElevation.Name = "nudSiteElevation";
            this.nudSiteElevation.Size = new System.Drawing.Size(47, 20);
            this.nudSiteElevation.TabIndex = 8;
            // 
            // grpOrientation
            // 
            this.grpOrientation.Controls.Add(this.optOtaEast);
            this.grpOrientation.Controls.Add(this.optOtaWest);
            this.grpOrientation.Controls.Add(this.optCounterweightDown);
            this.grpOrientation.Controls.Add(this.optCounterweightWest);
            this.grpOrientation.Controls.Add(this.chkKeepLastSync);
            this.grpOrientation.Controls.Add(this.chkAskAtStart);
            this.grpOrientation.Location = new System.Drawing.Point(229, 131);
            this.grpOrientation.Name = "grpOrientation";
            this.grpOrientation.Size = new System.Drawing.Size(285, 120);
            this.grpOrientation.TabIndex = 3;
            this.grpOrientation.TabStop = false;
            this.grpOrientation.Text = "Telescope Orientation";
            // 
            // optOtaEast
            // 
            this.optOtaEast.Location = new System.Drawing.Point(20, 20);
            this.optOtaEast.Name = "optOtaEast";
            this.optOtaEast.Size = new System.Drawing.Size(199, 20);
            this.optOtaEast.TabIndex = 0;
            this.optOtaEast.Text = "OTA East - Scope Pointing West";
            // 
            // optOtaWest
            // 
            this.optOtaWest.Location = new System.Drawing.Point(20, 42);
            this.optOtaWest.Name = "optOtaWest";
            this.optOtaWest.Size = new System.Drawing.Size(200, 20);
            this.optOtaWest.TabIndex = 1;
            this.optOtaWest.Text = "OTA West - Scope Pointing East";
            this.optOtaWest.CheckedChanged += new System.EventHandler(this.optOtaWest_CheckedChanged);
            // 
            // optCounterweightDown
            // 
            this.optCounterweightDown.Location = new System.Drawing.Point(20, 64);
            this.optCounterweightDown.Name = "optCounterweightDown";
            this.optCounterweightDown.Size = new System.Drawing.Size(133, 20);
            this.optCounterweightDown.TabIndex = 2;
            this.optCounterweightDown.Text = "Counterweight Down";
            // 
            // optCounterweightWest
            // 
            this.optCounterweightWest.Location = new System.Drawing.Point(17, 89);
            this.optCounterweightWest.Name = "optCounterweightWest";
            this.optCounterweightWest.Size = new System.Drawing.Size(136, 20);
            this.optCounterweightWest.TabIndex = 3;
            this.optCounterweightWest.Text = "Counterweight West";
            // 
            // chkKeepLastSync
            // 
            this.chkKeepLastSync.Location = new System.Drawing.Point(172, 65);
            this.chkKeepLastSync.Name = "chkKeepLastSync";
            this.chkKeepLastSync.Size = new System.Drawing.Size(107, 20);
            this.chkKeepLastSync.TabIndex = 4;
            this.chkKeepLastSync.Text = "Keep Last Sync";
            // 
            // chkAskAtStart
            // 
            this.chkAskAtStart.Location = new System.Drawing.Point(172, 90);
            this.chkAskAtStart.Name = "chkAskAtStart";
            this.chkAskAtStart.Size = new System.Drawing.Size(88, 20);
            this.chkAskAtStart.TabIndex = 5;
            this.chkAskAtStart.Text = "Ask At Start";
            // 
            // grpTelescopeSetup
            // 
            this.grpTelescopeSetup.Controls.Add(this.label14);
            this.grpTelescopeSetup.Controls.Add(this.label13);
            this.grpTelescopeSetup.Controls.Add(this.label12);
            this.grpTelescopeSetup.Controls.Add(this.nudAperture);
            this.grpTelescopeSetup.Controls.Add(this.nudCentralObstruction);
            this.grpTelescopeSetup.Controls.Add(this.nudFocalLength);
            this.grpTelescopeSetup.Controls.Add(this.chkHighPrecisionGoto);
            this.grpTelescopeSetup.Controls.Add(this.chkTrackingOffOnConnect);
            this.grpTelescopeSetup.Controls.Add(this.chkWarnBeforeMeridianFlip);
            this.grpTelescopeSetup.Location = new System.Drawing.Point(229, 257);
            this.grpTelescopeSetup.Name = "grpTelescopeSetup";
            this.grpTelescopeSetup.Size = new System.Drawing.Size(285, 200);
            this.grpTelescopeSetup.TabIndex = 4;
            this.grpTelescopeSetup.TabStop = false;
            this.grpTelescopeSetup.Text = "Telescope Setup";
            // 
            // nudAperture
            // 
            this.nudAperture.Location = new System.Drawing.Point(148, 30);
            this.nudAperture.Name = "nudAperture";
            this.nudAperture.Size = new System.Drawing.Size(52, 20);
            this.nudAperture.TabIndex = 0;
            // 
            // nudCentralObstruction
            // 
            this.nudCentralObstruction.Location = new System.Drawing.Point(148, 64);
            this.nudCentralObstruction.Name = "nudCentralObstruction";
            this.nudCentralObstruction.Size = new System.Drawing.Size(52, 20);
            this.nudCentralObstruction.TabIndex = 1;
            // 
            // nudFocalLength
            // 
            this.nudFocalLength.Location = new System.Drawing.Point(148, 95);
            this.nudFocalLength.Name = "nudFocalLength";
            this.nudFocalLength.Size = new System.Drawing.Size(52, 20);
            this.nudFocalLength.TabIndex = 2;
            // 
            // chkHighPrecisionGoto
            // 
            this.chkHighPrecisionGoto.Location = new System.Drawing.Point(23, 122);
            this.chkHighPrecisionGoto.Name = "chkHighPrecisionGoto";
            this.chkHighPrecisionGoto.Size = new System.Drawing.Size(180, 20);
            this.chkHighPrecisionGoto.TabIndex = 3;
            this.chkHighPrecisionGoto.Text = "Hi-Precision GOTO";
            // 
            // chkTrackingOffOnConnect
            // 
            this.chkTrackingOffOnConnect.Location = new System.Drawing.Point(23, 148);
            this.chkTrackingOffOnConnect.Name = "chkTrackingOffOnConnect";
            this.chkTrackingOffOnConnect.Size = new System.Drawing.Size(200, 20);
            this.chkTrackingOffOnConnect.TabIndex = 4;
            this.chkTrackingOffOnConnect.Text = "Tracking Off on Connection";
            // 
            // chkWarnBeforeMeridianFlip
            // 
            this.chkWarnBeforeMeridianFlip.Location = new System.Drawing.Point(23, 173);
            this.chkWarnBeforeMeridianFlip.Name = "chkWarnBeforeMeridianFlip";
            this.chkWarnBeforeMeridianFlip.Size = new System.Drawing.Size(200, 20);
            this.chkWarnBeforeMeridianFlip.TabIndex = 5;
            this.chkWarnBeforeMeridianFlip.Text = "Warn Before Meridian Flip";
            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(22, 430);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 28);
            this.btnOK.TabIndex = 5;
            this.btnOK.Text = "OK";
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(123, 428);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 28);
            this.btnCancel.TabIndex = 6;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(31, 29);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(50, 13);
            this.label1.TabIndex = 5;
            this.label1.Text = "Com Port";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(7, 56);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(75, 13);
            this.label2.TabIndex = 6;
            this.label2.Text = "Tracking Rate";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(33, 82);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(43, 13);
            this.label3.TabIndex = 7;
            this.label3.Text = "Voltage";
            this.label3.Click += new System.EventHandler(this.label3_Click);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(40, 107);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(36, 13);
            this.label4.TabIndex = 8;
            this.label4.Text = "Model";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(6, 27);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(48, 13);
            this.label5.TabIndex = 9;
            this.label5.Text = "Lattitude";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(6, 52);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(54, 13);
            this.label6.TabIndex = 10;
            this.label6.Text = "Longitude";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(6, 81);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(33, 13);
            this.label7.TabIndex = 11;
            this.label7.Text = "El (m)";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(78, 55);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(46, 13);
            this.label8.TabIndex = 8;
            this.label8.Text = "Alt  -  Az";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(74, 160);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(56, 13);
            this.label9.TabIndex = 9;
            this.label9.Text = "Ra  -  Dec";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label10.Location = new System.Drawing.Point(60, 30);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(83, 13);
            this.label10.TabIndex = 10;
            this.label10.Text = "Park Settings";
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label11.Location = new System.Drawing.Point(60, 135);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(90, 13);
            this.label11.TabIndex = 11;
            this.label11.Text = "Guide Settings";
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(17, 34);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(125, 13);
            this.label12.TabIndex = 6;
            this.label12.Text = "Telescope Aperture (mm)";
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(20, 71);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(122, 13);
            this.label13.TabIndex = 7;
            this.label13.Text = "Central Obstruction (mm)";
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(20, 97);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(94, 13);
            this.label14.TabIndex = 8;
            this.label14.Text = "Focal Length (mm)";
            // 
            // SetupDialogForm
            // 
            this.AcceptButton = this.btnOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(525, 470);
            this.Controls.Add(this.grpMountSetup);
            this.Controls.Add(this.grpParkSettings);
            this.Controls.Add(this.grpSiteInfo);
            this.Controls.Add(this.grpOrientation);
            this.Controls.Add(this.grpTelescopeSetup);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.btnCancel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SetupDialogForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Temma Setup";
            this.grpMountSetup.ResumeLayout(false);
            this.grpMountSetup.PerformLayout();
            this.grpParkSettings.ResumeLayout(false);
            this.grpParkSettings.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudParkAltitude)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudParkAzimuth)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudGuideRateRA)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudGuideRateDec)).EndInit();
            this.grpSiteInfo.ResumeLayout(false);
            this.grpSiteInfo.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudLatitudeDeg)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudLatitudeMin)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudLatitudeSec)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudLongitudeDeg)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudLongitudeMin)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudLongitudeSec)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudSiteElevation)).EndInit();
            this.grpOrientation.ResumeLayout(false);
            this.grpTelescopeSetup.ResumeLayout(false);
            this.grpTelescopeSetup.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudAperture)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudCentralObstruction)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudFocalLength)).EndInit();
            this.ResumeLayout(false);

        }

        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.Label label12;
    }
}
