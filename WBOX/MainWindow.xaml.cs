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
using System.Windows.Media.Animation;
using System.IO;

namespace WBOX
{
    public partial class MainWindow : Window
    {
        public static MainWindow Instance { get; private set; }
        private AppSettings settings;

        private Process watchedProcess;
        private string watchedProcessName;
        private DispatcherTimer timer;
        private GlobalKeyboardHook keyboardHook;

        public MainWindow()
        {
            InitializeComponent();
            versionText.Text = $"v{VersionInfo.version} alpha";

            // load settings
            settings = Settings.Load();
            steamOptimizedCheckbox.IsChecked = settings.SteamOptimized;
            steamWindowedCheckbox.IsChecked = settings.SteamWindowed;
            steamBorderlessCheckbox.IsChecked = settings.SteamBorderless;

            // intro logo
            if (settings.DefaultBoot == AppSettings.DefaultBoot_ControlCenter)
            {
                defaultBoot_ControlCenter.IsChecked = true;
                logoImage.Visibility = Visibility.Visible;
                FadeOutAndHide(logoImage, 1.0);
            }
            else
            {
                WindowState = WindowState.Minimized;
                if (settings.DefaultBoot == AppSettings.DefaultBoot_Steam)
                {
                    defaultBoot_Steam.IsChecked = true;
                    SteamButton_Click(null, null);
                }
                else if (settings.DefaultBoot == AppSettings.DefaultBoot_Playnite)
                {
                    defaultBoot_Playnite.IsChecked = true;
                    PlayniteButton_Click(null, null);
                }
                else if (settings.DefaultBoot == AppSettings.DefaultBoot_GOG)
                {
                    defaultBoot_GOG.IsChecked = true;
                    GOGButton_Click(null, null);
                }
                else if (settings.DefaultBoot == AppSettings.DefaultBoot_Itchio)
                {
                    defaultBoot_Itchio.IsChecked = true;
                    ItchioButton_Click(null, null);
                }
                else if (settings.DefaultBoot == AppSettings.DefaultBoot_Epic)
                {
                    defaultBoot_Epic.IsChecked = true;
                    EpicButton_Click(null, null);
                }
                else if (settings.DefaultBoot == AppSettings.DefaultBoot_Ubisoft)
                {
                    defaultBoot_Ubisoft.IsChecked = true;
                    UbisoftButton_Click(null, null);
                }
                else if (settings.DefaultBoot == AppSettings.DefaultBoot_EA)
                {
                    defaultBoot_EA.IsChecked = true;
                    EAButton_Click(null, null);
                }
                else if (settings.DefaultBoot == AppSettings.DefaultBoot_Battlenet)
                {
                    defaultBoot_Battlenet.IsChecked = true;
                    BattlenetButton_Click(null, null);
                }
            }

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

        private void FadeOutAndHide(UIElement element, double seconds = 0.5)
        {
            var animation = new DoubleAnimation
            {
                From = element.Opacity,
                To = 0.0,
                Duration = TimeSpan.FromSeconds(seconds),
                FillBehavior = FillBehavior.Stop
            };

            animation.Completed += (object sender, EventArgs e) =>
            {
                element.Opacity = 0.0;
                element.Visibility = Visibility.Hidden;
            };

            element.BeginAnimation(UIElement.OpacityProperty, animation);
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
            SaveSettings();
        }

		private void SaveSettings()
		{
			// save settings
            if (defaultBoot_ControlCenter.IsChecked == true) settings.DefaultBoot = AppSettings.DefaultBoot_ControlCenter;
            else if (defaultBoot_Steam.IsChecked == true) settings.DefaultBoot = AppSettings.DefaultBoot_Steam;
            else if (defaultBoot_Playnite.IsChecked == true) settings.DefaultBoot = AppSettings.DefaultBoot_Playnite;
            else if (defaultBoot_GOG.IsChecked == true) settings.DefaultBoot = AppSettings.DefaultBoot_GOG;
            else if (defaultBoot_Itchio.IsChecked == true) settings.DefaultBoot = AppSettings.DefaultBoot_Itchio;
            else if (defaultBoot_Epic.IsChecked == true) settings.DefaultBoot = AppSettings.DefaultBoot_Epic;
            else if (defaultBoot_Ubisoft.IsChecked == true) settings.DefaultBoot = AppSettings.DefaultBoot_Ubisoft;
            else if (defaultBoot_EA.IsChecked == true) settings.DefaultBoot = AppSettings.DefaultBoot_EA;
            else if (defaultBoot_Battlenet.IsChecked == true) settings.DefaultBoot = AppSettings.DefaultBoot_Battlenet;

            settings.SteamOptimized = steamOptimizedCheckbox.IsChecked == true;
            settings.SteamWindowed = steamWindowedCheckbox.IsChecked == true;
            settings.SteamBorderless = steamBorderlessCheckbox.IsChecked == true;

            Settings.Save(settings);
		}

		private void Timer_Tick(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(watchedProcessName)) return;

            // lazy watch for steam state
            var processes = Process.GetProcessesByName(watchedProcessName);
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

        public void LaunchGameApp(string path, string args, string watchProcessName)
        {
            watchedProcessName = null;
            try
            {
                // launch
                var startInfo = new ProcessStartInfo
                {
                    FileName = path,
                    Arguments = args,
                    UseShellExecute = true
                };
                watchedProcess = new Process { StartInfo = startInfo };
                watchedProcess.EnableRaisingEvents = true;
                watchedProcess.Exited += SteamProcess_Exited;
                watchedProcess.Start();
                watchedProcessName = watchProcessName;

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
                watchedProcess.Dispose();
                watchedProcess = null;
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

            // get install path
            string installPath = @"C:\Program Files (x86)\Steam\steam.exe";// default to typical
            using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Valve\Steam"))
            {
                if (key != null)
                {
                    var value = key.GetValue("InstallPath") as string;
                    if (!string.IsNullOrEmpty(value))
                    {
                        installPath = System.IO.Path.Combine(value, "steam.exe");
                    }
                }
            }

            // launch
            LaunchGameApp(installPath, args.Trim(), "steam");
        }

        private void PlayniteButton_Click(object sender, RoutedEventArgs e)
        {
            // get install path
            string userPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string installPath = System.IO.Path.Combine(userPath, @"AppData\Local\Playnite\Playnite.DesktopApp.exe");// default to typical
            using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\Playnite_is1"))
            {
                if (key != null)
                {
                    var value = key.GetValue("InstallLocation") as string;
                    if (!string.IsNullOrEmpty(value))
                    {
                        installPath = System.IO.Path.Combine(value, "Playnite.DesktopApp.exe");
                    }
                }
            }

            // launch
            LaunchGameApp(installPath, "", "Playnite.DesktopApp");
        }

        private void GOGButton_Click(object sender, RoutedEventArgs e)
        {
            // get install path
            string installPath = @"C:\Program Files (x86)\GOG Galaxy\GalaxyClient.exe";// default to typical
            using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\GOG.com\GalaxyClient\paths"))
            {
                if (key != null)
                {
                    var value = key.GetValue("client") as string;
                    if (!string.IsNullOrEmpty(value))
                    {
                        installPath = System.IO.Path.Combine(value, "GalaxyClient.exe");
                    }
                }
            }

            // launch
            LaunchGameApp(installPath, "", "GalaxyClient");
        }

        private void ItchioButton_Click(object sender, RoutedEventArgs e)
        {
            // get install path
            string userPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string installPath = System.IO.Path.Combine(userPath, @"AppData\Local\itch");// default to typical
            installPath = Utils.GetNewestSubPath(installPath);
            installPath = System.IO.Path.Combine(installPath, "itch.exe");

            using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\itch"))
            {
                if (key != null)
                {
                    var value = key.GetValue("InstallLocation") as string;
                    if (!string.IsNullOrEmpty(value))
                    {
                        installPath = Utils.GetNewestSubPath(value);
                        installPath = System.IO.Path.Combine(installPath, "itch.exe");
                    }
                }
            }

            // launch
            LaunchGameApp(installPath, "--prefer-launch --appname itch", "itch");
        }

        private void EpicButton_Click(object sender, RoutedEventArgs e)
        {
            // get install path
            string installPath = @"C:/Program Files/Epic Games/Launcher/Portal/Binaries/Win64/EpicGamesLauncher.exe";// default to typical
            using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Epic Games\EOS"))
            {
                if (key != null)
                {
                    var value = key.GetValue("ModSdkCommand") as string;
                    if (!string.IsNullOrEmpty(value))
                    {
                        installPath = value;
                    }
                }
            }

            // launch
            LaunchGameApp(installPath, "", "EpicGamesLauncher");
        }

        private void UbisoftButton_Click(object sender, RoutedEventArgs e)
        {
            // get install path
            string installPath = @"C:\Program Files (x86)\Ubisoft\Ubisoft Game Launcher\UbisoftConnect.exe";// default to typical
            using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Uplay"))
            {
                if (key != null)
                {
                    var value = key.GetValue("InstallLocation") as string;
                    if (!string.IsNullOrEmpty(value))
                    {
                        installPath = System.IO.Path.Combine(value, "UbisoftConnect.exe");
                    }
                }
            }

            // launch
            LaunchGameApp(installPath, "", "upc");
        }

        private void EAButton_Click(object sender, RoutedEventArgs e)
        {
            // get install path
            string installPath = @"C:\Program Files\Electronic Arts\EA Desktop\13.735.2.6250\EA Desktop\EADesktop.exe";// default to typical
            using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Electronic Arts\EA Desktop"))
            {
                if (key != null)
                {
                    var value = key.GetValue("InstallLocation") as string;
                    if (!string.IsNullOrEmpty(value))
                    {
                        installPath = Utils.GetNewestSubPath(value);
                        installPath = System.IO.Path.Combine(value, @"EA Desktop\EADesktop.exe");
                    }
                }
            }

            // launch
            LaunchGameApp(installPath, "", "EADesktop");
        }

        private void BattlenetButton_Click(object sender, RoutedEventArgs e)
        {
            // get install path
            string installPath = @"C:\Program Files\Epic Games\Launcher\Portal\Binaries\Win64\Battle.net Launcher.exe";// default to typical
            using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Battle.net"))
            {
                if (key != null)
                {
                    var value = key.GetValue("InstallLocation") as string;
                    if (!string.IsNullOrEmpty(value))
                    {
                        installPath = System.IO.Path.Combine(value, "Battle.net Launcher.exe");
                    }
                }
            }

            // launch
            LaunchGameApp(installPath, "", "Battle.net");
        }

        /*private void WindowsStoreButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start("ms-windows-store:");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void WindowsSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start("ms-settings:");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }*/

        private void DesktopButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DisableGameModeButton_Click(null, null);
                System.Threading.Thread.Sleep(1000);
                Process.Start("explorer.exe");
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FileExplorerButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string path = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                Process.Start("explorer.exe", $"/e,\"{path}\"");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TaskManagerButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start("taskmgr.exe");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SleepButton_Click(object sender, RoutedEventArgs e)
        {
            SaveSettings();
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
            SaveSettings();
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
            SaveSettings();
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
                    Arguments = $@"add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon"" /v Shell /t REG_SZ /d ""{shellValue}"" /f",//Computer\HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon\Shell
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
                    Arguments = $@"add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon"" /v Shell /t REG_SZ /d ""{shellValue}"" /f",//Computer\HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon\Shell
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

        private void AutoLoginManagerButton_Click(object sender, RoutedEventArgs e)
        {
            var manager = new AutoLoginManager();
            manager.Owner = this;
            manager.ShowDialog();
        }

        private void MinButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }
    }
}
