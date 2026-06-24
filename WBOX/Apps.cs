using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace WBOX
{
	public sealed class WinApp
	{
		public string Name;
		public string Path;

        public void Launch()
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = Path,
                UseShellExecute = true
            });
        }

		public override string ToString()
		{
			return Name;
		}
	}

    public sealed class StoreApp
    {
        public string Name { get; set; } = "";
        public string AppID { get; set; } = "";

        public void Launch()
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"shell:AppsFolder\\{AppID}",
                UseShellExecute = true
            });
        }

		public override string ToString()
		{
			return Name;
		}
    }

	static class Apps
	{
		public static List<WinApp> GetWinApps()
        {
            var results = new List<WinApp>();

            string[] startMenuFolders =
            {
                Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
                Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu)
            };

            foreach (var folder in startMenuFolders)
            {
                if (!Directory.Exists(folder)) continue;

                foreach (var file in Directory.EnumerateFiles(folder, "*.lnk", SearchOption.AllDirectories))
                {
                    results.Add(new WinApp
                    {
                        Name = Path.GetFileNameWithoutExtension(file),
                        Path = file
                    });
                }
            }

            return results;
        }

        public static List<StoreApp> GetStoreApps()
        {
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = "-NoProfile -ExecutionPolicy Bypass -Command \"Get-StartApps | ConvertTo-Json\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            using (var process = Process.Start(psi))
            {
                string json = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                if (string.IsNullOrWhiteSpace(json)) return new List<StoreApp>();

                // PowerShell returns either a single object or array depending on count.
                if (json.TrimStart().StartsWith("["))
                {
                    return JsonConvert.DeserializeObject<List<StoreApp>>(json);
                }
                else
                {
                    var single = JsonConvert.DeserializeObject<StoreApp>(json);
                    return single != null ? new List<StoreApp> { single } : new List<StoreApp>();
                }
            }
        }
	}
}
