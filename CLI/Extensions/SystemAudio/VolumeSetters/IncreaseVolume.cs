using Extensibility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SystemAudio.VolumeSetters
{
    public class IncreaseVolume : ControllerAction
    {
        public string GetActionName() => "Increase Volume";

        public void Do() => ExtensionsGlobals.Controller.DefaultPlaybackDevice.Volume++;
    }
}
