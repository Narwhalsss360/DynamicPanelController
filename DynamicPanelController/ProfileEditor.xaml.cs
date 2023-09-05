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
        readonly App App = (App)Application.Current;
        public PanelProfile EditiedVersion;
        PanelDescriptorEditor? CustomDescriptorEditor = null;
        bool PushedButtonSet = true;
        public List<OptionsListBoxItem> OptionsListBoxItems { get; } = new();
        readonly Dictionary<string, string?> EnteredOptions = new();

        public ProfileEditor(int SelectedIndex)
        {
            if (App is null)
                throw new Exception("Couldn't get current app.");
            InitializeComponent();
            if (SelectedIndex < 0)
            {
                MessageBox.Show($"The editor window was opened without a selected profile. Stack trace:\n{Environment.StackTrace}", "Opened incorrecty", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }

            Loaded += WindowLoaded;

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

        public void LoadDescriptor(PanelDescriptor? Descriptor)
        {
            PanelDescriptor? DescriptorToLoad = Descriptor;
            _ = Descriptor ?? App.Settings.GlobalPanelDescriptor;
            if (DescriptorToLoad is null)
                return;

            IOSelectorList.Items.Clear();

            EditiedVersion.PanelDescriptor = DescriptorToLoad;

            for (int i = 0; i < DescriptorToLoad.ButtonCount; i++)
                IOSelectorList.Items.Add($"Button {i}");

            for (int i = 0; i < DescriptorToLoad.AbsoluteCount; i++)
                IOSelectorList.Items.Add($"Absolute {i}");

            for (int i = 0; i < DescriptorToLoad.DisplayCount; i++)
                IOSelectorList.Items.Add($"Display {i}");
        }

        public void IOSelected(object? Sender, EventArgs Args)
        {
            if (IOSelectorList.SelectedIndex == -1)
                return;
            PanelItemSelectorList.Items.Clear();
            if (IOSelectorList.SelectedItem is not string Selection)
                return;

            bool IsButton = IOSelectorList.SelectedIndex < EditiedVersion.PanelDescriptor?.ButtonCount;
            bool IsSource = IOSelectorList.SelectedIndex >= EditiedVersion.PanelDescriptor?.ButtonCount + EditiedVersion.PanelDescriptor?.AbsoluteCount;
            bool IsAbsolute = !IsButton && !IsSource;

            byte? ID;

            if (IsButton)
                ID = (byte?)IOSelectorList.SelectedIndex;
            else if (IsSource)
                ID = (byte?)(IOSelectorList.SelectedIndex - (EditiedVersion.PanelDescriptor?.ButtonCount + EditiedVersion.PanelDescriptor?.AbsoluteCount));
            else
                ID = (byte?)(IOSelectorList.SelectedIndex - EditiedVersion.PanelDescriptor?.ButtonCount);

            if (IsButton || IsAbsolute)
            {
                foreach (var ActionType in App.Actions)
                    PanelItemSelectorList.Items.Add(ActionType.GetPanelActionDescriptor()?.Name);

                if (ID is not null)
                {
                    if (IsButton)
                    {
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
                                    PanelItemSelectorList.SelectedIndex = i;
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
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
                                        PanelItemSelectorList.SelectedIndex = i;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else if (IsSource)
            {
                foreach (var SourceType in App.Sources)
                    PanelItemSelectorList.Items.Add(SourceType.GetPanelSourceDescriptor()?.Name);

                if (ID is not null)
                {
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
                                    PanelItemSelectorList.SelectedIndex = i;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }

        public void PanelItemSelected(object? Sender, EventArgs Args)
        {
            OptionsListBoxItems.Clear();
            if (PanelItemSelectorList.SelectedIndex == -1)
                return;


            bool IsButton = IOSelectorList.SelectedIndex < EditiedVersion.PanelDescriptor?.ButtonCount;
            bool IsSource = IOSelectorList.SelectedIndex >= EditiedVersion.PanelDescriptor?.ButtonCount + EditiedVersion.PanelDescriptor?.AbsoluteCount;
            bool IsAbsolute = !IsButton && !IsSource;

            Type? ItemType;
            if (IsButton || IsAbsolute)
                ItemType = App.Actions[PanelItemSelectorList.SelectedIndex];
            else
                ItemType = App.Sources[PanelItemSelectorList.SelectedIndex];

            if (ItemType is null)
                return;

            byte? ID;

            if (IsButton)
                ID = (byte?)IOSelectorList.SelectedIndex;
            else if (IsSource)
                ID = (byte?)(IOSelectorList.SelectedIndex - (EditiedVersion.PanelDescriptor?.ButtonCount + EditiedVersion.PanelDescriptor?.AbsoluteCount));
            else
                ID = (byte?)(IOSelectorList.SelectedIndex - EditiedVersion.PanelDescriptor?.ButtonCount);

            if (ID is null)
                return;

            if (IsButton)
            {
                if (Activator.CreateInstance(ItemType) is not IPanelAction NewAction)
                    return;
                if (EditiedVersion.ActionMappings.Find(A => A.ID == ID && A.UpdateState == PushedButtonSet.ToPushedButtonUpdateState()) is ActionMapping ActionMapping)
                    EditiedVersion.ActionMappings.Remove(ActionMapping);
                EditiedVersion.ActionMappings.Add(new() { ID = (byte)ID, UpdateState = PushedButtonSet.ToPushedButtonUpdateState(), Action = NewAction });
                LoadActionOptions(EditiedVersion.ActionMappings.Last().Action);
            }
            else if (IsAbsolute)
            {
                if (Activator.CreateInstance(ItemType) is not IAbsolutePanelAction NewAbsoluteAction)
                    return;
                if (EditiedVersion.AbsoluteActionMappings.Find(A => A.ID == ID) is AbsoluteActionMapping AbsoluteActionMapping)
                    EditiedVersion.AbsoluteActionMappings.Remove(AbsoluteActionMapping);
                EditiedVersion.AbsoluteActionMappings.Add(new() { ID = (byte)ID, AbsoluteAction = NewAbsoluteAction });
                LoadActionOptions(EditiedVersion.AbsoluteActionMappings.Last().AbsoluteAction);
            }
            else
            {
                if (Activator.CreateInstance(ItemType) is not IPanelSource NewSource)
                    return;
                if (EditiedVersion.SourceMappings.Find(S => S.ID == ID) is SourceMapping SourceMapping)
                    EditiedVersion.SourceMappings.Remove(SourceMapping);
                EditiedVersion.SourceMappings.Add(new() { ID = (byte)ID, Source = NewSource });
                LoadActionOptions(EditiedVersion.SourceMappings.Last().Source);
            }
            OptionsSelectorList?.Items.Refresh();
        }

        public void PanelItemOptionSelected(object? Sender, EventArgs Args)
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

        void UpdateEnteredOptions(object? Sender, EventArgs Args)
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

                    string? Right;
                    if (OptionItem.Right is ComboBox RightCombo)
                        Right = RightCombo.Text;
                    else if (OptionItem.Right is TextBox RightTextBox)
                        Right = RightTextBox.Text;
                    else
                        Right = null;
                    if (EnteredOptions.ContainsKey(Left))
                    {
                        MessageBox.Show($"\"{Left}\" was entered more than once", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        continue;
                    }
                    EnteredOptions.Add(Left, Right);
                }
            }

            bool IsButton = IOSelectorList.SelectedIndex < EditiedVersion.PanelDescriptor?.ButtonCount;
            bool IsSource = IOSelectorList.SelectedIndex >= EditiedVersion.PanelDescriptor?.ButtonCount + EditiedVersion.PanelDescriptor?.AbsoluteCount;
            bool IsAbsolute = !IsButton && !IsSource;

            byte? ID;

            if (IsButton)
                ID = (byte?)IOSelectorList.SelectedIndex;
            else if (IsSource)
                ID = (byte?)(IOSelectorList.SelectedIndex - (EditiedVersion.PanelDescriptor?.ButtonCount + EditiedVersion.PanelDescriptor?.AbsoluteCount));
            else
                ID = (byte?)(IOSelectorList.SelectedIndex - EditiedVersion.PanelDescriptor?.ButtonCount);

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
                MessageBox.Show(Warning, "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);

            OptionsSelectorList.Items.Refresh();
        }

        void LoadActionOptions(Dictionary<string, string?>? Options, string?[]?[]? ValidOptions = null)
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
                        else if (OptionItem.Left is TextBox RightTextBox)
                            RightTextBox.Text = Options[Left];
                    }
                }
            }
            AddOptionButton.IsEnabled = AllowsAnyKey;
            OptionsSelectorList.ItemsSource = OptionsListBoxItems;
            OptionsSelectorList.Items.Refresh();
        }

        void LoadActionOptions(IPanelItem? PanelItem)
        {
            if (PanelItem is null)
                return;
            LoadActionOptions(PanelItem.GetOptions(), PanelItem.ValidOptions());
        }

        void EditorLoaded(object? Sender, EventArgs Args)
        {
            LoadDescriptor(EditiedVersion.PanelDescriptor);
        }

        void AddOptionButtonClicked(object? Sender, EventArgs Args)
        {
            TextBox KeyEntry = new() { Text = "", Margin = new Thickness(5), HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Center };
            TextBox ValueEntry = new() { Text = "", Margin = new Thickness(5), HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Center };
            KeyEntry.LostFocus += UpdateEnteredOptions;
            ValueEntry.LostFocus += UpdateEnteredOptions;
            OptionsListBoxItems.Add(new OptionsListBoxItem(KeyEntry, ValueEntry, null));
            OptionsSelectorList.Items.Refresh();
        }

        void RemoveOptionButtonClicked(object? Sender, EventArgs Args)
        {
            OptionsListBoxItems.RemoveAt(OptionsSelectorList.SelectedIndex);
            OptionsSelectorList.Items.Refresh();
        }

        void PanelDescriptorButtonClicked(object? Sender, EventArgs Args)
        {
            if (CustomDescriptorEditor is not null)
                return;

            CustomDescriptorEditor = new PanelDescriptorEditor(EditiedVersion.PanelDescriptor, true);
            CustomDescriptorEditor.Show();
            CustomDescriptorEditor.Closed += PanelDescriptorEditorClosed;
        }

        void PanelDescriptorEditorClosed(object? Sender, EventArgs Args)
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

        void PushedButtonPushed(object? Sender, EventArgs Args)
        {
            PushedButtonSet = true;
            PushedButton.IsEnabled = false;
            ReleasedButton.IsEnabled = true;
            IOSelectorList.SelectedIndex = -1;
        }

        void ReleasedButtonPushed(object? Sender, EventArgs Args)
        {
            PushedButtonSet = false;
            PushedButton.IsEnabled = true;
            ReleasedButton.IsEnabled = false;
            IOSelectorList.SelectedIndex = -1;
        }

        void OKClicked(object? Sender, EventArgs Args)
        {
            EditiedVersion.Name = PanelProfileNameTextBlock.Text;
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

    public class OptionsListBoxItem : Grid
    {
        public UIElement? Left = null;
        public UIElement? Right = null;
        public object? Context = null;

        public OptionsListBoxItem(UIElement? Left, UIElement? Right, object? Context)
            : base()
        {
            ColumnDefinitions.Add(new ColumnDefinition());
            ColumnDefinitions.Add(new ColumnDefinition());
            this.Left = Left;
            this.Right = Right;
            SetColumn(Left, 0);
            SetColumn(Right, 1);
            this.Context = Context;
            Children.Add(Left);
            Children.Add(Right);
        }
    }
}