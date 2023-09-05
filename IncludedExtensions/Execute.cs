using PanelExtension;
using Profiling.ProfilingTypes;
using System.Diagnostics;

namespace IncludedExtensions
{
    [PanelActionDescriptor("Execute")]
    public class Execute : IPanelAction
    {
        string? ProgramPath = null;
        Process? UserProcess = null;
        Dictionary<string, string?> CurrentOptions = new();

        public string?[]?[]? ValidOptions() => new string?[]?[] { new string?[] { "Path", null } };

        public Dictionary<string, string?>? GetOptions() => CurrentOptions;

        public string? SetOptions(Dictionary<string, string?> Options)
        {
            if (!Options.ContainsKey("Path"))
                return "Must specify a program path.";
            if (!File.Exists(Options["Path"]))
                return "File does not exist.";
            ProgramPath = Options["Path"];
            CurrentOptions = Options;
            return null;
        }

        public object? Do(object? Arguments = null)
        {
            Extension.Refresh();
            if (ProgramPath is null)
            {
                Extension.Logger?.Error("No program path specified.");
                return null;
            }

            if (!File.Exists(ProgramPath))
            {
                Extension.Logger?.Error($"\"{ProgramPath}\" Doesn't exist.");
                return null;
            }

            UserProcess = Process.Start(ProgramPath);
            UserProcess.Exited += UserProgramExitted;
            Extension.Logger?.Info($"Executed {UserProcess.ProcessName}  -> {ProgramPath}");

            return null;
        }

        void UserProgramExitted(object? Sender, EventArgs Args)
        {
            if (UserProcess is null)
                return;

            Extension.Logger?.Info($"{UserProcess.ProcessName} exited with code {UserProcess.ExitCode}.");
            if (UserProcess.ExitCode != 0)
                Extension.Logger?.Warn($"Non-zero exit. {UserProcess.StandardError.ReadToEnd()}");
            UserProcess = null;
        }
    }
}