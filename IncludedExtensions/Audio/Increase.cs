using Profiling.ProfilingTypes.PanelItems;

namespace IncludedExtensions.Volume
{
    [PanelActionDescriptor("Increase Volume")]
    public class Increase : IPanelAction
    {
        public object? Do()
        {
            Audio.Controller.DefaultPlaybackDevice.Volume++;
            return null;
        }
    }
}
