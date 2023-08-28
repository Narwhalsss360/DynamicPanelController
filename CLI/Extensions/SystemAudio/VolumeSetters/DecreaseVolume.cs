using Extensibility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SystemAudio.VolumeSetters
{
    public class DecreaseVolume : ControllerAction
    {
        public string GetActionName() => "Decrease Volume";

        public void Do() => ExtensionsGlobals.Controller.DefaultPlaybackDevice.Volume--;
    }
}
