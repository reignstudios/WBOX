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
using System.Windows.Threading;

namespace WBOX
{
	/// <summary>
	/// Interaction logic for VolumeWindow.xaml
	/// </summary>
	public partial class VolumeWindow : Window
	{
		private static VolumeWindow instance;
		private bool isClosed;
		private DispatcherTimer timer;
		private bool canClose = true;

		public VolumeWindow()
		{
			InitializeComponent();

			// close window after short time
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(2);
            timer.Tick += Timer_Tick;
            timer.Start();
		}

		private void Timer_Tick(object sender, EventArgs e)
		{
			if (canClose) Close();
			canClose = true;
		}

		protected override void OnClosed(EventArgs e)
		{
			isClosed = true;
			base.OnClosed(e);
		}

		private static void InitShow(float volume)
		{
			if (instance == null || instance.isClosed) instance = new VolumeWindow();
			if (instance.Owner == null) instance.Owner = MainWindow.Instance;

			instance.canClose = false;
			if (volume >= 0)
			{
				instance.volumeBar.Foreground = new SolidColorBrush(Colors.Green);
				instance.volumeBar.Value = volume * 100;
			}
			else
			{
				instance.volumeBar.Foreground = new SolidColorBrush(Colors.Red);
				instance.volumeBar.Value = 100;
			}
			instance.Show();
			instance.Focus();
		}

		public static void AdjustVolume(float step)
		{
			InitShow(Audio.AdjustVolume(step));
		}

		public static void MuteToggle()
		{
			InitShow(Audio.MuteToggle() ? -1 : Audio.GetVolume());
		}
	}
}
