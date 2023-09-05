using System.Collections.Generic;

namespace DynamicPanelController
{
    public class BindableDictionaryPair
    {
        public Dictionary<string, string>? Owner;
        private string ThisKey;
        private string ThisValue;

        public string Key
        {
            get => ThisKey;
            set
            {
                if (Owner is null)
                {
                    ThisKey = value;
                    return;
                }
                if (Owner is not null)
                    if (Owner.ContainsKey(value))
                        return;
                if (Owner is not null)
                    if (Owner.ContainsKey(ThisKey))
                        _ = Owner.Remove(ThisKey);
                ThisKey = value;
                Owner?.Add(ThisKey, ThisValue);
            }
        }
        public string Value
        {
            get => ThisValue;
            set
            {
                if (Owner is null)
                {
                    ThisValue = value;
                    return;
                }
                if (Owner is not null)
                    if (Owner.ContainsKey(ThisKey))
                        _ = Owner.Remove(ThisKey);
                ThisValue = value;
                Owner?.Add(ThisKey, ThisValue);
            }
        }

        public BindableDictionaryPair()
        {
            Owner = new Dictionary<string, string>();
            ThisKey = string.Empty;
            ThisValue = string.Empty;
        }

        public BindableDictionaryPair(Dictionary<string, string> Owner, string Key, string Value)
        {
            this.Owner = Owner;
            ThisKey = Key;
            ThisValue = Value;
        }
    }
}