using System.Reflection;
namespace Profiling.ProfilingTypes
{
    public interface IPanelItem
    {
        public string?[]?[]? ValidOptions() => null;
        public string? NewOptions(Dictionary<string, string?> Options) => null;
    }

    public static class PanelItemExtensions
    {
        public static PanelActionDescriptorAttribute? GetPanelActionDescriptor(this Type T) => T.GetCustomAttribute<PanelActionDescriptorAttribute>();

        public static AbsolutePanelActionDescriptorAttribute? GetAbsolutePanelActionDescriptor(this Type T) => T.GetCustomAttribute<AbsolutePanelActionDescriptorAttribute>();

        public static PanelSourceDescriptorAttribute? GetPanelSourceDescriptor(this Type T) => T.GetCustomAttribute<PanelSourceDescriptorAttribute>();
    }
}