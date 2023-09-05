using Panel.Communication;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace DynamicPanelController
{
    public partial class PanelEmulator : Window
    {
        App App = (App)Application.Current;

        public PanelEmulator()
        {
            App.EmulatorDisplay = EmulatorDisplay.EmulatorDisplayReceive;
            InitializeComponent();
            Loaded += WindowLoaded;
            Closing += WindowClosing;
        }

        void WindowLoaded(object? Sender, EventArgs Args)
        {
            if (App.Settings.GlobalPanelDescriptor is null)
            {
                MessageBox.Show("Emulator uses global panel descriptor, please set it.", "", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            for (byte i = 0; i < App.Settings.GlobalPanelDescriptor.ButtonCount; i++)
                ButtonStack.Children.Add(new EmulatorButton(i));

            for (byte i = 0; i < App.Settings.GlobalPanelDescriptor.AbsoluteCount; i++)
                AbsoluteStack.Children.Add(new EmulatorAbsolute(i));

            for (byte i = 0; i < App.Settings.GlobalPanelDescriptor.DisplayCount; i++)
                DisplayStack.Children.Add(new EmulatorDisplay(i));
        }

        private void WindowClosing(object? sender, EventArgs e)
        {
            EmulatorDisplay.InstanceMapping.Clear();
            App.EmulatorDisplay = null;
        }
    }

    class EmulatorButton : Button
    {
        readonly byte ID;
        App App = (App)Application.Current;

        public EmulatorButton(byte ID)
            : base()
        {
            this.ID = ID;
            PreviewMouseLeftButtonDown += Pushed;
            PreviewMouseLeftButtonUp += Released;
            Margin = new Thickness(5);
            Content = $"Button {ID}";
        }

        private void Pushed(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            App.RouteUpdate(MessageReceiveIDs.ButtonStateUpdate, ID, ButtonUpdateStates.Pushed);
        }

        private void Released(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            App.RouteUpdate(MessageReceiveIDs.ButtonStateUpdate, ID, ButtonUpdateStates.Released);
        }
    }

    class EmulatorAbsolute : Grid
    {
        readonly byte ID;
        App App = (App)Application.Current;

        TextBlock SliderTitle = new() { Margin = new Thickness(5), HorizontalAlignment = HorizontalAlignment.Center };
        Slider Slider = new() { Margin = new Thickness(5), Maximum = 100 };
        Button RangeButton = new() { Margin = new Thickness(5), Content = "Range" };

        EditSliderMinMax? Editor = null;

        public EmulatorAbsolute(byte ID)
            : base()
        {
            this.ID = ID;
            ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(65) });
            ColumnDefinitions.Add(new ColumnDefinition());
            ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(65) });

            SetColumn(Slider, 1);
            SetColumn(RangeButton, 2);

            SliderTitle.Text = $"Slider {ID}";
            Slider.ValueChanged += SliderMoved;

            RangeButton.Click += RangeClicked;

            Children.Add(SliderTitle);
            Children.Add(Slider);
            Children.Add(RangeButton);
        }

        private void SliderMoved(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            App.RouteUpdate(MessageReceiveIDs.AbsolutePosition, ID, Slider.Value);
        }

        void EditorClosed(object? Sender, EventArgs Args)
        {
            if (Editor is null)
                return;

            Slider.Minimum = Editor.Minimum;
            Slider.Maximum = Editor.Maximum;
            Editor = null;
        }

        void RangeClicked(object? Sender, EventArgs Args)
        {
            if (Editor is not null)
                return;

            Editor = new() { Title = SliderTitle.Text };
            Editor.Closed += EditorClosed;
            Editor.Show();
        }
    }

    class EmulatorDisplay : Grid
    {
        public delegate void SetDataFunction(string Data);

        public static Dictionary<byte, Tuple<TextBlock, SetDataFunction>> InstanceMapping = new();
        App App = (App)Application.Current;

        readonly byte ID;

        TextBlock DisplayTitle = new() { Margin = new Thickness(5), HorizontalAlignment = HorizontalAlignment.Center };
        TextBlock DisplayData = new() { Margin = new Thickness(5), HorizontalAlignment = HorizontalAlignment.Center };

        public EmulatorDisplay(byte ID)
        {
            this.ID = ID;
            DisplayTitle.Text = $"Display {ID}";

            ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(60) });
            ColumnDefinitions.Add(new ColumnDefinition());
            SetColumn(DisplayData, 1);

            Children.Add(DisplayTitle);
            Children.Add(DisplayData);

            InstanceMapping.Add(ID, new(DisplayData, SetData));

            Unloaded += ElementUnloaded;
        }

        void ElementUnloaded(object? Sender, EventArgs Args)
        {
            if (InstanceMapping.ContainsKey(ID))
                InstanceMapping.Remove(ID);
        }

        void SetData(string Data)
        {
            DisplayData.Text = Data;
        }

        public static void EmulatorDisplayReceive(byte ID, string Data)
        {
            if (!InstanceMapping.ContainsKey(ID))
                return;
            Tuple<TextBlock, SetDataFunction> Invoker = InstanceMapping[ID];
            Invoker.Item1.Dispatcher.Invoke(Invoker.Item2, Data);
        }
    }
}