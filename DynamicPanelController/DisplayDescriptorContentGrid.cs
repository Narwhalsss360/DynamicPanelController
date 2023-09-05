using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace DynamicPanelController
{
    public class DisplayDescriptorContentGrid : Grid
    {
        public UIElement[]? Elements = null;
        public Dictionary<object, EventHandler> CustomEvents = new();
        public object? Context;

        public DisplayDescriptorContentGrid(UIElement[]? Elements = null, object? Context = null)
            : base()
        {
            if (Elements is not null)
            {
                this.Elements = Elements;
                for (int i = 0; i < Elements.Length; i++)
                {
                    ColumnDefinitions.Add(new ColumnDefinition());
                    SetColumn(this.Elements[i], i);
                    _ = Children.Add(this.Elements[i]);
                }
            }
            this.Context = Context;
        }

        public void Update()
        {
            Children.RemoveRange(0, Children.Count);
            for (int i = 0; i < Elements?.Length; i++)
            {
                SetColumn(Elements[i], i);
                _ = Children.Add(Elements[i]);
            }
        }

        public void AddKeyedEvent(object Key, EventHandler Handler)
        {
            CustomEvents.Add(Key, Handler);
        }

        public void RemoveKeyedEvent(object Key)
        {
            _ = CustomEvents.Remove(Key);
        }

        public void CallEvent(object Key, object? Sender, EventArgs Args)
        {
            if (CustomEvents.ContainsKey(Key))
                CustomEvents[Key].Invoke(Sender is null ? this : Sender, Args);
        }
    }
}