using Extensibility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SystemAudio.VolumeSetters
{
    public class SetVolume : AbsoluteControllerAction
    {
        public string GetActionName() => "Set Volume";

        public object? Get() => null;

        public void Set(object? value)
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
                return;

            if (0 <= CastedValue && CastedValue <= 100)
                ExtensionsGlobals.Controller.DefaultPlaybackDevice.Volume = (double)CastedValue;
        }

        public void Do()
        {
        }
    }
}