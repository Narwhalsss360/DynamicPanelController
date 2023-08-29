using Profiling.ProfilingTypes;

namespace IncludedExtensions.Volume
{
    [AbsolutePanelActionDescriptor("Set Volume")]
    internal class SetVolume : IAbsolutePanelAction
    {
        public object? Set(object? value)
        {
            float? TryFloat = value as float?;
            int? TryInt = value as int?;
            double? CastedValue = value as double?;

            if (TryInt is not null)
                CastedValue = (double)TryInt;
            else if (TryFloat is not null)
                CastedValue = (double)TryFloat;
            else
                CastedValue = value as int?;

            if (CastedValue is null)
                return null;

            if (0 <= CastedValue && CastedValue <= 100)
                Volume.Controller.DefaultPlaybackDevice.Volume = (double)CastedValue;
            return null;
        }
    }
}