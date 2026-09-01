using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace WBOX
{
	static class FSE
	{
		[DllImport("api-ms-win-core-apiquery-l2-1-0.dll", CharSet = CharSet.Ansi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool IsApiSetImplemented(string contract);

        [DllImport("api-ms-win-gaming-experience-l1-1-0.dll", ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool IsGamingFullScreenExperienceActive();

		public static bool IsActive()
        {
            try
            {
                if (!IsApiSetImplemented("api-ms-win-gaming-experience-l1-1-0")) return false;
                return IsGamingFullScreenExperienceActive();
            }
            catch
            {
                return false;
            }
        }
	}
}
