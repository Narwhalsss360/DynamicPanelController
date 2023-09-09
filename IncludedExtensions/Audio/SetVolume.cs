using Profiling.ProfilingTypes.PanelItems;

namespace IncludedExtensions.Volume
{
    [PanelAbsoluteActionDescriptor("Set Volume")]
    internal class SetVolume : IAbsolutePanelAction
    {
        public object? Set(object? value)
        {
            float? TryFloat = value as float?;
            int? TryInt = value as int?;
            double? CastedValue =
                TryInt is not null ? (double)TryInt : TryFloat is not null ? (double)TryFloat : value as double?;
            if (CastedValue is null)
                return null;

            if (CastedValue is >= 0 and <= 100)
                Audio.Controller.DefaultPlaybackDevice.Volume = (double)CastedValue;
            return null;
        }
    }
}