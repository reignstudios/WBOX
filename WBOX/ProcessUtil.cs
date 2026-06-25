using System;
using System.Diagnostics;
using System.Windows;

namespace WBOX
{
	static class ProcessUtil
	{
		public static void LaunchHiddenProcess(string processName, string args = "")
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = processName,
                    Arguments = args,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
	}
}
