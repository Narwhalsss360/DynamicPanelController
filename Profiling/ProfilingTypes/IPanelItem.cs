using System.Reflection;
namespace Profiling.ProfilingTypes
{
    public interface IPanelItem
    {
        public string?[]?[]? ValidOptions()
        {
            return null;
        }

        public string? SetOptions(Dictionary<string, string?> Options)
        {
            return null;
        }

        public Dictionary<string, string?>? GetOptions()
        {
            return null;
        }
    }

    public static class PanelItemExtensions
    {
        public static PanelActionDescriptorAttribute? GetPanelActionDescriptor(this Type T)
        {
            return T.GetCustomAttribute<PanelActionDescriptorAttribute>();
        }

        public static PanelAbsoluteActionDescriptorAttribute? GetPanelAbsoluteActionDescriptor(this Type T)
        {
            return T.GetCustomAttribute<PanelAbsoluteActionDescriptorAttribute>();
        }

        public static PanelSourceDescriptorAttribute? GetPanelSourceDescriptor(this Type T)
        {
            return T.GetCustomAttribute<PanelSourceDescriptorAttribute>();
        }
    }
}