using System;
using System.Runtime.InteropServices;
using ASCOM.Utilities;

namespace ASCOM.CCDASTROTemma.Telescope
{
    /// <summary>
    /// ASCOM profile registration methods.
    /// Compatible with C# 7.3.
    /// </summary>
    public partial class Telescope
    {
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
    }
}
