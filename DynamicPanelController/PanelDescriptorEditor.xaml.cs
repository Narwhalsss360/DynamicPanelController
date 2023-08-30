using Panel;
using System;
using System.Windows;

namespace DynamicPanelController
{
    public partial class PanelDescriptorEditor : Window
    {
        PanelDescriptor? Descriptor = null;

        public PanelDescriptorEditor(PanelDescriptor? Template = null)
        {
            InitializeComponent();
        }

        string? CheckValid()
        {
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