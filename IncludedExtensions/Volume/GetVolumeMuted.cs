using Profiling.ProfilingTypes;

namespace IncludedExtensions.Volume
{
    [PanelSourceDescriptor("Volume(Mute=0)")]
    public class GetVolumeMuted : IPanelSource
    {
        public object? GetSourceValue(object? Arguments = null)
        {
            return $"{(Volume.Controller.DefaultPlaybackDevice.IsMuted ? "0" : Volume.Controller.DefaultPlaybackDevice.Volume)}";
        }
    }
}