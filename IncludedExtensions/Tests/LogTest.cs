﻿using PanelExtension;
using Profiling.ProfilingTypes.PanelItems;

namespace IncludedExtensions.Tests
{
    [PanelActionDescriptor("Log Test")]
    public class LogTest : Extension, IPanelAction
    {
        public object? Do()
        {
            Application?.Logger.Log(ILogger.Levels.Info, "Test action ran.", "Log Test Action");
            return null;
        }
    }
}