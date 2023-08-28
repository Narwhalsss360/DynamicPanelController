using Profiling.ProfilingTypes;

namespace IncludedExtensions.Volume
{
    [PanelActionDescriptor("Increase Volume")]
    public class Increase : IPanelAction
    {
        public object? Do()
        {
            Volume.Controller.DefaultPlaybackDevice.Volume++;
            return null;
        }
    }
}
