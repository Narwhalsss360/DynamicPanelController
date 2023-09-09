using PanelExtension;
using Profiling.ProfilingTypes.PanelItems;
using System.Reflection;

namespace IncludedExtensions.Internals
{
    [PanelSourceDescriptor("Dynamic Source")]
    internal class DynamicSource : Extension, IPanelSource
    {
        private const string SourceValueKey = "SourceValue";
        private Dictionary<string, string?> Options = new();

        public DynamicSource()
            : base()
        {
        }

        public string?[]?[]? ValidOptions()
        {
            return new string?[]?[] { new string?[] { SourceValueKey, null } };
        }

        public string? SetOptions(Dictionary<string, string?> Options)
        {
            this.Options = Options;
            return null;
        }

        public Dictionary<string, string?>? GetOptions()
        {
            return Options;
        }

        public object? GetSourceValue(object? Arguments = null)
        {
            if (Application is null)
                return null;
            if (!Options.ContainsKey(SourceValueKey))
                return null;
            if (Options[SourceValueKey] is not string PropertyName)
                return null;
            return GetProperty<ApplicationVariables>(PropertyName, Application)?.ToString();
        }

        private static object? GetProperty<ClassType>(string PropertyName, object? Instance = null)
        {
            return typeof(ClassType).GetProperty(PropertyName, BindingFlags.NonPublic | BindingFlags.Public | (Instance is null ? BindingFlags.Static : BindingFlags.Instance))?.GetValue(Instance);
        }
    }
}