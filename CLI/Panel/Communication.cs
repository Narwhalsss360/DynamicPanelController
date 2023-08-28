namespace Panel
{
    public enum MessageReceiveIDs : ushort
    {
        Reserved,
        ButtonStateUpdate,
        RotaryStateUpdate,
        PotentiometerPosition
    }

    public enum MessageSendIDs: ushort
    {
        Reserved,
        Extensions,
        DisplayString
    }

    public enum ButtonUpdate : byte
    {
        Pushed,
        Released
    }

    public enum RotaryUpdate : byte
    {
        CounterClockwise,
        Clockwise
    }

    public enum DisplayTypes
    {
        RowColumn,
        SevenSegment
    }
}