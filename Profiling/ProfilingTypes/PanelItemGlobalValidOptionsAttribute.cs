namespace Profiling.ProfilingTypes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
    public class PanelItemGlobalValidOptionsAttribute : Attribute
    {
        public readonly string[] ValidOptions;

        public PanelItemGlobalValidOptionsAttribute(string[] ValidOptions)
        {
            this.ValidOptions = ValidOptions;
        }
    }
}