using System;

namespace ASCOM.CCDASTROTemma.Telescope
{
    /// <summary>
    /// Encapsulates Takahashi Temma protocol command formatting and response parsing.
    ///
    /// This class is independent of ASCOM interfaces and serial communications.
    /// It only knows how to build command strings and interpret responses.
    /// </summary>
    public static class TemmaProtocol
    {
        /// <summary>
        /// Command used to query the current mount position.
        /// </summary>
        public static string BuildCoordinateQueryCommand()
        {
            return "E";
        }

        /// <summary>
        /// Command used to abort a slew.
        /// </summary>
        public static string BuildAbortCommand()
        {
            return "PS";
        }

        /// <summary>
        /// Build the command used to turn tracking on or off.
        ///
        /// Temma2 mounts use:
        ///   STN-OFF = tracking ON
        ///   STN-ON  = tracking OFF
        /// </summary>
        public static string BuildTrackingCommand(bool enableTracking)
        {
            return enableTracking ? "STN-OFF" : "STN-ON";
        }

        /// <summary>
        /// Build a slew command to the specified coordinates.
        ///
        /// RA is expressed in decimal hours.
        /// Dec is expressed in decimal degrees.
        /// </summary>
        public static string BuildSlewCommand(
    double rightAscensionHours,
    double declinationDegrees)
        {
            string ra = FormatRa(rightAscensionHours);
            string dec = FormatDec(declinationDegrees);

            return "D" + ra + dec;
        }

        /// <summary>
        /// Build a sync command.
        ///
        /// For now, sync uses the same coordinate format as the slew command.
        /// </summary>
        public static string BuildSyncCommand(
    double rightAscensionHours,
    double declinationDegrees)
        {
            string ra = FormatRa(rightAscensionHours);
            string dec = FormatDec(declinationDegrees);

            // Temma sync command:
            // P + HHMMSS + ±DDMMm
            return "P" + ra + dec;
        }

        /// <summary>
        /// Parse the Temma E response.
        ///
        /// Expected format:
        ///   EHHMMSSsDDMMm...
        ///
        /// Example:
        ///   E123456+45123
        ///
        /// Returns:
        ///   RA in decimal hours.
        ///   Dec in decimal degrees.
        /// </summary>
        public static bool TryParseCoordinates(
            string response,
            out double rightAscensionHours,
            out double declinationDegrees)
        {
            rightAscensionHours = 0.0;
            declinationDegrees = 0.0;

            if (string.IsNullOrWhiteSpace(response))
                return false;

            response = response.Trim();

            if (response.Length < 13)
                return false;

            if (response[0] != 'E')
                return false;

            try
            {
                // RA: HHMMSS
                int hh = int.Parse(response.Substring(1, 2));
                int mm = int.Parse(response.Substring(3, 2));
                int ss = int.Parse(response.Substring(5, 2));

                rightAscensionHours = hh + (mm / 60.0) + (ss / 3600.0);

                // Dec sign
                char signChar = response[7];

                // DDMMm
                int degrees = int.Parse(response.Substring(8, 2));
                int minutes = int.Parse(response.Substring(10, 2));
                int tenthMinute = int.Parse(response.Substring(12, 1));

                declinationDegrees =
                    degrees +
                    (minutes / 60.0) +
                    (tenthMinute / 600.0);

                if (signChar == '-')
                    declinationDegrees = -declinationDegrees;

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Convert decimal hours to HHMMSS.
        /// </summary>
        public static string FormatRa(double rightAscensionHours)
        {
            // Normalize to 0–24 hours.
            while (rightAscensionHours < 0.0)
                rightAscensionHours += 24.0;

            while (rightAscensionHours >= 24.0)
                rightAscensionHours -= 24.0;

            TimeSpan ts = TimeSpan.FromHours(rightAscensionHours);

            return string.Format(
                "{0:00}{1:00}{2:00}",
                ts.Hours,
                ts.Minutes,
                ts.Seconds);
        }

        /// <summary>
        /// Convert decimal degrees to ±DD:MM:SS.
        /// </summary>
        public static string FormatDec(double declinationDegrees)
        {
            string sign = declinationDegrees >= 0.0 ? "+" : "-";

            double absDec = Math.Abs(declinationDegrees);

            int degrees = (int)Math.Floor(absDec);
            int minutes = (int)Math.Floor((absDec - degrees) * 60.0);
            int seconds = (int)Math.Round(
                ((((absDec - degrees) * 60.0) - minutes) * 60.0));

            return string.Format(
                "{0}{1:00}:{2:00}:{3:00}",
                sign,
                degrees,
                minutes,
                seconds);
        }
    }
}
