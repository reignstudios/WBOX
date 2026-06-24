using System.Runtime.InteropServices;

namespace WBOX
{
    static class Power
    {
        [DllImport("PowrProf.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsPwrSuspendAllowed();

        [DllImport("PowrProf.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsPwrHibernateAllowed();

        [DllImport("PowrProf.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetSuspendState(bool hibernate, bool forceCritical, bool disableWakeEvent);
    }
}
