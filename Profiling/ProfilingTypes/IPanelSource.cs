using System.Reflection;

namespace Profiling.ProfilingTypes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class PanelSourceDescriptorAttribute : Attribute
    {
        public string Name;

        public PanelSourceDescriptorAttribute(string Name) => this.Name = Name;
    }

    public interface IPanelSource : IPanelItem
    {
        object? GetSourceValue(object? Arguments = null);
    }

    public static class PanelSourceExtensions
    {
        public static PanelSourceDescriptorAttribute? GetDescriptorAttribute(this IPanelSource Source) => Source.GetType().GetCustomAttribute<PanelSourceDescriptorAttribute>();
    }
}