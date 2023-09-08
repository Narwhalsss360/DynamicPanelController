using Profiling.ProfilingTypes.PanelItems;

namespace IncludedExtensions
{
    [PanelSourceDescriptor("Static Source")]
    internal class StaticSource : IPanelSource
    {
        private const string SourceValueKey = "SourceValue";
        Dictionary<string, string?> ExtensionOptions = new();

        public string?[]?[]? ValidOptions() => new string?[]?[] { new string?[] { SourceValueKey, null } };

        public string? SetOptions(Dictionary<string, string?> Options)
        {
            ExtensionOptions = Options;
            return null;
        }

        public Dictionary<string, string?>? GetOptions() => ExtensionOptions;

        public object? GetSourceValue(object? Arguments = null) => ExtensionOptions.ContainsKey(SourceValueKey) ? ExtensionOptions[SourceValueKey] : null;
    }
}