namespace Panel.Communication
{
    public enum MessageReceiveIDs : ushort
    {
        Reserved,
        ButtonStateUpdate,
        AbsolutePosition
    }

    public enum MessageSendIDs : ushort
    {
        Reserved,
        DisplayString
    }

    public enum ButtonUpdateStates : byte
    {
        Pushed,
        Released
    }

    public enum DisplayTypes : byte
    {
        RowColumn,
        SevenSegment
    }

    public static class EnumExtensions
    {
        public static MessageReceiveIDs ToReceiveID(this ushort ID)
        {
            return (MessageReceiveIDs)ID;
        }

        public static byte ToByte(this ButtonUpdateStates State)
        {
            return (byte)State;
        }

        public static ButtonUpdateStates ToButtonUpdateState(this byte State)
        {
            return (ButtonUpdateStates)State;
        }

        public static string ToString(this ButtonUpdateStates State)
        {
            return State switch
            {
                ButtonUpdateStates.Pushed => "Pushed",
                ButtonUpdateStates.Released => "Released",
                _ => "",
            };
        }

        public static ButtonUpdateStates ToButtonUpdateState(this string State)
        {
            return State == "Pushed" ? ButtonUpdateStates.Pushed : ButtonUpdateStates.Released;
        }

        public static ButtonUpdateStates ToPushedButtonUpdateState(this bool State)
        {
            return State ? ButtonUpdateStates.Pushed : ButtonUpdateStates.Released;
        }

        public static string ToString(this DisplayTypes Type)
        {
            return Type switch
            {
                DisplayTypes.RowColumn => "RowColumn",
                DisplayTypes.SevenSegment => "SevenSegment",
                _ => "",
            };
        }

        public static DisplayTypes? ToDisplayType(this string State)
        {
            return State == "RowColumn" ? DisplayTypes.RowColumn : State == "SevenSegment" ? DisplayTypes.SevenSegment : null;
        }
    }
}