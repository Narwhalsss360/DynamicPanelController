using System.Reflection;

namespace Profiling.ProfilingTypes
{
    public class PanelActionDescriptorAttribute : Attribute
    {
        public string Name;

        public PanelActionDescriptorAttribute(string Name) => this.Name = Name;
    }

    public interface PanelAction
    {
        object? Do();
    }

    public static class PanelActionExtensions
    {
        public static PanelActionDescriptorAttribute? GetDescriptorAttribute(this PanelAction Action) => Action.GetType().GetCustomAttribute<PanelActionDescriptorAttribute>();
    }
}