using System;

namespace ASCOM.CCDASTROTemma.Telescope
{
    /// <summary>
    /// Takahashi Temma protocol formatting and parsing.
    /// Formats are matched to the supplied working VB6 driver.
    /// </summary>
    public static class TemmaProtocol
    {
        public static string BuildCoordinateQueryCommand() { return "E"; }
        public static string BuildAbortCommand() { return "PS"; }

        // Temma2:
        // STN-OFF = tracking ON
        // STN-ON  = tracking OFF
        public static string BuildTrackingCommand(bool enableTracking)
        {
            return enableTracking ? "STN-OFF" : "STN-ON";
        }

        // GOTO uses P + HHMMtt + +/-DDMMt and returns R0..R5.
        public static string BuildSlewCommand(double rightAscensionHours, double declinationDegrees)
        {
            return "P" + FormatRa(rightAscensionHours) + FormatDec(declinationDegrees);
        }

        // Sync uses D + HHMMtt + +/-DDMMt after the T/T LST sequence.
        public static string BuildSyncCommand(double rightAscensionHours, double declinationDegrees)
        {
            return "D" + FormatRa(rightAscensionHours) + FormatDec(declinationDegrees);
        }

        /// <summary>
        /// Parse EHHMMtt+/-DDMMt...
        /// HHMMtt uses hundredths of a minute, not seconds.
        /// DDMMt uses tenths of an arcminute.
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
            if (response.Length < 13 || response[0] != 'E')
                return false;

            try
            {
                int hh = int.Parse(response.Substring(1, 2));
                int mm = int.Parse(response.Substring(3, 2));
                int hundredthMinute = int.Parse(response.Substring(5, 2));

                if (hh < 0 || hh > 23 || mm < 0 || mm > 59 ||
                    hundredthMinute < 0 || hundredthMinute > 99)
                    return false;

                rightAscensionHours =
                    hh + (mm / 60.0) + (hundredthMinute / 6000.0);

                char signChar = response[7];
                if (signChar != '+' && signChar != '-' && signChar != ' ')
                    return false;

                int degrees = int.Parse(response.Substring(8, 2));
                int minutes = int.Parse(response.Substring(10, 2));
                int tenthMinute = int.Parse(response.Substring(12, 1));

                if (degrees < 0 || degrees > 90 || minutes < 0 || minutes > 59 ||
                    tenthMinute < 0 || tenthMinute > 9)
                    return false;

                declinationDegrees =
                    degrees + (minutes / 60.0) + (tenthMinute / 600.0);

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
        /// Formats Right Ascension as Temma HHMMtt,
        /// where tt is hundredths of a minute.
        /// Equivalent to the VB6 TAKhms() routine.
        /// </summary>
        public static string FormatRa(double rightAscensionHours)
        {
            while (rightAscensionHours < 0.0) rightAscensionHours += 24.0;
            while (rightAscensionHours >= 24.0) rightAscensionHours -= 24.0;

            int hh = (int)Math.Floor(rightAscensionHours);
            double minuteValue = (rightAscensionHours - hh) * 60.0;
            int mm = (int)Math.Floor(minuteValue);
            int tt = (int)Math.Floor((minuteValue - mm) * 100.0);

            if (tt >= 100) { tt = 0; mm++; }
            if (mm >= 60) { mm = 0; hh++; }
            if (hh >= 24) hh = 0;

            return string.Format("{0:00}{1:00}{2:00}", hh, mm, tt);
        }

        /// <summary>
        /// Formats Declination as Temma ±DDMMt,
        /// where t is tenths of a minute.
        /// Equivalent to the VB6 TAKdms() routine.
        /// </summary>
        public static string FormatDec(double declinationDegrees)
        {
            string sign = declinationDegrees < 0.0 ? "-" : "+";
            double value = Math.Abs(declinationDegrees);

            int dd = (int)Math.Floor(value);
            double minuteValue = (value - dd) * 60.0;
            int mm = (int)Math.Floor(minuteValue);
            int t = (int)Math.Floor((minuteValue - mm) * 10.0);

            if (t >= 10) { t = 0; mm++; }
            if (mm >= 60) { mm = 0; dd++; }

            // Match the VB6 pole guard.
            if (dd >= 90)
            {
                dd = 89;
                mm = 59;
                t = 9;
            }

            return string.Format("{0}{1:00}{2:00}{3:0}", sign, dd, mm, t);
        }
    }
}
