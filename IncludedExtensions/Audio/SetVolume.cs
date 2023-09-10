using Profiling.ProfilingTypes.PanelItems;

namespace IncludedExtensions.Volume
{
    [PanelAbsoluteActionDescriptor("Set Volume")]
    internal class SetVolume : IAbsolutePanelAction
    {
        public object? Set(float Value)
        {
            Audio.Controller.DefaultPlaybackDevice.Volume = Value;
            return null;
        }
    }
}