using System.Reflection;

namespace Profiling.ProfilingTypes.PanelItems
{
    public class PanelActionDescriptorAttribute : PanelItemDescriptorAttribute
    {
        public PanelActionDescriptorAttribute(string Name)
            : base(Name, typeof(IPanelItem))
        {
        }
    }

    public interface IPanelAction : IPanelItem
    {
        public object? Do();
    }

    public static class PanelActionExtensions
    {
        public static PanelActionDescriptorAttribute? GetDescriptorAttribute(this IPanelAction Action)
        {
            return Action.GetType().GetCustomAttribute<PanelActionDescriptorAttribute>();
        }
    }
}