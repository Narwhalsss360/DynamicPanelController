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

        private SettingsWindow? SettingsWindow { get; set; } = null;

        public LogWindow()
        {
            if (App is null)
                throw new Exception("Couldn't get current app.");
            InitializeComponent();
            Loaded += WindowLoaded;
            Log.LogChanged += ApplicationLogChanged;
            App.CommunicationsStarted += SwapToggleConnectionButtonText;
            App.CommunicationsStopped += SwapToggleConnectionButtonText;
        }

        private void WindowLoaded(object Sender, EventArgs Args)
        {
            UpdateProfileSelectionItems();
            ProfileSelection.SelectedIndex = 0;
            LogBox.Text = Log.GetLog();
        }

        private void SwapToggleConnectionButtonText(object? Sender = null, EventArgs? Args = null)
        {
            PortConnectionToggle.Content = App.Communicating ? "Disconnect" : "Connect";
        }

        private void ToggleConnection(object Sender, EventArgs Args)
        {
            if (App.Communicating)
            {
                PortSelection.IsEnabled = true;
                App.StopPortCommunication();
                SwapToggleConnectionButtonText();
            }
            else
            {
                PortSelection.IsEnabled = false;
                App.StartPortCommunication(PortSelection.Text == "Emulator");
                SwapToggleConnectionButtonText();
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
                PortSelection.Items.Add("Emulator");
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
            ProfileSelection.Items.Clear();
            foreach (var Profile in App.Profiles)
                _ = ProfileSelection.Items.Add(Profile.Name);
        }

        private void ProfileSelectionOpened(object? Sender, EventArgs Args)
        {
            UpdateProfileSelectionItems();
        }

        private void ProfileSelectionChanged(object? Sender, EventArgs Args)
        {
            DeleteProfile.IsEnabled = false;
            EditProfile.IsEnabled = false;
            if (ProfileSelection.SelectedIndex == -1)
                ProfileSelection.SelectedIndex = 0;
            if (ProfileSelection.SelectedIndex == -1)
                return;
            App.SelectedProfileIndex = ProfileSelection.SelectedIndex;
            EditProfile.IsEnabled = true;
            DeleteProfile.IsEnabled = true;
        }

        private void NewProfileButtonClicked(object? Sender, EventArgs Args)
        {
            App.Profiles.Add(new PanelProfile() { Name = "New Profile"});
            UpdateProfileSelectionItems();
            ProfileSelection.SelectedIndex = ProfileSelection.Items.Count - 1;
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
            ProfileSelection.SelectedIndex = App.SelectedProfileIndex;
        }

        private void SettingsButtonClicked(object? Sender, EventArgs Args)
        {
            if (SettingsWindow is not null)
                return;
            SettingsWindow = new SettingsWindow(App.Settings);
            SettingsWindow.Closed += SettingsWindowClosed;
            SettingsWindow.Show();
        }

        private void SettingsWindowClosed(object? Sender, EventArgs Args)
        {
            if (SettingsWindow is null)
                return;
            if (SettingsWindow.Validated)
                App.Settings = SettingsWindow.EditedSettings;
            SettingsWindow = null;
        }

        private void ApplicationLogChanged(object? Sender, EventArgs Args)
        {
            _ = LogBox.Dispatcher.Invoke(SetLogBoxText, Log.GetLog());
        }
    }
}