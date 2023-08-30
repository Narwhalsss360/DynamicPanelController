using Profiling.ProfilingTypes;

namespace IncludedExtensions.Volume
{
    [PanelSourceDescriptor("Volume")]
    internal class GetVolume : IPanelSource
    {
        public object? GetSourceValue(object? Arguments = null) => $"{ Volume.Controller.DefaultPlaybackDevice.Volume }";
    }
}