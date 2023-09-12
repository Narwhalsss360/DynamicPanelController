using PanelExtension;
using System;
using System.Collections.Generic;
using System.Windows;

namespace DynamicPanelController
{
    public partial class SettingsWindow : Window
    {
        public ApplicationSettings EditedSettings;
        public App App = (App)Application.Current;

        private List<BindableDictionaryPair> Options { get; set; } = new();

        private PanelDescriptorEditor? DescriptorEditor;
        public bool Validated = false;

        public delegate void SetSettings();

        public SetSettings? SettingsSetter;

        public SettingsWindow(ApplicationSettings? SettingsTemplate = null, SetSettings? SettingsSetter = null)
        {
            EditedSettings = SettingsTemplate is null ? new ApplicationSettings() : new(SettingsTemplate);
            this.SettingsSetter = SettingsSetter;
            InitializeComponent();
            Loaded += WindowLoaded;
            Closed += WindowClosed;
        }

        private void WindowClosed(object? sender, EventArgs e)
        {
            DescriptorEditor?.Close();
        }

        private string? VerifyValid()
        {
            EditedSettings.ExtensionsDirectory = ExtensionsDirectoryEntry.Text;
            EditedSettings.ProfilesDirectory = ProfilesDirectoryEntry.Text;
            EditedSettings.LogDirectory = LogDirectoryEntry.Text;
            Validated = true;
            return null;
        }

        private void OKClicked(object? Sender, EventArgs Args)
        {
            if (VerifyValid() is string ErrorMessage)
            {
                _ = MessageBox.Show(ErrorMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (SettingsSetter is not null)
                SettingsSetter();
            Close();
        }

        private void CancelClicked(object? Sender, EventArgs Args)
        {
            Validated = false;
            Close();
        }

        private void ApplyClicked(object? Sender, EventArgs Args)
        {
            if (VerifyValid() is string ErrorMessage)
            {
                _ = MessageBox.Show(ErrorMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (SettingsSetter is not null)
                SettingsSetter();
        }

        private void GlobalOptionSelected(object? Sender, EventArgs Args)
        {
            RemoveOptionButton.IsEnabled = GlobalOptionsPanel.SelectedIndex >= 0;
        }

        private void RemoveButtonClicked(object? Sender, EventArgs Args)
        {
            if (GlobalOptionsPanel.SelectedIndex == -1)
                return;
            if (EditedSettings.GlobalOptions.ContainsKey(Options[GlobalOptionsPanel.SelectedIndex].Key))
                _ = EditedSettings.GlobalOptions.Remove(Options[GlobalOptionsPanel.SelectedIndex].Key);
            Options.RemoveAt(GlobalOptionsPanel.SelectedIndex);
            GlobalOptionsPanel.Items.Refresh();
        }

        private void AddButtonClicked(object? Sender, EventArgs Args)
        {
            string KeyName = "New Key";
            for (int i = 1; EditedSettings.GlobalOptions.ContainsKey(KeyName); i++)
                KeyName = $"New Key({i})";

            EditedSettings.GlobalOptions.Add(KeyName, "New Value");
            Options.Add(new(EditedSettings.GlobalOptions, KeyName, "New Value"));
            GlobalOptionsPanel.Items.Refresh();
        }

        private void EditDescriptorClicked(object? Sender, EventArgs Args)
        {
            if (DescriptorEditor is not null)
                return;
            DescriptorEditor = new PanelDescriptorEditor(EditedSettings.GlobalPanelDescriptor);
            DescriptorEditor.Closed += DescriptorEditorClosed;
            DescriptorEditor.Show();
        }

        private void DescriptorEditorClosed(object? Sender, EventArgs Args)
        {
            if (DescriptorEditor is null)
                return;
            EditedSettings.GlobalPanelDescriptor = DescriptorEditor.Descriptor;
            DescriptorEditor = null;
        }

        private void WindowLoaded(object? Sender, EventArgs Args)
        {
            ExtensionsDirectoryEntry.Text = EditedSettings.ExtensionsDirectory;
            ProfilesDirectoryEntry.Text = EditedSettings.ProfilesDirectory;
            LogDirectoryEntry.Text = EditedSettings.LogDirectory;
            foreach (var KVP in EditedSettings.GlobalOptions)
                Options.Add(new(EditedSettings.GlobalOptions, KVP.Key, KVP.Value));
            GlobalOptionsPanel.ItemsSource = Options;
            for (int i = 0; i <= (int)ILogger.Levels.Error; i++)
                _ = LogLevelSelector.Items.Add((ILogger.Levels)i);
            LogLevelSelector.SelectedIndex = (int)EditedSettings.LogLevel;
        }

        private void LogLevelSelectorClosed(object sender, EventArgs e)
        {
            if (LogLevelSelector.SelectedIndex == -1)
                LogLevelSelector.SelectedIndex = 0;
            EditedSettings.LogLevel = (ILogger.Levels)LogLevelSelector.SelectedIndex;
        }
    }
}