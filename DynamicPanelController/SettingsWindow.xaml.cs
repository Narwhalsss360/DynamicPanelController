using System.Windows;

namespace DynamicPanelController
{
    public partial class SettingsWindow : Window
    {
        readonly App App = (App)Application.Current;

        public SettingsWindow()
        {
            InitializeComponent();
        }
    }
}