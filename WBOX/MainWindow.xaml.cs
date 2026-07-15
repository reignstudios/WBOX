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
using System.Threading;

namespace WBOX
{
    public partial class MainWindow : Window
    {
        public static MainWindow Instance { get; private set; }
        private AppSettings settings;
        private bool isDesktopMode;

        private Process watchedProcess;
        private string watchedProcessName;
        private DispatcherTimer timer;
        private KeyboardHook keyboardHook;

        private XInputInstance xinput;
        private Thread inputThread;
        private bool inputThreadAlive;
        private bool virtualMouseActive;

        public MainWindow()
        {
            Instance = this;
            InitializeComponent();
            versionText.Text = $"v{VersionInfo.version}";

            // load settings
            settings = Settings.Load();
            steamOptimizedCheckbox.IsChecked = settings.SteamOptimized;
            steamWindowedCheckbox.IsChecked = settings.SteamWindowed;
            steamBorderlessCheckbox.IsChecked = settings.SteamBorderless;

            autoMinCheckbox.IsChecked = settings.AutoMinimize;

            steamEnabledCheckbox.IsChecked = settings.SteamEnabled;
            playniteEnabledCheckbox.IsChecked = settings.PlayniteEnabled;
            gogEnabledCheckbox.IsChecked = settings.GOGEnabled;
            itchioEnabledCheckbox.IsChecked = settings.ItchioEnabled;
            epicEnabledCheckbox.IsChecked = settings.EpicEnabled;
            ubisoftEnabledCheckbox.IsChecked = settings.UbisoftEnabled;
            eaEnabledCheckbox.IsChecked = settings.EAEnabled;
            battlenetEnabledCheckbox.IsChecked = settings.BattlenetEnabled;
            polymegaEnabledCheckbox.IsChecked = settings.PolymegaEnabled;

            customAppStackPanel.Items.Clear();
            foreach (var customApp in settings.CustomAppSettings)
            {
                var item = new CustomApp();
                item.enabledCheckbox.IsChecked = customApp.Enabled;
                item.autoStartCheckbox.IsChecked = customApp.AutoStart;
                item.nameTextBox.Text = customApp.Name;
                item.pathTextBox.Text = customApp.Path;
                item.argsTextBox.Text = customApp.Args;
                //item.watchTextBox.Text = customApp.Watch;
                customAppStackPanel.Items.Add(item);

                if (customApp.AutoStart) ProcessUtil.LaunchProcess(customApp.Path, customApp.Args, true, false, waitForExit:false);
            }
            customAppStackPanel.Items.Refresh();

            RefreshSettingChanges();

            // check desktop mode
            isDesktopMode = Process.GetProcessesByName("explorer").Length != 0;

            // default boot
            if (settings.DefaultBoot == AppSettings.DefaultBoot_ControlCenter)
            {
                defaultBoot_ControlCenter.IsChecked = true;
            }
            else
            {
                if (!isDesktopMode) WindowState = WindowState.Minimized;
                if (settings.DefaultBoot == AppSettings.DefaultBoot_Steam)
                {
                    defaultBoot_Steam.IsChecked = true;
                    if (!isDesktopMode) SteamButton_Click(null, null);
                }
                else if (settings.DefaultBoot == AppSettings.DefaultBoot_Playnite)
                {
                    defaultBoot_Playnite.IsChecked = true;
                    if (!isDesktopMode) PlayniteButton_Click(null, null);
                }
                else if (settings.DefaultBoot == AppSettings.DefaultBoot_GOG)
                {
                    defaultBoot_GOG.IsChecked = true;
                    if (!isDesktopMode) GOGButton_Click(null, null);
                }
                else if (settings.DefaultBoot == AppSettings.DefaultBoot_Itchio)
                {
                    defaultBoot_Itchio.IsChecked = true;
                    if (!isDesktopMode) ItchioButton_Click(null, null);
                }
                else if (settings.DefaultBoot == AppSettings.DefaultBoot_Epic)
                {
                    defaultBoot_Epic.IsChecked = true;
                    if (!isDesktopMode) EpicButton_Click(null, null);
                }
                else if (settings.DefaultBoot == AppSettings.DefaultBoot_Ubisoft)
                {
                    defaultBoot_Ubisoft.IsChecked = true;
                    if (!isDesktopMode) UbisoftButton_Click(null, null);
                }
                else if (settings.DefaultBoot == AppSettings.DefaultBoot_EA)
                {
                    defaultBoot_EA.IsChecked = true;
                    if (!isDesktopMode) EAButton_Click(null, null);
                }
                else if (settings.DefaultBoot == AppSettings.DefaultBoot_Battlenet)
                {
                    defaultBoot_Battlenet.IsChecked = true;
                    if (!isDesktopMode) BattlenetButton_Click(null, null);
                }
                else if (settings.DefaultBoot == AppSettings.DefaultBoot_Polymega)
                {
                    defaultBoot_Polymega.IsChecked = true;
                    if (!isDesktopMode) PolymegaButton_Click(null, null);
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

            // init input
            inputThreadAlive = true;
            inputThread = new Thread(InputThread);
            inputThread.IsBackground = true;
            inputThread.Start();

            // TODO: add app list and custom launch button with custom args
            //var winApps = Apps.GetWinApps();
            //var storeApps = Apps.GetStoreApps();
            //var app = storeApps.FirstOrDefault(x => x.Name.ToLower().Contains("codex"));
        }

        private void RefreshSettingChanges()
        {
            defaultBoot_Steam.Visibility = steamButton.Visibility = steamButtonOptions.Visibility = settings.SteamEnabled ? Visibility.Visible : Visibility.Collapsed;
            defaultBoot_Playnite.Visibility = playniteButton.Visibility = settings.PlayniteEnabled ? Visibility.Visible : Visibility.Collapsed;
            defaultBoot_GOG.Visibility = gogButton.Visibility = settings.GOGEnabled ? Visibility.Visible : Visibility.Collapsed;
            defaultBoot_Itchio.Visibility = itchioButton.Visibility = settings.ItchioEnabled ? Visibility.Visible : Visibility.Collapsed;
            defaultBoot_Epic.Visibility = epicButton.Visibility = settings.EpicEnabled ? Visibility.Visible : Visibility.Collapsed;
            defaultBoot_Ubisoft.Visibility = ubisoftButton.Visibility = settings.UbisoftEnabled ? Visibility.Visible : Visibility.Collapsed;
            defaultBoot_EA.Visibility = eaButton.Visibility = settings.EAEnabled ? Visibility.Visible : Visibility.Collapsed;
            defaultBoot_Battlenet.Visibility = battlenetButton.Visibility = settings.BattlenetEnabled ? Visibility.Visible : Visibility.Collapsed;
            defaultBoot_Polymega.Visibility = polymegaButton.Visibility = settings.PolymegaEnabled ? Visibility.Visible : Visibility.Collapsed;

            var removeList = new List<UIElement>();
            foreach (var child in defaultBootStack.Children)
            {
                if (child is RadioButton radio)
                {
                    if (radio.Tag is CustomAppSettings) removeList.Add(radio);
                }
            }

            foreach (var item in removeList) defaultBootStack.Children.Remove(item);

            customAppButtonListBox.Items.Clear();
            foreach (var customApp in settings.CustomAppSettings)
            {
                if (!customApp.Enabled) continue;

                var button = new Button();
                button.Tag = customApp;
                button.Content = customApp.Name;
				button.Click += CustomAppButton_Click;
                customAppButtonListBox.Items.Add(button);

                if (!customApp.AutoStart)
                {
                    var radio = new RadioButton();
                    radio.Tag = customApp;
                    radio.Content = customApp.Name;
                    defaultBootStack.Children.Add(radio);
                }
            }
            customAppButtonListBox.Items.Refresh();
        }

		private void CustomAppButton_Click(object sender, RoutedEventArgs e)
		{
			var customApp = (CustomAppSettings)((Button)sender).Tag;
            LaunchGameApp(customApp.Path, customApp.Args, System.IO.Path.GetFileNameWithoutExtension(customApp.Path));
		}

		private void InputThread()
		{
            // init xinput
			try
            {
                xinput = new XInputInstance();
                xinput.Init();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "XInput Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // update
            while (inputThreadAlive)
            {
                // capture at 60fps
                Thread.Sleep(1000 / 60);
                xinput.Update();

                // check input events to relay
                foreach (var device in xinput.devices)
                {
                    if (!device.connected) continue;

                    if (device.Back.on)
                    {
                        // use scan keys at Steam uses HID
                        if (device.BumperLeft.down)
                        {
                            /*KeyboardSimulator.KeyDownScan(KeyboardSimulator.SC_LCONTROL);
                            Thread.Sleep(100);
                            KeyboardSimulator.KeyDownScan(KeyboardSimulator.SC_1);
                            Thread.Sleep(100);
                            KeyboardSimulator.KeyUpScan(KeyboardSimulator.SC_1);
                            Thread.Sleep(100);
                            KeyboardSimulator.KeyUpScan(KeyboardSimulator.SC_LCONTROL);*/

                            KeyboardSimulator.KeyDownScan(KeyboardSimulator.SC_LSHIFT);
                            Thread.Sleep(100);
                            KeyboardSimulator.KeyDownScan(KeyboardSimulator.SC_TAB);
                            Thread.Sleep(100);
                            KeyboardSimulator.KeyUpScan(KeyboardSimulator.SC_TAB);
                            Thread.Sleep(100);
                            KeyboardSimulator.KeyUpScan(KeyboardSimulator.SC_LSHIFT);
                        }
                        else if (device.BumperRight.down)
                        {
                            KeyboardSimulator.KeyDownScan(KeyboardSimulator.SC_LCONTROL);
                            Thread.Sleep(100);
                            KeyboardSimulator.KeyDownScan(KeyboardSimulator.SC_2);
                            Thread.Sleep(100);
                            KeyboardSimulator.KeyUpScan(KeyboardSimulator.SC_2);
                            Thread.Sleep(100);
                            KeyboardSimulator.KeyUpScan(KeyboardSimulator.SC_LCONTROL);
                        }
                    }

                    // virtual mouse
                    if (virtualMouseActive)
                    {
                        const int mouseSpeed = 10;
                        int deltaX = (int)((device.JoystickLeft.value.x * mouseSpeed) + (device.JoystickRight.value.x * mouseSpeed));
                        int deltaY = (int)((device.JoystickLeft.value.y * mouseSpeed) + (device.JoystickRight.value.y * mouseSpeed));
                        MouseSimulator.MoveMouse(deltaX, -deltaY);
                        if (device.A.down || device.BumperRight.down || device.TriggerButtonRight.down || device.BumperLeft.down || device.TriggerButtonLeft.down) MouseSimulator.LeftClick();
                    }
                }
            }
		}
		
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            keyboardHook = new KeyboardHook();
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
            inputThreadAlive = false;
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
            else if (defaultBoot_Polymega.IsChecked == true) settings.DefaultBoot = AppSettings.DefaultBoot_Polymega;

            settings.SteamOptimized = steamOptimizedCheckbox.IsChecked == true;
            settings.SteamWindowed = steamWindowedCheckbox.IsChecked == true;
            settings.SteamBorderless = steamBorderlessCheckbox.IsChecked == true;

            settings.AutoMinimize = autoMinCheckbox.IsChecked == true;

            settings.SteamEnabled = steamEnabledCheckbox.IsChecked == true;
            settings.PlayniteEnabled = playniteEnabledCheckbox.IsChecked == true;
            settings.GOGEnabled = gogEnabledCheckbox.IsChecked == true;
            settings.ItchioEnabled = itchioEnabledCheckbox.IsChecked == true;
            settings.EpicEnabled = epicEnabledCheckbox.IsChecked == true;
            settings.UbisoftEnabled = ubisoftEnabledCheckbox.IsChecked == true;
            settings.EAEnabled = eaEnabledCheckbox.IsChecked == true;
            settings.BattlenetEnabled = battlenetEnabledCheckbox.IsChecked == true;
            settings.PolymegaEnabled = polymegaEnabledCheckbox.IsChecked == true;

            settings.CustomAppSettings.Clear();
            foreach (CustomApp item in customAppStackPanel.Items)
            {
                var customAppSetting = new CustomAppSettings()
                {
                    Enabled = item.enabledCheckbox.IsChecked == true,
                    AutoStart = item.autoStartCheckbox.IsChecked == true,
                    Name = item.nameTextBox.Text.Trim(),
                    Path = item.pathTextBox.Text.Trim(),
                    Args = item.argsTextBox.Text.Trim(),
                    //Watch = item.watchTextBox.Text.Trim()
                };
                settings.CustomAppSettings.Add(customAppSetting);
            }

            Settings.Save(settings);
		}

		private void Timer_Tick(object sender, EventArgs e)
        {
            // only enable virtual mouse when we're in focus
            virtualMouseActive = IsActive && WindowState == WindowState.Maximized;

            // lazy watch for app state
            if (!string.IsNullOrEmpty(watchedProcessName))
            {
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
                    UseShellExecute = true,
                    WorkingDirectory = System.IO.Path.GetDirectoryName(path)
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
                watchedProcessName = null;
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

        private void PolymegaButton_Click(object sender, RoutedEventArgs e)
        {
            // get install path
            string installPath = @"C:\Program Files\Polymega\PolymegaApp.exe";// default to typical
            using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Polymega"))
            {
                if (key != null)
                {
                    var value = key.GetValue("UninstallString") as string;
                    if (!string.IsNullOrEmpty(value))
                    {
                        installPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(value), "PolymegaApp.exe");
                    }
                }
            }

            // launch
            LaunchGameApp(installPath, "", "PolymegaApp");
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
                Thread.Sleep(1000);
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
                    Arguments = $@"add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon"" /v Shell /t REG_SZ /d ""{shellValue}"" /f",//Computer\HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon
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
                    Arguments = $@"add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon"" /v Shell /t REG_SZ /d ""{shellValue}"" /f",//Computer\HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon
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

        private void DesktopSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var manager = new DesktopSettingsWindow();
            manager.Owner = this;
            manager.ShowDialog();
        }

        private void MinButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

		private void SettingsButton_Click(object sender, RoutedEventArgs e)
		{
            settingsGrid.Visibility = Visibility.Visible;
		}

		private void SettingsDoneButton_Click(object sender, RoutedEventArgs e)
		{
            settingsGrid.Visibility = Visibility.Hidden;
            SaveSettings();
            RefreshSettingChanges();
		}

		private void AddCustomAppButton_Click(object sender, RoutedEventArgs e)
		{
            var item = new CustomApp();
            customAppStackPanel.Items.Add(item);
            customAppStackPanel.Items.Refresh();
		}

        public void RemoveCustomApp(CustomApp customApp)
        {
            customAppStackPanel.Items.Remove(customApp);
            customAppStackPanel.Items.Refresh();
        }
	}
}
