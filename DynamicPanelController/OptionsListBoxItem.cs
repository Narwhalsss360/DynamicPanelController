using System.Windows;
using System.Windows.Controls;

namespace DynamicPanelController
{
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
            _ = Children.Add(Left);
            _ = Children.Add(Right);
        }
    }
}