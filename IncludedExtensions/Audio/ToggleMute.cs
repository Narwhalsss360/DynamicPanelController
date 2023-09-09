using Profiling.ProfilingTypes.PanelItems;

namespace IncludedExtensions.Volume
{
    [PanelActionDescriptor("Toggle Mute")]
    internal class ToggleMute : IPanelAction
    {
        public object? Do(object? Arguments = null)
        {
            _ = Audio.Controller.DefaultPlaybackDevice.ToggleMute();
            return null;
        }
    }
}