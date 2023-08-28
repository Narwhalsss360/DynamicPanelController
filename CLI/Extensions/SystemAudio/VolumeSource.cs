using Extensibility;

namespace SystemAudio
{
    public class VolumeSource : StringSource
    {
        public string GetSourceName() => "Volume";
        public string GetString() => $"{ ExtensionsGlobals.Controller.DefaultPlaybackDevice.Volume }";
    }
}