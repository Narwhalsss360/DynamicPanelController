using System.Reflection;

namespace Extensibility
{
    public class LoadedExtensions
    {
        public static Tuple<Assembly, string>? LoadRequest { get; private set; } = null;
        public static object? LoadResult { get; private set; } = null;
        public static Type[]? ControllerActions { get; private set; } = null;
        public static Type[]? StringSources { get; private set; } = null;

        private LoadedExtensions()
        {
        }

        public static void RequestLoad(Assembly AssemblyToLoad, string ModuleName)
        {
            LoadRequest = new Tuple<Assembly, string>(AssemblyToLoad, ModuleName);
        }
    }
}