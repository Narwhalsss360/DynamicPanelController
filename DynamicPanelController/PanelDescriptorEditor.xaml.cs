using Panel;
using Panel.Communication;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace DynamicPanelController
{
    public partial class PanelDescriptorEditor : Window
    {
        public PanelDescriptor? Descriptor = null;
        private readonly List<DisplayDescriptorContentGrid> UIDisplayDescriptors = new();
        private readonly List<byte> DisplayTypesList = new();
        private readonly List<object> DisplayDescriptors = new();

        public PanelDescriptorEditor(PanelDescriptor? Template = null, bool GlobalAvailable = false)
        {
            Descriptor = Template;
            Descriptor ??= new();
            InitializeComponent();
            GlobalButton.IsEnabled = GlobalAvailable;
            Loaded += WindowLoaded;
        }

        public void WindowLoaded(object? Sender, EventArgs Args)
        {
            if (Descriptor is null)
                return;
            ButtonCountEntry.Text = Descriptor.ButtonCount.ToString();
            AbsoluteCountEntry.Text = Descriptor.AbsoluteCount.ToString();
            DisplayCountEntry.Text = Descriptor.DisplayCount.ToString();

        }

        public void UpdateDisplayDescriptor()
        {
            if (!byte.TryParse(DisplayCountEntry.Text, out byte DisplayCount))
            {
                _ = MessageBox.Show("Display count is not a number from 0 -> 255.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (DisplayCount > UIDisplayDescriptors.Count)
            {
                List<DisplayDescriptorContentGrid> Default = new();
                for (int i = 0; i < DisplayCount - UIDisplayDescriptors.Count; i++)
                {
                    UIElement[] Elements = new UIElement[4];
                    Elements[0] = new TextBlock() { Text = $"Display: {i + UIDisplayDescriptors.Count}", HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                    Elements[1] = new ComboBox() { ItemsSource = new string[] { "RowColumn", "SevenSegment" }, Margin = new Thickness() { Left = 5, Right = 5 } };
                    Elements[2] = new ComboBox() { ItemsSource = new string[] { "Rows", "Columns" }, Margin = new Thickness() { Left = 5, Right = 5 } };
                    Elements[3] = new TextBox() { VerticalAlignment = VerticalAlignment.Center };
                    DisplayDescriptorContentGrid NewContent = new(Elements);
                    if (NewContent.Elements?[1] is ComboBox Combo1)
                        Combo1.SelectionChanged += (Sender, Args) => { NewContent.CallEvent((object)DisplayDescriptorTypeChanged, null, Args); };
                    NewContent.AddKeyedEvent((object)DisplayDescriptorTypeChanged, DisplayDescriptorTypeChanged);
                    if (NewContent.Elements?[3] is TextBox Text3)
                        Text3.TextChanged += (Sender, Args) => { NewContent.CallEvent((object)EntryTextChanged, null, Args); };
                    NewContent.AddKeyedEvent((object)EntryTextChanged, EntryTextChanged);
                    Default.Add(NewContent);
                }
                UIDisplayDescriptors.AddRange(Default);
            }
            DisplayDescriptorStackPanel.Children.Clear();
            for (int i = 0; i < DisplayCount; i++)
                _ = DisplayDescriptorStackPanel.Children.Add(UIDisplayDescriptors[i]);
            if (Descriptor is null)
                return;
            if (Descriptor.DisplayTypes is null)
                return;
            if (Descriptor.DisplayDescriptor is null)
                return;
            DisplayTypesList.Clear();
            foreach (var item in Descriptor.DisplayTypes)
                DisplayTypesList.Add((byte)item);
            DisplayDescriptors.Clear();
            DisplayDescriptors.AddRange(Descriptor.DisplayDescriptor);
        }

        public void DisplayCountChanged(object? Sender, EventArgs Args)
        {
            UpdateDisplayDescriptor();
        }

        private void RowColumnSelected(object? Sender, EventArgs Args)
        {
            if (Sender is not DisplayDescriptorContentGrid ThisDescriptor)
                return;

            if (ThisDescriptor.Elements?[2] is not ComboBox)
                return;

            if (ThisDescriptor.Elements?[3] is TextBox Text3)
                Text3.Text = "";
        }

        public void EntryTextChanged(object? Sender, EventArgs Args)
        {
            if (Sender is not DisplayDescriptorContentGrid ThisDescriptor)
                return;

            if (ThisDescriptor.Context is byte)
            {
                if (byte.TryParse((ThisDescriptor.Elements?[3] as TextBox)?.Text, out byte Out))
                    ThisDescriptor.Context = Out;
            }
            else if (ThisDescriptor.Context is byte[])
            {
                if (byte.TryParse((ThisDescriptor.Elements?[3] as TextBox)?.Text, out byte Out))
                    if (ThisDescriptor.Context is byte[] Context)
                        if (ThisDescriptor.Elements?[1] is ComboBox Combo1)
                            Context[Combo1.SelectedIndex] = Out;
            }
        }

        private void DisplayDescriptorTypeChanged(object? Sender, EventArgs Args)
        {
            if (Sender is not DisplayDescriptorContentGrid ThisDescriptor)
                return;

            if (ThisDescriptor.Elements?[1] is not ComboBox TypeCombo)
                return;

            switch ((DisplayTypes)TypeCombo.SelectedIndex)
            {
                case DisplayTypes.RowColumn:
                    ThisDescriptor.Elements[2] = new ComboBox() { ItemsSource = new string[] { "Rows", "Columns" }, Margin = new Thickness() { Left = 5, Right = 5 } };
                    if (ThisDescriptor.Elements?[2] is ComboBox Combo2)
                        Combo2.SelectionChanged += (Sender, Args) => { ThisDescriptor.CallEvent((object)RowColumnSelected, null, Args); };
                    ThisDescriptor.AddKeyedEvent((object)RowColumnSelected, RowColumnSelected);
                    ThisDescriptor.Context = new byte[2];
                    break;
                case DisplayTypes.SevenSegment:
                    ThisDescriptor.Elements[2] = new TextBlock() { Text = "Digits:", Margin = new Thickness() { Left = 5, Right = 5 } };
                    if (ThisDescriptor.Elements[3] is TextBox Text3Inner)
                        Text3Inner.TextChanged += (Sender, Args) => { ThisDescriptor.CallEvent((object)EntryTextChanged, null, Args); };
                    if (ThisDescriptor.CustomEvents.ContainsKey((object)RowColumnSelected))
                        _ = ThisDescriptor.CustomEvents.Remove((object)RowColumnSelected);
                    ThisDescriptor.Context = new byte();
                    break;
                default:
                    break;
            }
            if (ThisDescriptor.Elements?[3] is TextBox Text3)
                Text3.Text = "";
            ThisDescriptor.Update();
        }

        private string? CheckValid()
        {
            if (!byte.TryParse(ButtonCountEntry.Text, out byte ButtonCount))
                return "Button count is not a number from 0 -> 255.";
            if (!byte.TryParse(AbsoluteCountEntry.Text, out byte AbsoluteCount))
                return "Absolute count is not a number from 0 -> 255.";
            if (!byte.TryParse(DisplayCountEntry.Text, out byte DisplayCount))
                return "Display count is not a number from 0 -> 255.";

            Descriptor ??= new();

            Descriptor.ButtonCount = ButtonCount;
            Descriptor.AbsoluteCount = AbsoluteCount;
            Descriptor.DisplayCount = DisplayCount;

            Descriptor.DisplayTypes = new DisplayTypes[DisplayCount];
            Descriptor.DisplayDescriptor = new byte[DisplayCount][];
            for (int i = 0; i < UIDisplayDescriptors.Count; i++)
            {
                if (UIDisplayDescriptors[i].Elements?[1] is not ComboBox Combo1)
                    continue;
                DisplayTypes DisplayType = Combo1.SelectedIndex == (int)DisplayTypes.RowColumn ? DisplayTypes.RowColumn : DisplayTypes.SevenSegment;
                Descriptor.DisplayTypes[i] = DisplayType;
                if (DisplayType == DisplayTypes.RowColumn)
                {
                    Descriptor.DisplayDescriptor[i] = new byte[2];
                    if (UIDisplayDescriptors[i].Context is byte[] RowsColumns)
                        Descriptor.DisplayDescriptor[i] = RowsColumns;
                }
                else
                {
                    Descriptor.DisplayDescriptor[i] = new byte[1];
                    if (UIDisplayDescriptors[i].Context is byte Digits)
                        Descriptor.DisplayDescriptor[i][0] = Digits;
                }

            }

            return null;
        }

        private void GlobalClicked(object? Sender, EventArgs Args)
        {
            Descriptor = null;
            Close();
        }

        private void CancelClicked(object? Sender, EventArgs Args)
        {
            Close();
        }

        private void ApplyClicked(object? Sender, EventArgs Args)
        {
            if (CheckValid() is string ErrorMessage)
            {
                _ = MessageBox.Show(ErrorMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            Close();
        }
    }
}