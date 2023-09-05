using System;
using System.Collections.Generic;
using System.Windows;

namespace DynamicPanelController
{
    public partial class SettingsWindow : Window
    {
        public App.AppSettings EditedSettings;

        private List<BindableDictionaryPair> Options { get; set; } = new();

        private PanelDescriptorEditor? DescriptorEditor;
        public bool Validated = false;

        public SettingsWindow(App.AppSettings? SettingsTemplate = null)
        {
            InitializeComponent();
            EditedSettings = SettingsTemplate ?? new App.AppSettings();
            Loaded += WindowLoaded;
            foreach (var Pair in EditedSettings.GlobalSettings)
                Options.Add(new(EditedSettings.GlobalSettings, Pair.Key, Pair.Value));
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
            Close();
        }

        private void GlobalOptionSelected(object? Sender, EventArgs Args)
        {
            RemoveOptionButton.IsEnabled = GlobalOptionsPanel.SelectedIndex >= 0;
        }

        private void RemoveButtonClicked(object? Sender, EventArgs Args)
        {
            if (GlobalOptionsPanel.SelectedIndex == -1)
                return;
            if (EditedSettings.GlobalSettings.ContainsKey(Options[GlobalOptionsPanel.SelectedIndex].Key))
                _ = EditedSettings.GlobalSettings.Remove(Options[GlobalOptionsPanel.SelectedIndex].Key);
            Options.RemoveAt(GlobalOptionsPanel.SelectedIndex);
            GlobalOptionsPanel.Items.Refresh();
        }

        private void AddButtonClicked(object? Sender, EventArgs Args)
        {
            string KeyName = "New Key";
            for (int i = 1; EditedSettings.GlobalSettings.ContainsKey(KeyName); i++)
                KeyName = $"New Key({i})";

            EditedSettings.GlobalSettings.Add(KeyName, "New Value");
            Options.Add(new(EditedSettings.GlobalSettings, KeyName, "New Value"));
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
            foreach (var KVP in EditedSettings.GlobalSettings)
                Options.Add(new(EditedSettings.GlobalSettings, KVP.Key, KVP.Value));
            GlobalOptionsPanel.ItemsSource = Options;
        }
    }

    internal class BindableDictionaryPair
    {
        public Dictionary<string, string>? Owner;
        private string ThisKey;
        private string ThisValue;

        public string Key
        {
            get => ThisKey;
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
                        _ = Owner.Remove(ThisKey);
                ThisKey = value;
                Owner?.Add(ThisKey, ThisValue);
            }
        }
        public string Value
        {
            get => ThisValue;
            set
            {
                if (Owner is null)
                {
                    ThisValue = value;
                    return;
                }
                if (Owner is not null)
                    if (Owner.ContainsKey(ThisKey))
                        _ = Owner.Remove(ThisKey);
                ThisValue = value;
                Owner?.Add(ThisKey, ThisValue);
            }
        }

        public BindableDictionaryPair()
        {
            Owner = new Dictionary<string, string>();
            ThisKey = string.Empty;
            ThisValue = string.Empty;
        }

        public BindableDictionaryPair(Dictionary<string, string> Owner, string Key, string Value)
        {
            this.Owner = Owner;
            ThisKey = Key;
            ThisValue = Value;
        }
    }
}