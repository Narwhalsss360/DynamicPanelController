namespace Profiling
{
    public class PanelProfileException : Exception
    {
        public PanelProfileException(string? Message = null, Exception? InnerException = null)
            : base(Message, InnerException)
        {
        }
    }
}