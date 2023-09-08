using PanelExtension;
using Profiling.ProfilingTypes.PanelItems;
using System.ComponentModel;
using System.Diagnostics;

namespace IncludedExtensions
{
    [PanelActionDescriptor("Execute")]
    public class Execute : Extension, IPanelAction
    {
        private const string PathKey = "Path";
        private const string ArgumentsKey = "Arguments";
        private Dictionary<string, string?> Options = new();

        public string?[]?[]? ValidOptions()
        {
            return new string?[]?[] { new string?[] { PathKey, null }, new string?[] { ArgumentsKey, null } };
        }

        public Dictionary<string, string?>? GetOptions()
        {
            return Options;
        }

        public string? SetOptions(Dictionary<string, string?> Options)
        {
            this.Options = Options;
            if (!Options.ContainsKey(PathKey))
                return "Must specify a program path.";
            if (!File.Exists(Options[PathKey]))
                return "File does not exist.";
            if (Options.ContainsKey(ArgumentsKey))
            {
                if (Options[ArgumentsKey] is null)
                {
                    Options[ArgumentsKey] = "";
                }
            }
            else
            {
                Options.Add(PathKey, "");
            }
            return null;
        }

        public object? Do(object? Arguments = null)
        {
            if (Options[PathKey] is not string ProgramPath)
            {
                Application?.Logger.Log(ILogger.Levels.Error, "No program path specified.", "Execute");
                return null;
            }

            string ProgramArguments = "";

            if (Options[ArgumentsKey] is string ProgramArgs)
            {
                ProgramArguments = ProgramArgs;
            }

            if (!File.Exists(Options[PathKey]))
            {
                Application?.Logger.Log(ILogger.Levels.Error, $"\"{Options[PathKey]}\" Doesn't exist.", "Execute");
                return null;
            }

            string? Caught = null;

            try
            {
                _ = Process.Start(ProgramPath, ProgramArguments);
            }
            catch (Win32Exception E)
            {
                Caught = E.Message;
            }
            catch (ObjectDisposedException E)
            {
                Caught = E.Message;
            }
            catch (FileNotFoundException E)
            {
                Caught = E.Message;
            }

            if (Caught is not null)
                Application?.Logger.Log(ILogger.Levels.Error, $"An error occured trying to execute {Options[PathKey]}. {Caught}", "Execute");
            return Caught;
        }
    }
}