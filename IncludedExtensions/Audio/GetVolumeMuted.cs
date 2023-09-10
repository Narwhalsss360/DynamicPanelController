using Profiling.ProfilingTypes.PanelItems;

namespace IncludedExtensions.Volume
{
    [PanelSourceDescriptor("Volume(Mute=0)")]
    public class GetVolumeMuted : IPanelSource
    {
        public object? GetSourceValue()
        {
            return $"{(Audio.Controller.DefaultPlaybackDevice.IsMuted ? "0" : Audio.Controller.DefaultPlaybackDevice.Volume)}";
        }
    }
}