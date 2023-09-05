namespace PanelExtension
{
    public interface ILogger
    {
        public event EventHandler? LogChanged;
        public void Info(string Message);
        public void Warn(string Message);
        public void Error(string Message);
        public string GetLog();
    }
}