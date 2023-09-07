using Panel.Communication;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace DynamicPanelController
{
    public partial class PanelEmulator : Window
    {
        private readonly App App = (App)Application.Current;

        public PanelEmulator()
        {
            App.EmulatorDisplay = EmulatorDisplay.EmulatorDisplayReceive;
            InitializeComponent();
            Loaded += WindowLoaded;
            Closing += WindowClosing;
        }

        private void WindowLoaded(object? Sender, EventArgs Args)
        {
            if (App.Settings.GlobalPanelDescriptor is null)
            {
                _ = MessageBox.Show("Emulator uses global panel descriptor, please set it.", "", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            for (byte i = 0; i < App.Settings.GlobalPanelDescriptor.ButtonCount; i++)
                _ = ButtonStack.Children.Add(new EmulatorButton(i));

            for (byte i = 0; i < App.Settings.GlobalPanelDescriptor.AbsoluteCount; i++)
                _ = AbsoluteStack.Children.Add(new EmulatorAbsolute(i));

            for (byte i = 0; i < App.Settings.GlobalPanelDescriptor.DisplayCount; i++)
                _ = DisplayStack.Children.Add(new EmulatorDisplay(i));
        }

        private void WindowClosing(object? sender, EventArgs e)
        {
            EmulatorDisplay.InstanceMapping.Clear();
            App.EmulatorDisplay = null;
        }
    }

    internal class EmulatorButton : Button
    {
        private readonly byte ID;
        private readonly App App = (App)Application.Current;

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

    internal class EmulatorAbsolute : Grid
    {
        private readonly byte ID;
        private readonly App App = (App)Application.Current;
        private readonly TextBlock SliderTitle = new() { Margin = new Thickness(5), HorizontalAlignment = HorizontalAlignment.Center };
        private readonly Slider Slider = new() { Margin = new Thickness(5), Maximum = 100 };
        private readonly Button RangeButton = new() { Margin = new Thickness(5), Content = "Range" };
        private EditSliderMinMax? Editor = null;

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

            _ = Children.Add(SliderTitle);
            _ = Children.Add(Slider);
            _ = Children.Add(RangeButton);
            Unloaded += ElementUnloaded;
        }

        private void ElementUnloaded(object sender, RoutedEventArgs e)
        {
            Editor?.Close();
        }

        private void SliderMoved(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            App.RouteUpdate(MessageReceiveIDs.AbsolutePosition, ID, Slider.Value);
        }

        private void EditorClosed(object? Sender, EventArgs Args)
        {
            if (Editor is null)
                return;

            Slider.Minimum = Editor.Minimum;
            Slider.Maximum = Editor.Maximum;
            Editor = null;
        }

        private void RangeClicked(object? Sender, EventArgs Args)
        {
            if (Editor is not null)
                return;

            Editor = new() { Title = SliderTitle.Text };
            Editor.Closed += EditorClosed;
            Editor.Show();
        }
    }

    internal class EmulatorDisplay : Grid
    {
        public delegate void SetDataFunction(string Data);

        public static Dictionary<byte, Tuple<TextBlock, SetDataFunction>> InstanceMapping = new();
        private readonly App App = (App)Application.Current;
        private readonly byte ID;
        private readonly TextBlock DisplayTitle = new() { Margin = new Thickness(5), HorizontalAlignment = HorizontalAlignment.Center };
        private readonly TextBlock DisplayData = new() { Margin = new Thickness(5), HorizontalAlignment = HorizontalAlignment.Center };

        public EmulatorDisplay(byte ID)
        {
            this.ID = ID;
            DisplayTitle.Text = $"Display {ID}";

            ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(60) });
            ColumnDefinitions.Add(new ColumnDefinition());
            SetColumn(DisplayData, 1);

            _ = Children.Add(DisplayTitle);
            _ = Children.Add(DisplayData);

            InstanceMapping.Add(ID, new(DisplayData, SetData));

            Unloaded += ElementUnloaded;
        }

        private void ElementUnloaded(object? Sender, EventArgs Args)
        {
            if (InstanceMapping.ContainsKey(ID))
                _ = InstanceMapping.Remove(ID);
        }

        private void SetData(string Data)
        {
            DisplayData.Text = Data;
        }

        public static void EmulatorDisplayReceive(byte ID, string Data)
        {
            if (!InstanceMapping.ContainsKey(ID))
                return;
            Tuple<TextBlock, SetDataFunction> Invoker = InstanceMapping[ID];
            _ = Invoker.Item1.Dispatcher.Invoke(Invoker.Item2, Data);
        }
    }
}