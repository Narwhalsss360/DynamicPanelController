namespace Profiling.ProfilingTypes
{
    public class Mapping
    {
        public byte ID { get; set; }
    }

    public static class MappingExtensions
    {
        public static TMapping? GetMappingFromID<TMapping>(this TMapping[] Mappings, byte ID) where TMapping : Mapping => Array.Find(Mappings, Mapping => Mapping.ID == ID);
    }
}