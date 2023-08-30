using Profiling;
using System;
using System.Windows;

namespace DynamicPanelController
{
    public partial class ProfileEditor : Window
    {
        readonly App App = (App)Application.Current;
        readonly int SelectedIndexToEdit = -1;
        PanelProfile EditiedVersion;

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
        }

        void EditorLoaded(object? Sender, EventArgs Args)
        {

        }

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