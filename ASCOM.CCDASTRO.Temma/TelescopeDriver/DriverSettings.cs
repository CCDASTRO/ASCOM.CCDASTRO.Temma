using ASCOM.Utilities;
using System;
using System.Globalization;

namespace ASCOM.CCDASTROTemma.Telescope
{
    public enum TemmaMountModel
    {
        EM11,
        EM11M,
        EM200,
        EM200M,
        NJP,
        EM500
    }

    public class DriverSettings
    {
        private readonly string progId;

        public DriverSettings(string progId)
        {
            this.progId = progId;
        }

        private string ReadString(string name, string def)
        {
            using (var p = new Profile())
            {
                p.DeviceType = "Telescope";
                return p.GetValue(progId, name, string.Empty, def);
            }
        }

        private void WriteString(string name, string value)
        {
            using (var p = new Profile())
            {
                p.DeviceType = "Telescope";
                p.WriteValue(progId, name, value ?? string.Empty);
            }
        }

        private bool ReadBool(string name, bool def)
        {
            bool v;
            return bool.TryParse(ReadString(name, def.ToString()), out v) ? v : def;
        }

        private void WriteBool(string name, bool v)
        {
            WriteString(name, v.ToString());
        }

        private double ReadDouble(string name, double def)
        {
            double v;
            return double.TryParse(
                ReadString(name, def.ToString(CultureInfo.InvariantCulture)),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out v) ? v : def;
        }

        private void WriteDouble(string name, double v)
        {
            WriteString(name, v.ToString(CultureInfo.InvariantCulture));
        }

        private short ReadShort(string name, short def)
        {
            short v;
            return short.TryParse(ReadString(name, def.ToString()), out v) ? v : def;
        }

        private void WriteShort(string name, short v)
        {
            WriteString(name, v.ToString());
        }

        // Basic settings
        public string ComPort { get { return ReadString("ComPort", "COM1"); } set { WriteString("ComPort", value); } }
        public string TrackingRate { get { return ReadString("TrackingRate", "Sidereal"); } set { WriteString("TrackingRate", value); } }
        public string MountVoltage { get { return ReadString("MountVoltage", "12V"); } set { WriteString("MountVoltage", value); } }
        public bool TraceEnabled { get { return ReadBool("TraceEnabled", false); } set { WriteBool("TraceEnabled", value); } }

        // New strongly-typed mount settings
        public TemmaMountModel MountModel
        {
            get
            {
                string s = ReadString("MountModel", TemmaMountModel.EM200.ToString());
                TemmaMountModel model;
                return Enum.TryParse(s, out model) ? model : TemmaMountModel.EM200;
            }
            set
            {
                WriteString("MountModel", value.ToString());
            }
        }

        public bool Use24Volts
        {
            get
            {
                // Backward compatibility: if old MountVoltage exists, use it.
                string oldValue = ReadString("MountVoltage", "24V");
                bool defaultValue = string.Equals(oldValue, "24V", StringComparison.OrdinalIgnoreCase);
                return ReadBool("Use24Volts", defaultValue);
            }
            set
            {
                WriteBool("Use24Volts", value);
                WriteString("MountVoltage", value ? "24V" : "12V");
            }
        }

        // Existing settings
        public double GuideRateRA { get { return ReadDouble("GuideRateRA", 0.5); } set { WriteDouble("GuideRateRA", value); } }
        public double GuideRateDec { get { return ReadDouble("GuideRateDec", 0.5); } set { WriteDouble("GuideRateDec", value); } }
        public double SiteLatitude { get { return ReadDouble("SiteLatitude", 0); } set { WriteDouble("SiteLatitude", value); } }
        public double SiteLongitude { get { return ReadDouble("SiteLongitude", 0); } set { WriteDouble("SiteLongitude", value); } }
        public double SiteElevation { get { return ReadDouble("SiteElevation", 0); } set { WriteDouble("SiteElevation", value); } }

        public bool KeepLastSync { get { return ReadBool("KeepLastSync", true); } set { WriteBool("KeepLastSync", value); } }
        public double SyncOffsetRA { get { return ReadDouble("SyncOffsetRA", 0.0); } set { WriteDouble("SyncOffsetRA", value); } }
        public double SyncOffsetDec { get { return ReadDouble("SyncOffsetDec", 0.0); } set { WriteDouble("SyncOffsetDec", value); } }
        public bool ParkCurrentPosition { get { return ReadBool("ParkCurrentPosition", true); } set { WriteBool("ParkCurrentPosition", value); } }
        public double ParkAltitude { get { return ReadDouble("ParkAltitude", 0); } set { WriteDouble("ParkAltitude", value); } }
        public double ParkAzimuth { get { return ReadDouble("ParkAzimuth", 0); } set { WriteDouble("ParkAzimuth", value); } }
        public bool UnparkOnReconnect { get { return ReadBool("UnparkOnReconnect", false); } set { WriteBool("UnparkOnReconnect", value); } }
        public bool SendRate { get { return ReadBool("SendRate", false); } set { WriteBool("SendRate", value); } }
        public string Orientation { get { return ReadString("Orientation", "CounterweightDown"); } set { WriteString("Orientation", value); } }
        public bool AskAtStart { get { return ReadBool("AskAtStart", false); } set { WriteBool("AskAtStart", value); } }
        public double Aperture { get { return ReadDouble("Aperture", 0); } set { WriteDouble("Aperture", value); } }
        public double CentralObstruction { get { return ReadDouble("CentralObstruction", 0); } set { WriteDouble("CentralObstruction", value); } }
        public double FocalLength { get { return ReadDouble("FocalLength", 0); } set { WriteDouble("FocalLength", value); } }
        public bool HighPrecisionGoto { get { return ReadBool("HighPrecisionGoto", false); } set { WriteBool("HighPrecisionGoto", value); } }
        public bool TrackingOffOnConnect { get { return ReadBool("TrackingOffOnConnect", false); } set { WriteBool("TrackingOffOnConnect", value); } }
        public bool WarnBeforeMeridianFlip { get { return ReadBool("WarnBeforeMeridianFlip", false); } set { WriteBool("WarnBeforeMeridianFlip", value); } }
        public short SlewSettleTime { get { return ReadShort("SlewSettleTime", 0); } set { WriteShort("SlewSettleTime", value); } }
        public bool IsParked { get { return ReadBool("IsParked", false); } set { WriteBool("IsParked", value); } }
    }
}
