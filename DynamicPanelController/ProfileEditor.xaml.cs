using Panel;
using Panel.Communication;
using Profiling;
using Profiling.ProfilingTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace DynamicPanelController
{
    public partial class ProfileEditor : Window
    {
        private readonly App App = (App)Application.Current;
        public PanelProfile EditiedVersion;
        private PanelDescriptorEditor? CustomDescriptorEditor = null;
        private bool PushedButtonSet = true;
        private bool DontInstantiate = true;
        private List<OptionsListBoxItem> OptionsListBoxItems { get; } = new();
        int SelectedIndex = -1;

        private readonly Dictionary<string, string?> EnteredOptions = new();

        public ProfileEditor(int SelectedIndex)
        {
            if (App is null)
                throw new Exception("Couldn't get current app.");
            InitializeComponent();
            if (SelectedIndex < 0)
            {
                _ = MessageBox.Show($"The editor window was opened without a selected profile. Stack trace:\n{Environment.StackTrace}", "Opened incorrecty", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }

            Loaded += WindowLoaded;
            this.SelectedIndex = SelectedIndex;
            EditiedVersion = App.Profiles[SelectedIndex];
            PanelProfileNameTextBlock.Text = EditiedVersion.Name;
        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            if (EditiedVersion.PanelDescriptor is null)
                LoadDescriptor(App.Settings.GlobalPanelDescriptor);
            else
            {
                PanelDescriptorButton.Content = "Panel Descriptor: Custom";
                LoadDescriptor(EditiedVersion.PanelDescriptor);
            }
        }

        private void LoadDescriptor(PanelDescriptor? Descriptor)
        {
            PanelDescriptor? DescriptorToLoad = Descriptor;
            _ = Descriptor ?? App.Settings.GlobalPanelDescriptor;
            if (DescriptorToLoad is null)
                return;

            IOSelectorList.Items.Clear();
            for (int i = 0; i < DescriptorToLoad.ButtonCount; i++)
                _ = IOSelectorList.Items.Add($"Button {i}");

            for (int i = 0; i < DescriptorToLoad.AbsoluteCount; i++)
                _ = IOSelectorList.Items.Add($"Absolute {i}");

            for (int i = 0; i < DescriptorToLoad.DisplayCount; i++)
                _ = IOSelectorList.Items.Add($"Display {i}");
        }

        private void IOSelected(object? Sender, EventArgs Args)
        {
            if (IOSelectorList.SelectedIndex == -1)
                return;
            PanelItemSelectorList.Items.Clear();
            PanelItemSelectorList.SelectedIndex = -1;
            if (IOSelectorList.SelectedItem is not string Selection)
                return;

            PanelDescriptor? Descriptor = EditiedVersion.PanelDescriptor is not null ? EditiedVersion.PanelDescriptor : App.Settings.GlobalPanelDescriptor;

            bool IsButton = IOSelectorList.SelectedIndex < Descriptor?.ButtonCount;
            bool IsSource = IOSelectorList.SelectedIndex >= Descriptor?.ButtonCount + Descriptor?.AbsoluteCount;
            bool IsAbsolute = !IsButton && !IsSource;

            byte? ID = IsButton
                ? (byte?)IOSelectorList.SelectedIndex
                : IsSource
                ? (byte?)(IOSelectorList.SelectedIndex - (Descriptor?.ButtonCount + Descriptor?.AbsoluteCount))
                : (byte?)(IOSelectorList.SelectedIndex - Descriptor?.ButtonCount);

            if (ID is null)
                return;

            if (IsButton)
            {
                foreach (var ActionType in App.Actions)
                    _ = PanelItemSelectorList.Items.Add(ActionType.GetPanelActionDescriptor()?.Name);

                if (EditiedVersion.ActionMappings.Find(A => A.ID == ID && A.UpdateState == PushedButtonSet.ToPushedButtonUpdateState()) is not ActionMapping Mapping)
                    return;

                if (Mapping.Action.GetDescriptorAttribute()?.Name is string MappedActionName)
                {
                    for (int i = 0; i < PanelItemSelectorList.Items.Count; i++)
                    {
                        if (PanelItemSelectorList.Items[i] is not string PanelListItemName)
                            continue;
                        if (PanelListItemName == MappedActionName)
                        {
                            DontInstantiate = true;
                            PanelItemSelectorList.SelectedIndex = i;
                            break;
                        }
                    }
                }
            }
            else if (IsAbsolute)
            {
                foreach (var AbsoluteActionType in App.AbsoluteActions)
                    _ = PanelItemSelectorList.Items.Add(AbsoluteActionType.GetPanelAbsoluteActionDescriptor()?.Name);

                if (EditiedVersion.AbsoluteActionMappings.Find(A => A.ID == ID) is AbsoluteActionMapping Mapping)
                {
                    if (Mapping.AbsoluteAction.GetDescriptorAttribute()?.Name is string MappedAbsoluteActionName)
                    {
                        for (int i = 0; i < PanelItemSelectorList.Items.Count; i++)
                        {
                            if (PanelItemSelectorList.Items[i] is not string PanelListItemName)
                                continue;
                            if (PanelListItemName == MappedAbsoluteActionName)
                            {
                                DontInstantiate = true;
                                PanelItemSelectorList.SelectedIndex = i;
                                break;
                            }
                        }
                    }
                }
            }
            else if (IsSource)
            {
                foreach (var SourceType in App.Sources)
                    _ = PanelItemSelectorList.Items.Add(SourceType.GetPanelSourceDescriptor()?.Name);

                if (EditiedVersion.SourceMappings.Find(S => S.ID == ID) is SourceMapping Mapping)
                {
                    if (Mapping.Source.GetType().GetPanelSourceDescriptor()?.Name is string MappedSourceName)
                    {
                        for (int i = 0; i < PanelItemSelectorList.Items.Count; i++)
                        {
                            if (PanelItemSelectorList.Items[i] is not string PanelListItemName)
                                continue;
                            if (PanelListItemName == MappedSourceName)
                            {
                                DontInstantiate = true;
                                PanelItemSelectorList.SelectedIndex = i;
                                break;
                            }
                        }
                    }
                }
            }
        }

        private void PanelItemSelected(object? Sender, EventArgs Args)
        {
            OptionsListBoxItems.Clear();
            OptionsSelectorList.Items.Refresh();
            RemoveMappingButton.IsEnabled = false;
            TypeNameTextBlock.Text = string.Empty;
            if (PanelItemSelectorList.SelectedIndex == -1)
                return;
            RemoveMappingButton.IsEnabled = true;

            PanelDescriptor? Descriptor = EditiedVersion.PanelDescriptor is not null ? EditiedVersion.PanelDescriptor : App.Settings.GlobalPanelDescriptor;

            bool IsButton = IOSelectorList.SelectedIndex < Descriptor?.ButtonCount;
            bool IsSource = IOSelectorList.SelectedIndex >= Descriptor?.ButtonCount + Descriptor?.AbsoluteCount;
            bool IsAbsolute = !IsButton && !IsSource;

            Type? ItemType = IsButton
                ? App.Actions[PanelItemSelectorList.SelectedIndex]
                : IsAbsolute
                ? App.AbsoluteActions[PanelItemSelectorList.SelectedIndex]
                : App.Sources[PanelItemSelectorList.SelectedIndex];

            if (ItemType is null)
                return;

            byte? ID = IsButton
                ? (byte?)IOSelectorList.SelectedIndex
                : IsSource
                ? (byte?)(IOSelectorList.SelectedIndex - (Descriptor?.ButtonCount + Descriptor?.AbsoluteCount))
                : (byte?)(IOSelectorList.SelectedIndex - Descriptor?.ButtonCount);

            if (ID is null)
                return;

            if (IsButton)
            {
                if (!DontInstantiate)
                {
                    if (Activator.CreateInstance(ItemType) is not IPanelAction NewAction)
                        return;
                    if (EditiedVersion.ActionMappings.Find(Action => Action.ID == ID && Action.UpdateState == PushedButtonSet.ToPushedButtonUpdateState()) is ActionMapping ActionMapping)
                        _ = EditiedVersion.ActionMappings.Remove(ActionMapping);
                    EditiedVersion.ActionMappings.Add(new() { ID = (byte)ID, UpdateState = PushedButtonSet.ToPushedButtonUpdateState(), Action = NewAction });
                }
                DontInstantiate = false;
                LoadActionOptions(EditiedVersion.ActionMappings.Find(Action => Action.ID == ID && Action.UpdateState == PushedButtonSet.ToPushedButtonUpdateState())?.Action);
            }
            else if (IsAbsolute)
            {
                if (!DontInstantiate)
                {
                    if (Activator.CreateInstance(ItemType) is not IAbsolutePanelAction NewAbsoluteAction)
                        return;
                    if (EditiedVersion.AbsoluteActionMappings.Find(AbsoluteAction => AbsoluteAction.ID == ID) is AbsoluteActionMapping AbsoluteActionMapping)
                        _ = EditiedVersion.AbsoluteActionMappings.Remove(AbsoluteActionMapping);
                    EditiedVersion.AbsoluteActionMappings.Add(new() { ID = (byte)ID, AbsoluteAction = NewAbsoluteAction });
                }
                DontInstantiate = false;
                LoadActionOptions(EditiedVersion.AbsoluteActionMappings.Find(AbsoluteAction => AbsoluteAction.ID == ID)?.AbsoluteAction);
            }
            else
            {
                if (!DontInstantiate)
                {
                    if (Activator.CreateInstance(ItemType) is not IPanelSource NewSource)
                        return;
                    if (EditiedVersion.SourceMappings.Find(Source => Source.ID == ID) is SourceMapping SourceMapping)
                        _ = EditiedVersion.SourceMappings.Remove(SourceMapping);
                    EditiedVersion.SourceMappings.Add(new() { ID = (byte)ID, Source = NewSource });
                }
                DontInstantiate = false;
                LoadActionOptions(EditiedVersion.SourceMappings.Find(Source => Source.ID == ID)?.Source);
            }
            TypeNameTextBlock.Text = ItemType.FullName;
            OptionsSelectorList.Items.Refresh();
        }

        private void PanelItemOptionSelected(object? Sender, EventArgs Args)
        {
            if (OptionsSelectorList.SelectedIndex == -1)
            {
                RemoveOptionButton.IsEnabled = false;
                return;
            }

            bool AllowsAnyKey = AddOptionButton.IsEnabled;

            if (!AllowsAnyKey)
                return;
        }

        private void RemoveMappingClicked(object? Sender, EventArgs Args)
        {
            RemoveMappingButton.IsEnabled = false;
            if (IOSelectorList.SelectedIndex == -1)
                return;
            PanelDescriptor? Descriptor = EditiedVersion.PanelDescriptor is not null ? EditiedVersion.PanelDescriptor : App.Settings.GlobalPanelDescriptor;

            bool IsButton = IOSelectorList.SelectedIndex < Descriptor?.ButtonCount;
            bool IsSource = IOSelectorList.SelectedIndex >= Descriptor?.ButtonCount + Descriptor?.AbsoluteCount;
            bool IsAbsolute = !IsButton && !IsSource;

            byte? ID = IsButton
                ? (byte?)IOSelectorList.SelectedIndex
                : IsSource
                ? (byte?)(IOSelectorList.SelectedIndex - (Descriptor?.ButtonCount + Descriptor?.AbsoluteCount))
                : (byte?)(IOSelectorList.SelectedIndex - Descriptor?.ButtonCount);

            if (ID is null)
                return;

            if (IsButton)
                EditiedVersion.ActionMappings.RemoveAll(ActionMapping => ActionMapping.ID == ID);
            else if (IsAbsolute)
                EditiedVersion.AbsoluteActionMappings.RemoveAll(AbsoluteActionMapping => AbsoluteActionMapping.ID == ID);
            else if (IsSource)
                EditiedVersion.SourceMappings.RemoveAll(SourceMapping => SourceMapping.ID == ID);
            PanelItemSelectorList.SelectedIndex = -1;
            OptionsSelectorList.Items.Refresh();
        }

        private void UpdateEnteredOptions(object? Sender, EventArgs Args)
        {
            EnteredOptions.Clear();

            foreach (var Item in OptionsSelectorList.Items)
            {
                if (Item is OptionsListBoxItem OptionItem)
                {
                    string Left;
                    if (OptionItem.Left is ComboBox LeftCombo)
                        Left = LeftCombo.Text;
                    else if (OptionItem.Left is TextBox LeftTextBox)
                        Left = LeftTextBox.Text;
                    else if (OptionItem.Left is TextBlock LeftTextBlock)
                        Left = LeftTextBlock.Text;
                    else
                        continue;

                    string? Right = OptionItem.Right is ComboBox RightCombo
                        ? RightCombo.Text
                        : OptionItem.Right is TextBox RightTextBox ? RightTextBox.Text : null;
                    if (EnteredOptions.ContainsKey(Left))
                    {
                        _ = MessageBox.Show($"\"{Left}\" was entered more than once", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        continue;
                    }
                    EnteredOptions.Add(Left, Right);
                }
            }

            PanelDescriptor? Descriptor = EditiedVersion.PanelDescriptor is not null ? EditiedVersion.PanelDescriptor : App.Settings.GlobalPanelDescriptor;

            bool IsButton = IOSelectorList.SelectedIndex < Descriptor?.ButtonCount;
            bool IsSource = IOSelectorList.SelectedIndex >= Descriptor?.ButtonCount + Descriptor?.AbsoluteCount;
            bool IsAbsolute = !IsButton && !IsSource;

            byte? ID = IsButton
                ? (byte?)IOSelectorList.SelectedIndex
                : IsSource
                ? (byte?)(IOSelectorList.SelectedIndex - (Descriptor?.ButtonCount + Descriptor?.AbsoluteCount))
                : (byte?)(IOSelectorList.SelectedIndex - Descriptor?.ButtonCount);

            if (ID is null)
                return;

            string? Warning = null;

            if (IsButton)
            {
                if (EditiedVersion.ActionMappings.Find(A => A.ID == ID && A.UpdateState == PushedButtonSet.ToPushedButtonUpdateState()) is ActionMapping ActionMapping)
                    Warning = ActionMapping.Action.SetOptions(EnteredOptions);
            }
            else if (IsAbsolute)
            {
                if (EditiedVersion.AbsoluteActionMappings.Find(A => A.ID == ID) is AbsoluteActionMapping AbsoluteActionMapping)
                    Warning = AbsoluteActionMapping.AbsoluteAction.SetOptions(EnteredOptions);
            }
            else
            {
                if (EditiedVersion.SourceMappings.Find(S => S.ID == ID) is SourceMapping SourceMapping)
                    Warning = SourceMapping.Source.SetOptions(EnteredOptions);
            }

            if (Warning is not null)
                _ = MessageBox.Show(Warning, "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);

            OptionsSelectorList.Items.Refresh();
        }

        private void LoadActionOptions(Dictionary<string, string?>? Options, string?[]?[]? ValidOptions = null)
        {
            EnteredOptions.Clear();
            OptionsListBoxItems.Clear();

            bool AllowsAnyKey = ValidOptions is null;
            Dictionary<string, string[]?> OptionsKeyValuePairs = new();

            if (ValidOptions is not null)
                foreach (var OptionKeyValuePairs in ValidOptions)
                {
                    if (OptionKeyValuePairs is null)
                        continue;
                    if (OptionKeyValuePairs[0] is not string Key)
                    {
                        AllowsAnyKey = true;
                        continue;
                    }

                    bool AllowsAnyValue = false;
                    List<string> ValidValues = new();

                    for (int i = 1; i < OptionKeyValuePairs.Length; i++)
                    {
                        if (OptionKeyValuePairs[i] is not string ValidOption)
                        {
                            AllowsAnyValue = true;
                            break;
                        }
                        ValidValues.Add(ValidOption);
                    }
                    OptionsKeyValuePairs.Add(Key, AllowsAnyValue ? null : ValidValues.ToArray());
                }

            foreach (var KeyValuePair in OptionsKeyValuePairs)
            {
                TextBlock Left = new() { Text = KeyValuePair.Key, Margin = new Thickness(5), HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Center };
                UIElement Right;
                if (KeyValuePair.Value is not null && KeyValuePair.Value.Length > 0)
                {
                    ComboBox RightComboBox = new()
                    {
                        ItemsSource = KeyValuePair.Value,
                        Margin = new Thickness(5),
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        VerticalAlignment = VerticalAlignment.Center,
                        SelectedIndex = 0
                    };
                    RightComboBox.DropDownClosed += UpdateEnteredOptions;
                    Right = RightComboBox;
                }
                else
                {
                    TextBox RightTextBox = new() { Text = "", Margin = new Thickness(5), HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Center };
                    RightTextBox.LostFocus += UpdateEnteredOptions;
                    Right = RightTextBox;
                }

                OptionsListBoxItems.Add(new OptionsListBoxItem(Left, Right, null));
            }

            if (Options is not null)
            {
                foreach (var Item in OptionsListBoxItems)
                {
                    if (Item is OptionsListBoxItem OptionItem)
                    {
                        string Left;
                        if (OptionItem.Left is ComboBox LeftCombo)
                            Left = LeftCombo.Text;
                        else if (OptionItem.Left is TextBox LeftTextBox)
                            Left = LeftTextBox.Text;
                        else if (OptionItem.Left is TextBlock LeftTextBlock)
                            Left = LeftTextBlock.Text;
                        else
                            continue;

                        if (!Options.ContainsKey(Left))
                            continue;

                        if (OptionItem.Left is ComboBox RightCombo)
                        {
                            for (int i = 0; i < RightCombo.Items.Count; i++)
                            {
                                if (RightCombo.Items[i] is string IndexString)
                                {
                                    if (IndexString == Options[Left])
                                    {
                                        RightCombo.SelectedIndex = i;
                                        break;
                                    }
                                }
                            }
                        }
                        else if (OptionItem.Right is TextBox RightTextBox)
                            RightTextBox.Text = Options[Left];
                    }
                }
            }
            AddOptionButton.IsEnabled = AllowsAnyKey;
            OptionsSelectorList.ItemsSource = OptionsListBoxItems;
            OptionsSelectorList.Items.Refresh();
        }

        private void LoadActionOptions(IPanelItem? PanelItem)
        {
            if (PanelItem is null)
                return;
            LoadActionOptions(PanelItem.GetOptions(), PanelItem.ValidOptions());
        }

        private void EditorLoaded(object? Sender, EventArgs Args)
        {
            LoadDescriptor(EditiedVersion.PanelDescriptor);
        }

        private void AddOptionButtonClicked(object? Sender, EventArgs Args)
        {
            TextBox KeyEntry = new() { Text = "", Margin = new Thickness(5), HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Center };
            TextBox ValueEntry = new() { Text = "", Margin = new Thickness(5), HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Center };
            KeyEntry.LostFocus += UpdateEnteredOptions;
            ValueEntry.LostFocus += UpdateEnteredOptions;
            OptionsListBoxItems.Add(new OptionsListBoxItem(KeyEntry, ValueEntry, null));
            OptionsSelectorList.Items.Refresh();
        }

        private void RemoveOptionButtonClicked(object? Sender, EventArgs Args)
        {
            OptionsListBoxItems.RemoveAt(OptionsSelectorList.SelectedIndex);
            OptionsSelectorList.Items.Refresh();
        }

        private void PanelDescriptorButtonClicked(object? Sender, EventArgs Args)
        {
            if (CustomDescriptorEditor is not null)
                return;

            CustomDescriptorEditor = new PanelDescriptorEditor(EditiedVersion.PanelDescriptor, true);
            CustomDescriptorEditor.Show();
            CustomDescriptorEditor.Closed += PanelDescriptorEditorClosed;
        }

        private void PanelDescriptorEditorClosed(object? Sender, EventArgs Args)
        {
            if (CustomDescriptorEditor is null)
                return;
            if (CustomDescriptorEditor.Descriptor is null)
            {
                PanelDescriptorButton.Content = "Panel Descriptor: Global";
                LoadDescriptor(App.Settings.GlobalPanelDescriptor);
            }
            else
            {
                PanelDescriptorButton.Content = "Panel Descriptor: Custom";
                LoadDescriptor(EditiedVersion.PanelDescriptor);
            }
            EditiedVersion.PanelDescriptor = CustomDescriptorEditor.Descriptor;
            CustomDescriptorEditor = null;
        }

        private void PushedButtonPushed(object? Sender, EventArgs Args)
        {
            PushedButtonSet = true;
            PushedButton.IsEnabled = false;
            ReleasedButton.IsEnabled = true;
            IOSelectorList.SelectedIndex = -1;
        }

        private void ReleasedButtonPushed(object? Sender, EventArgs Args)
        {
            PushedButtonSet = false;
            PushedButton.IsEnabled = true;
            ReleasedButton.IsEnabled = false;
            IOSelectorList.SelectedIndex = -1;
        }

        private void OKClicked(object? Sender, EventArgs Args)
        {
            ApplyClicked(this, Args);
            Close();
        }

        private void CancelClicked(object? Sender, EventArgs Args)
        {
            Close();
        }

        private void ApplyClicked(object? Sender, EventArgs Args)
        {
            EditiedVersion.Name = PanelProfileNameTextBlock.Text;
            if (SelectedIndex == -1)
                App.Profiles[SelectedIndex] = EditiedVersion;
        }
    }
}