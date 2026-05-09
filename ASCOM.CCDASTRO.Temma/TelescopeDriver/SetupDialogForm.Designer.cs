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
            this.components = new System.ComponentModel.Container();

            this.grpMountSetup = new System.Windows.Forms.GroupBox();
            this.grpParkSettings = new System.Windows.Forms.GroupBox();
            this.grpSiteInfo = new System.Windows.Forms.GroupBox();
            this.grpOrientation = new System.Windows.Forms.GroupBox();
            this.grpTelescopeSetup = new System.Windows.Forms.GroupBox();

            this.cboComPort = new System.Windows.Forms.ComboBox();
            this.cboTrackingRate = new System.Windows.Forms.ComboBox();
            this.cboMountVoltage = new System.Windows.Forms.ComboBox();
            this.cboMountModel = new System.Windows.Forms.ComboBox();
            this.cboLatitudeSign = new System.Windows.Forms.ComboBox();
            this.cboLongitudeSign = new System.Windows.Forms.ComboBox();

            this.nudLatitudeDeg = new System.Windows.Forms.NumericUpDown();
            this.nudLatitudeMin = new System.Windows.Forms.NumericUpDown();
            this.nudLatitudeSec = new System.Windows.Forms.NumericUpDown();
            this.nudLongitudeDeg = new System.Windows.Forms.NumericUpDown();
            this.nudLongitudeMin = new System.Windows.Forms.NumericUpDown();
            this.nudLongitudeSec = new System.Windows.Forms.NumericUpDown();
            this.nudSiteElevation = new System.Windows.Forms.NumericUpDown();
            this.nudParkAltitude = new System.Windows.Forms.NumericUpDown();
            this.nudParkAzimuth = new System.Windows.Forms.NumericUpDown();
            this.nudGuideRateRA = new System.Windows.Forms.NumericUpDown();
            this.nudGuideRateDec = new System.Windows.Forms.NumericUpDown();
            this.nudAperture = new System.Windows.Forms.NumericUpDown();
            this.nudCentralObstruction = new System.Windows.Forms.NumericUpDown();
            this.nudFocalLength = new System.Windows.Forms.NumericUpDown();

            this.chkTrace = new System.Windows.Forms.CheckBox();
            this.chkUnparkOnReconnect = new System.Windows.Forms.CheckBox();
            this.chkKeepLastSync = new System.Windows.Forms.CheckBox();
            this.chkAskAtStart = new System.Windows.Forms.CheckBox();
            this.chkSendRate = new System.Windows.Forms.CheckBox();
            this.chkHighPrecisionGoto = new System.Windows.Forms.CheckBox();
            this.chkTrackingOffOnConnect = new System.Windows.Forms.CheckBox();
            this.chkWarnBeforeMeridianFlip = new System.Windows.Forms.CheckBox();

            this.optParkCurrentPosition = new System.Windows.Forms.RadioButton();
            this.optParkSlewToPosition = new System.Windows.Forms.RadioButton();
            this.optOtaEast = new System.Windows.Forms.RadioButton();
            this.optOtaWest = new System.Windows.Forms.RadioButton();
            this.optCounterweightDown = new System.Windows.Forms.RadioButton();
            this.optCounterweightWest = new System.Windows.Forms.RadioButton();

            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();

            this.SuspendLayout();

            // Form
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(840, 540);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Temma Setup";

            // Group boxes
            this.grpMountSetup.Text = "Mount Setup";
            this.grpMountSetup.SetBounds(12, 12, 230, 220);
            this.grpParkSettings.Text = "Park Settings";
            this.grpParkSettings.SetBounds(12, 240, 230, 220);
            this.grpSiteInfo.Text = "Site Information Setup";
            this.grpSiteInfo.SetBounds(255, 12, 570, 110);
            this.grpOrientation.Text = "Telescope Orientation";
            this.grpOrientation.SetBounds(255, 130, 570, 120);
            this.grpTelescopeSetup.Text = "Telescope Setup";
            this.grpTelescopeSetup.SetBounds(255, 260, 570, 200);

            // Mount controls
            this.cboComPort.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboComPort.SetBounds(20, 25, 180, 21);
            this.cboTrackingRate.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboTrackingRate.SetBounds(20, 60, 180, 21);
            this.cboMountVoltage.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboMountVoltage.SetBounds(20, 95, 180, 21);
            this.cboMountModel.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboMountModel.SetBounds(20, 130, 180, 21);
            this.chkTrace.Text = "Enable Trace Logging";
            this.chkTrace.SetBounds(20, 170, 180, 20);
            this.grpMountSetup.Controls.Add(this.cboComPort);
            this.grpMountSetup.Controls.Add(this.cboTrackingRate);
            this.grpMountSetup.Controls.Add(this.cboMountVoltage);
            this.grpMountSetup.Controls.Add(this.cboMountModel);
            this.grpMountSetup.Controls.Add(this.chkTrace);

            // Site controls
            this.cboLatitudeSign.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboLatitudeSign.SetBounds(20, 25, 60, 21);
            this.nudLatitudeDeg.SetBounds(90, 25, 60, 20);
            this.nudLatitudeMin.SetBounds(160, 25, 60, 20);
            this.nudLatitudeSec.SetBounds(230, 25, 60, 20);

            this.cboLongitudeSign.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboLongitudeSign.SetBounds(20, 60, 60, 21);
            this.nudLongitudeDeg.SetBounds(90, 60, 60, 20);
            this.nudLongitudeMin.SetBounds(160, 60, 60, 20);
            this.nudLongitudeSec.SetBounds(230, 60, 60, 20);

            this.nudSiteElevation.SetBounds(20, 85, 100, 20);
            this.nudSiteElevation.Minimum = -500;
            this.nudSiteElevation.Maximum = 10000;

            this.grpSiteInfo.Controls.Add(this.cboLatitudeSign);
            this.grpSiteInfo.Controls.Add(this.nudLatitudeDeg);
            this.grpSiteInfo.Controls.Add(this.nudLatitudeMin);
            this.grpSiteInfo.Controls.Add(this.nudLatitudeSec);
            this.grpSiteInfo.Controls.Add(this.cboLongitudeSign);
            this.grpSiteInfo.Controls.Add(this.nudLongitudeDeg);
            this.grpSiteInfo.Controls.Add(this.nudLongitudeMin);
            this.grpSiteInfo.Controls.Add(this.nudLongitudeSec);
            this.grpSiteInfo.Controls.Add(this.nudSiteElevation);

            // Orientation
            this.optOtaEast.Text = "OTA East - Scope Pointing West";
            this.optOtaEast.SetBounds(20, 20, 250, 20);
            this.optOtaWest.Text = "OTA West - Scope Pointing East";
            this.optOtaWest.SetBounds(20, 42, 250, 20);
            this.optCounterweightDown.Text = "Counterweight Down";
            this.optCounterweightDown.SetBounds(20, 64, 200, 20);
            this.optCounterweightWest.Text = "Counterweight West";
            this.optCounterweightWest.SetBounds(20, 86, 200, 20);
            this.chkKeepLastSync.Text = "Keep Last Sync";
            this.chkKeepLastSync.SetBounds(320, 20, 120, 20);
            this.chkAskAtStart.Text = "Ask At Start";
            this.chkAskAtStart.SetBounds(440, 20, 100, 20);
            this.grpOrientation.Controls.Add(this.optOtaEast);
            this.grpOrientation.Controls.Add(this.optOtaWest);
            this.grpOrientation.Controls.Add(this.optCounterweightDown);
            this.grpOrientation.Controls.Add(this.optCounterweightWest);
            this.grpOrientation.Controls.Add(this.chkKeepLastSync);
            this.grpOrientation.Controls.Add(this.chkAskAtStart);

            // Park settings
            this.nudParkAltitude.SetBounds(20, 25, 80, 20);
            this.nudParkAzimuth.SetBounds(120, 25, 80, 20);
            this.optParkCurrentPosition.Text = "Current Position";
            this.optParkCurrentPosition.SetBounds(20, 55, 110, 20);
            this.optParkSlewToPosition.Text = "Slew To Position";
            this.optParkSlewToPosition.SetBounds(130, 55, 100, 20);
            this.chkUnparkOnReconnect.Text = "Unpark On Reconnect";
            this.chkUnparkOnReconnect.SetBounds(20, 80, 160, 20);
            this.nudGuideRateRA.DecimalPlaces = 2;
            this.nudGuideRateRA.SetBounds(20, 115, 80, 20);
            this.nudGuideRateDec.DecimalPlaces = 2;
            this.nudGuideRateDec.SetBounds(120, 115, 80, 20);
            this.chkSendRate.Text = "Send Rate";
            this.chkSendRate.SetBounds(20, 145, 100, 20);
            this.grpParkSettings.Controls.Add(this.nudParkAltitude);
            this.grpParkSettings.Controls.Add(this.nudParkAzimuth);
            this.grpParkSettings.Controls.Add(this.optParkCurrentPosition);
            this.grpParkSettings.Controls.Add(this.optParkSlewToPosition);
            this.grpParkSettings.Controls.Add(this.chkUnparkOnReconnect);
            this.grpParkSettings.Controls.Add(this.nudGuideRateRA);
            this.grpParkSettings.Controls.Add(this.nudGuideRateDec);
            this.grpParkSettings.Controls.Add(this.chkSendRate);

            // Telescope setup
            this.nudAperture.SetBounds(220, 20, 80, 20);
            this.nudCentralObstruction.SetBounds(220, 55, 80, 20);
            this.nudFocalLength.SetBounds(220, 90, 80, 20);
            this.chkHighPrecisionGoto.Text = "Hi-Precision GOTO";
            this.chkHighPrecisionGoto.SetBounds(20, 125, 180, 20);
            this.chkTrackingOffOnConnect.Text = "Tracking Off on Connection";
            this.chkTrackingOffOnConnect.SetBounds(20, 148, 200, 20);
            this.chkWarnBeforeMeridianFlip.Text = "Warn Before Meridian Flip";
            this.chkWarnBeforeMeridianFlip.SetBounds(20, 171, 200, 20);
            this.grpTelescopeSetup.Controls.Add(this.nudAperture);
            this.grpTelescopeSetup.Controls.Add(this.nudCentralObstruction);
            this.grpTelescopeSetup.Controls.Add(this.nudFocalLength);
            this.grpTelescopeSetup.Controls.Add(this.chkHighPrecisionGoto);
            this.grpTelescopeSetup.Controls.Add(this.chkTrackingOffOnConnect);
            this.grpTelescopeSetup.Controls.Add(this.chkWarnBeforeMeridianFlip);

            // Buttons
            this.btnOK.Text = "OK";
            this.btnOK.SetBounds(660, 490, 75, 28);
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            this.btnCancel.Text = "Cancel";
            this.btnCancel.SetBounds(745, 490, 75, 28);
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);

            // Add to form
            this.Controls.Add(this.grpMountSetup);
            this.Controls.Add(this.grpParkSettings);
            this.Controls.Add(this.grpSiteInfo);
            this.Controls.Add(this.grpOrientation);
            this.Controls.Add(this.grpTelescopeSetup);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.btnCancel);

            this.AcceptButton = this.btnOK;
            this.CancelButton = this.btnCancel;

            this.ResumeLayout(false);
        }
    }
}
