using Profiling.ProfilingTypes.PanelItems;

namespace IncludedExtensions.Volume
{
    [PanelActionDescriptor("Decrease Volume")]
    public class Decrease : IPanelAction
    {
        public object? Do(object? Arguments = null)
        {
            Audio.Controller.DefaultPlaybackDevice.Volume--;
            return null;
        }
    }
}