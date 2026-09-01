using Microsoft.Win32;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WBOX
{
	/// <summary>
	/// Interaction logic for CustomApp.xaml
	/// </summary>
	public partial class CustomApp : UserControl
	{
		public CustomApp()
		{
			InitializeComponent();
		}

		private void SelectButton_Click(object sender, RoutedEventArgs e)
		{
			var dialog = new OpenFileDialog
			{
				Title = "Select Executable",
				Filter = "Executable or Batch files (*.exe;*.bat)|*.exe;*.bat",
				CheckFileExists = true,
				CheckPathExists = true
			};

			if (dialog.ShowDialog() == true)
			{
				pathTextBox.Text = dialog.FileName;
			}
		}

		private void StartButton_Click(object sender, RoutedEventArgs e)
		{
            MainWindow.Instance.LaunchApp(pathTextBox.Text, argsTextBox.Text, System.IO.Path.GetFileNameWithoutExtension(pathTextBox.Text));
		}

		private void RemoveButton_Click(object sender, RoutedEventArgs e)
		{
			MainWindow.Instance.RemoveCustomApp(this);
		}
	}
}
