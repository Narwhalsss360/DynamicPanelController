using Profiling.ProfilingTypes;

namespace IncludedExtensions.Volume
{
    [PanelSourceDescriptor("Volume")]
    internal class GetVolume : IPanelSource
    {
        public object? GetSourceValue() => $"{ Volume.Controller.DefaultPlaybackDevice.Volume }";
    }
}