using System;
using System.Windows;
namespace DynamicPanelController
{
    public partial class EditSliderMinMax : Window
    {
        public double Minimum { get; private set; } = 0;
        public double Maximum { get; private set; } = 100;

        public EditSliderMinMax()
        {
            InitializeComponent();
        }

        private bool Validate()
        {
            if (!double.TryParse(MinimumEntry.Text, out double MinimumOut))
                return false;
            Minimum = MinimumOut;

            if (!double.TryParse(MaximumEntry.Text, out double MaximumOut))
                return false;
            Maximum = MaximumOut;

            return true;
        }

        private void OKClicked(object? Sender, EventArgs Args)
        {
            if (Validate())
                Close();
            else
                MessageBox.Show("Must enter a number.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}