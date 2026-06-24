using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using System.Windows.Threading;

using Microsoft.Win32;
using System.Windows.Interop;
using System.ComponentModel;

namespace WBOX
{
    public partial class MainWindow : Window
    {
        public static MainWindow Instance { get; private set; }
        private Process steamProcess;
        private DispatcherTimer timer;
        private GlobalKeyboardHook keyboardHook;

        public MainWindow()
        {
            InitializeComponent();

            // watch for app activity
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(5);
            timer.Tick += Timer_Tick;
            timer.Start();

            // bind events
            Loaded += MainWindow_Loaded;
            Closed += MainWindow_Closed;

            // TODO: add app list and custom launch button with custom args
            //var winApps = Apps.GetWinApps();
            //var storeApps = Apps.GetStoreApps();
            //var app = storeApps.FirstOrDefault(x => x.Name.ToLower().Contains("codex"));
        }
		
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            keyboardHook = new GlobalKeyboardHook();
            keyboardHook.OnKeyPressed += (s, args) =>
            {
                const float volumeStep = 1f / 20f;
                if (args.Key == Key.VolumeUp)
                {
                    VolumeWindow.AdjustVolume(volumeStep);
                }
                else if (args.Key == Key.VolumeDown)
                {
                    VolumeWindow.AdjustVolume(-volumeStep);
                }
                else if (args.Key == Key.VolumeMute)
                {
                    VolumeWindow.MuteToggle();
                }
            };
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            if (keyboardHook != null) keyboardHook.Dispose();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            // lazy watch for steam state
            var processes = Process.GetProcessesByName("steam");
            bool isAlive = processes != null && processes.Length > 0;
            if (isAlive)
            {
                if (WindowState != WindowState.Minimized)
                {
                    if (autoMinCheckbox.IsChecked == true) WindowState = WindowState.Minimized;
                    minButton.Visibility = Visibility.Visible;
                }
            }
            else
            {
                if (WindowState != WindowState.Maximized)
                {
                    WindowState = WindowState.Maximized;
                    minButton.Visibility = Visibility.Hidden;
                }
            }
        }

        public void LaunchSteam(string args)
        {
            try
            {
                // get steam install path
                string installPath = @"C:\Program Files (x86)\Steam";// default to typical
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Valve\Steam"))
                {
                    if (key != null)
                    {
                        var value = key.GetValue("InstallPath") as string;
                        if (!string.IsNullOrEmpty(value)) installPath = value;
                    }
                }
                installPath = System.IO.Path.Combine(installPath, "steam.exe");

                // launch steam
                var startInfo = new ProcessStartInfo
                {
                    FileName = installPath,
                    Arguments = args,
                    UseShellExecute = true
                };
                steamProcess = new Process { StartInfo = startInfo };
                steamProcess.EnableRaisingEvents = true;
                steamProcess.Exited += SteamProcess_Exited;
                steamProcess.Start();

                // minimize window to reduce overhead
                WindowState = WindowState.Minimized;
                minButton.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SteamProcess_Exited(object sender, EventArgs e)
        {
            try
            {
                Dispatcher.InvokeAsync(new Action(() =>
                {
                    WindowState = WindowState.Maximized;
                    minButton.Visibility = Visibility.Hidden;
                }));
                steamProcess.Dispose();
                steamProcess = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SteamButton_Click(object sender, RoutedEventArgs e)
        {
            string args = "";

            // steam mode
            if (steamWindowedCheckbox.IsChecked == true) args += " -windowed";
            else args += " -bigpicture";

            // borderless game windows
            if (steamBorderlessCheckbox.IsChecked == true) args += " -noborder";

            // optimized args
            if (steamOptimizedCheckbox.IsChecked == true) args += " -no-browser";

            // launch
            LaunchSteam(args.Trim());
        }

        private void WindowsStoreButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "ms-windows-store:",
                UseShellExecute = true
            });
        }

        private void DesktopButton_Click(object sender, RoutedEventArgs e)
        {
            DisableGameModeButton_Click(null, null);
            System.Threading.Thread.Sleep(1000);
            Process.Start("explorer.exe");
            Close();
        }

        private void FileExplorerButton_Click(object sender, RoutedEventArgs e)
        {
            string path = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            Process.Start("explorer.exe", $"/e,\"{path}\"");
        }

        private void TaskManagerButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("taskmgr.exe");
        }

        private void SleepButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!Power.SetSuspendState(false, false, false))
                {
                    if (!Power.SetSuspendState(true, false, false))
                    {
                        MessageBox.Show("Suspend & Hibernate not supported", "Info", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RebootButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start("shutdown", "/r /t 0");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShutdownButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start("shutdown", "/s /t 0");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EnableGameModeButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string shellValue = System.IO.Path.Combine(AppContext.BaseDirectory, "WBOX.exe");
                var startInfo = new ProcessStartInfo
                {
                    FileName = "reg.exe",
                    Arguments = $@"add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon"" /v Shell /t REG_SZ /d ""{shellValue}"" /f",
                    Verb = "runas",
                    UseShellExecute = true,
                    CreateNoWindow = false
                };
                Process.Start(startInfo).WaitForExit();
                RebootButton_Click(null, null);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DisableGameModeButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                const string shellValue = "explorer.exe";
                var startInfo = new ProcessStartInfo
                {
                    FileName = "reg.exe",
                    Arguments = $@"add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon"" /v Shell /t REG_SZ /d ""{shellValue}"" /f",
                    Verb = "runas",
                    UseShellExecute = true,
                    CreateNoWindow = false
                };
                Process.Start(startInfo).WaitForExit();
                if (sender != null) RebootButton_Click(null, null);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MinButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }
    }
}
