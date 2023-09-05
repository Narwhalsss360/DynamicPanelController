using System.Reflection;

namespace Profiling.ProfilingTypes
{
    public class PanelAbsoluteActionDescriptorAttribute : PanelActionDescriptorAttribute
    {
        public PanelAbsoluteActionDescriptorAttribute(string Name) : base(Name)
        {
        }
    }

    public interface IAbsolutePanelAction : IPanelItem
    {
        object? Set(object? Value);
    }

    public static class AbsolutePanelActionExtensions
    {
        public static PanelAbsoluteActionDescriptorAttribute? GetDescriptorAttribute(this IAbsolutePanelAction Action)
        {
            return Action.GetType().GetCustomAttribute<PanelAbsoluteActionDescriptorAttribute>();
        }
    }
}