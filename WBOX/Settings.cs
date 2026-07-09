using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace WBOX
{
	public class CustomAppSettings
	{
		public bool Enabled { get; set; }
		public string Path { get; set; }
		public string Args { get; set; }
		public string Watch { get; set; }
	}

	public class AppSettings
	{
		public const string DefaultBoot_ControlCenter = "ControlCenter";
		public const string DefaultBoot_Steam = "Steam";
		public const string DefaultBoot_Playnite = "Playnite";
		public const string DefaultBoot_GOG = "GOG";
		public const string DefaultBoot_Itchio = "Itchio";
		public const string DefaultBoot_Epic = "Epic";
		public const string DefaultBoot_Ubisoft = "Ubisoft";
		public const string DefaultBoot_EA = "EA";
		public const string DefaultBoot_Battlenet = "Battlenet";
		public const string DefaultBoot_Polymega = "Polymega";

		public string DefaultBoot { get; set; }
		public bool SteamOptimized { get; set; }
		public bool SteamWindowed { get; set; }
		public bool SteamBorderless { get; set; }

		public bool AutoMinimize { get; set; }

		public List<CustomAppSettings> CustomAppSettings { get; set; }

		public AppSettings()
		{
			// set defaults
			DefaultBoot = DefaultBoot_ControlCenter;
			SteamOptimized = true;
			AutoMinimize = true;
			CustomAppSettings = new List<CustomAppSettings>();
		}
	}

	static class Settings
	{
		private readonly static string path;

		static Settings()
		{
			string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			path = Path.Combine(appDataPath, "WBOX");
			if (!Directory.Exists(path)) Directory.CreateDirectory(path);
			path = Path.Combine(path, "Settings.xml");
		}

		public static AppSettings Load()
		{
			try
			{
				var serializer = new XmlSerializer(typeof(AppSettings));
				using (var stream = new FileStream(path, FileMode.Open))
				{
					return (AppSettings)serializer.Deserialize(stream);
				}
			}
			catch { }

			return new AppSettings();
		}

		public static void Save(AppSettings settings)
		{
			try
			{
				var serializer = new XmlSerializer(typeof(AppSettings));
				using (var stream = new FileStream(path, FileMode.Create))
				{
					serializer.Serialize(stream, settings);
				}
			}
			catch { }
		}
	}
}
