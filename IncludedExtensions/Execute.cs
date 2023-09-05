using Profiling.ProfilingTypes;
using System.Diagnostics;

namespace IncludedExtensions
{
    [PanelActionDescriptor("Execute")]
    public class Execute : PanelExtension.Extension, IPanelAction
    {
        private string? ProgramPath = null;
        private Process? UserProcess = null;
        private Dictionary<string, string?> CurrentOptions = new();

        public Execute()
            : base()
        {
            Exit += ApplicationExiting;
        }

        public string?[]?[]? ValidOptions()
        {
            return new string?[]?[] { new string?[] { "Path", null } };
        }

        public Dictionary<string, string?>? GetOptions()
        {
            return CurrentOptions;
        }

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
            if (ProgramPath is null)
            {
                Application?.Logger.Error("No program path specified.");
                return null;
            }

            if (!File.Exists(ProgramPath))
            {
                Application?.Logger.Error($"\"{ProgramPath}\" Doesn't exist.");
                return null;
            }

            UserProcess = Process.Start(ProgramPath);
            UserProcess.Exited += UserProgramExitted;
            Application?.Logger.Info($"Executed {UserProcess.ProcessName}  -> {ProgramPath}");
            return null;
        }

        private void UserProgramExitted(object? Sender, EventArgs Args)
        {
            if (UserProcess is null)
                return;

            Application?.Logger.Info($"{UserProcess.ProcessName} exited with code {UserProcess.ExitCode}.");
            if (UserProcess.ExitCode != 0)
                Application?.Logger.Warn($"Non-zero exit. {UserProcess.StandardError.ReadToEnd()}");
            UserProcess = null;
        }

        private void ApplicationExiting(object? Sender, EventArgs Args)
        {
            if (UserProcess is not null)
                Application?.Logger.Warn("Exitting Controller while executed process has not exitted.");
        }
    }
}