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
        List<DisplayDescriptorContentGrid> DisplayDescriptors = new();
        List<Dictionary<string, string?>> DisplayDescriptorSettings = new();

        public PanelDescriptorEditor(PanelDescriptor? Template = null)
        {
            Descriptor = Template;
            InitializeComponent();
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

            if (DisplayCount > DisplayDescriptors.Count)
            {
                List<DisplayDescriptorContentGrid> Default = new();
                for (int i = 0; i < DisplayCount - DisplayDescriptors.Count; i++)
                {
                    UIElement[] Elements = new UIElement[4];
                    Elements[0] = new TextBlock() { Text = $"Display {i + DisplayDescriptors.Count}", HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                    Elements[1] = new ComboBox() { ItemsSource = new string[] { "RowColumn", "SevenSegment" }, Margin = new Thickness() { Left = 5, Right = 5 } };
                    Elements[2] = new ComboBox() { ItemsSource = new string[] { "Rows", "Columns" }, Margin = new Thickness() { Left = 5, Right = 5 } };
                    Elements[3] = new TextBlock() { VerticalAlignment = VerticalAlignment.Center };
                    Default.Add(new DisplayDescriptorContentGrid(Elements));
                }
                DisplayDescriptors.AddRange(Default);
            }
            DisplayDescriptorStackPanel.Children.Clear();
            for (int i = 0; i < DisplayCount; i++)
                DisplayDescriptorStackPanel.Children.Add(DisplayDescriptors[i]);
        }

        public void DisplayCountChanged(object? Sender, EventArgs Args)
        {
            UpdateDisplayDescriptor();
        }

        void DisplayDescriptorTypeChanged(object? Sender, EventArgs Args)
        {
            DisplayDescriptorContentGrid? ThisDescriptor = null;
            if (ThisDescriptor is null)
                return;

            if (ThisDescriptor.Elements?[1] is not ComboBox TypeCombo)
                return;

            switch ((DisplayTypes)TypeCombo.SelectedIndex)
            {
                case DisplayTypes.RowColumn:
                    ComboBox NewCombo = new();
                    NewCombo.Items.Add("Rows");
                    NewCombo.Items.Add("Columns");
                    break;
                case DisplayTypes.SevenSegment:
                    ThisDescriptor.Elements[2] = new TextBlock() { Text = "Digits:" };
                    break;
                default:
                    break;
            }
        }

        string? CheckValid()
        {
            if (!byte.TryParse(ButtonCountEntry.Text, out byte ButtonCount))
                return "Button count is not a number from 0 -> 255.";
            if (!byte.TryParse(ButtonCountEntry.Text, out byte AbsoluteCount))
                return "Absolute count is not a number from 0 -> 255.";
            if (!byte.TryParse(ButtonCountEntry.Text, out byte DisplayCount))
                return "Display count is not a number from 0 -> 255.";

            Descriptor ??= new();

            Descriptor.ButtonCount = ButtonCount;
            Descriptor.AbsoluteCount = AbsoluteCount;
            Descriptor.DisplayCount = DisplayCount;

            return null;
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