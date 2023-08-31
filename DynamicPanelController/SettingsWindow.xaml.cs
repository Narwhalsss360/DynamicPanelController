using System;
using System.Collections.Generic;
using System.Windows;

namespace DynamicPanelController
{
    public partial class SettingsWindow : Window
    {
        readonly App App = (App)Application.Current;
        public App.AppSettings EditedSettings;
        List<BindableDictionaryPair> Options { get; set; } = new();
        PanelDescriptorEditor? DescriptorEditor;
        public bool Validated = false;

        public SettingsWindow(App.AppSettings? SettingsTemplate = null)
        {
            InitializeComponent();
            EditedSettings = SettingsTemplate ?? new App.AppSettings();
            Loaded += WindowLoaded;
            foreach (var Pair in EditedSettings.GlobalSettings)
                Options.Add(new() { Owner=EditedSettings.GlobalSettings, ThisKey=Pair.Key, ThisValue=Pair.Value });
        }

        string? VerifyValid()
        {
            EditedSettings.ExtensionsDirectory = ExtensionsDirectoryEntry.Text;
            EditedSettings.ProfilesDirectory = ProfilesDirectoryEntry.Text;
            EditedSettings.LogPath = LogDirectoryEntry.Text;
            Validated = true;
            return null;
        }

        void OKClicked(object? Sender, EventArgs Args)
        {
            if (VerifyValid() is string ErrorMessage)
            {
                MessageBox.Show(ErrorMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }

        void CancelClicked(object? Sender, EventArgs Args)
        {
            Validated = false;
            Close();
        }

        void ApplyClicked(object? Sender, EventArgs Args)
        {
            if (VerifyValid() is string ErrorMessage)
            {
                MessageBox.Show(ErrorMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            Close();
        }

        private void GlobalOptionSelected(object? Sender, EventArgs Args) => RemoveOptionButton.IsEnabled = GlobalOptionsPanel.SelectedIndex >= 0;

        private void RemoveButtonClicked(object? Sender, EventArgs Args)
        {
            if (GlobalOptionsPanel.SelectedIndex == -1)
                return;
            if (EditedSettings.GlobalSettings.ContainsKey(Options[GlobalOptionsPanel.SelectedIndex].ThisKey))
                EditedSettings.GlobalSettings.Remove(Options[GlobalOptionsPanel.SelectedIndex].Key);
            Options.RemoveAt(GlobalOptionsPanel.SelectedIndex);
            GlobalOptionsPanel.Items.Refresh();
        }

        private void AddButtonClicked(object? Sender, EventArgs Args)
        {
            string KeyName = "New Key";
            for (int i = 1; EditedSettings.GlobalSettings.ContainsKey(KeyName); i++)
                KeyName = $"New Key({i})";
                
            EditedSettings.GlobalSettings.Add(KeyName, "New Value");
            Options.Add(new() { Owner=EditedSettings.GlobalSettings, ThisKey = KeyName, ThisValue = "New Value" });
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
            GlobalOptionsPanel.ItemsSource = Options;
        }
    }

    class BindableDictionaryPair
    {
        public Dictionary<string, string>? Owner;
        public string? ThisKey;
        public string? ThisValue;

        public string Key
        {
            get
            {
                return ThisKey;
            }
            set
            {
                if (Owner is null)
                {
                    ThisKey = value;
                    return;
                }
                if (Owner is not null)
                    if (Owner.ContainsKey(value))
                        return;
                if (Owner is not null)
                    if (Owner.ContainsKey(ThisKey))
                        Owner.Remove(ThisKey);
                ThisKey = value;
                Owner?.Add(ThisKey, ThisValue);
            }
        }
        public string Value
        {
            get
            {
                return ThisValue;
            }
            set
            {
                if (Owner is null)
                {
                    ThisValue = value;
                    return;
                }
                if (Owner is not null)
                    if (Owner.ContainsKey(ThisKey))
                        Owner.Remove(ThisKey);
                ThisValue = value;
                Owner?.Add(ThisKey, ThisValue);
            }
        }
    }
}