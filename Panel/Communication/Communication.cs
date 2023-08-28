using System.Runtime.CompilerServices;

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
        public static MessageReceiveIDs ToReceiveID(this ushort ID) => (MessageReceiveIDs)ID;

        public static byte ToByte(this ButtonUpdateStates State) => (byte)State;

        public static ButtonUpdateStates ToButtonUpdateState(this byte State) => (ButtonUpdateStates)State;

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
            if (State == "Pushed") return ButtonUpdateStates.Pushed;
            else return ButtonUpdateStates.Released;
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
            if (State == "RowColumn") return DisplayTypes.RowColumn;
            else if (State == "SevenSegment") return DisplayTypes.SevenSegment;
            else return null;
        }
    }
}