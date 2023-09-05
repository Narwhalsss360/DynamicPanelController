using Panel.Communication;

namespace Panel
{
    public class PanelDescriptor
    {
        public byte? DisplayCount { get; set; } = null;
        public DisplayTypes[]? DisplayTypes { get; set; } = null;
        public byte[][]? DisplayDescriptor { get; set; } = null;
        public byte? ButtonCount { get; set; } = null;
        public byte? AbsoluteCount { get; set; } = null;

        public PanelDescriptor()
        {
        }

        public PanelDescriptor(Serializable Serialized)
        {
            DisplayCount = (byte?)Serialized.DisplayCount;
            DisplayTypes = Serialized.DisplayTypes;
            if (Serialized.DisplayDescriptor is not null)
            {
                DisplayDescriptor = new byte[Serialized.DisplayDescriptor.Length][];
                for (int iDisplay = 0; iDisplay < Serialized.DisplayDescriptor.Length; iDisplay++)
                {
                    DisplayDescriptor[iDisplay] = new byte[Serialized.DisplayDescriptor[iDisplay].Length];
                    for (int iDescription = 0; iDescription < Serialized.DisplayDescriptor[iDisplay].Length; iDescription++)
                        DisplayDescriptor[iDisplay][iDescription] = (byte)Serialized.DisplayDescriptor[iDisplay][iDescription];
                }
            }
            ButtonCount = (byte?)Serialized.ButtonCount;
            AbsoluteCount = (byte?)Serialized.AbsoluteCount;
        }

        public class Serializable
        {
            public int? DisplayCount { get; set; } = null;
            public DisplayTypes[]? DisplayTypes { get; set; } = null;
            public int[][]? DisplayDescriptor { get; set; } = null;
            public int? ButtonCount { get; set; } = null;
            public int? AbsoluteCount { get; set; } = null;

            public Serializable()
            {
            }

            public Serializable(PanelDescriptor Descriptor)
            {
                DisplayCount = Descriptor.DisplayCount;
                if (Descriptor.DisplayTypes is not null)
                {
                    DisplayTypes = new DisplayTypes[Descriptor.DisplayTypes.Length];
                    for (int i = 0; i < Descriptor.DisplayTypes.Length; i++)
                        DisplayTypes[i] = Descriptor.DisplayTypes[i];
                }
                if (Descriptor.DisplayDescriptor is not null)
                {
                    DisplayDescriptor = new int[Descriptor.DisplayDescriptor.Length][];
                    for (int i = 0; i < Descriptor.DisplayDescriptor.Length; i++)
                    {
                        DisplayDescriptor[i] = new int[Descriptor.DisplayDescriptor[i].Length];
                        for (int i2 = 0; i2 < Descriptor.DisplayDescriptor[i].Length; i2++)
                            DisplayDescriptor[i][i2] = Descriptor.DisplayDescriptor[i][i2];
                    }
                }
                ButtonCount = Descriptor.ButtonCount;
                AbsoluteCount = Descriptor.AbsoluteCount;
            }
        }
    }
}