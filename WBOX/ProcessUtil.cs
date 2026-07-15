using System;
using System.Diagnostics;
using System.Windows;

namespace WBOX
{
	static class ProcessUtil
	{
        public static string LaunchProcess(string processName, string args, bool setWorkingPath, bool readOutput, bool waitForExit = true)
		{
			try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = processName,
                    Arguments = args,
                    UseShellExecute = !readOutput,
                    CreateNoWindow = false,
                    RedirectStandardOutput = readOutput
                };
                if (setWorkingPath) startInfo.WorkingDirectory = System.IO.Path.GetDirectoryName(processName);
                using (var process = Process.Start(startInfo))
                {
                    if (waitForExit) process.WaitForExit();
                    if (readOutput) return process.StandardOutput.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return "";
		}

        public static string LaunchAdminProcess(string processName, string args, bool setWorkingPath, bool readOutput, bool waitForExit = true)
		{
			try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = processName,
                    Arguments = args,
                    Verb = "runas",
                    UseShellExecute = !readOutput,
                    CreateNoWindow = false,
                    RedirectStandardOutput = readOutput
                };
                if (setWorkingPath) startInfo.WorkingDirectory = System.IO.Path.GetDirectoryName(processName);
                using (var process = Process.Start(startInfo))
                {
                    if (waitForExit) process.WaitForExit();
                    if (readOutput) return process.StandardOutput.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return "";
		}

		public static void LaunchHiddenProcess(string processName, string args, bool setWorkingPath)
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
                if (setWorkingPath) startInfo.WorkingDirectory = System.IO.Path.GetDirectoryName(processName);
                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
	}
}
