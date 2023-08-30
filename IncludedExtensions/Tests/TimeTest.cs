﻿using Profiling.ProfilingTypes;

namespace IncludedExtensions.Tests
{
    [PanelSourceDescriptor("24-Hour Time: HHmm")]
    internal class TimeTest : IPanelSource
    {
        public object? GetSourceValue() => DateTime.Now.ToString("HHmm");
    }
}