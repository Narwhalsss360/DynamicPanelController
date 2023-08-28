using Profiling.ProfilingTypes;
using PanelExtension;

namespace IncludedExtensions.Tests
{
    [PanelActionDescriptor("Log Test")]
    public class LogTest : IPanelAction
    {
        public object? Do()
        {
            if (!Extension.Refresh())
            {
                Extension.Logger?.Error("Did not refresh.");
            }
            Extension.Logger?.Info("Test action ran.");
            return null;
        }
    }
}