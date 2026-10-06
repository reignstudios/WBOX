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

namespace WBOX
{
	/// <summary>
	/// Interaction logic for SystemOptimizations.xaml
	/// </summary>
	public partial class SystemOptimizations : Window
	{
		public SystemOptimizations()
		{
			InitializeComponent();
			coreIsolation.IsChecked = Reg.GetDWORDValue(@"HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity", "Enabled") == 1;
		}

		private void ApplyButton_Click(object sender, RoutedEventArgs e)
		{
			Reg.SetDWORDValue(@"HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity", "Enabled", coreIsolation.IsChecked == true ? 1 : 0, false);
			Close();
		}

		private void CancelButton_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}
	}
}
