using System.Reflection;

namespace Profiling.ProfilingTypes.PanelItems
{
    public class PanelAbsoluteActionDescriptorAttribute : PanelItemDescriptorAttribute
    {
        public PanelAbsoluteActionDescriptorAttribute(string Name)
            : base(Name, typeof(IAbsolutePanelAction))
        {
        }
    }

    public interface IAbsolutePanelAction : IPanelItem
    {
        public object? Set(object? Value);
    }

    public static class AbsolutePanelActionExtensions
    {
        public static PanelAbsoluteActionDescriptorAttribute? GetDescriptorAttribute(this IAbsolutePanelAction Action)
        {
            return Action.GetType().GetCustomAttribute<PanelAbsoluteActionDescriptorAttribute>();
        }
    }
}