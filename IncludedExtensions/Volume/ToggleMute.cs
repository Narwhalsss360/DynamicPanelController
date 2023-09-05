using Profiling.ProfilingTypes;

namespace IncludedExtensions.Volume
{
    [PanelActionDescriptor("Toggle Mute")]
    internal class ToggleMute : IPanelAction
    {
        public object? Do(object? Arguments = null)
        {
            _ = Volume.Controller.DefaultPlaybackDevice.ToggleMute();
            return null;
        }
    }
}