namespace PanelExtension
{
    public interface ILogger
    {
        public enum LogLevels
        {
            Verbose,
            Info,
            Warning,
            Error
        }

        public event EventHandler? LogChanged;

        public string FormatMessage(LogLevels? Level, object? Sender, string Message);
        public void Verbose(string Message, object? Sender = null);
        public void Info(string Message, object? Sender = null);
        public void Warn(string Message, object? Sender = null);
        public void Error(string Message, object? Sender = null);
        public string GetLog();

        public void Log(LogLevels Level, object? Sender, string Message)
        {
            switch (Level)
            {
                case LogLevels.Verbose:
                    Verbose(Message, Sender);
                    break;
                case LogLevels.Info:
                    Info(Message, Sender);
                    break;
                case LogLevels.Warning:
                    Warn(Message, Sender);
                    break;
                case LogLevels.Error:
                    Error(Message, Sender);
                    break;
                default:
                    break;
            }
        }
    }
}