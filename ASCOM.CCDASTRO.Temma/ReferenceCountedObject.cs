using System.Runtime.InteropServices;
using System.Threading;

namespace ASCOM.LocalServer
{
    /// <summary>
    /// Class to keep track of COM object instanciations and destructions
    /// </summary>
    [ComVisible(false)]
    public class ReferenceCountedObjectBase
    {
        private int serverReferenceReleased;

        protected void ReleaseServerReference()
        {
            if (Interlocked.Exchange(ref serverReferenceReleased, 1) != 0) return;
            Server.DecrementObjectCount();
            Server.ExitIf();
        }
        /// <summary>
        /// Called every time a driver instance is created
        /// </summary>
        public ReferenceCountedObjectBase()
        {
            // We increment the global count of objects.
            Server.IncrementObjectCount();
        }

        /// <summary>
        /// Called automatically by the garbage collection mechanic every time a driver instance is finalised (destroyed).
        /// </summary>
        ~ReferenceCountedObjectBase()
        {
            // We decrement the global count of objects.
            ReleaseServerReference();
        }
    }
}
