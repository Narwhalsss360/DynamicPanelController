using Extensibility;

namespace SystemAudio.VolumeSetters
{
    public class ToggleMute : ControllerAction
    {
        public string GetActionName() => "Toggle Mute";
        public void Do() => ExtensionsGlobals.Controller.DefaultPlaybackDevice.ToggleMute();
    }
}