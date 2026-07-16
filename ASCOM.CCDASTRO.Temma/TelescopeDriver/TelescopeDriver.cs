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
using System.Threading.Tasks;
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
        private bool connectionInitializing;
        private bool disposedValue;
        private bool isSlewing;
        private bool tracking;
        private bool isParked;
        private bool pulseGuiding;

        private int slewVerificationGeneration = 0;
        private readonly object slewVerificationLock = new object();
        private const int SlewStartVerificationDelayMs = 2500;
        private const int SlewStartMaximumAttempts = 5;
        private const double SlewStartRaMovementThresholdHours = 0.0002;
        private const double SlewStartDecMovementThresholdDegrees = 0.002;

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
        public string DriverVersion
        {
            get { return typeof(Telescope).Assembly.GetName().Version.ToString(3); }
        }
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
                    if (raError > 12.0) raError = 24.0 - raError;
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
            CancelSlewStartVerification();
            SendBlindTemmaCommand(TemmaProtocol.BuildAbortCommand());
            isSlewing = false;
            Thread.Sleep(500);
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
            if (isParked) throw new ParkedException("The mount is parked.");
            if (isSlewing) return;
            if (rightAscension < 0.0 || rightAscension >= 24.0)
                throw new InvalidValueException("RightAscension", rightAscension.ToString(), "0 to less than 24 hours");
            if (declination < -90.0 || declination > 90.0)
                throw new InvalidValueException("Declination", declination.ToString(), "-90 to +90 degrees");

            double startRa = currentRightAscension;
            double startDec = currentDeclination;
            TargetRightAscension = rightAscension;
            TargetDeclination = declination;

            CancelSlewStartVerification();
            SendTemmaGotoOrThrow(rightAscension, declination);
            isSlewing = true;

            int generation;
            lock (slewVerificationLock)
                generation = ++slewVerificationGeneration;

            Task.Run(() => VerifySlewStartedAsync(
                generation, startRa, startDec, rightAscension, declination));
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

            if (isParked)
                throw new ParkedException("The mount is parked.");

            if (rightAscension < 0.0 || rightAscension >= 24.0)
                throw new InvalidValueException(
                    "RightAscension", rightAscension.ToString(), "0 to less than 24 hours");

            if (declination < -90.0 || declination > 90.0)
                throw new InvalidValueException(
                    "Declination", declination.ToString(), "-90 to +90 degrees");

            // Match the proven VB6 SyncToCoordinates sequence:
            // Clear buffers, send T + LST twice with 100 ms delays,
            // then blindly transmit the D coordinate command.
            string lst = FormatTemmaSiderealTime(SiderealTime);

            serial.ClearBuffers();

            SendBlindTemmaCommand("T" + lst);
            System.Threading.Thread.Sleep(100);

            SendBlindTemmaCommand("T" + lst);
            System.Threading.Thread.Sleep(100);

            // Build the Temma Sync (D) command.
            string syncCommand = TemmaProtocol.BuildSyncCommand(
                rightAscension, declination);

            string framedSyncCommand = syncCommand.EndsWith("\r\n")
                ? syncCommand
                : syncCommand + "\r\n";

            LogMessage("Sync TX", EscapeForLog(framedSyncCommand));
            serial.Transmit(framedSyncCommand);

            serial.ClearBuffers();
            System.Threading.Thread.Sleep(100);

            TargetRightAscension = rightAscension;
            TargetDeclination = declination;



            LogMessage("Sync", string.Format(
               "Sync command sent. Target RA={0:F6} h, Dec={1:F6} deg",
               rightAscension, declination));
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

            connectionInitializing = true;

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

                // A parked mount's physical position is restored by Unpark()
                // from its saved Alt/Az reference. Do not overwrite that
                // reference with normal power-on synchronization.
                if (!isParked)
                    ApplyInitialMountSynchronization();
                else
                    LogMessage("Connect", "Mount is parked; startup synchronization deferred until Unpark.");

                ConfigureMountSpeed();
                ConfigureAxisRates();

                connectedState = true;
                LogMessage("Connect", "Temma initialization completed successfully.");
            }
            catch (Exception ex)
            {
                connectedState = false;
                LogMessage("Connect ERROR", ex.ToString());

                if (serial != null && serial.Connected)
                    serial.Connected = false;

                throw new NotConnectedException(
                    "Unable to communicate with the Temma mount. " + ex.Message);
            }
            finally
            {
                connectionInitializing = false;
            }

            // A persisted parked mount was explicitly stopped by Park(). Keep
            // the software state stopped so Unpark can restore tracking with
            // STN-OFF when it was enabled before parking.
            tracking = isParked ? false : !settings.TrackingOffOnConnect;
            isSlewing = false;
            lastCoordinateUpdate = DateTime.MinValue;

            if (isParked && settings.UnparkOnReconnect)
                Unpark();

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

            // Suppress repetitive coordinate-poll logging ("E" command).
            bool logTransaction = !string.Equals(
                command.Trim(),
                TemmaProtocol.BuildCoordinateQueryCommand(),
                StringComparison.OrdinalIgnoreCase);

            if (logTransaction)
                LogMessage("TX", EscapeForLog(framedCommand));

            serial.Transmit(framedCommand);

            string response = serial.ReceiveTerminated("\r\n");

            if (logTransaction)
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

        private void CancelSlewStartVerification()
        {
            lock (slewVerificationLock) slewVerificationGeneration++;
        }

        private bool IsSlewVerificationCurrent(int generation)
        {
            lock (slewVerificationLock) return generation == slewVerificationGeneration;
        }

        private void SendTemmaGotoOrThrow(double rightAscension, double declination)
        {
            string lst = FormatTemmaSiderealTime(SiderealTime);
            string slewCommand = TemmaProtocol.BuildSlewCommand(rightAscension, declination);

            for (int attempt = 0; attempt < 2; attempt++)
            {
                serial.ClearBuffers();

                SendBlindTemmaCommand("T" + lst);
                Thread.Sleep(100);

                string response = SendCommand(slewCommand);
                string status = (response ?? string.Empty).Trim();

                if (status == "R0")
                    return;

                // The original VB6 driver always retried the slew once before failing.
                if (attempt == 0)
                {
                    Thread.Sleep(100);
                    continue;
                }

                switch (status)
                {
                    case "R1":
                        throw new InvalidOperationException("Temma GOTO failed: RA format/error (R1).");

                    case "R2":
                        throw new InvalidOperationException("Temma GOTO failed: Declination format/error (R2).");

                    case "R3":
                        throw new InvalidOperationException("Temma GOTO failed: Too many digits (R3).");

                    case "R4":
                        throw new InvalidOperationException("Temma GOTO failed: Target is below the horizon (R4).");

                    case "R5":
                        throw new InvalidOperationException("Temma GOTO failed: Mount is on standby (R5).");

                    default:
                        throw new InvalidOperationException(
                            "Temma GOTO returned an unexpected response: " +
                            EscapeForLog(response));
                }
            }
        }

        private async Task VerifySlewStartedAsync(int generation, double startRa, double startDec, double targetRa, double targetDec)
        {
            try
            {
                for (int attempt = 1; attempt <= SlewStartMaximumAttempts; attempt++)
                {
                    await Task.Delay(SlewStartVerificationDelayMs).ConfigureAwait(false);

                    if (!IsSlewVerificationCurrent(generation) || !Connected || isParked || !isSlewing)
                        return;

                    double raMoved = Math.Abs(currentRightAscension - startRa);
                    if (raMoved > 12.0) raMoved = 24.0 - raMoved;
                    double decMoved = Math.Abs(currentDeclination - startDec);

                    if (raMoved >= SlewStartRaMovementThresholdHours ||
                        decMoved >= SlewStartDecMovementThresholdDegrees)
                    {
                        LogMessage("SlewVerify", string.Format(
                            "Mount movement confirmed on attempt {0}. RA moved {1:F6} h, Dec moved {2:F6} deg.",
                            attempt, raMoved, decMoved));
                        return;
                    }

                    LogMessage("SlewVerify", string.Format(
                        "No movement after attempt {0}; retrying complete Temma T/P GOTO sequence.", attempt));

                    if (attempt < SlewStartMaximumAttempts)
                    {
                        if (!IsSlewVerificationCurrent(generation)) return;
                        SendTemmaGotoOrThrow(targetRa, targetDec);
                        isSlewing = true;
                    }
                }

                if (!IsSlewVerificationCurrent(generation)) return;
                LogMessage("SlewVerify", "Mount failed to begin moving after 5 accepted GOTO attempts. Sending PS.");
                SendBlindTemmaCommand(TemmaProtocol.BuildAbortCommand());
                isSlewing = false;
            }
            catch (Exception ex)
            {
                if (IsSlewVerificationCurrent(generation))
                {
                    isSlewing = false;
                    LogMessage("SlewVerify", "Slew-start verification failed: " + ex);
                    try { SendBlindTemmaCommand(TemmaProtocol.BuildAbortCommand()); }
                    catch (Exception abortEx) { LogMessage("SlewVerify", "Abort also failed: " + abortEx.Message); }
                }
            }
        }

        private void ApplyInitialMountSynchronization()
        {
            string orientation = settings.Orientation ?? "CounterweightDown";
            string lst = FormatTemmaSiderealTime(SiderealTime);
            string latitude = FormatTemmaLatitude(SiteLatitude);

            LogMessage("InitialSync", "Applying startup orientation: " + orientation);

            serial.ClearBuffers();

            // Always initialize LST and latitude.
            SendBlindTemmaCommand("T" + lst);
            Thread.Sleep(100);

            SendBlindTemmaCommand("I" + latitude);
            Thread.Sleep(100);

            switch (orientation)
            {
                case "OtaEast":
                case "OtaWest":
                    {
                        string e = SendCommand(TemmaProtocol.BuildCoordinateQueryCommand());
                        string trimmed = (e ?? string.Empty).Trim();

                        bool eastState = trimmed.Length > 13 && trimmed[13] == 'E';
                        bool wantEastState = (orientation == "OtaEast");

                        if (eastState != wantEastState)
                        {
                            SendBlindTemmaCommand("PT");
                            Thread.Sleep(100);
                        }

                        // Establish Temma reference exactly as VB6.
                        SendBlindTemmaCommand("T" + lst);
                        Thread.Sleep(100);

                        SendBlindTemmaCommand("Z");
                        Thread.Sleep(100);

                        SendBlindTemmaCommand("D" + lst + latitude);

                        serial.ClearBuffers();
                        Thread.Sleep(100);
                        break;
                    }

                case "CounterweightDown":
                    {
                        EnsureTemmaPierReference();

                        double ra = NormalizeHours(SiderealTime - 6.0);
                        double dec = SiteLatitude >= 0.0
                            ? Math.Min(89.999, SiteLatitude + 0.01)
                            : Math.Max(-89.999, SiteLatitude - 0.01);

                        SyncToCoordinates(ra, dec);
                        break;
                    }

                case "CounterweightWest":
                    {
                        EnsureTemmaPierReference();

                        double ra = NormalizeHours(SiderealTime);
                        double dec = SiteLatitude >= 0.0
                            ? Math.Min(89.999, SiteLatitude + 0.01)
                            : Math.Max(-89.999, SiteLatitude - 0.01);

                        SyncToCoordinates(ra, dec);
                        break;
                    }
            }

            serial.ClearBuffers();

            LogMessage("InitialSync", "Startup synchronization completed.");
        }
        private void EnsureTemmaPierReference()
        {
            string e = SendCommand(TemmaProtocol.BuildCoordinateQueryCommand());
            string trimmed = (e ?? string.Empty).Trim();
            if (trimmed.Length <= 13 || trimmed[13] != 'E')
            {
                SendBlindTemmaCommand("PT");
                Thread.Sleep(200);
            }
        }

        private static double NormalizeHours(double hours)
        {
            while (hours < 0.0) hours += 24.0;
            while (hours >= 24.0) hours -= 24.0;
            return hours;
        }

        private static double NormalizeSignedHours(double hours)
        {
            hours = NormalizeHours(hours);
            return hours > 12.0 ? hours - 24.0 : hours;
        }

        private static double DegreesToRadians(double degrees) { return degrees * Math.PI / 180.0; }
        private static double RadiansToDegrees(double radians) { return radians * 180.0 / Math.PI; }

        // Convert a topocentric equatorial coordinate to the physical mount
        // direction. Azimuth is degrees clockwise from north.
        private void EquatorialToHorizontal(double raHours, double decDegrees,
                                            out double azimuthDegrees, out double altitudeDegrees)
        {
            double latitude = DegreesToRadians(SiteLatitude);
            double declination = DegreesToRadians(decDegrees);
            double hourAngle = DegreesToRadians(NormalizeSignedHours(SiderealTime - raHours) * 15.0);

            double sinAltitude = Math.Sin(declination) * Math.Sin(latitude) +
                                 Math.Cos(declination) * Math.Cos(latitude) * Math.Cos(hourAngle);
            sinAltitude = Math.Max(-1.0, Math.Min(1.0, sinAltitude));
            double altitude = Math.Asin(sinAltitude);

            double azimuth = Math.Atan2(-Math.Sin(hourAngle) * Math.Cos(declination),
                                         Math.Sin(declination) * Math.Cos(latitude) -
                                         Math.Cos(declination) * Math.Sin(latitude) * Math.Cos(hourAngle));
            azimuthDegrees = RadiansToDegrees(azimuth);
            if (azimuthDegrees < 0.0) azimuthDegrees += 360.0;
            altitudeDegrees = RadiansToDegrees(altitude);
        }

        // Reconstruct the sky coordinate for a mount that has remained fixed
        // at a saved physical Alt/Az position while the sky rotates.
        private void HorizontalToEquatorial(double azimuthDegrees, double altitudeDegrees,
                                            out double raHours, out double decDegrees)
        {
            double latitude = DegreesToRadians(SiteLatitude);
            double azimuth = DegreesToRadians(azimuthDegrees);
            double altitude = DegreesToRadians(altitudeDegrees);

            double sinDeclination = Math.Sin(altitude) * Math.Sin(latitude) +
                                    Math.Cos(altitude) * Math.Cos(latitude) * Math.Cos(azimuth);
            sinDeclination = Math.Max(-1.0, Math.Min(1.0, sinDeclination));
            double declination = Math.Asin(sinDeclination);

            double hourAngle = Math.Atan2(-Math.Sin(azimuth) * Math.Cos(altitude),
                                          Math.Sin(altitude) * Math.Cos(latitude) -
                                          Math.Cos(altitude) * Math.Sin(latitude) * Math.Cos(azimuth));
            raHours = NormalizeHours(SiderealTime - RadiansToDegrees(hourAngle) / 15.0);
            decDegrees = RadiansToDegrees(declination);
        }

        private void ReadMountCoordinatesOrThrow()
        {
            string response = SendCommand(TemmaProtocol.BuildCoordinateQueryCommand());
            double ra;
            double dec;
            if (!TemmaProtocol.TryParseCoordinates(response, out ra, out dec))
                throw new InvalidOperationException("Temma returned invalid coordinates: " + EscapeForLog(response));
            currentRightAscension = ra;
            currentDeclination = dec;
            lastCoordinateUpdate = DateTime.UtcNow;
        }

        private PierSide ReadPierSide()
        {
            string response = SendCommand(TemmaProtocol.BuildCoordinateQueryCommand());
            string trimmed = (response ?? string.Empty).Trim();
            if (trimmed.Length > 13)
            {
                if (trimmed[13] == 'E') return PierSide.pierEast;
                if (trimmed[13] == 'W') return PierSide.pierWest;
            }
            return PierSide.pierUnknown;
        }

        private static string FormatTemmaLatitude(double latitudeDegrees)
        {
            string sign = latitudeDegrees < 0.0 ? "-" : "+";
            double value = Math.Abs(latitudeDegrees);
            int degrees = (int)Math.Floor(value);
            double minuteValue = (value - degrees) * 60.0;
            int minutes = (int)Math.Floor(minuteValue);
            int tenths = (int)Math.Floor((minuteValue - minutes) * 10.0);

            if (tenths >= 10) { tenths = 0; minutes++; }
            if (minutes >= 60) { minutes = 0; degrees++; }
            if (degrees > 89) { degrees = 89; minutes = 59; tenths = 9; }

            return string.Format("{0}{1:00}{2:00}{3:0}", sign, degrees, minutes, tenths);
        }

        private void SendBlindTemmaCommand(string command)
        {
            CheckConnected("SendBlindTemmaCommand");

            string framedCommand = command.EndsWith("\r\n")
                ? command
                : command + "\r\n";

            LogMessage("Blind TX", EscapeForLog(framedCommand));
            serial.Transmit(framedCommand);
        }

        private static string FormatTemmaSiderealTime(double siderealHours)
        {
            while (siderealHours < 0.0) siderealHours += 24.0;
            while (siderealHours >= 24.0) siderealHours -= 24.0;

            int totalSeconds = (int)Math.Round(siderealHours * 3600.0) % 86400;
            int hours = totalSeconds / 3600;
            int minutes = (totalSeconds % 3600) / 60;
            int seconds = totalSeconds % 60;

            return string.Format("{0:00}{1:00}{2:00}", hours, minutes, seconds);
        }

        private void CheckConnected(string member)
        {
            // Permit internal startup commands while the physical serial connection
            // is open, but do not publish Connected=true until initialization succeeds.
            if (!connectedState && !connectionInitializing)
                throw new NotConnectedException(DriverDescription + " is not connected: " + member);
        }

        private void LogMessage(string identifier, string message)
        {
            if (tl != null)
                tl.LogMessage(identifier, message);
        }

        #endregion

        #region Required ITelescopeV4 Members

        public double Altitude
        {
            get
            {
                CheckConnected("Altitude");
                UpdateCoordinates();
                double azimuth;
                double altitude;
                EquatorialToHorizontal(currentRightAscension, currentDeclination, out azimuth, out altitude);
                return altitude;
            }
        }
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

        public double Azimuth
        {
            get
            {
                CheckConnected("Azimuth");
                UpdateCoordinates();
                double azimuth;
                double altitude;
                EquatorialToHorizontal(currentRightAscension, currentDeclination, out azimuth, out altitude);
                return azimuth;
            }
        }
        public bool CanFindHome { get { return false; } }
        public bool CanMoveAxis(TelescopeAxes axis) { return axis != TelescopeAxes.axisTertiary; }
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
        public void MoveAxis(TelescopeAxes axis, double rate)
        {
            CheckConnected("MoveAxis");

            if (isParked)
                throw new ParkedException("The mount is parked.");

            if (axis == TelescopeAxes.axisTertiary)
                throw new MethodNotImplementedException("MoveAxis axisTertiary");

            if (rate == 0.0)
            {
                SendBlindTemmaCommand("M@");
                isSlewing = false;
                return;
            }

            // Match the working VB6 direction mapping.
            // Low/high speed selection is based on half of the advertised
            // maximum rate for the selected axis.
            double maxRate = 0.0;
            foreach (IRate r in AxisRates(axis))
                if (r.Maximum > maxRate) maxRate = r.Maximum;

            bool highSpeed = maxRate > 0.0 && Math.Abs(rate) >= (maxRate / 2.0);
            string command;

            if (axis == TelescopeAxes.axisSecondary)
                command = rate > 0.0 ? (highSpeed ? "MI" : "MH")
                                     : (highSpeed ? "MQ" : "MP");
            else
                command = rate > 0.0 ? (highSpeed ? "ME" : "MB")
                                     : (highSpeed ? "MC" : "MD");

            SendBlindTemmaCommand(command);
            isSlewing = true;
        }

        public void PulseGuide(GuideDirections direction, int duration)
        {
            CheckConnected("PulseGuide");

            if (isParked)
                throw new ParkedException("The mount is parked.");

            if (duration < 0)
                throw new InvalidValueException("Duration must be zero or greater.");

            string directionCommand;
            switch (direction)
            {
                case GuideDirections.guideNorth: directionCommand = "MH"; break;
                case GuideDirections.guideSouth: directionCommand = "MP"; break;
                case GuideDirections.guideEast:  directionCommand = "MD"; break;
                case GuideDirections.guideWest:  directionCommand = "MB"; break;
                default:
                    throw new InvalidValueException("Unsupported guide direction.");
            }

            pulseGuiding = true;
            try
            {
                int remaining = Math.Max(duration, 100);

                // The working VB6 driver limits each continuous pulse to 3000 ms.
                while (remaining > 0)
                {
                    int segment = Math.Min(remaining, 3000);
                    SendBlindTemmaCommand(directionCommand);
                    Thread.Sleep(segment);
                    SendBlindTemmaCommand("M@");
                    remaining -= segment;
                }
            }
            finally
            {
                pulseGuiding = false;
            }
        }

        public double RightAscensionRate { get { return 0.0; } set { } }
        public void SetPark()
        {
            CheckConnected("SetPark");
            if (isSlewing) throw new InvalidOperationException("Cannot set the park position while slewing.");

            ReadMountCoordinatesOrThrow();
            double azimuth;
            double altitude;
            EquatorialToHorizontal(currentRightAscension, currentDeclination, out azimuth, out altitude);
            settings.ParkAzimuth = azimuth;
            settings.ParkAltitude = altitude;
            settings.ParkCurrentPosition = false;
            LogMessage("SetPark", string.Format("Saved park position: Az={0:F4}, Alt={1:F4}", azimuth, altitude));
        }
        public PierSide SideOfPier { get { return ReadPierSide(); } set { throw new MethodNotImplementedException("SideOfPier set"); } }
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
            if (isParked) return;
            CancelSlewStartVerification();

            if (isSlewing)
                AbortSlew();

            // ParkCurrentPosition matches the old driver's "Park Current"
            // option. Otherwise move to the configured physical park position.
            if (!settings.ParkCurrentPosition)
            {
                double parkRa;
                double parkDec;
                HorizontalToEquatorial(settings.ParkAzimuth, settings.ParkAltitude, out parkRa, out parkDec);
                SlewToCoordinates(parkRa, parkDec);
            }

            ReadMountCoordinatesOrThrow();
            double actualAzimuth;
            double actualAltitude;
            EquatorialToHorizontal(currentRightAscension, currentDeclination, out actualAzimuth, out actualAltitude);
            settings.ParkAzimuth = actualAzimuth;
            settings.ParkAltitude = actualAltitude;
            settings.ParkHourAngle = NormalizeSignedHours(SiderealTime - currentRightAscension);
            settings.ParkPierSide = ReadPierSide().ToString();
            settings.TrackingWasEnabledBeforePark = tracking;

            if (tracking) Tracking = false;

            isParked = true;
            settings.IsParked = true;
            LogMessage("Park", string.Format("Parked at Az={0:F4}, Alt={1:F4}, HA={2:F4}, Pier={3}.",
                settings.ParkAzimuth, settings.ParkAltitude, settings.ParkHourAngle, settings.ParkPierSide));
        }

        public void Unpark()
        {
            CheckConnected("Unpark");
            if (!isParked) return;

            try
            {
                // A Temma has no persistent park command. Restore its celestial
                // reference from the saved physical Alt/Az at the current LST.
                double referenceRa;
                double referenceDec;
                HorizontalToEquatorial(settings.ParkAzimuth, settings.ParkAltitude, out referenceRa, out referenceDec);

                PierSide savedPier;
                if (!Enum.TryParse(settings.ParkPierSide, out savedPier))
                    savedPier = PierSide.pierUnknown;
                PierSide currentPier = ReadPierSide();
                if (savedPier != PierSide.pierUnknown && currentPier != PierSide.pierUnknown && currentPier != savedPier)
                    SendBlindTemmaCommand("PT");

                // SyncToCoordinates deliberately rejects a parked mount.
                // Clear only the in-memory guard until the restore succeeds.
                isParked = false;
                SyncToCoordinates(referenceRa, referenceDec);

                settings.IsParked = false;
                if (settings.TrackingWasEnabledBeforePark)
                    Tracking = true;
                LogMessage("Unpark", string.Format("Restored RA={0:F6}, Dec={1:F6} from saved park position.", referenceRa, referenceDec));
            }
            catch
            {
                isParked = true;
                settings.IsParked = true;
                throw;
            }
        }

        #endregion
    }
}
