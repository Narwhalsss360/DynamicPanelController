using System.Reflection;

namespace Profiling.ProfilingTypes
{
    public class PanelActionDescriptorAttribute : Attribute
    {
        public string Name;

        public PanelActionDescriptorAttribute(string Name) => this.Name = Name;
    }

    public interface IPanelAction
    {
        object? Do();
    }

    public static class PanelActionExtensions
    {
        public static PanelActionDescriptorAttribute? GetDescriptorAttribute(this IPanelAction Action) => Action.GetType().GetCustomAttribute<PanelActionDescriptorAttribute>();
    }
}