using System.Reflection;

namespace Profiling.ProfilingTypes.PanelItems
{
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Struct | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class PanelItemDescriptorAttribute : Attribute
    {
        public readonly string Name;
        public readonly Type? IPanelItemImplementor;

        public PanelItemDescriptorAttribute(string Name, Type? IPanelItemImplementor = null)
        {
            this.Name = Name;
            this.IPanelItemImplementor = IPanelItemImplementor;
        }
    }

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

        public static PanelItemDescriptorAttribute? GetPanelItemDescriptor(this Type T)
        {
            return T.GetCustomAttribute<PanelItemDescriptorAttribute>();
        }

        public static PanelItemDescriptorAttribute? GetPanelItemDescriptor(this object PanelItem)
        {
            return PanelItem.GetType().GetCustomAttribute<PanelItemDescriptorAttribute>();
        }
    }
}