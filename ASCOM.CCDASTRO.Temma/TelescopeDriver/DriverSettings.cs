using ASCOM.Utilities;
using System.Globalization;

namespace ASCOM.CCDASTROTemma.Telescope
{
    public class DriverSettings
    {
        private readonly string progId;
        public DriverSettings(string progId) { this.progId = progId; }

        private string ReadString(string name, string def)
        {
            using (var p = new Profile()) { p.DeviceType = "Telescope"; return p.GetValue(progId, name, string.Empty, def); }
        }
        private void WriteString(string name, string value)
        {
            using (var p = new Profile()) { p.DeviceType = "Telescope"; p.WriteValue(progId, name, value ?? string.Empty); }
        }
        private bool ReadBool(string name, bool def) => bool.TryParse(ReadString(name, def.ToString()), out bool v) ? v : def;
        private void WriteBool(string name, bool v) => WriteString(name, v.ToString());
        private double ReadDouble(string name, double def) => double.TryParse(ReadString(name, def.ToString(CultureInfo.InvariantCulture)), NumberStyles.Float, CultureInfo.InvariantCulture, out double v) ? v : def;
        private void WriteDouble(string name, double v) => WriteString(name, v.ToString(CultureInfo.InvariantCulture));
        private short ReadShort(string name, short def) => short.TryParse(ReadString(name, def.ToString()), out short v) ? v : def;
        private void WriteShort(string name, short v) => WriteString(name, v.ToString());

        public string ComPort { get => ReadString("ComPort", "COM1"); set => WriteString("ComPort", value); }
        public string MountModel { get => ReadString("MountModel", "Temma2"); set => WriteString("MountModel", value); }
        public string TrackingRate { get => ReadString("TrackingRate", "Sidereal"); set => WriteString("TrackingRate", value); }
        public string MountVoltage { get => ReadString("MountVoltage", "12V"); set => WriteString("MountVoltage", value); }
        public bool TraceEnabled { get => ReadBool("TraceEnabled", false); set => WriteBool("TraceEnabled", value); }

        public double GuideRateRA { get => ReadDouble("GuideRateRA", 0.5); set => WriteDouble("GuideRateRA", value); }
        public double GuideRateDec { get => ReadDouble("GuideRateDec", 0.5); set => WriteDouble("GuideRateDec", value); }
        public double SiteLatitude { get => ReadDouble("SiteLatitude", 0); set => WriteDouble("SiteLatitude", value); }
        public double SiteLongitude { get => ReadDouble("SiteLongitude", 0); set => WriteDouble("SiteLongitude", value); }
        public double SiteElevation { get => ReadDouble("SiteElevation", 0); set => WriteDouble("SiteElevation", value); }
        
        public bool KeepLastSync
        {
            get { return ReadBool("KeepLastSync", true); }
            set { WriteBool("KeepLastSync", value); }
        }

        public double SyncOffsetRA
        {
            get { return ReadDouble("SyncOffsetRA", 0.0); }
            set { WriteDouble("SyncOffsetRA", value); }
        }

        public double SyncOffsetDec
        {
            get { return ReadDouble("SyncOffsetDec", 0.0); }
            set { WriteDouble("SyncOffsetDec", value); }
        }

        public bool ParkCurrentPosition
        {
            get { return ReadBool("ParkCurrentPosition", true); }
            set { WriteBool("ParkCurrentPosition", value); }
        }

        public double ParkAltitude { get => ReadDouble("ParkAltitude", 0); set => WriteDouble("ParkAltitude", value); }
        public double ParkAzimuth { get => ReadDouble("ParkAzimuth", 0); set => WriteDouble("ParkAzimuth", value); }
        
        public bool UnparkOnReconnect { get => ReadBool("UnparkOnReconnect", false); set => WriteBool("UnparkOnReconnect", value); }
        public bool SendRate { get => ReadBool("SendRate", false); set => WriteBool("SendRate", value); }

        public string Orientation { get => ReadString("Orientation", "CounterweightDown"); set => WriteString("Orientation", value); }
        
        public bool AskAtStart { get => ReadBool("AskAtStart", false); set => WriteBool("AskAtStart", value); }

        public double Aperture { get => ReadDouble("Aperture", 0); set => WriteDouble("Aperture", value); }
        public double CentralObstruction { get => ReadDouble("CentralObstruction", 0); set => WriteDouble("CentralObstruction", value); }
        public double FocalLength { get => ReadDouble("FocalLength", 0); set => WriteDouble("FocalLength", value); }

        public bool HighPrecisionGoto { get => ReadBool("HighPrecisionGoto", false); set => WriteBool("HighPrecisionGoto", value); }
        public bool TrackingOffOnConnect { get => ReadBool("TrackingOffOnConnect", false); set => WriteBool("TrackingOffOnConnect", value); }
        public bool WarnBeforeMeridianFlip { get => ReadBool("WarnBeforeMeridianFlip", false); set => WriteBool("WarnBeforeMeridianFlip", value); }

        public short SlewSettleTime { get => ReadShort("SlewSettleTime", 0); set => WriteShort("SlewSettleTime", value); }
        public bool IsParked { get => ReadBool("IsParked", false); set => WriteBool("IsParked", value); }
    }
}
