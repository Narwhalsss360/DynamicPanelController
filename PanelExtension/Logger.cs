namespace PanelExtension
{
    public interface ILogger
    {
        public enum Levels
        {
            Verbose,
            Info,
            Warning,
            Error
        }

        public event EventHandler? LogChanged;

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