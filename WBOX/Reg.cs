using System;
using System.Diagnostics;
using System.Windows;
using System.Globalization;

namespace WBOX
{
	static class Reg
	{
		public static void SetStringValue(string path, string key, string value, bool admin)
		{
			try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "reg.exe",
                    Arguments = $@"add ""{path}"" /v {key} /t REG_SZ /d ""{value}"" /f",
                    Verb = "runas",
                    UseShellExecute = true,
                    CreateNoWindow = false
                };
                Process.Start(startInfo).WaitForExit();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
		}

        public static void SetDWORDValue(string path, string key, int value, bool admin)
		{
			try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "reg.exe",
                    Arguments = $@"add ""{path}"" /v {key} /t REG_DWORD /d ""{value}"" /f",
                    Verb = "runas",
                    UseShellExecute = true,
                    CreateNoWindow = false
                };
                Process.Start(startInfo).WaitForExit();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
		}

        public static string GetStringValue(string path, string key)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "reg.exe",
                    Arguments = $@"query ""{path}"" /v ""{key}""",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using (var process = Process.Start(startInfo))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();
                    if (process.ExitCode != 0) return null;

                    foreach (string line in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        int typeIndex = line.IndexOf("REG_SZ", StringComparison.OrdinalIgnoreCase);
                        if (typeIndex >= 0) return line.Substring(typeIndex + "REG_SZ".Length).Trim();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return null;
        }

        public static int? GetDWORDValue(string path, string key)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "reg.exe",
                    Arguments = $@"query ""{path}"" /v ""{key}""",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using (var process = Process.Start(startInfo))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();
                    if (process.ExitCode != 0) return null;

                    foreach (string line in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        int typeIndex = line.IndexOf("REG_DWORD", StringComparison.OrdinalIgnoreCase);
                        if (typeIndex >= 0)
                        {
                            string value = line.Substring(typeIndex + "REG_DWORD".Length).Trim();
                            if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))// reg.exe normally outputs DWORDs as 0xXXXXXXXX
                            {
                                if (int.TryParse(value.Substring(2), NumberStyles.HexNumber, null, out int result)) return result;
                            }
                            else if (int.TryParse(value, out int result))
                            {
                                return result;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return null;
        }
	}
}
