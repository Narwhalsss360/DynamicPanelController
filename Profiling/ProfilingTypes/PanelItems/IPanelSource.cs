using System.Reflection;

namespace Profiling.ProfilingTypes.PanelItems
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class PanelSourceDescriptorAttribute : PanelItemDescriptorAttribute
    {
        public PanelSourceDescriptorAttribute(string Name)
            : base(Name, typeof(IPanelSource))
        {
        }
    }

    public interface IPanelSource : IPanelItem
    {
        public object? GetSourceValue();
    }

    public static class PanelSourceExtensions
    {
        public static PanelSourceDescriptorAttribute? GetDescriptorAttribute(this IPanelSource Source)
        {
            return Source.GetType().GetCustomAttribute<PanelSourceDescriptorAttribute>();
        }
    }
}