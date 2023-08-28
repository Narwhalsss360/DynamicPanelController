using PanelExtension;
using Profiling.ProfilingTypes;
using System.Diagnostics;

namespace IncludedExtensions
{
    [PanelActionDescriptor("Execute")]
    public class Execute : IPanelAction
    {
        public object? Do()
        {
            Extension.Refresh();
            if (!File.Exists($"{Environment.CurrentDirectory}\\ExecutePath.txt"))
            {
                Extension.Logger?.Error("$\"{Environment.CurrentDirectory}\\\\ExecutePath.txt\" Doesn't exist.");
                return null;
            }

            string FilePath = new StreamReader($"{Environment.CurrentDirectory}\\ExecutePath.txt").ReadToEnd();

            if (!File.Exists(FilePath))
            {
                Extension.Logger?.Error($"\"{FilePath}\" Doesn't exist.");
                return null;
            }

            Process.Start(FilePath);
            Extension.Logger?.Info($"Executed { FilePath }");

            return null;
        }
    }
}