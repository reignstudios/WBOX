using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WBOX
{
	static class Reg
	{
		public static void SetStringValue(string path, string key, string value)
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
	}
}
