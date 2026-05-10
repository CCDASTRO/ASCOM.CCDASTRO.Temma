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
    [ServedClassName("CCDAstro Temma Telescope Driver")]
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

        private double currentRightAscension;
        private double currentDeclination;
        private DateTime lastCoordinateUpdate = DateTime.MinValue;

        #region ASCOM Registration

        private const string RegistrationProgId = "ASCOM.CCDASTROTemma.Telescope";
        private const string RegistrationDescription = "CCDAstro Temma Telescope Driver";

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
                        if (serial.Connected) serial.Connected = false;
                    }
                    catch { }

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
        }

        #region ITelescopeV4 additions

        public void Connect() { Connected = true; }
        public void Disconnect() { Connected = false; }
        public bool Connecting => false;
        public IStateValueCollection DeviceState => new StateValueCollection();

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

        public ArrayList SupportedActions => new ArrayList();

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
                if (value == connectedState) return;

                if (value)
                    ConnectToMount();
                else
                    DisconnectFromMount();

                connectedState = value;
            }
        }

        public string Description => RegistrationDescription;
        public string DriverInfo => "Takahashi Temma Telescope Driver (C# port)";
        public string DriverVersion => "1.0.0";
        public short InterfaceVersion => 4;
        public string Name => "Takahashi Temma";

        #endregion

        #region Telescope Properties

        public AlignmentModes AlignmentMode => AlignmentModes.algGermanPolar;

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

        public void SlewToTarget() => SlewToCoordinates(TargetRightAscension, TargetDeclination);
        public void SlewToTargetAsync() => SlewToCoordinatesAsync(TargetRightAscension, TargetDeclination);

        public void SyncToCoordinates(double rightAscension, double declination)
        {
            CheckConnected("SyncToCoordinates");

            TargetRightAscension = rightAscension;
            TargetDeclination = declination;

            SendCommand(TemmaProtocol.BuildSyncCommand(rightAscension, declination));

            currentRightAscension = rightAscension;
            currentDeclination = declination;
        }

        public void SyncToTarget() => SyncToCoordinates(TargetRightAscension, TargetDeclination);

        #endregion

        #region Connection Helpers

        private void ConnectToMount()
        {
            serial.PortName = settings.ComPort;
            serial.Speed = SerialSpeed.ps19200;
            serial.Connected = true;

            tracking = !settings.TrackingOffOnConnect;
            isSlewing = false;
            lastCoordinateUpdate = DateTime.MinValue;

            if (isParked && settings.UnparkOnReconnect)
            {
                isParked = false;
                settings.IsParked = false;
            }

            if (settings.SendRate)
            {
                // Placeholder for future guide rate transmission to mount.
            }

            LogMessage("Connect", "Connected to " + settings.ComPort);
        }

        private void DisconnectFromMount()
        {
            if (serial != null && serial.Connected)
                serial.Connected = false;

            tracking = false;
            isSlewing = false;

            LogMessage("Disconnect", "Disconnected");
        }

        private string SendCommand(string command, bool raw = false)
        {
            CheckConnected("SendCommand");

            LogMessage("TX", command);
            serial.Transmit(command);
            string response = serial.ReceiveTerminated("#");
            LogMessage("RX", response);

            return response;
        }

        private void UpdateCoordinates()
        {
            if (!connectedState) return;
            if ((DateTime.UtcNow - lastCoordinateUpdate).TotalSeconds < 1.0) return;

            try
            {
                string response = SendCommand(TemmaProtocol.BuildCoordinateQueryCommand());

                double ra, dec;
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

        public double Altitude => 0.0;
        public double ApertureArea => 0.0;
        public double ApertureDiameter => settings.Aperture;
        public bool AtHome => false;
        public bool AtPark => isParked;
        public IAxisRates AxisRates(TelescopeAxes axis) => new AxisRates(axis);
        public double Azimuth => 0.0;
        public bool CanFindHome => false;
        public bool CanMoveAxis(TelescopeAxes axis) => false;
        public bool CanPark => true;
        public bool CanPulseGuide => true;
        public bool CanSetDeclinationRate => false;
        public bool CanSetGuideRates => true;
        public bool CanSetPark => true;
        public bool CanSetPierSide => false;
        public bool CanSetRightAscensionRate => false;
        public bool CanSetTracking => true;
        public bool CanSlew => true;
        public bool CanSlewAltAz => false;
        public bool CanSlewAltAzAsync => false;
        public bool CanSlewAsync => true;
        public bool CanSync => true;
        public bool CanSyncAltAz => false;
        public bool CanUnpark => true;
        public double DeclinationRate { get => 0.0; set { } }
        public PierSide DestinationSideOfPier(double rightAscension, double declination) => PierSide.pierUnknown;
        public bool DoesRefraction { get => false; set { } }
        public EquatorialCoordinateType EquatorialSystem => EquatorialCoordinateType.equTopocentric;
        public void FindHome() { }
        public double FocalLength => settings.FocalLength;
        public double GuideRateDeclination { get => settings.GuideRateDec; set => settings.GuideRateDec = value; }
        public double GuideRateRightAscension { get => settings.GuideRateRA; set => settings.GuideRateRA = value; }
        public bool IsPulseGuiding => pulseGuiding;
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

        public double RightAscensionRate { get => 0.0; set { } }
        public void SetPark() { }
        public PierSide SideOfPier { get => PierSide.pierUnknown; set { } }
        public double SiteElevation { get => settings.SiteElevation; set => settings.SiteElevation = value; }
        public double SiteLatitude { get => settings.SiteLatitude; set => settings.SiteLatitude = value; }
        public double SiteLongitude { get => settings.SiteLongitude; set => settings.SiteLongitude = value; }
        public short SlewSettleTime { get => settings.SlewSettleTime; set => settings.SlewSettleTime = value; }
        public void SlewToAltAz(double azimuth, double altitude) { }
        public void SlewToAltAzAsync(double azimuth, double altitude) { }
        public void SyncToAltAz(double azimuth, double altitude) { }
        public DriveRates TrackingRate { get => DriveRates.driveSidereal; set { } }
        public ITrackingRates TrackingRates => new TrackingRates();
        public DateTime UTCDate { get => DateTime.UtcNow; set { } }

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
