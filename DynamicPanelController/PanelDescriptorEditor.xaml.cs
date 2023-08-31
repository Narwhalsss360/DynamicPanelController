using Panel;
using Panel.Communication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace DynamicPanelController
{
    public partial class PanelDescriptorEditor : Window
    {
        public PanelDescriptor? Descriptor = null;
        List<DisplayDescriptorContentGrid> UIDisplayDescriptors = new();
        List<byte> DisplayTypesList = new();
        List<object> DisplayDescriptors = new();

        public PanelDescriptorEditor(PanelDescriptor? Template = null, bool GlobalAvailable = false)
        {
            Descriptor = Template;
            if (Descriptor is null)
                Descriptor = new();
            InitializeComponent();
            GlobalButton.IsEnabled = GlobalAvailable;
        }

        public void WindowLoaded(object? Sender, EventArgs Args)
        {

        }

        public void UpdateDisplayDescriptor()
        {
            if (!byte.TryParse(DisplayCountEntry.Text, out byte DisplayCount))
            {
                MessageBox.Show("Display count is not a number from 0 -> 255.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    (NewContent.Elements[1] as ComboBox).SelectionChanged += (Sender, Args) => { NewContent.CallEvent((object)DisplayDescriptorTypeChanged, null, Args); };
                    NewContent.AddKeyedEvent((object)DisplayDescriptorTypeChanged, DisplayDescriptorTypeChanged);
                    Default.Add(NewContent);
                }
                UIDisplayDescriptors.AddRange(Default);
            }
            DisplayDescriptorStackPanel.Children.Clear();
            for (int i = 0; i < DisplayCount; i++)
                DisplayDescriptorStackPanel.Children.Add(UIDisplayDescriptors[i]);
            if (Descriptor.DisplayTypes is null)
                return;
            DisplayTypesList.Clear();
            foreach (var item in Descriptor?.DisplayTypes)
                DisplayTypesList.Add((byte)item);
            DisplayDescriptors.Clear();
            DisplayDescriptors.AddRange(Descriptor.DisplayDescriptor);
        }

        public void DisplayCountChanged(object? Sender, EventArgs Args)
        {
            UpdateDisplayDescriptor();
        }

        void RowColumnSelected(object? Sender, EventArgs Args)
        {
            DisplayDescriptorContentGrid? ThisDescriptor = Sender as DisplayDescriptorContentGrid;
            if (ThisDescriptor is null)
                return;

            if (ThisDescriptor.Elements?[2] is not ComboBox TypeCombo)
                return;


        }

        public void EntryTextChanged(object? Sender, EventArgs Args)
        {
            DisplayDescriptorContentGrid? ThisDescriptor = Sender as DisplayDescriptorContentGrid;
            if (ThisDescriptor is null)
                return;

            if (ThisDescriptor.Context is not byte Digits)
                return;

            
        }

        void DisplayDescriptorTypeChanged(object? Sender, EventArgs Args)
        {
            DisplayDescriptorContentGrid? ThisDescriptor = Sender as DisplayDescriptorContentGrid;
            if (ThisDescriptor is null)
                return;

            if (ThisDescriptor.Elements?[1] is not ComboBox TypeCombo)
                return;

            switch ((DisplayTypes)TypeCombo.SelectedIndex)
            {
                case DisplayTypes.RowColumn:
                    ThisDescriptor.Elements[2] = new ComboBox() { ItemsSource = new string[] { "Rows", "Columns" }, Margin = new Thickness() { Left = 5, Right = 5 } };
                    (ThisDescriptor.Elements[2] as ComboBox).SelectionChanged += (Sender, Args) => { ThisDescriptor.CallEvent((object)RowColumnSelected, null, Args); };
                    ThisDescriptor.AddKeyedEvent((object)RowColumnSelected, RowColumnSelected);
                    if (ThisDescriptor.CustomEvents.ContainsKey((object)EntryTextChanged))
                        ThisDescriptor.CustomEvents.Remove((object)EntryTextChanged);
                    break;
                case DisplayTypes.SevenSegment:
                    ThisDescriptor.Elements[2] = new TextBlock() { Text = "Digits:", Margin = new Thickness() { Left = 5, Right = 5 } };
                    (ThisDescriptor.Elements[3] as TextBox).TextChanged += (Sender, Args) => { ThisDescriptor.CallEvent((object)EntryTextChanged, null, Args); };
                    if (ThisDescriptor.CustomEvents.ContainsKey((object)RowColumnSelected))
                        ThisDescriptor.CustomEvents.Remove((object)RowColumnSelected);
                    ThisDescriptor.AddKeyedEvent((object)EntryTextChanged, EntryTextChanged);
                    break;
                default:
                    break;
            }
            ThisDescriptor.Update();
        }

        string? CheckValid()
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

            return null;
        }

        void GlobalClicked(object? Sender, EventArgs Args)
        {
            Descriptor = null;
            Close();
        }

        void CancelClicked(object? Sender, EventArgs Args)
        {
            Close();
        }

        void ApplyClicked(object? Sender, EventArgs Args)
        {
            if (CheckValid() is string ErrorMessage)
            {
                MessageBox.Show(ErrorMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            Close();
        }
    }
}