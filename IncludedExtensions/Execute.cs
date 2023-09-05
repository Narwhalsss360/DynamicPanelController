﻿using Profiling.ProfilingTypes;
using System.ComponentModel;
using System.Diagnostics;
using PanelExtension;

namespace IncludedExtensions
{
    [PanelActionDescriptor("Execute")]
    public class Execute : Extension, IPanelAction
    {
        private string? ProgramPath = null;
        private Dictionary<string, string?> CurrentOptions = new();

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

            string? Caught = null;

            try
            {
                Process.Start(ProgramPath);

            }
            catch (Win32Exception E)
            {
                Caught = E.Message;
            }
            catch (ObjectDisposedException E)
            {
                Caught = E.Message;
            }
            catch(FileNotFoundException E)
            {
                Caught= E.Message;
            }

            if (Caught is not null)
                Application?.Logger.Error($"An error occured trying to execute {ProgramPath}. {Caught}");
            return Caught;
        }
    }
}