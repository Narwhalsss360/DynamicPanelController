using PanelExtension;
using Profiling.ProfilingTypes;

namespace IncludedExtensions.Tests
{
    [PanelActionDescriptor("Log Test")]
    public class LogTest : Extension, IPanelAction
    {
        public object? Do()
        {
            Application?.Logger.Info("Test action ran.");
            return null;
        }
    }
}