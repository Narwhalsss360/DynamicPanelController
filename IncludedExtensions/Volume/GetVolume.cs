﻿using Profiling.ProfilingTypes.PanelItems;

namespace IncludedExtensions.Volume
{
    [PanelSourceDescriptor("Volume")]
    internal class GetVolume : IPanelSource
    {
        public object? GetSourceValue(object? Arguments = null)
        {
            return $"{Volume.Controller.DefaultPlaybackDevice.Volume}";
        }
    }
}