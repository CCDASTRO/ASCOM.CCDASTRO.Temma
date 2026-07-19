using ASCOM.Utilities;
using System;

namespace ASCOM.LocalServer
{
    /// <summary>
    /// Owns the single physical Temma serial connection used by every driver
    /// instance in this local-server process.
    /// </summary>
    [HardwareClass]
    public static class SharedResources
    {
        private static readonly object lockObject = new object();
        private static Serial sharedSerial = new Serial();
        private static int serialConnectionCount;
        private static string connectedPortName;

        /// <summary>
        /// Acquire a logical connection. The first client opens and initializes
        /// the physical port; later clients share the already initialized port.
        /// </summary>
        /// <returns>True when this caller opened the physical connection.</returns>
        public static bool AcquireConnection(string portName, Action<Serial> initialize)
        {
            if (string.IsNullOrWhiteSpace(portName))
                throw new ArgumentException("A COM port must be selected.", "portName");
            if (initialize == null)
                throw new ArgumentNullException("initialize");

            lock (lockObject)
            {
                if (serialConnectionCount > 0)
                {
                    if (!sharedSerial.Connected)
                        throw new InvalidOperationException("The shared Temma serial connection is no longer open.");
                    if (!string.Equals(connectedPortName, portName, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException(
                            "The Temma mount is already connected on " + connectedPortName +
                            "; this client is configured for " + portName + ".");

                    serialConnectionCount++;
                    return false;
                }

                try
                {
                    // Publish a provisional lease while the initialization callback
                    // runs. The callback can therefore use WithSerial for its
                    // multi-command startup sequence; the enclosing lock still
                    // prevents another client from observing this state.
                    connectedPortName = portName;
                    serialConnectionCount = 1;
                    initialize(sharedSerial);
                    if (!sharedSerial.Connected)
                        throw new InvalidOperationException("Temma serial initialization did not open the port.");

                    return true;
                }
                catch
                {
                    try
                    {
                        if (sharedSerial.Connected)
                            sharedSerial.Connected = false;
                    }
                    catch { }

                    connectedPortName = null;
                    serialConnectionCount = 0;
                    throw;
                }
            }
        }

        /// <summary>Release one logical connection and close the port after the last client.</summary>
        public static void ReleaseConnection()
        {
            lock (lockObject)
            {
                if (serialConnectionCount == 0)
                    return;

                serialConnectionCount--;
                if (serialConnectionCount == 0)
                {
                    try
                    {
                        if (sharedSerial.Connected)
                            sharedSerial.Connected = false;
                    }
                    finally
                    {
                        connectedPortName = null;
                    }
                }
            }
        }

        /// <summary>Run a complete serial operation without another client interleaving traffic.</summary>
        public static T WithSerial<T>(Func<Serial, T> operation)
        {
            if (operation == null)
                throw new ArgumentNullException("operation");

            lock (lockObject)
            {
                if (serialConnectionCount <= 0 || !sharedSerial.Connected)
                    throw new InvalidOperationException("The shared Temma serial connection is not open.");
                return operation(sharedSerial);
            }
        }

        /// <summary>Run a blind or buffer-management operation under the shared serial lock.</summary>
        public static void WithSerial(Action<Serial> operation)
        {
            WithSerial<object>(serial =>
            {
                operation(serial);
                return null;
            });
        }

        public static int Connections
        {
            get { lock (lockObject) return serialConnectionCount; }
        }

        public static bool Connected
        {
            get { lock (lockObject) return serialConnectionCount > 0 && sharedSerial.Connected; }
        }

        /// <summary>Called once when the local server is irretrievably shutting down.</summary>
        public static void Dispose()
        {
            lock (lockObject)
            {
                try
                {
                    if (sharedSerial != null)
                    {
                        if (sharedSerial.Connected)
                            sharedSerial.Connected = false;
                        sharedSerial.Dispose();
                    }
                }
                catch { }
                finally
                {
                    sharedSerial = null;
                    connectedPortName = null;
                    serialConnectionCount = 0;
                }
            }
        }
    }
}
