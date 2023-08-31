using System.Windows;
using System.Windows.Controls;

namespace DynamicPanelController
{
    public class DisplayDescriptorContentGrid : Grid
    {
        public UIElement[]? Elements = null;
        public DisplayDescriptorContentGrid(UIElement[]? Elements = null)
            : base()
        {
            if (Elements is not null)
            {
                this.Elements = Elements;
                for (int i = 0; i < Elements.Length; i++)
                {
                    ColumnDefinitions.Add(new ColumnDefinition());
                    SetColumn(this.Elements[i], i);
                    Children.Add(this.Elements[i]);
                }
            }
        }
    }
}