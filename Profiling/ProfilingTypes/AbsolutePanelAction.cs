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

    public interface AbsolutePanelAction : PanelAction
    {
        object? Set(object? Value);
    }

    public static class AbsolutePanelActionExtensions
    {
        public static AbsolutePanelActionDescriptorAttribute? GetDescriptorAttribute(this AbsolutePanelAction Action) => Action.GetType().GetCustomAttribute<AbsolutePanelActionDescriptorAttribute>();
    }
}