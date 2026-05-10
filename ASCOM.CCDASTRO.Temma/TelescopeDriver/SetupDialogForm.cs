using System;
using System.ComponentModel;
using System.IO.Ports;
using System.Linq;
using System.Windows.Forms;

namespace ASCOM.CCDASTROTemma.Telescope
{
    public partial class SetupDialogForm : Form
    {
        private readonly DriverSettings settings;

        public SetupDialogForm()
        {
            InitializeComponent();
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;
        }

        public SetupDialogForm(string progId) : this()
        {
            settings = new DriverSettings(progId);
            LoadSettings();
        }

        private void LoadSettings()
        {
            if (settings == null) return;

            LoadCombo(cboComPort, SerialPort.GetPortNames().OrderBy(x => x).DefaultIfEmpty("COM1").ToArray(), settings.ComPort);
            LoadCombo(cboTrackingRate, new[] { "Sidereal", "Lunar", "Solar", "King" }, settings.TrackingRate);
            LoadCombo(cboMountVoltage, new[] { "12V", "24V" }, settings.MountVoltage);
            LoadCombo(cboMountModel, Enum.GetNames(typeof(TemmaMountModel)), settings.MountModel);

            chkTrace.Checked = settings.TraceEnabled;
            chkUnparkOnReconnect.Checked = settings.UnparkOnReconnect;
            chkKeepLastSync.Checked = settings.KeepLastSync;
            chkAskAtStart.Checked = settings.AskAtStart;
            chkSendRate.Checked = settings.SendRate;
            chkHighPrecisionGoto.Checked = settings.HighPrecisionGoto;
            chkTrackingOffOnConnect.Checked = settings.TrackingOffOnConnect;
            chkWarnBeforeMeridianFlip.Checked = settings.WarnBeforeMeridianFlip;

            SetNumeric(nudGuideRateRA, settings.GuideRateRA);
            SetNumeric(nudGuideRateDec, settings.GuideRateDec);
            SetNumeric(nudSiteElevation, settings.SiteElevation);
            SetNumeric(nudParkAltitude, settings.ParkAltitude);
            SetNumeric(nudParkAzimuth, settings.ParkAzimuth);
            SetNumeric(nudAperture, settings.Aperture);
            SetNumeric(nudCentralObstruction, settings.CentralObstruction);
            SetNumeric(nudFocalLength, settings.FocalLength);

            optParkCurrentPosition.Checked = settings.ParkCurrentPosition;
            optParkSlewToPosition.Checked = !settings.ParkCurrentPosition;

            SelectOrientation(settings.Orientation);
            SetDms(settings.SiteLatitude, cboLatitudeSign, nudLatitudeDeg, nudLatitudeMin, nudLatitudeSec, "N", "S");
            SetDms(settings.SiteLongitude, cboLongitudeSign, nudLongitudeDeg, nudLongitudeMin, nudLongitudeSec, "E", "W");
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            if (settings != null)
            {
                settings.ComPort = cboComPort.Text;
                settings.TrackingRate = cboTrackingRate.Text;
                settings.MountVoltage = cboMountVoltage.Text;
                settings.MountModel = cboMountModel.Text;
                settings.TraceEnabled = chkTrace.Checked;
                settings.UnparkOnReconnect = chkUnparkOnReconnect.Checked;
                settings.KeepLastSync = chkKeepLastSync.Checked;
                settings.AskAtStart = chkAskAtStart.Checked;
                settings.SendRate = chkSendRate.Checked;
                settings.HighPrecisionGoto = chkHighPrecisionGoto.Checked;
                settings.TrackingOffOnConnect = chkTrackingOffOnConnect.Checked;
                settings.WarnBeforeMeridianFlip = chkWarnBeforeMeridianFlip.Checked;

                settings.GuideRateRA = (double)nudGuideRateRA.Value;
                settings.GuideRateDec = (double)nudGuideRateDec.Value;
                settings.SiteElevation = (double)nudSiteElevation.Value;
                settings.ParkAltitude = (double)nudParkAltitude.Value;
                settings.ParkAzimuth = (double)nudParkAzimuth.Value;
                settings.Aperture = (double)nudAperture.Value;
                settings.CentralObstruction = (double)nudCentralObstruction.Value;
                settings.FocalLength = (double)nudFocalLength.Value;

                settings.ParkCurrentPosition = optParkCurrentPosition.Checked;
                settings.Orientation = GetOrientation();
                settings.SiteLatitude = GetDms(cboLatitudeSign, nudLatitudeDeg, nudLatitudeMin, nudLatitudeSec, "S");
                settings.SiteLongitude = GetDms(cboLongitudeSign, nudLongitudeDeg, nudLongitudeMin, nudLongitudeSec, "W");
            }

            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void LoadCombo(ComboBox combo, string[] items, string selected)
        {
            combo.Items.Clear();
            combo.Items.AddRange(items);
            combo.Text = selected;
            if (combo.SelectedIndex < 0 && combo.Items.Count > 0) combo.SelectedIndex = 0;
        }

        private void SetNumeric(NumericUpDown nud, double value)
        {
            decimal d = (decimal)value;
            if (d < nud.Minimum) d = nud.Minimum;
            if (d > nud.Maximum) d = nud.Maximum;
            nud.Value = d;
        }

        private void SelectOrientation(string orientation)
        {
            optOtaEast.Checked = orientation == "OtaEast";
            optOtaWest.Checked = orientation == "OtaWest";
            optCounterweightWest.Checked = orientation == "CounterweightWest";
            optCounterweightDown.Checked = !optOtaEast.Checked && !optOtaWest.Checked && !optCounterweightWest.Checked;
        }

        private string GetOrientation()
        {
            if (optOtaEast.Checked) return "OtaEast";
            if (optOtaWest.Checked) return "OtaWest";
            if (optCounterweightWest.Checked) return "CounterweightWest";
            return "CounterweightDown";
        }

        private void SetDms(double value, ComboBox sign, NumericUpDown deg, NumericUpDown min, NumericUpDown sec, string pos, string neg)
        {
            sign.Items.Clear(); sign.Items.Add(pos); sign.Items.Add(neg);
            sign.SelectedItem = value >= 0 ? pos : neg;
            value = Math.Abs(value);
            int d = (int)value;
            int m = (int)((value - d) * 60);
            int s = (int)Math.Round((((value - d) * 60) - m) * 60);
            deg.Value = Math.Min(deg.Maximum, d);
            min.Value = Math.Min(min.Maximum, m);
            sec.Value = Math.Min(sec.Maximum, s);
        }

        private double GetDms(ComboBox sign, NumericUpDown deg, NumericUpDown min, NumericUpDown sec, string neg)
        {
            double v = (double)deg.Value + (double)min.Value / 60.0 + (double)sec.Value / 3600.0;
            if ((sign.SelectedItem?.ToString() ?? "") == neg) v = -v;
            return v;
        }

        private void nudParkAzimuth_ValueChanged(object sender, EventArgs e)
        {

        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void optOtaWest_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void nudGuideRateDec_ValueChanged(object sender, EventArgs e)
        {

        }

        private void label12_Click(object sender, EventArgs e)
        {

        }
    }
}
