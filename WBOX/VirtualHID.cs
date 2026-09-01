using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WBOX
{
	static class VirtualHID
	{
		private const string lib = "WBOX_VirtualHID.dll";

		[DllImport(lib, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
		private static extern int WBOX_VirtualHID_Init();

		[DllImport(lib, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
		private static extern void WBOX_VirtualHID_Dispose();

		[DllImport(lib, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
		private static extern void WBOX_VirtualHID_TriggerLeftMenu();

		[DllImport(lib, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
		private static extern void WBOX_VirtualHID_TriggerLeftInGameMenu();

		[DllImport(lib, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
		private static extern void WBOX_VirtualHID_TriggerRightMenu();

		public static bool Init()
		{
			try
			{
				return WBOX_VirtualHID_Init() != 0;
			}
			catch { }
			/*catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}*/
			return false;
		}

		public static void Dispose()
		{
			try
			{
				WBOX_VirtualHID_Dispose();
			}
			catch { }
			/*catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}*/
		}

		public static void TriggerLeftMenu()
		{
			try
			{
				WBOX_VirtualHID_TriggerLeftMenu();
			}
			catch { }
			/*catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}*/
		}

		public static void TriggerLeftInGameMenu()
		{
			try
			{
				WBOX_VirtualHID_TriggerLeftInGameMenu();
			}
			catch { }
			/*catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}*/
		}

		public static void TriggerRightMenu()
		{
			try
			{
				WBOX_VirtualHID_TriggerRightMenu();
			}
			catch { }
			/*catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}*/
		}
	}
}
