using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using System.Text.RegularExpressions;

namespace WBOX
{
	/// <summary>
	/// Interaction logic for DesktopSettingsWindow.xaml
	/// </summary>
	public partial class DesktopSettingsWindow : Window
	{
		public Color color;

		public DesktopSettingsWindow()
		{
			InitializeComponent();

			string bash = System.IO.Path.Combine(AppContext.BaseDirectory, "DesktopSettings_Get.bat");
			string output = ProcessUtil.LaunchProcess(bash, "", false, true);
			try
			{
				var match = Regex.Match(output, "(\".*\") (\".*\") (\".*\") (\".*\") (\".*\") (\".*\")");
				if (match.Success)
				{
					var colorValue = match.Groups[1].Value.Replace("\"", "").Split(' ');
					color.R = byte.Parse(colorValue[0]);
					color.G = byte.Parse(colorValue[1]);
					color.B = byte.Parse(colorValue[2]);
					color.A = 255;
					colorGrid.Background = new SolidColorBrush(color);
					
					transparancyEffects.IsChecked = Convert.ToInt32(match.Groups[2].Value.Replace("\"", ""), 16) != 0;
					activationWatermark.IsChecked = Convert.ToInt32(match.Groups[3].Value.Replace("\"", ""), 16) != 0;
					wallpaperText.Text = match.Groups[4].Value.Replace("\"", "");
					darkMode.IsChecked = Convert.ToInt32(match.Groups[5].Value.Replace("\"", ""), 16) == 0 && Convert.ToInt32(match.Groups[6].Value.Replace("\"", ""), 16) == 0;
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void PickColorButton_Click(object sender, RoutedEventArgs e)
		{
			using (var dialog = new System.Windows.Forms.ColorDialog())
			{
				if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
				{
					var c = dialog.Color;
					color = Color.FromArgb(255, c.R, c.G, c.B);
					colorGrid.Background = new SolidColorBrush(color);
				}
			}
		}

		private void CancelButton_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}

		private void ApplyButton_Click(object sender, RoutedEventArgs e)
		{
			string bash = System.IO.Path.Combine(AppContext.BaseDirectory, "DesktopSettings_Set.bat");
			string c = $"{color.R} {color.G} {color.B}";
			string transparancy = transparancyEffects.IsChecked == true ? "1" : "0";
			string watermark = activationWatermark.IsChecked == true ? "1" : "0";
			string wallpapaer = wallpaperText.Text;
			string lightTheme = darkMode.IsChecked == true ? "0" : "1";
			ProcessUtil.LaunchProcess(bash, $"\"{c}\" {transparancy} {watermark} \"{wallpapaer}\" {lightTheme}", false, false);

			int watermark_check = activationWatermark.IsChecked == true ? 3 : 4;
			Reg.SetDWORDValue(@"HKLM\SYSTEM\CurrentControlSet\Services\svsvc", "Start", watermark_check, true);

			Close();
		}

		private void SelectWallpaperButton_Click(object sender, RoutedEventArgs e)
		{
			var dialog = new Microsoft.Win32.OpenFileDialog
			{
				Title = "Select Image",
				Filter = "Images (*.png;*.jpg)|*.png;*.jpg",
				CheckFileExists = true,
				CheckPathExists = true
			};

			if (dialog.ShowDialog() == true)
			{
				wallpaperText.Text = dialog.FileName;
			}
		}
	}
}
