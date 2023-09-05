using System.Reflection;

namespace Profiling.ProfilingTypes
{
    public class AbsolutePanelActionDescriptorAttribute : PanelActionDescriptorAttribute
    {
        public AbsolutePanelActionDescriptorAttribute(string Name) : base(Name)
        {
        }
    }

    public interface IAbsolutePanelAction : IPanelAction
    {
        object? Set(object? Value);
    }

    public static class AbsolutePanelActionExtensions
    {
        public static AbsolutePanelActionDescriptorAttribute? GetDescriptorAttribute(this IAbsolutePanelAction Action)
        {
            return Action.GetType().GetCustomAttribute<AbsolutePanelActionDescriptorAttribute>();
        }
    }
}