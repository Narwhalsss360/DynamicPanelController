using Profiling;
using System.Reflection;

namespace PanelExtension
{
    public delegate int ExtensionLoader(Assembly AssemblyToLoad, string? ModuleName);
    public delegate void Refresher();

    public class Extension
    {
        public static ExtensionLoader? ExtensionLoader { get; private set; } = null;
        public static Refresher? Refresher { get; private set; } = null;
        public static PanelProfile? CurrentProfile { get; private set; } = null;
        public static PanelProfile[]? Profiles { get; private set; } = null;
        public static Type[]? PanelActions { get; private set; } = null;
        public static Type[]? PanelSources { get; private set; } = null;
        private static List<EventHandler> ExitingHandlers = new();
        public static ILogger? Logger { get; private set; } = null;

        private Extension()
        {
        }

        public static bool Refresh()
        {
            if (Refresher is null)
                return false;
            Refresher();
            return true;
        }

        public static void AddExitHandler(EventHandler Handler)
        {
            ExitingHandlers.Add(Handler);
        }

        public static void RemoveExitHandler(EventHandler Handler)
        {
            while (ExitingHandlers.Contains(Handler))
                ExitingHandlers.Remove(Handler);
        }

        public static int RequestLoad(Assembly AssemblyToLoad, string? ModuleName = null)
        {
            if (ExtensionLoader is null)
                return int.MinValue;
            return ExtensionLoader(AssemblyToLoad, ModuleName);
        }
    }
}