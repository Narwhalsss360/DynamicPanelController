using Profiling.ProfilingTypes.PanelItems;

namespace IncludedExtensions.Tests
{
    [PanelSourceDescriptor("24-Hour Time: HHmm")]
    internal class TimeTest : IPanelSource
    {
        public object? GetSourceValue()
        {
            return DateTime.Now.ToString("HHmm");
        }
    }
}