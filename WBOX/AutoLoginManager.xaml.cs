using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WBOX
{
	/// <summary>
	/// Interaction logic for AutoLoginManager.xaml
	/// </summary>
	public partial class AutoLoginManager : Window
	{
		public AutoLoginManager()
		{
			InitializeComponent();
			usernameText.Text = Environment.UserName;
		}

		private void EnableButton_Click(object sender, RoutedEventArgs e)
		{
			SetAutoLogin(true);
		}

		private void DisableButton_Click(object sender, RoutedEventArgs e)
		{
			SetAutoLogin(false);
		}

		private void SetAutoLogin(bool enable)
		{
			string bash = System.IO.Path.Combine(AppContext.BaseDirectory, "AutoLogin.bat");
			string enableArg = enable ? "1" : "0";
			string userArg = enable ? usernameText.Text : "";
			string passArg = enable ? passwordText.Password : "";
			ProcessUtil.LaunchAdminProcess(bash, $"{enableArg} {userArg} {passArg}", false, false);
			Close();
		}
	}
}
