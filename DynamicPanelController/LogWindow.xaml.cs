using PanelExtension;
using Profiling;
using System;
using System.IO.Ports;
using System.Windows;

namespace DynamicPanelController
{
    public partial class LogWindow : Window
    {
        private readonly App App = (App)Application.Current;
        private readonly ILogger Log = App.Logger;
        private ProfileEditor? EditorWindow = null;
        private PanelEmulator? EmulatorWindow = null;
        private bool IgnoreNextSelectionChange = true;
        private SettingsWindow? SettingsWindow { get; set; } = null;

        public LogWindow()
        {
            if (App is null)
                throw new Exception("Couldn't get current app.");
            InitializeComponent();
            Loaded += WindowLoaded;
            Closed += WindowClosed;
            Log.LogChanged += ApplicationLogChanged;
            App.CommunicationsStarted += ApplicationStartedCommunicating;
            App.CommunicationsStopped += ApplicationStoppedCommunicating;
            App.SelectedProfileChanged += ProfileSelectionChanged;
        }

        private void WindowLoaded(object Sender, EventArgs Args)
        {
            UpdateProfileSelectionItems();
            ProfileSelection.SelectedIndex = 0;
            ApplicationLogChanged(Sender, Args);
        }

        private void WindowClosed(object? sender, EventArgs e)
        {
            SettingsWindow?.Close();
            EditorWindow?.Close();
            EmulatorWindow?.Close();
        }

        private void SwapToggleConnectionButtonText(object? Sender = null, EventArgs? Args = null)
        {
            PortConnectionToggle.Dispatcher.Invoke(() => { PortConnectionToggle.Content = App.Communicating ? "Disconnect" : "Connect"; });
        }

        private void ToggleConnection(object? Sender, EventArgs Args)
        {
            if (App.Communicating)
            {
                App.StopPortCommunication();
            }
            else
            {
                bool Emulate = PortSelection.Text == "Emulator";
                App.StartPortCommunication(Emulate);
                if (Emulate)
                {
                    EmulatorWindow = new();
                    EmulatorWindow.Closed += EmulatorClosed;
                    EmulatorWindow.Show();
                }
            }
        }

        private void ApplicationStartedCommunicating(object? Sender, EventArgs Args)
        {
            PortSelection.Dispatcher.Invoke(() => { PortSelection.IsEnabled = false; });
            PortConnectionToggle.Dispatcher.Invoke(() => { PortConnectionToggle.Content = "Disconnect"; });
        }

        private void ApplicationStoppedCommunicating(object? Sender, EventArgs Args)
        {
            PortSelection.Dispatcher.Invoke(() => { PortSelection.IsEnabled = true; });
            PortConnectionToggle.Dispatcher.Invoke(() => { PortConnectionToggle.Content = "Connect"; });
            EmulatorWindow?.Dispatcher.Invoke(() => { EmulatorWindow.Close(); });
        }

        private void EmulatorClosed(object? Sender, EventArgs Args)
        {
            if (App.Communicating)
            {
                ToggleConnection(Sender, Args);
                return;
            }
        }

        private void LogBoxTextChanged(object Sender, EventArgs E)
        {
            LogBox.ScrollToEnd();
        }

        private void SetLogBoxText(string Text)
        {
            LogBox.Text = Text;
        }

        private void PortSelectionOpened(object Sender, EventArgs Args)
        {
            PortSelection.Items.Clear();
            if (App.AllowEmulator)
                _ = PortSelection.Items.Add("Emulator");
            foreach (var item in SerialPort.GetPortNames())
                _ = PortSelection.Items.Add(item);
        }

        private void PortSelectionClosed(object Sender, EventArgs Args)
        {
            PortConnectionToggle.IsEnabled = false;
            if (PortSelection.SelectedIndex == -1)
                return;
            PortConnectionToggle.IsEnabled = true;
            if (App.Communicating)
                return;
            if (PortSelection.Text != "Emulator")
                App.Port.PortName = PortSelection.Text;
        }

        private void UpdateProfileSelectionItems()
        {
            IgnoreNextSelectionChange = true;
            App.RefreshProfiles();
            ProfileSelection.Items.Clear();
            foreach (var Profile in App.Profiles)
                _ = ProfileSelection.Items.Add(Profile.Name);
            ProfileSelection.SelectedIndex = App.SelectedProfileIndex;
        }

        private void ProfileSelectorOpened(object? Sender, EventArgs Args)
        {
            UpdateProfileSelectionItems();
        }

        private void ProfileSelectorSelectionChanged(object? Sender, EventArgs Args)
        {
            DeleteProfile.IsEnabled = false;
            EditProfile.IsEnabled = false;
            if (ProfileSelection.SelectedIndex == -1)
                return;
            IgnoreNextSelectionChange = true;
            App.SelectIndex(ProfileSelection.SelectedIndex);
            EditProfile.IsEnabled = true;
            DeleteProfile.IsEnabled = true;
        }

        private void ProfileSelectorClosed(object? Sender, EventArgs Args)
        {
            if (ProfileSelection.SelectedIndex == -1)
                ProfileSelection.SelectedIndex = 0;
        }

        private void ProfileSelectionChanged(object? Sender, SelectedProfileChangedEventArgs Args)
        {
            if (IgnoreNextSelectionChange)
            {
                IgnoreNextSelectionChange = false;
                return;
            }
            UpdateProfileSelectionItems();
            ProfileSelection.SelectedIndex = Args.NewIndex;
        }

        private void NewProfileButtonClicked(object? Sender, EventArgs Args)
        {
            App.Profiles.Add(new PanelProfile() { Name = "New Profile" });
            App.SelectIndex(App.Profiles.Count - 1);
            UpdateProfileSelectionItems();
        }

        private void DeleteProfileButtonClicked(object? Sender, EventArgs Args)
        {
            if (MessageBox.Show($"Are you sure you want to delete {ProfileSelection.SelectedItem}?", "Are you sure?", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                return;
            App.Profiles.RemoveAt(ProfileSelection.SelectedIndex);
            UpdateProfileSelectionItems();
            ProfileSelection.SelectedIndex = 0;
        }

        private void EditProfileButtonClicked(object? Sender, EventArgs Args)
        {
            if (EditorWindow is not null)
                return;

            EditorWindow = new ProfileEditor(App.SelectedProfileIndex);
            EditorWindow.Closed += EditorWindowClosed;
            EditorWindow.Show();
        }

        private void EditorWindowClosed(object? Sender, EventArgs Args)
        {
            if (EditorWindow is not null)
                App.Profiles[App.SelectedProfileIndex] = EditorWindow.EditiedVersion;
            EditorWindow = null;
            UpdateProfileSelectionItems();
        }

        private void SettingsButtonClicked(object? Sender, EventArgs Args)
        {
            if (SettingsWindow is not null)
                return;
            SettingsWindow = new SettingsWindow(App.Settings, SetSettings);
            SettingsWindow.Closed += SettingsWindowClosed;
            SettingsWindow.Show();
        }

        private void SetSettings()
        {
            if (SettingsWindow is not null)
            {
                if (SettingsWindow.Validated)
                    App.Settings = SettingsWindow.EditedSettings;
                ApplicationLogChanged(this, new EventArgs());
            }
        }

        private void SettingsWindowClosed(object? Sender, EventArgs Args)
        {
            SettingsWindow = null;
        }

        private void ApplicationLogChanged(object? Sender, EventArgs Args)
        {
            if (App.Settings.LogLevel != ILogger.Levels.Verbose)
            {
                string LeveledLog = string.Empty;
                string[] ExcludeLevelStrings = new string[(int)App.Settings.LogLevel];

                for (int i = 0; i < ExcludeLevelStrings.Length; i++)
                    ExcludeLevelStrings[i] = ((ILogger.Levels)i).ToString();

                foreach (var LogLine in Log.GetLog().Split('\n'))
                {
                    bool Exclude = false;
                    foreach (var ExcludeString in ExcludeLevelStrings)
                    {
                        if (LogLine.Contains(ExcludeString))
                        {
                            Exclude = true;
                            break;
                        }
                    }
                    if (Exclude)
                        continue;
                    LeveledLog += $"{LogLine}\n";
                }

                _ = LogBox.Dispatcher.Invoke(SetLogBoxText, LeveledLog);
            }
            else
            {
                _ = LogBox.Dispatcher.Invoke(SetLogBoxText, Log.GetLog());
            }
        }
    }
}