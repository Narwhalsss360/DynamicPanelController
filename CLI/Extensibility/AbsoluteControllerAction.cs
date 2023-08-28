namespace Extensibility
{
    public interface AbsoluteControllerAction : ControllerAction
    {
        public object? Get();

        public void Set(object? value);
    }
}