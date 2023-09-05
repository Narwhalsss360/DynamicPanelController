using PanelExtension;
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
            LogBox.Text = Log.GetLog();
        }

        private void SwapToggleConnectionButtonText(object? Sender = null, EventArgs? Args = null)
        {
            PortConnectionToggle.Content = App.Communicating ? "Disconnect" : "Connect";
        }

        private void ToggleConnection(object Sender, EventArgs Args)
        {
            if (App.Port.IsOpen)
            {
                PortSelection.IsEnabled = true;
                App.StopPortCommunication();
                SwapToggleConnectionButtonText();
            }
            else
            {
                PortSelection.IsEnabled = false;
                App.StartPortCommunication();
                PortConnectionToggle.Content = "Disconnect";
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
            foreach (var item in SerialPort.GetPortNames())
                _ = PortSelection.Items.Add(item);
        }

        private void PortSelectionClosed(object Sender, EventArgs Args)
        {
            PortConnectionToggle.IsEnabled = false;
            if (PortSelection.SelectedIndex == -1)
                return;
            PortConnectionToggle.IsEnabled = true;
            if (App.Port.IsOpen)
                return;
            App.Port.PortName = PortSelection.Text;
        }

        private void ProfileSelectionOpened(object? Sender, EventArgs Args)
        {
            ProfileSelection.Items.Clear();
            foreach (var Profile in App.Profiles)
                _ = ProfileSelection.Items.Add(Profile.Name);
        }

        private void ProfileSelectionClosed(object? Sender, EventArgs Args)
        {
            App.SelectedProfileIndex = ProfileSelection.SelectedIndex;
            EditProfile.IsEnabled = false;
            if (ProfileSelection.SelectedIndex == -1)
                return;
            EditProfile.IsEnabled = true;
        }

        private void EditButtonClicked(object? Sender, EventArgs Args)
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
            ProfileSelectionOpened(Sender, Args);
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
        }

        private void ApplicationLogChanged(object? Sender, EventArgs Args)
        {
            _ = LogBox.Dispatcher.Invoke(SetLogBoxText, Log.GetLog());
        }
    }
}