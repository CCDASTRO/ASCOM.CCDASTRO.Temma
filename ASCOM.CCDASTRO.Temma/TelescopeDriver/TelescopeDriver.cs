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
using System.Globalization;
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
        private volatile bool pulseGuiding;
        private readonly object pulseGuideLock = new object();
        private CancellationTokenSource pulseGuideRaCancellation;
        private CancellationTokenSource pulseGuideDecCancellation;
        private string pulseGuideRaCommand;
        private string pulseGuideDecCommand;

        private enum PulseGuideAxis
        {
            RightAscension,
            Declination
        }

        private int slewVerificationGeneration = 0;
        private readonly object slewVerificationLock = new object();
        private const int SlewStartVerificationDelayMs = 2500;
        private const int SlewStartMaximumAttempts = 5;
        private const double SlewStartRaMovementThresholdHours = 0.0002;
        private const double SlewStartDecMovementThresholdDegrees = 0.002;

        // Temma does not provide a reliable, universal end-of-GOTO response.
        // Match the proven VB6 driver's approach: after movement has been
        // confirmed, completion is based on the mount settling rather than on
        // an unrealistically exact match to the requested coordinates.
        private readonly object slewCompletionLock = new object();
        private const double SlewCompletionRaMotionThresholdHours = 0.001;
        private const double SlewCompletionDecMotionThresholdDegrees = 0.001;
        private const int SlewCompletionSettleDelayMs = 2000;
        private bool slewStartConfirmed;
        private bool slewCompletionSampleValid;
        private double slewCompletionPreviousRa;
        private double slewCompletionPreviousDec;
        private DateTime slewCompletionSettledSinceUtc = DateTime.MinValue;

        private TraceLogger tl;
        private DriverSettings settings;

        private const double SIDEREAL_RATE_DEG_SEC = 360.0 / 86164.0905;

        private double currentRightAscension;
        private double currentDeclination;
        private DateTime lastCoordinateUpdate = DateTime.MinValue;
        private double targetRightAscension;
        private double targetDeclination;
        private bool targetRightAscensionSet;
        private bool targetDeclinationSet;

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

            tracking = true;
            targetRightAscension = 0.0;
            targetDeclination = 0.0;
            targetRightAscensionSet = false;
            targetDeclinationSet = false;
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
                if (connectedState)
                {
                    try
                    {
                        CancelPulseGuide(true);
                        SharedResources.ReleaseConnection();
                    }
                    catch
                    {
                    }
                    connectedState = false;
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
            SendBlindTemmaCommand(command, raw);
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
                    ResetTargetCoordinates();
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
                if (value && isParked)
                    throw new ParkedException("Tracking cannot be enabled while the mount is parked.");
                if (tracking == value) return;

                SetTrackingStateOrThrow(value);
            }
        }

        public bool Slewing
        {
            get
            {
                if (isSlewing)
                {
                    if (UpdateCoordinates())
                        UpdateSlewCompletionState();
                }

                return isSlewing;
            }
        }

        public double TargetRightAscension
        {
            get
            {
                CheckConnected("TargetRightAscension");
                LogMessage("TargetRightAscension Get", "Initialized=" + targetRightAscensionSet);
                if (!targetRightAscensionSet)
                    throw new ASCOM.InvalidOperationException("TargetRightAscension has not been set.");
                if (isParked) throw new ParkedException("The mount is parked.");
                return targetRightAscension;
            }
            set
            {
                CheckConnected("TargetRightAscension");
                if (isParked) throw new ParkedException("The mount is parked.");
                if (value < 0.0 || value > 24.0)
                    throw new InvalidValueException("TargetRightAscension", value.ToString(CultureInfo.InvariantCulture), "0 to 24 hours");
                targetRightAscension = value;
                targetRightAscensionSet = true;
            }
        }

        public double TargetDeclination
        {
            get
            {
                CheckConnected("TargetDeclination");
                LogMessage("TargetDeclination Get", "Initialized=" + targetDeclinationSet);
                if (!targetDeclinationSet)
                    throw new ASCOM.InvalidOperationException("TargetDeclination has not been set.");
                if (isParked) throw new ParkedException("The mount is parked.");
                return targetDeclination;
            }
            set
            {
                CheckConnected("TargetDeclination");
                if (isParked) throw new ParkedException("The mount is parked.");
                if (value < -90.0 || value > 90.0)
                    throw new InvalidValueException("TargetDeclination", value.ToString(CultureInfo.InvariantCulture), "-90 to +90 degrees");
                targetDeclination = value;
                targetDeclinationSet = true;
            }
        }

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
            if (isParked) throw new ParkedException("The mount is parked.");
            CancelPulseGuide(true);
            CancelSlewStartVerification();
            SendBlindTemmaCommand(TemmaProtocol.BuildAbortCommand());
            isSlewing = false;
            ResetSlewCompletionTracking();

            // PS is the only reliable abort command across the tested Temma
            // controllers. Some firmwares do not reply to the documented S
            // status query, so do not block a subsequent slew waiting for it.
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
            if (rightAscension < 0.0 || rightAscension > 24.0)
                throw new InvalidValueException("RightAscension", rightAscension.ToString(), "0 to 24 hours");
            if (declination < -90.0 || declination > 90.0)
                throw new InvalidValueException("Declination", declination.ToString(), "-90 to +90 degrees");

            double startRa = currentRightAscension;
            double startDec = currentDeclination;
            TargetRightAscension = rightAscension;
            TargetDeclination = declination;

            CancelSlewStartVerification();
            SendTemmaGotoOrThrow(rightAscension, declination);
            ResetSlewCompletionTracking();
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

            // Clear buffers, send T + LST twice with 100 ms delays,
            // then send D. Real Temma controllers acknowledge D with R0.
            string lst = FormatTemmaSiderealTime(SiderealTime);

            SharedResources.WithSerial(port => port.ClearBuffers());

            SendBlindTemmaCommand("T" + lst);
            System.Threading.Thread.Sleep(100);

            SendBlindTemmaCommand("T" + lst);
            System.Threading.Thread.Sleep(100);

            // Build the Temma Sync (D) command.
            string syncCommand = TemmaProtocol.BuildSyncCommand(
                rightAscension, declination);

            SendTemmaSyncOrThrow(syncCommand, "coordinate sync");
            System.Threading.Thread.Sleep(100);

            // The Temma accepted the new reference. Keep ASCOM clients from
            // seeing the previous E-query coordinates until their next poll.
            currentRightAscension = rightAscension;
            currentDeclination = declination;
            lastCoordinateUpdate = DateTime.UtcNow;
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
            connectionInitializing = true;
            bool acquired = false;

            try
            {
                LogMessage("Connect", string.Format(
                    "Opening {0}: 19200,E,8,1; DTR=True; ReceiveTimeout=5s",
                    settings.ComPort));

                bool openedPhysicalPort = SharedResources.AcquireConnection(settings.ComPort, port =>
                {
                    port.PortName = settings.ComPort;
                    port.Speed = SerialSpeed.ps19200;
                    port.Parity = SerialParity.Even;
                    port.DTREnable = true;
                    port.ReceiveTimeout = 5;
                    port.Connected = true;
                    port.ClearBuffers();

                    string command = TemmaProtocol.BuildCoordinateQueryCommand() + "\r\n";
                    LogMessage("TX", EscapeForLog(command));
                    port.Transmit(command);
                    string response = port.ReceiveTerminated("\r\n");
                    LogMessage("RX", EscapeForLog(response));

                    double ra;
                    double dec;
                    if (!TemmaProtocol.TryParseCoordinates(response, out ra, out dec))
                        throw new Exception("Temma returned a response that could not be parsed: " +
                                            EscapeForLog(response));

                    currentRightAscension = ra;
                    currentDeclination = dec;

                    if (!isParked)
                        ApplyInitialMountSynchronization();
                    else
                    {
                        RestoreParkedCoordinateCache();
                        LogMessage("Connect", "Mount is parked; startup synchronization deferred until Unpark.");
                    }

                    ConfigureMountSpeed();
                    if (settings.SendRate)
                    {
                        SetTemmaGuideRate("LA", settings.GuideRateRA * SIDEREAL_RATE_DEG_SEC, "RA");
                        SetTemmaGuideRate("LB", settings.GuideRateDec * SIDEREAL_RATE_DEG_SEC, "Declination");
                    }
                    ConfigureAxisRates();
                });
                acquired = true;

                if (!openedPhysicalPort)
                {
                    LogMessage("Connect", "Reusing the shared Temma serial connection.");
                    string response = SendCommand(TemmaProtocol.BuildCoordinateQueryCommand());
                    double ra;
                    double dec;
                    if (!TemmaProtocol.TryParseCoordinates(response, out ra, out dec))
                        throw new Exception("Temma returned a response that could not be parsed: " +
                                            EscapeForLog(response));
                    currentRightAscension = ra;
                    currentDeclination = dec;
                    ConfigureAxisRates();
                }

                connectedState = true;
                LogMessage("Connect", "Temma initialization completed successfully.");
            }
            catch (Exception ex)
            {
                connectedState = false;
                LogMessage("Connect ERROR", ex.ToString());

                if (acquired)
                    SharedResources.ReleaseConnection();

                throw new NotConnectedException(
                    "Unable to communicate with the Temma mount. " + ex.Message);
            }
            finally
            {
                connectionInitializing = false;
            }

            try
            {
                // Temma reports standby state with STN-COD. Preserve the mount's
                // existing tracking state on a normal connection; only change it
                // when the explicit "tracking off on connect" option is enabled.
                if (settings.TrackingOffOnConnect)
                    SetTrackingStateOrThrow(false);
                else
                    tracking = QueryTrackingStateOrThrow();

                isSlewing = false;
                ResetSlewCompletionTracking();
                if (!isParked)
                    lastCoordinateUpdate = DateTime.MinValue;

                if (isParked && settings.UnparkOnReconnect)
                    Unpark();

                LogMessage("Connect", "Successfully connected to Temma mount.");
            }
            catch (Exception ex)
            {
                connectedState = false;
                if (acquired)
                    SharedResources.ReleaseConnection();
                LogMessage("Connect ERROR", ex.ToString());
                throw new NotConnectedException(
                    "Unable to finish initializing the Temma mount. " + ex.Message);
            }
        }

        private void DisconnectFromMount()
        {
            CancelPulseGuide(true);
            SharedResources.ReleaseConnection();

            tracking = false;
            isSlewing = false;
            ResetSlewCompletionTracking();
            pulseGuiding = false;

            LogMessage("Disconnect", "Disconnected");
        }

        private void ResetTargetCoordinates()
        {
            targetRightAscension = 0.0;
            targetDeclination = 0.0;
            targetRightAscensionSet = false;
            targetDeclinationSet = false;
            LogMessage("Target coordinates", "Reset to unset for new connection.");
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

            string response = SharedResources.WithSerial(port =>
            {
                port.Transmit(framedCommand);
                return port.ReceiveTerminated("\r\n");
            });

            if (logTransaction)
                LogMessage("RX", EscapeForLog(response));

            return response;
        }

        private static string EscapeForLog(string value)
        {
            if (value == null) return "<null>";
            return value.Replace("\r", "<CR>").Replace("\n", "<LF>");
        }

        private bool UpdateCoordinates()
        {
            if (!connectedState) return false;
            // Park is an ASCOM driver-side state for Temma. While parked,
            // expose the stored physical park position rather than an
            // arbitrary raw E response after a mount power cycle.
            if (isParked) return false;
            if ((DateTime.UtcNow - lastCoordinateUpdate).TotalSeconds < 1.0) return false;

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
                    return true;
                }
            }
            catch (Exception ex)
            {
                LogMessage("UpdateCoordinates", ex.Message);
            }

            return false;
        }

        #endregion

        #region Utility Methods

        private void CancelSlewStartVerification()
        {
            lock (slewVerificationLock) slewVerificationGeneration++;
        }

        private void ResetSlewCompletionTracking()
        {
            lock (slewCompletionLock)
            {
                slewStartConfirmed = false;
                slewCompletionSampleValid = false;
                slewCompletionSettledSinceUtc = DateTime.MinValue;
            }
        }

        private void ConfirmSlewStarted()
        {
            lock (slewCompletionLock)
            {
                slewStartConfirmed = true;
                slewCompletionSampleValid = false;
                slewCompletionSettledSinceUtc = DateTime.MinValue;
            }
        }

        private void UpdateSlewCompletionState()
        {
            bool slewCompleted = false;

            lock (slewCompletionLock)
            {
                // Never declare completion before the independent start
                // verification has observed real mount movement.
                if (!slewStartConfirmed) return;

                DateTime now = DateTime.UtcNow;
                if (!slewCompletionSampleValid)
                {
                    slewCompletionPreviousRa = currentRightAscension;
                    slewCompletionPreviousDec = currentDeclination;
                    slewCompletionSampleValid = true;
                    return;
                }

                double raMovement = Math.Abs(currentRightAscension - slewCompletionPreviousRa);
                if (raMovement > 12.0) raMovement = 24.0 - raMovement;
                double decMovement = Math.Abs(currentDeclination - slewCompletionPreviousDec);

                slewCompletionPreviousRa = currentRightAscension;
                slewCompletionPreviousDec = currentDeclination;

                if (raMovement > SlewCompletionRaMotionThresholdHours ||
                    decMovement > SlewCompletionDecMotionThresholdDegrees)
                {
                    slewCompletionSettledSinceUtc = DateTime.MinValue;
                    return;
                }

                if (slewCompletionSettledSinceUtc == DateTime.MinValue)
                {
                    slewCompletionSettledSinceUtc = now;
                    return;
                }

                if ((now - slewCompletionSettledSinceUtc).TotalMilliseconds >= SlewCompletionSettleDelayMs)
                    slewCompleted = true;
            }

            if (slewCompleted)
            {
                isSlewing = false;
                LogMessage("SlewComplete", "Mount movement settled; marking slew complete.");
                ResetSlewCompletionTracking();
            }
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
                SharedResources.WithSerial(port => port.ClearBuffers());

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
                        ConfirmSlewStarted();
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
                ResetSlewCompletionTracking();
            }
            catch (Exception ex)
            {
                if (IsSlewVerificationCurrent(generation))
                {
                    isSlewing = false;
                    ResetSlewCompletionTracking();
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

            SharedResources.WithSerial(port => port.ClearBuffers());

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

                        // D returns R0 on a real Temma. Consume and validate
                        // that acknowledgement before issuing another command.
                        SendTemmaSyncOrThrow("D" + lst + latitude,
                            "startup synchronization");
                        Thread.Sleep(100);
                        break;
                    }

                case "CounterweightDown":
                    {
                        EnsureTemmaPierReference();

                        double ra;
                        double dec;
                        GetCounterweightDownReference(out ra, out dec);

                        SyncToCoordinates(ra, dec);
                        break;
                    }

                case "CounterweightWest":
                    {
                        EnsureTemmaPierReference();

                        double ra;
                        double dec;
                        GetCounterweightWestReference(out ra, out dec);

                        SyncToCoordinates(ra, dec);
                        break;
                    }
            }

            SharedResources.WithSerial(port => port.ClearBuffers());

            LogMessage("InitialSync", "Startup synchronization completed.");
        }

        // The old driver calculated the counterweight reference positions from
        // physical Alt/Az, then synchronized Temma to the resulting sky
        // declination. Do not substitute SiteLatitude for declination: at the
        // northern counterweight-down reference the OTA points near the NCP,
        // so declination is near +90 degrees, not the site latitude.
        private void GetCounterweightDownReference(out double rightAscension, out double declination)
        {
            double referenceAltitude;
            double referenceAzimuth;

            if (SiteLatitude < 0.0)
            {
                // Match VB6 Init Down: just south of the south celestial pole.
                referenceAltitude = Math.Abs(SiteLatitude - 0.01);
                referenceAzimuth = 179.09;
            }
            else
            {
                // Just east of true north avoids the pole's undefined azimuth.
                referenceAltitude = SiteLatitude + 0.01;
                referenceAzimuth = 0.01;
            }

            double ignoredRightAscension;
            HorizontalToEquatorial(referenceAzimuth, referenceAltitude,
                                   out ignoredRightAscension, out declination);
            rightAscension = NormalizeHours(SiderealTime - 6.0);
        }

        private void GetCounterweightWestReference(out double rightAscension, out double declination)
        {
            // This intentionally follows the old VB6 Init CW West geometry,
            // including its southern-hemisphere reference altitude.
            double referenceAltitude = SiteLatitude < 0.0
                ? SiteLatitude - 0.01
                : SiteLatitude + 0.01;

            double ignoredRightAscension;
            HorizontalToEquatorial(0.01, referenceAltitude,
                                   out ignoredRightAscension, out declination);
            rightAscension = NormalizeHours(SiderealTime);
        }

        private void RestoreParkedCoordinateCache()
        {
            HorizontalToEquatorial(settings.ParkAzimuth, settings.ParkAltitude,
                                   out currentRightAscension, out currentDeclination);
            lastCoordinateUpdate = DateTime.UtcNow;

            LogMessage("Connect", string.Format(
                "Restored parked position: Az={0:F4}, Alt={1:F4}, RA={2:F6}, Dec={3:F6}.",
                settings.ParkAzimuth, settings.ParkAltitude,
                currentRightAscension, currentDeclination));
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

        private void SendBlindTemmaCommand(string command, bool raw = false)
        {
            CheckConnected("SendBlindTemmaCommand");

            string framedCommand = raw || command.EndsWith("\r\n")
                ? command
                : command + "\r\n";

            LogMessage("Blind TX", EscapeForLog(framedCommand));
            SharedResources.WithSerial(port => port.Transmit(framedCommand));
        }

        private void SendTemmaSyncOrThrow(string syncCommand, string operation)
        {
            string response = (SendCommand(syncCommand) ?? string.Empty).Trim();
            if (!string.Equals(response, "R0", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    "Temma " + operation + " failed; expected R0, received " +
                    EscapeForLog(response));
        }

        private bool QueryTrackingStateOrThrow()
        {
            // A preceding Temma blind command can leave a late status byte in
            // the shared receive buffer. Start this state query clean so a
            // second ASCOM client cannot consume that stale reply as STN-COD.
            const string command = "STN-COD\r\n";
            LogMessage("TX", EscapeForLog(command));
            string response = SharedResources.WithSerial(port =>
            {
                port.ClearBuffers();
                port.Transmit(command);
                return port.ReceiveTerminated("\r\n");
            });
            LogMessage("RX", EscapeForLog(response));
            response = (response ?? string.Empty).Trim();

            if (string.Equals(response, "stn-off", StringComparison.OrdinalIgnoreCase))
                return true; // Standby OFF: RA motor is tracking.

            if (string.Equals(response, "stn-on", StringComparison.OrdinalIgnoreCase))
                return false; // Standby ON: RA motor is stopped.

            throw new InvalidOperationException(
                "Temma returned an invalid standby status: " + EscapeForLog(response));
        }

        private void SetTrackingStateOrThrow(bool enableTracking)
        {
            string expectedReply = enableTracking ? "stn-off" : "stn-on";
            string response = (SendCommand(
                TemmaProtocol.BuildTrackingCommand(enableTracking)) ?? string.Empty).Trim();

            if (!string.Equals(response, expectedReply, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    "Temma did not confirm the requested tracking state; expected " +
                    expectedReply + ", received " + EscapeForLog(response));

            tracking = enableTracking;
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
                axis != TelescopeAxes.axisSecondary &&
                axis != TelescopeAxes.axisTertiary)
            {
                throw new InvalidValueException(
                    "Axis",
                    ((int)axis).ToString(),
                    "0, 1 or 2");
            }

            if (axis == TelescopeAxes.axisTertiary)
                return new AxisRates(axis, 0.0, 0.0);

            double guideRate = axis == TelescopeAxes.axisPrimary
                ? settings.GuideRateRA * SIDEREAL_RATE_DEG_SEC
                : settings.GuideRateDec * SIDEREAL_RATE_DEG_SEC;
            double slewRate = GetSlewRateMultiplier() * SIDEREAL_RATE_DEG_SEC;
            return new AxisRates(axis, guideRate, slewRate);
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
        public bool CanMoveAxis(TelescopeAxes axis)
        {
            if (axis != TelescopeAxes.axisPrimary &&
                axis != TelescopeAxes.axisSecondary &&
                axis != TelescopeAxes.axisTertiary)
            {
                throw new InvalidValueException("Axis", ((int)axis).ToString(), "0, 1 or 2");
            }
            return axis != TelescopeAxes.axisTertiary;
        }
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
        public double DeclinationRate
        {
            get { CheckConnected("DeclinationRate"); return 0.0; }
            set { throw new PropertyNotImplementedException("DeclinationRate", true); }
        }
        public PierSide DestinationSideOfPier(double rightAscension, double declination)
        {
            CheckConnected("DestinationSideOfPier");
            if (rightAscension < 0.0 || rightAscension > 24.0)
                throw new InvalidValueException("RightAscension", rightAscension.ToString(CultureInfo.InvariantCulture), "0 to 24 hours");
            if (declination < -90.0 || declination > 90.0)
                throw new InvalidValueException("Declination", declination.ToString(CultureInfo.InvariantCulture), "-90 to +90 degrees");

            double hourAngle = NormalizeSignedHours(SiderealTime - rightAscension);
            return hourAngle >= 0.0 ? PierSide.pierEast : PierSide.pierWest;
        }
        public bool DoesRefraction { get { return false; } set { } }
        public EquatorialCoordinateType EquatorialSystem { get { return EquatorialCoordinateType.equTopocentric; } }
        public void FindHome() { throw new MethodNotImplementedException("FindHome"); }
        public double FocalLength { get { return settings.FocalLength; } }
        public double GuideRateDeclination
        {
            get
            {
                CheckConnected("GuideRateDeclination");
                return QueryTemmaGuideRate("lb", "Declination");
            }
            set
            {
                SetTemmaGuideRate("LB", value, "Declination");
                settings.GuideRateDec = value / SIDEREAL_RATE_DEG_SEC;
            }
        }

        public double GuideRateRightAscension
        {
            get
            {
                CheckConnected("GuideRateRightAscension");
                return QueryTemmaGuideRate("la", "RA");
            }
            set
            {
                SetTemmaGuideRate("LA", value, "RA");
                settings.GuideRateRA = value / SIDEREAL_RATE_DEG_SEC;
            }
        }

        private double QueryTemmaGuideRate(string command, string axisName)
        {
            string response = (SendCommand(command) ?? string.Empty).Trim();
            int percentage;

            // Match the VB6 driver: Mid$(response, 5, 2).
            if (response.Length < 6 ||
                !int.TryParse(
                    response.Substring(4, 2),
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out percentage))
            {
                throw new InvalidOperationException(
                    "Temma returned an invalid " + axisName +
                    " guide-rate response: " + EscapeForLog(response));
            }

            return (percentage / 100.0) * SIDEREAL_RATE_DEG_SEC;
        }

        private void SetTemmaGuideRate(string command, double rateDegreesPerSecond, string axisName)
        {
            CheckConnected("GuideRate" + axisName);

            double maximumRate = SIDEREAL_RATE_DEG_SEC * 0.99;
            if (double.IsNaN(rateDegreesPerSecond) ||
                double.IsInfinity(rateDegreesPerSecond) ||
                rateDegreesPerSecond < 0.0 ||
                rateDegreesPerSecond > maximumRate)
            {
                throw new InvalidValueException(
                    "GuideRate" + axisName,
                    rateDegreesPerSecond.ToString("R", CultureInfo.InvariantCulture),
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "0 to {0:R} degrees per second (0% to 99% sidereal)",
                        maximumRate));
            }

            int percentage = (int)Math.Round(
                (rateDegreesPerSecond / SIDEREAL_RATE_DEG_SEC) * 100.0);

            // The legacy VB6 implementation deliberately used CommandBlind for
            // LA/LB. Some Temma controllers still emit a status byte for these
            // commands; discard it before another client performs a query.
            string framedCommand =
                command + percentage.ToString("00", CultureInfo.InvariantCulture) + "\r\n";
            LogMessage("Blind TX", EscapeForLog(framedCommand));
            SharedResources.WithSerial(port =>
            {
                port.Transmit(framedCommand);
                Thread.Sleep(50);
                port.ClearBuffers();
            });
            LogMessage(
                "GuideRate",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} guide rate set to {1}% ({2:R} degrees/second)",
                    axisName,
                    percentage,
                    (percentage / 100.0) * SIDEREAL_RATE_DEG_SEC));
        }
        public bool IsPulseGuiding
        {
            get { CheckConnected("IsPulseGuiding"); return pulseGuiding; }
        }
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
            bool supportedRate = false;
            foreach (IRate r in AxisRates(axis))
            {
                if (r.Maximum > maxRate) maxRate = r.Maximum;
                if (Math.Abs(Math.Abs(rate) - r.Maximum) < 0.000000001)
                    supportedRate = true;
            }

            if (!supportedRate)
                throw new InvalidValueException(
                    "Rate",
                    rate.ToString("R", CultureInfo.InvariantCulture),
                    "one of the discrete rates returned by AxisRates");

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
            if (!tracking)
                throw new ASCOM.InvalidOperationException("PulseGuide requires tracking to be enabled.");
            if (isSlewing)
                throw new ASCOM.InvalidOperationException("PulseGuide cannot start while the mount is slewing.");

            string directionCommand;
            PulseGuideAxis pulseAxis;
            switch (direction)
            {
                case GuideDirections.guideNorth:
                    directionCommand = "MH";
                    pulseAxis = PulseGuideAxis.Declination;
                    break;
                case GuideDirections.guideSouth:
                    directionCommand = "MP";
                    pulseAxis = PulseGuideAxis.Declination;
                    break;
                case GuideDirections.guideEast:
                    directionCommand = "MD";
                    pulseAxis = PulseGuideAxis.RightAscension;
                    break;
                case GuideDirections.guideWest:
                    directionCommand = "MB";
                    pulseAxis = PulseGuideAxis.RightAscension;
                    break;
                default:
                    throw new InvalidValueException("Unsupported guide direction.");
            }

            if (duration == 0)
                return;

            CancellationTokenSource cancellation;
            lock (pulseGuideLock)
            {
                CancellationTokenSource activeAxisCancellation =
                    pulseAxis == PulseGuideAxis.RightAscension
                        ? pulseGuideRaCancellation
                        : pulseGuideDecCancellation;
                if (activeAxisCancellation != null)
                    throw new ASCOM.InvalidOperationException(
                        "A PulseGuide operation is already active on this axis.");

                cancellation = new CancellationTokenSource();
                SetPulseGuideAxisState(pulseAxis, cancellation, directionCommand);
                pulseGuiding = true;

                try
                {
                    // Temma latches RA and Declination motion independently,
                    // so a command on the other axis can be added in parallel.
                    SendBlindTemmaCommand(directionCommand);
                }
                catch
                {
                    ClearPulseGuideAxisState(pulseAxis, cancellation);
                    pulseGuiding = pulseGuideRaCancellation != null || pulseGuideDecCancellation != null;
                    cancellation.Dispose();
                    throw;
                }
            }

            Task.Run(() => ExecutePulseGuide(pulseAxis, duration, cancellation));
        }

        private void ExecutePulseGuide(
            PulseGuideAxis pulseAxis,
            int duration,
            CancellationTokenSource cancellation)
        {
            try
            {
                int remaining = duration;

                // The working VB6 driver limits each continuous pulse to 3000 ms.
                while (remaining > 0 && !cancellation.IsCancellationRequested)
                {
                    int segment = Math.Min(remaining, 3000);
                    bool cancelled = cancellation.Token.WaitHandle.WaitOne(segment);
                    if (cancelled) break;
                    remaining -= segment;
                    CompletePulseGuideSegment(pulseAxis, cancellation, remaining > 0);
                }
            }
            catch (Exception ex)
            {
                LogMessage("PulseGuide ERROR", ex.ToString());
                StopPulseAxisAfterError(pulseAxis, cancellation);
            }
            finally
            {
                lock (pulseGuideLock)
                {
                    ClearPulseGuideAxisState(pulseAxis, cancellation);
                    pulseGuiding = pulseGuideRaCancellation != null || pulseGuideDecCancellation != null;
                }
                cancellation.Dispose();
            }
        }

        private void CompletePulseGuideSegment(
            PulseGuideAxis pulseAxis,
            CancellationTokenSource cancellation,
            bool continueAxis)
        {
            lock (pulseGuideLock)
            {
                if (!IsPulseGuideAxisState(pulseAxis, cancellation))
                    return;

                if (!continueAxis)
                    ClearPulseGuideAxisState(pulseAxis, cancellation);

                // M@ stops both axes. Re-issue every command that remains
                // active, including this axis after a three-second segment.
                SendBlindTemmaCommand("M@");
                if (pulseGuideRaCancellation != null)
                    SendBlindTemmaCommand(pulseGuideRaCommand);
                if (pulseGuideDecCancellation != null)
                    SendBlindTemmaCommand(pulseGuideDecCommand);

                pulseGuiding = pulseGuideRaCancellation != null || pulseGuideDecCancellation != null;
            }
        }

        private void StopPulseAxisAfterError(
            PulseGuideAxis pulseAxis,
            CancellationTokenSource cancellation)
        {
            lock (pulseGuideLock)
            {
                if (!IsPulseGuideAxisState(pulseAxis, cancellation))
                    return;

                ClearPulseGuideAxisState(pulseAxis, cancellation);
                try
                {
                    if (SharedResources.Connected)
                    {
                        SendBlindTemmaCommand("M@");
                        if (pulseGuideRaCancellation != null)
                            SendBlindTemmaCommand(pulseGuideRaCommand);
                        if (pulseGuideDecCancellation != null)
                            SendBlindTemmaCommand(pulseGuideDecCommand);
                    }
                }
                catch { }
                pulseGuiding = pulseGuideRaCancellation != null || pulseGuideDecCancellation != null;
            }
        }

        private void SetPulseGuideAxisState(
            PulseGuideAxis pulseAxis,
            CancellationTokenSource cancellation,
            string command)
        {
            if (pulseAxis == PulseGuideAxis.RightAscension)
            {
                pulseGuideRaCancellation = cancellation;
                pulseGuideRaCommand = command;
            }
            else
            {
                pulseGuideDecCancellation = cancellation;
                pulseGuideDecCommand = command;
            }
        }

        private bool IsPulseGuideAxisState(
            PulseGuideAxis pulseAxis,
            CancellationTokenSource cancellation)
        {
            return ReferenceEquals(
                pulseAxis == PulseGuideAxis.RightAscension
                    ? pulseGuideRaCancellation
                    : pulseGuideDecCancellation,
                cancellation);
        }

        private void ClearPulseGuideAxisState(
            PulseGuideAxis pulseAxis,
            CancellationTokenSource cancellation)
        {
            if (!IsPulseGuideAxisState(pulseAxis, cancellation))
                return;

            if (pulseAxis == PulseGuideAxis.RightAscension)
            {
                pulseGuideRaCancellation = null;
                pulseGuideRaCommand = null;
            }
            else
            {
                pulseGuideDecCancellation = null;
                pulseGuideDecCommand = null;
            }
        }

        private void CancelPulseGuide(bool sendStop)
        {
            CancellationTokenSource raCancellation;
            CancellationTokenSource decCancellation;
            lock (pulseGuideLock)
            {
                raCancellation = pulseGuideRaCancellation;
                decCancellation = pulseGuideDecCancellation;
                pulseGuideRaCancellation = null;
                pulseGuideDecCancellation = null;
                pulseGuideRaCommand = null;
                pulseGuideDecCommand = null;
                pulseGuiding = false;
            }

            try { if (raCancellation != null) raCancellation.Cancel(); } catch { }
            try { if (decCancellation != null) decCancellation.Cancel(); } catch { }

            if (sendStop && connectedState && (raCancellation != null || decCancellation != null))
            {
                try { SendBlindTemmaCommand("M@"); } catch { }
            }
        }

        public double RightAscensionRate
        {
            get { CheckConnected("RightAscensionRate"); return 0.0; }
            set { throw new PropertyNotImplementedException("RightAscensionRate", true); }
        }
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
        public double SiteElevation
        {
            get { return settings.SiteElevation; }
            set
            {
                if (value < -300.0 || value > 10000.0)
                    throw new InvalidValueException("SiteElevation", value.ToString(CultureInfo.InvariantCulture), "-300 to 10000 metres");
                settings.SiteElevation = value;
            }
        }
        public double SiteLatitude
        {
            get { return settings.SiteLatitude; }
            set
            {
                if (value < -90.0 || value > 90.0)
                    throw new InvalidValueException("SiteLatitude", value.ToString(CultureInfo.InvariantCulture), "-90 to +90 degrees");
                settings.SiteLatitude = value;
            }
        }
        public double SiteLongitude
        {
            get { return settings.SiteLongitude; }
            set
            {
                if (value < -180.0 || value > 180.0)
                    throw new InvalidValueException("SiteLongitude", value.ToString(CultureInfo.InvariantCulture), "-180 to +180 degrees");
                settings.SiteLongitude = value;
            }
        }
        public short SlewSettleTime
        {
            get { return settings.SlewSettleTime; }
            set
            {
                if (value < 0)
                    throw new InvalidValueException("SlewSettleTime", value.ToString(CultureInfo.InvariantCulture), "zero or greater");
                settings.SlewSettleTime = value;
            }
        }
        public void SlewToAltAz(double azimuth, double altitude) { throw new MethodNotImplementedException("SlewToAltAz"); }
        public void SlewToAltAzAsync(double azimuth, double altitude) { throw new MethodNotImplementedException("SlewToAltAzAsync"); }
        public void SyncToAltAz(double azimuth, double altitude) { throw new MethodNotImplementedException("SyncToAltAz"); }
        public DriveRates TrackingRate
        {
            get { CheckConnected("TrackingRate"); return DriveRates.driveSidereal; }
            set
            {
                CheckConnected("TrackingRate");
                if (value != DriveRates.driveSidereal)
                    throw new InvalidValueException("TrackingRate", ((int)value).ToString(CultureInfo.InvariantCulture), "Sidereal");
            }
        }
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
