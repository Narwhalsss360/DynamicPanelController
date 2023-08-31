using Panel;
using Panel.Communication;
using Profiling;
using Profiling.ProfilingTypes;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace DynamicPanelController
{
    public partial class ProfileEditor : Window
    {
        readonly App App = (App)Application.Current;
        readonly int SelectedIndexToEdit = -1;
        public PanelProfile EditiedVersion;
        PanelDescriptorEditor? CustomDescriptorEditor = null;
        bool PushedButtonSet = false;
        bool IgnoreNextItemSelection = false;

        public ProfileEditor(int SelectedIndex)
        {
            if (App is null)
                throw new Exception("Couldn't get current app.");
            InitializeComponent();
            if (SelectedIndex < 0)
            {
                MessageBox.Show($"The editor window was opened without a selected profile. Stack trace:\n{ Environment.StackTrace }", "Opened incorrecty", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }

            SelectedIndexToEdit = SelectedIndex;
            EditiedVersion = App.Profiles[SelectedIndexToEdit];
            PanelProfileNameTextBlock.Text = EditiedVersion.Name;
        }

        public void LoadDescriptor(PanelDescriptor? Descriptor)
        {
            PanelDescriptor? DescriptorToLoad = Descriptor;
            _ = Descriptor ?? App.Settings.GlobalPanelDescriptor;
            if (DescriptorToLoad is null)
                return;

            EditiedVersion.PanelDescription = DescriptorToLoad;

            for (int i = 0; i < DescriptorToLoad.ButtonCount; i++)
                IOSelectorList.Items.Add($"Button {i}");

            for (int i = 0; i < DescriptorToLoad.AbsoluteCount; i++)
                IOSelectorList.Items.Add($"Absolute {i}");

            for (int i = 0; i < DescriptorToLoad.DisplayCount; i++)
                IOSelectorList.Items.Add($"Display {i}");
        }

        public void IOSelected(object? Sender, EventArgs Args)
        {
            PanelItemSelectorList.Items.Clear();
            OptionsSelectorList.Items.Clear();
            if (IOSelectorList.SelectedItem is not string Selection)
                return;
            if (IOSelectorList.SelectedIndex < EditiedVersion.PanelDescription?.ButtonCount)
            {
                PushedButton.IsEnabled = true;
                ReleasedButton.IsEnabled = true;
            }
            else
            {
                PushedButton.IsEnabled = false;
                ReleasedButton.IsEnabled = false;
            }

            if (IOSelectorList.SelectedIndex < EditiedVersion.PanelDescription?.ButtonCount + EditiedVersion.PanelDescription?.AbsoluteCount || Selection.StartsWith("Button") || Selection.StartsWith("Absolute"))
            {
                foreach (var ActionType in App.Actions)
                    PanelItemSelectorList.Items.Add(ActionType.GetPanelActionDescriptor()?.Name);
            }
            else if (IOSelectorList.SelectedIndex >= EditiedVersion.PanelDescription?.ButtonCount + EditiedVersion.PanelDescription?.AbsoluteCount || Selection.StartsWith("Display"))
            {
                foreach (var SourceType in App.Sources)
                    PanelItemSelectorList.Items.Add(SourceType.GetPanelSourceDescriptor()?.Name);
            }
        }

        public void PanelItemSelected(object? Sender, EventArgs Args)
        {
            if (IgnoreNextItemSelection)
                return;
            if (PanelItemSelectorList.SelectedIndex == -1)
                return;
            OptionsSelectorList?.Items.Clear();
            Type? ItemType;
            if (PanelItemSelectorList.SelectedIndex < App.Actions.Count)
                ItemType = App.Actions[PanelItemSelectorList.SelectedIndex];
            else
                ItemType = App.Sources[PanelItemSelectorList.SelectedIndex - App.Actions.Count];

            if (ItemType is null)
                return;

            if (IOSelectorList.SelectedIndex < EditiedVersion.PanelDescription?.ButtonCount)
            {
                byte ID = (byte)(IOSelectorList.SelectedIndex - EditiedVersion.PanelDescription?.ButtonCount);
                ButtonUpdateStates UpdateState = (PushedButtonSet) ? ButtonUpdateStates.Pushed : ButtonUpdateStates.Released;
                if (EditiedVersion.ActionMappings.ContainsKey(ID))
                    EditiedVersion.ActionMappings.Remove(ID);
                if (ItemType.GetInterfaces().Contains(typeof(IPanelAction)))
                    EditiedVersion.ActionMappings.Add(ID, new Tuple<ButtonUpdateStates, IPanelAction>(UpdateState, (IPanelAction)Activator.CreateInstance(ItemType)));
            }
            else if (IOSelectorList.SelectedIndex < EditiedVersion.PanelDescription?.ButtonCount + EditiedVersion.PanelDescription?.AbsoluteCount)
            {
                byte ID = (byte)(IOSelectorList.SelectedIndex - EditiedVersion.PanelDescription?.ButtonCount);
                if (EditiedVersion.AbsoluteActionMappings.ContainsKey(ID))
                    EditiedVersion.AbsoluteActionMappings.Remove(ID);
                if (ItemType.GetInterfaces().Contains(typeof(IAbsolutePanelAction)))
                    EditiedVersion.AbsoluteActionMappings.Add(ID, (IAbsolutePanelAction)Activator.CreateInstance(ItemType));
            }
            else
            {
                byte ID = (byte)(IOSelectorList.SelectedIndex - (EditiedVersion.PanelDescription?.ButtonCount + EditiedVersion.PanelDescription?.AbsoluteCount));
                if (EditiedVersion.SourceMappings.ContainsKey(ID))
                    EditiedVersion.SourceMappings.Remove(ID);
                if (ItemType.GetInterfaces().Contains(typeof(IPanelSource)))
                    EditiedVersion.SourceMappings.Add(ID, (IPanelSource)Activator.CreateInstance(ItemType));
            }
        }

        void EditorLoaded(object? Sender, EventArgs Args)
        {
            LoadDescriptor(App.Profiles[SelectedIndexToEdit].PanelDescription);
        }

        void PanelDescriptorButtonClicked(object? Sender, EventArgs Args)
        {
            if (CustomDescriptorEditor is not null)
                return;

            CustomDescriptorEditor = new PanelDescriptorEditor(EditiedVersion.PanelDescription, true);
            CustomDescriptorEditor.Show();
            CustomDescriptorEditor.Closed += PanelDescriptorEditorClosed;
        }

        void PanelDescriptorEditorClosed(object? Sender, EventArgs Args)
        {
            EditiedVersion.PanelDescription = CustomDescriptorEditor?.Descriptor;
            LoadDescriptor(EditiedVersion.PanelDescription);
            CustomDescriptorEditor = null;
        }

        void PushedButtonPushed(object? Sender, EventArgs Args) => PushedButtonSet = true;

        void ReleasedButtonPushed(object? Sender, EventArgs Args) => PushedButtonSet = false;

        void OKClicked(object? Sender, EventArgs Args)
        {
            App.Profiles[SelectedIndexToEdit] = EditiedVersion;
        }

        void CancelClicked(object? Sender, EventArgs Args)
        {
            Close();
        }

        void ApplyClicked(object? Sender, EventArgs Args)
        {
            OKClicked(this, Args);
            Close();
        }
    }
}