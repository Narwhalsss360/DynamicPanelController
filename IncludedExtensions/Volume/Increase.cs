using Profiling.ProfilingTypes.PanelItems;

namespace IncludedExtensions.Volume
{
    [PanelActionDescriptor("Increase Volume")]
    public class Increase : IPanelAction
    {
        public object? Do(object? Arguments = null)
        {
            Volume.Controller.DefaultPlaybackDevice.Volume++;
            return null;
        }
    }
}
