using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
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

		#region Force App to Foreground
		private const int SW_RESTORE = 9;

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool BringWindowToTop(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern IntPtr SetFocus(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr lpdwProcessId);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        [DllImport("user32.dll")]
        private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        public static async void ForceForegroundAsync(Process process, int timeoutMs = 5000)
        {
            if (process == null) return;

            int elapsed = 0;
            const int delay = 1000;
            while (elapsed < timeoutMs + 500)
            {
                try
                {
                    if (process.HasExited) return;

                    process.Refresh();
                    IntPtr hwnd = process.MainWindowHandle;
                    if (hwnd != IntPtr.Zero)
                    {
                        ForceForeground(hwnd);
                    }
                }
                catch
                {
                    return;
                }

                await Task.Delay(delay);
                elapsed += delay;
            }
        }

        public static bool ForceForeground(Process process)
        {
            if (process == null) return false;

            try
            {
                if (process.HasExited) return false;

                process.Refresh();
                IntPtr hwnd = process.MainWindowHandle;
                if (hwnd == IntPtr.Zero) return false;
                return ForceForeground(hwnd);
            }
            catch
            {
                return false;
            }
        }

        private static bool ForceForeground(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero) return false;

            IntPtr foregroundWindow = GetForegroundWindow();
            uint foregroundThread = foregroundWindow != IntPtr.Zero ? GetWindowThreadProcessId(foregroundWindow, IntPtr.Zero) : 0;
            uint currentThread = GetCurrentThreadId();
            bool attached = false;

            try
            {
                if (foregroundThread != 0 && foregroundThread != currentThread)
                {
                    attached = AttachThreadInput(currentThread, foregroundThread, true);
                }

                ShowWindow(hwnd, SW_RESTORE);
                BringWindowToTop(hwnd);

                bool result = SetForegroundWindow(hwnd);
                SetFocus(hwnd);

                return result;
            }
            finally
            {
                if (attached)
                {
                    AttachThreadInput(currentThread, foregroundThread, false);
                }
            }
        }
		#endregion
	}
}
