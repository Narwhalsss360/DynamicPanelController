using AudioSwitcher.AudioApi;
using AudioSwitcher.AudioApi.CoreAudio;
using System.Runtime.CompilerServices;

namespace IncludedExtensions.Volume
{
    internal static class Volume
    {
        public static CoreAudioController Controller = new();
    }
}