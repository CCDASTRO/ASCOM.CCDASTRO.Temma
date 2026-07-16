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

            // Prevent designer errors.
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
                return;
        }

        public SetupDialogForm(string progId)
            : this()
        {
            this.TopMost = true;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.ShowInTaskbar = false;
            this.Shown += SetupDialogForm_Shown;

            settings = new DriverSettings(progId);
            LoadSettings();

            this.BringToFront();
            this.Activate();
        }

        private void SetupDialogForm_Shown(object sender, EventArgs e)
        {
            this.TopMost = true;
            this.BringToFront();
            this.Activate();
        }

        private void LoadSettings()
        {
            if (settings == null)
                return;

            LoadCombo(
                cboComPort,
                SerialPort.GetPortNames()
                    .OrderBy(x => x)
                    .DefaultIfEmpty("COM1")
                    .ToArray(),
                settings.ComPort);

            LoadCombo(
                cboTrackingRate,
                new[] { "Sidereal", "Lunar", "Solar", "King" },
                settings.TrackingRate);

            LoadCombo(
                cboMountVoltage,
                new[] { "12V", "24V" },
                settings.MountVoltage);

            // Mount model combo box uses enum names.
            LoadCombo(
                cboMountModel,
                Enum.GetNames(typeof(TemmaMountModel)),
                settings.MountModel.ToString());

            chkTrace.Checked = settings.TraceEnabled;
            chkUnparkOnReconnect.Checked = settings.UnparkOnReconnect;
            chkSendRate.Checked = settings.SendRate;
            
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

            SetDms(
                settings.SiteLatitude,
                cboLatitudeSign,
                nudLatitudeDeg,
                nudLatitudeMin,
                nudLatitudeSec,
                "N",
                "S");

            SetDms(
                settings.SiteLongitude,
                cboLongitudeSign,
                nudLongitudeDeg,
                nudLongitudeMin,
                nudLongitudeSec,
                "E",
                "W");
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            if (settings != null)
            {
                settings.ComPort = cboComPort.Text;
                settings.TrackingRate = cboTrackingRate.Text;
                settings.MountVoltage = cboMountVoltage.Text;

                // Convert combo box text to enum.
                TemmaMountModel model;
                if (Enum.TryParse(cboMountModel.Text, out model))
                    settings.MountModel = model;
                else
                    settings.MountModel = TemmaMountModel.EM200;

                settings.Use24Volts =
                    string.Equals(
                        cboMountVoltage.Text,
                        "24V",
                        StringComparison.OrdinalIgnoreCase);

                settings.TraceEnabled = chkTrace.Checked;
                settings.UnparkOnReconnect = chkUnparkOnReconnect.Checked;
                settings.SendRate = chkSendRate.Checked;
                
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

                settings.SiteLatitude = GetDms(
                    cboLatitudeSign,
                    nudLatitudeDeg,
                    nudLatitudeMin,
                    nudLatitudeSec,
                    "S");

                settings.SiteLongitude = GetDms(
                    cboLongitudeSign,
                    nudLongitudeDeg,
                    nudLongitudeMin,
                    nudLongitudeSec,
                    "W");
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

            if (combo.SelectedIndex < 0 && combo.Items.Count > 0)
                combo.SelectedIndex = 0;
        }

        private void SetNumeric(NumericUpDown nud, double value)
        {
            decimal d = (decimal)value;

            if (d < nud.Minimum)
                d = nud.Minimum;

            if (d > nud.Maximum)
                d = nud.Maximum;

            nud.Value = d;
        }

        private void SelectOrientation(string orientation)
        {
            optOtaEast.Checked = orientation == "OtaEast";
            optOtaWest.Checked = orientation == "OtaWest";
            optCounterweightWest.Checked = orientation == "CounterweightWest";

            optCounterweightDown.Checked =
                !optOtaEast.Checked &&
                !optOtaWest.Checked &&
                !optCounterweightWest.Checked;
        }

        private string GetOrientation()
        {
            if (optOtaEast.Checked) return "OtaEast";
            if (optOtaWest.Checked) return "OtaWest";
            if (optCounterweightWest.Checked) return "CounterweightWest";
            return "CounterweightDown";
        }

        private void SetDms(
            double value,
            ComboBox sign,
            NumericUpDown deg,
            NumericUpDown min,
            NumericUpDown sec,
            string pos,
            string neg)
        {
            sign.Items.Clear();
            sign.Items.Add(pos);
            sign.Items.Add(neg);
            sign.SelectedItem = value >= 0 ? pos : neg;

            value = Math.Abs(value);

            int d = (int)value;
            int m = (int)((value - d) * 60);
            int s = (int)Math.Round((((value - d) * 60) - m) * 60);

            deg.Value = Math.Min(deg.Maximum, d);
            min.Value = Math.Min(min.Maximum, m);
            sec.Value = Math.Min(sec.Maximum, s);
        }

        private double GetDms(
            ComboBox sign,
            NumericUpDown deg,
            NumericUpDown min,
            NumericUpDown sec,
            string neg)
        {
            double value =
                (double)deg.Value +
                (double)min.Value / 60.0 +
                (double)sec.Value / 3600.0;

            if ((sign.SelectedItem?.ToString() ?? string.Empty) == neg)
                value = -value;

            return value;
        }

        // Empty event handlers retained so the designer remains stable.

        private void nudParkAzimuth_ValueChanged(object sender, EventArgs e) { }
        private void label3_Click(object sender, EventArgs e) { }
        private void optOtaWest_CheckedChanged(object sender, EventArgs e) { }
        private void nudGuideRateDec_ValueChanged(object sender, EventArgs e) { }
        private void label12_Click(object sender, EventArgs e) { }
        private void SetupDialogForm_Load(object sender, EventArgs e) { }

        

        private void grpTelescopeSetup_Enter(object sender, EventArgs e)
        {

        }

        

        private void SetupDialogForm_Load_1(object sender, EventArgs e)
        {

        }

        
    }
}
