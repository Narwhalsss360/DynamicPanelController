namespace Panel
{
    public struct PanelDescriptor
    {
        public byte? DisplayCount = null;
        public byte[]? DisplayTypes = null;
        public byte[][]? DisplayDescriptor = null;
        public byte? ButtonCount = null;
        public byte? RotaryEncoderCount = null;
        public byte? PotentiometerCount = null;
        
        public PanelDescriptor()
        {
        }

        public PanelDescriptor(byte[] Bytes)
        {
        }
    }
}