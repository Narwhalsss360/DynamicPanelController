namespace Extensibility
{
    public class StringSourceDescriptor : Attribute
    {
        public readonly string SourceName;

        public StringSourceDescriptor(string SourceName = "")
        {
            this.SourceName = SourceName;
        }
    }

    public interface StringSource
    {
        string GetSourceName();

        string GetString();
    }
}