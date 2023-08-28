using Profiling.ProfilingTypes;

namespace IncludedExtensions.Volume
{
    [PanelActionDescriptor("Decrease Volume")]
    public class Decrease : IPanelAction
    {
        public object? Do()
        {
            Volume.Controller.DefaultPlaybackDevice.Volume--;
            return null;
        }
    }
}