using Extensibility;

namespace SystemAudio
{
    public class VolumeSourceMuteZero : StringSource
    {
        public string GetSourceName() => "Volume Muted Zero";
        public string GetString() => $"{ (ExtensionsGlobals.Controller.DefaultPlaybackDevice.IsMuted ? "0" : ExtensionsGlobals.Controller.DefaultPlaybackDevice.Volume) }";
    }
}