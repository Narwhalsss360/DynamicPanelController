namespace PanelExtension
{
    public class LogChangedEventArgs : EventArgs
    {
        public readonly string? AppendedText;

        public LogChangedEventArgs(string? Change = null)
            : base()
        {
            this.AppendedText = Change;
        }
    }

    public delegate void LogChangedEventHanlder(object? Sender, LogChangedEventArgs Args);

    public interface ILogger
    {
        public enum Levels
        {
            Verbose,
            Info,
            Warning,
            Error
        }

        public event LogChangedEventHanlder? LogChanged;

        public string FormatMessage(Levels? Level, object? Sender, string Message);
        public void Verbose(string Message, object? Sender = null);
        public void Info(string Message, object? Sender = null);
        public void Warn(string Message, object? Sender = null);
        public void Error(string Message, object? Sender = null);
        public string GetLog();

        public void Log(Levels Level, string Message, object? Sender)
        {
            switch (Level)
            {
                case Levels.Verbose:
                    Verbose(Message, Sender);
                    break;
                case Levels.Info:
                    Info(Message, Sender);
                    break;
                case Levels.Warning:
                    Warn(Message, Sender);
                    break;
                case Levels.Error:
                    Error(Message, Sender);
                    break;
                default:
                    break;
            }
        }
    }
}