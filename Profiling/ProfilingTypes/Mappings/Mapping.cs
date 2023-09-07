using Profiling.ProfilingTypes.PanelItems;

namespace Profiling.ProfilingTypes.Mappings
{
    [PanelItemDescriptor("Mapping Base", typeof(Mapping))]
    public class Mapping : IPanelItem
    {
        public byte ID { get; set; }
    }

    public static class MappingExtensions
    {
        public static TMapping? GetMappingFromID<TMapping>(this TMapping[] Mappings, byte ID) where TMapping : Mapping
        {
            return Array.Find(Mappings, Mapping => Mapping.ID == ID);
        }
    }
}