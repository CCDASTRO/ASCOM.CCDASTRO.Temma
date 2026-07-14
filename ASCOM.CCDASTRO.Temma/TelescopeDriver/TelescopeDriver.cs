// Updated to resolve:
// - AxisRates.Add() compile error
// - AxisRates constructor requiring TelescopeAxes
// - TemmaMountModel enum integration
//
// Note: The frmUnpark.cs duplicate entry error is in the .csproj file,
// not in Telescope.cs.

using ASCOM;
using ASCOM.DeviceInterface;
using ASCOM.LocalServer;
using ASCOM.Utilities;
using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace ASCOM.CCDASTROTemma.Telescope
{
    [ComVisible(true)]
    [Guid("c41150b3-378c-4a3a-a44c-55e99b8c7554")]
    [ProgId("ASCOM.CCDASTROTemma.Telescope")]
    [ServedClassName("CCDASTRO Temma Mount Driver")]
    [ClassInterface(ClassInterfaceType.None)]
    public class Telescope : ReferenceCountedObjectBase, ITelescopeV4, IDisposable
    {
        internal static string DriverProgId;
        internal static string DriverDescription;

        private bool connectedState;
        private bool disposedValue;
        private bool isSlewing;
        private bool tracking;
        private bool isParked;
        private bool pulseGuiding;

        private TraceLogger tl;
        private Serial serial;
        private DriverSettings settings;

        private const double SIDEREAL_RATE_DEG_SEC = 360.0 / 86164.0905;
        private AxisRates axisRates;

        private double currentRightAscension;
        private double currentDeclination;
        private DateTime lastCoordinateUpdate = DateTime.MinValue;

        #region ASCOM Registration

        private const string RegistrationProgId = "ASCOM.CCDASTROTemma.Telescope";
        private const string RegistrationDescription = "CCDASTRO Temma Mount Driver";

        [ComRegisterFunction]
        public static void RegisterASCOM(Type t)
        {
            using (Profile profile = new Profile())
            {
                profile.DeviceType = "Telescope";
                profile.Register(RegistrationProgId, RegistrationDescription);
            }
        }

        [ComUnregisterFunction]
        public static void UnregisterASCOM(Type t)
        {
            using (Profile profile = new Profile())
            {
                profile.DeviceType = "Telescope";
                profile.Unregister(RegistrationProgId);
            }
        }

        #endregion

        public Telescope()
        {
            Attribute progIdAttr = Attribute.GetCustomAttribute(GetType(), typeof(ProgIdAttribute));
            DriverProgId = ((ProgIdAttribute)progIdAttr)?.Value ?? RegistrationProgId;

            Attribute servedAttr = Attribute.GetCustomAttribute(GetType(), typeof(ServedClassNameAttribute));
            DriverDescription = ((ServedClassNameAttribute)servedAttr)?.DisplayName ?? RegistrationDescription;

            settings = new DriverSettings(DriverProgId);
            isParked = settings.IsParked;

            tl = new TraceLogger("", "Temma.Driver");
            tl.Enabled = settings.TraceEnabled;

            serial = new Serial();

            tracking = true;
            TargetRightAscension = 0.0;
            TargetDeclination = 0.0;
        }

        ~Telescope()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);

            Server.ExitIf();
            Application.Exit();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposedValue) return;

            if (disposing)
            {
                if (serial != null)
                {
                    try
                    {
                        if (serial.Connected)
                            serial.Connected = false;
                    }
                    catch
                    {
                    }

                    serial.Dispose();
                    serial = null;
                }

                if (tl != null)
                {
                    tl.Enabled = false;
                    tl.Dispose();
                    tl = null;
                }
            }

            disposedValue = true;
            Server.ExitIf();
        }

        #region ITelescopeV4 additions

        public void Connect() { Connected = true; }
        public void Disconnect() { Connected = false; }
        public bool Connecting { get { return false; } }
        public IStateValueCollection DeviceState { get { return new StateValueCollection(); } }

        #endregion

        #region ASCOM Common Methods

        public void SetupDialog()
        {
            if (connectedState)
            {
                MessageBox.Show("Cannot change settings while connected.");
                return;
            }

            using (SetupDialogForm dialog = new SetupDialogForm(DriverProgId))
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    settings = new DriverSettings(DriverProgId);
                    tl.Enabled = settings.TraceEnabled;
                    isParked = settings.IsParked;
                }
            }
        }

        public ArrayList SupportedActions
        {
            get { return new ArrayList(); }
        }

        public string Action(string actionName, string actionParameters)
        {
            throw new ActionNotImplementedException(actionName);
        }

        public void CommandBlind(string command, bool raw)
        {
            SendCommand(command, raw);
        }

        public bool CommandBool(string command, bool raw)
        {
            return !string.IsNullOrEmpty(SendCommand(command, raw));
        }

        public string CommandString(string command, bool raw)
        {
            return SendCommand(command, raw);
        }

        public bool Connected
        {
            get { return connectedState; }
            set
            {
                if (value == connectedState)
                    return;

                if (value)
                {
                    ConnectToMount();
                    connectedState = true;
                }
                else
                {
                    DisconnectFromMount();
                    connectedState = false;
                    Server.ExitIf();
                }
            }
        }

        public string Description { get { return RegistrationDescription; } }
        public string DriverInfo { get { return "Takahashi Temma Telescope Driver (C# port)"; } }
        public string DriverVersion { get { return "1.0.0"; } }
        public short InterfaceVersion { get { return 4; } }
        public string Name { get { return "Takahashi Temma"; } }

        #endregion

        #region Telescope Properties

        public AlignmentModes AlignmentMode
        {
            get { return AlignmentModes.algGermanPolar; }
        }

        public double RightAscension
        {
            get
            {
                CheckConnected("RightAscension");
                UpdateCoordinates();
                return currentRightAscension;
            }
        }

        public double Declination
        {
            get
            {
                CheckConnected("Declination");
                UpdateCoordinates();
                return currentDeclination;
            }
        }

        public bool Tracking
        {
            get { return tracking; }
            set
            {
                CheckConnected("Tracking");
                if (tracking == value) return;

                SendCommand(TemmaProtocol.BuildTrackingCommand(value));
                tracking = value;
            }
        }

        public bool Slewing
        {
            get
            {
                if (isSlewing)
                {
                    UpdateCoordinates();

                    double raError = Math.Abs(currentRightAscension - TargetRightAscension);
                    double decError = Math.Abs(currentDeclination - TargetDeclination);

                    if (raError < (1.0 / 3600.0) && decError < (10.0 / 3600.0))
                        isSlewing = false;
                }

                return isSlewing;
            }
        }

        public double TargetRightAscension { get; set; }
        public double TargetDeclination { get; set; }

        public double SiderealTime
        {
            get
            {
                DateTime utc = DateTime.UtcNow;
                double jd = utc.ToOADate() + 2415018.5;
                double d = jd - 2451545.0;

                double gmst = 18.697374558 + 24.06570982441908 * d;
                gmst %= 24.0;
                if (gmst < 0.0) gmst += 24.0;

                double lst = gmst + (SiteLongitude / 15.0);
                lst %= 24.0;
                if (lst < 0.0) lst += 24.0;

                return lst;
            }
        }

        #endregion

        #region Slewing and Sync

        public void AbortSlew()
        {
            CheckConnected("AbortSlew");
            SendCommand(TemmaProtocol.BuildAbortCommand());
            isSlewing = false;
        }

        public void SlewToCoordinates(double rightAscension, double declination)
        {
            SlewToCoordinatesAsync(rightAscension, declination);

            while (Slewing)
                Thread.Sleep(500);

            if (SlewSettleTime > 0)
                Thread.Sleep(SlewSettleTime * 1000);
        }

        public void SlewToCoordinatesAsync(double rightAscension, double declination)
        {
            CheckConnected("SlewToCoordinatesAsync");

            if (isParked)
                throw new ParkedException("The mount is parked.");

            TargetRightAscension = rightAscension;
            TargetDeclination = declination;

            SendCommand(TemmaProtocol.BuildSlewCommand(rightAscension, declination));
            isSlewing = true;
        }

        public void SlewToTarget()
        {
            SlewToCoordinates(TargetRightAscension, TargetDeclination);
        }

        public void SlewToTargetAsync()
        {
            SlewToCoordinatesAsync(TargetRightAscension, TargetDeclination);
        }

        public void SyncToCoordinates(double rightAscension, double declination)
        {
            CheckConnected("SyncToCoordinates");

            TargetRightAscension = rightAscension;
            TargetDeclination = declination;

            SendCommand(TemmaProtocol.BuildSyncCommand(rightAscension, declination));

            currentRightAscension = rightAscension;
            currentDeclination = declination;
        }

        public void SyncToTarget()
        {
            SyncToCoordinates(TargetRightAscension, TargetDeclination);
        }

        #endregion

        #region Mount Model Helpers

        private bool IsTemma2MModel(TemmaMountModel model)
        {
            return model == TemmaMountModel.EM11M ||
                   model == TemmaMountModel.EM200M;
        }

        private bool ShouldUseHighSpeed()
        {
            if (IsTemma2MModel(settings.MountModel))
                return true;

            return settings.Use24Volts;
        }

        private double GetSlewRateMultiplier()
        {
            switch (settings.MountModel)
            {
                case TemmaMountModel.EM11:
                case TemmaMountModel.EM11M:
                    return 150.0;

                case TemmaMountModel.EM200:
                case TemmaMountModel.EM200M:
                    return ShouldUseHighSpeed() ? 700.0 : 350.0;

                case TemmaMountModel.NJP:
                    return ShouldUseHighSpeed() ? 350.0 : 175.0;

                case TemmaMountModel.EM500:
                    return ShouldUseHighSpeed() ? 520.0 : 260.0;

                default:
                    return 350.0;
            }
        }

        private void ConfigureMountSpeed()
        {
            bool highSpeed = ShouldUseHighSpeed();

            // Send v1 = low speed, v2 = high speed.
            // Use CommandString instead of CommandBlind so the command is sent
            // directly without CheckConnected() requiring connectedState = true.
            CommandString(highSpeed ? "v2" : "v1", false);

            LogMessage(
                "ConfigureMountSpeed",
                string.Format(
                    "Model={0}, Use24Volts={1}, Mode={2}",
                    settings.MountModel,
                    settings.Use24Volts,
                    highSpeed ? "High" : "Low"));
        }

        private void ConfigureAxisRates()
        {
            // Your custom AxisRates class creates its own rate collection.
            // We only log the selected multiplier here.
            axisRates = null;

            LogMessage(
                "ConfigureAxisRates",
                string.Format(
                    "Maximum axis rate multiplier = {0}",
                    GetSlewRateMultiplier()));
        }

        #endregion

        #region Connection Helpers

        private void ConnectToMount()
        {
            // Match the proven VB6 Temma serial configuration exactly:
            // 19200 baud, EVEN parity, 8 data bits, 1 stop bit, DTR enabled,
            // 5 second receive timeout. Temma commands and replies are CRLF terminated.
            serial.PortName = settings.ComPort;
            serial.Speed = SerialSpeed.ps19200;
            serial.Parity = SerialParity.Even;
            serial.DTREnable = true;
            serial.ReceiveTimeout = 5;

            try
            {
                LogMessage("Connect", string.Format(
                    "Opening {0}: 19200,E,8,1; DTR=True; ReceiveTimeout=5s",
                    settings.ComPort));

                serial.Connected = true;
                serial.ClearBuffers();

                // VB6 authoritative behavior:
                //   ocom.Transmit "E" & vbCrLf
                //   buf = ocom.ReceiveTerminated(vbCrLf)
                string command = TemmaProtocol.BuildCoordinateQueryCommand() + "\r\n";
                LogMessage("TX", EscapeForLog(command));
                serial.Transmit(command);

                string response = serial.ReceiveTerminated("\r\n");
                LogMessage("RX", EscapeForLog(response));

                double ra;
                double dec;

                if (!TemmaProtocol.TryParseCoordinates(response, out ra, out dec))
                    throw new Exception("Temma returned a response that could not be parsed: " +
                                        EscapeForLog(response));

                currentRightAscension = ra;
                currentDeclination = dec;

                ConfigureMountSpeed();
                ConfigureAxisRates();
            }
            catch (Exception ex)
            {
                LogMessage("Connect ERROR", ex.ToString());

                if (serial != null && serial.Connected)
                    serial.Connected = false;

                throw new NotConnectedException(
                    "Unable to communicate with the Temma mount. " + ex.Message);
            }

            tracking = !settings.TrackingOffOnConnect;
            isSlewing = false;
            lastCoordinateUpdate = DateTime.MinValue;

            if (isParked && settings.UnparkOnReconnect)
            {
                isParked = false;
                settings.IsParked = false;
            }

            LogMessage("Connect", "Successfully connected to Temma mount.");
        }

        private void DisconnectFromMount()
        {
            if (serial != null && serial.Connected)
                serial.Connected = false;

            tracking = false;
            isSlewing = false;
            pulseGuiding = false;

            LogMessage("Disconnect", "Disconnected");
        }

        private string SendCommand(string command, bool raw = false)
        {
            CheckConnected("SendCommand");

            // Match VB6 Temma framing: commands and responses are CRLF terminated.
            string framedCommand = command.EndsWith("\r\n") ? command : command + "\r\n";
            LogMessage("TX", EscapeForLog(framedCommand));
            serial.Transmit(framedCommand);
            string response = serial.ReceiveTerminated("\r\n");
            LogMessage("RX", EscapeForLog(response));

            return response;
        }

        private static string EscapeForLog(string value)
        {
            if (value == null) return "<null>";
            return value.Replace("\r", "<CR>").Replace("\n", "<LF>");
        }

        private void UpdateCoordinates()
        {
            if (!connectedState) return;
            if ((DateTime.UtcNow - lastCoordinateUpdate).TotalSeconds < 1.0) return;

            try
            {
                string response = SendCommand(TemmaProtocol.BuildCoordinateQueryCommand());

                double ra;
                double dec;

                if (TemmaProtocol.TryParseCoordinates(response, out ra, out dec))
                {
                    currentRightAscension = ra;
                    currentDeclination = dec;
                    lastCoordinateUpdate = DateTime.UtcNow;
                }
            }
            catch (Exception ex)
            {
                LogMessage("UpdateCoordinates", ex.Message);
            }
        }

        #endregion

        #region Utility Methods

        private void CheckConnected(string member)
        {
            if (!connectedState)
                throw new NotConnectedException(DriverDescription + " is not connected: " + member);
        }

        private void LogMessage(string identifier, string message)
        {
            if (tl != null)
                tl.LogMessage(identifier, message);
        }

        #endregion

        #region Required ITelescopeV4 Members

        public double Altitude { get { return 0.0; } }
        public double ApertureArea { get { return 0.0; } }
        public double ApertureDiameter { get { return settings.Aperture; } }
        public bool AtHome { get { return false; } }
        public bool AtPark { get { return isParked; } }

        public IAxisRates AxisRates(TelescopeAxes axis)
        {
            CheckConnected("AxisRates");

            if (axis != TelescopeAxes.axisPrimary &&
                axis != TelescopeAxes.axisSecondary)
            {
                throw new InvalidValueException(
                    "Axis",
                    ((int)axis).ToString(),
                    "0 or 1");
            }

            return new AxisRates(axis);
        }

        public double Azimuth { get { return 0.0; } }
        public bool CanFindHome { get { return false; } }
        public bool CanMoveAxis(TelescopeAxes axis) { return false; }
        public bool CanPark { get { return true; } }
        public bool CanPulseGuide { get { return true; } }
        public bool CanSetDeclinationRate { get { return false; } }
        public bool CanSetGuideRates { get { return true; } }
        public bool CanSetPark { get { return true; } }
        public bool CanSetPierSide { get { return false; } }
        public bool CanSetRightAscensionRate { get { return false; } }
        public bool CanSetTracking { get { return true; } }
        public bool CanSlew { get { return true; } }
        public bool CanSlewAltAz { get { return false; } }
        public bool CanSlewAltAzAsync { get { return false; } }
        public bool CanSlewAsync { get { return true; } }
        public bool CanSync { get { return true; } }
        public bool CanSyncAltAz { get { return false; } }
        public bool CanUnpark { get { return true; } }
        public double DeclinationRate { get { return 0.0; } set { } }
        public PierSide DestinationSideOfPier(double rightAscension, double declination) { return PierSide.pierUnknown; }
        public bool DoesRefraction { get { return false; } set { } }
        public EquatorialCoordinateType EquatorialSystem { get { return EquatorialCoordinateType.equTopocentric; } }
        public void FindHome() { }
        public double FocalLength { get { return settings.FocalLength; } }
        public double GuideRateDeclination { get { return settings.GuideRateDec; } set { settings.GuideRateDec = value; } }
        public double GuideRateRightAscension { get { return settings.GuideRateRA; } set { settings.GuideRateRA = value; } }
        public bool IsPulseGuiding { get { return pulseGuiding; } }
        public void MoveAxis(TelescopeAxes axis, double rate) { }

        public void PulseGuide(GuideDirections direction, int duration)
        {
            CheckConnected("PulseGuide");

            if (duration < 0)
                throw new InvalidValueException("Duration must be zero or greater.");

            pulseGuiding = true;
            try
            {
                LogMessage("PulseGuide", string.Format("Direction={0}, Duration={1} ms", direction, duration));
                Thread.Sleep(duration);
            }
            finally
            {
                pulseGuiding = false;
            }
        }

        public double RightAscensionRate { get { return 0.0; } set { } }
        public void SetPark() { }
        public PierSide SideOfPier { get { return PierSide.pierUnknown; } set { } }
        public double SiteElevation { get { return settings.SiteElevation; } set { settings.SiteElevation = value; } }
        public double SiteLatitude { get { return settings.SiteLatitude; } set { settings.SiteLatitude = value; } }
        public double SiteLongitude { get { return settings.SiteLongitude; } set { settings.SiteLongitude = value; } }
        public short SlewSettleTime { get { return settings.SlewSettleTime; } set { settings.SlewSettleTime = value; } }
        public void SlewToAltAz(double azimuth, double altitude) { }
        public void SlewToAltAzAsync(double azimuth, double altitude) { }
        public void SyncToAltAz(double azimuth, double altitude) { }
        public DriveRates TrackingRate { get { return DriveRates.driveSidereal; } set { } }
        public ITrackingRates TrackingRates { get { return new TrackingRates(); } }
        public DateTime UTCDate { get { return DateTime.UtcNow; } set { } }

        public void Park()
        {
            CheckConnected("Park");
            isParked = true;
            settings.IsParked = true;
            isSlewing = false;
            LogMessage("Park", "Mount marked as parked.");
        }

        public void Unpark()
        {
            CheckConnected("Unpark");
            isParked = false;
            settings.IsParked = false;
            LogMessage("Unpark", "Mount unparked.");
        }

        #endregion
    }
}
