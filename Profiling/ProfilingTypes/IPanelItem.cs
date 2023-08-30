namespace Profiling.ProfilingTypes
{
    public interface IPanelItem
    {
        public string?[]?[]? ValidOptions() => null;
        public string? NewOptions(Dictionary<string, string?> Options) => null;
    }
}