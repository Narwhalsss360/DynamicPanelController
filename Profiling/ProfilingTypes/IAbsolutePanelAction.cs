using System.Reflection;
using System.Runtime.CompilerServices;

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
        public static AbsolutePanelActionDescriptorAttribute? GetDescriptorAttribute(this IAbsolutePanelAction Action) => Action.GetType().GetCustomAttribute<AbsolutePanelActionDescriptorAttribute>();
    }
}