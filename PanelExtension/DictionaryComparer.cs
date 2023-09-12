namespace PanelExtension
{
    public static class DictionaryComparer
    {
        public static bool CompareKVPs<Tkey, TValue>(this Dictionary<Tkey, TValue> This, Dictionary<Tkey, TValue> Other) where Tkey : notnull where TValue : IEquatable<TValue>
        {
            if (This.Count != Other.Count)
                return false;

            foreach (var ThisKVP in This)
            {
                if (!Other.ContainsKey(ThisKVP.Key))
                    return false;
                if (!ThisKVP.Value.Equals(Other[ThisKVP.Key]))
                    return false;
            }

            return true;
        }
    }
}