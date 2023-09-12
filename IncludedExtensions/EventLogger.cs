using PanelExtension;
using Profiling.ProfilingTypes;
using Profiling.ProfilingTypes.PanelItems;

namespace IncludedExtensions
{
    public static class StringBool
    {
        private static readonly string[] DefaultTrueValues = { "true", "yes", "on", "enabled", "enable" };
        private static readonly string[] DefaultFalseValues = { "false", "no", "off", "disabled", "disable" };

        public static bool IsTruePhrase(this string Text, string[]? TrueValues = null)
        {
            if (TrueValues is null)
                TrueValues = DefaultTrueValues;

            return TrueValues.Contains(Text.Trim().ToLower());
        }

        public static bool IsFalsePhrase(this string Text, string[]? FalseValues = null)
        {
            if (FalseValues is null)
                FalseValues = DefaultFalseValues;

            return FalseValues.Contains(Text.Trim().ToLower());
        }
    }

    [PanelItemDescriptor("Event Logger", typeof(EventLogger))]
    [PanelItemGlobalValidOptions(new string[] { $"{LogEnableKey}|True|False" })]
    public class EventLogger : Extension, IPanelItem
    {
        public const string LogEnableKey = "EventLogger Enable";
        private const string EventLoggerSenderName = "Event Logger";
        private static bool AlreadyInstantiated = false;

        private ILogger.Levels LogLevel = ILogger.Levels.Verbose;
        bool Log = true;

        public EventLogger()
            : base()
        {
            if (AlreadyInstantiated)
                return;
            AlreadyInstantiated = true;

            CommunicationsStarted += CommunicationsStartEvent;
            CommunicationsStopped += CommunicationsStoppedEvent;
            Exit += ExitEvent;
            ProfilesListChanged += ProfileListChangedEvent;
            SelectedProfileChanged += SelectedProfileChangedEvent;
            SettingsChanged += SettingsChangedEvent;
            Disposed += InstanceDisposed;
        }

        private void InstanceDisposed(object? sender, EventArgs e)
        {
            if (!Log)
                return;
            AlreadyInstantiated = false;
        }

        private void SettingsChangedEvent(object? Sender, SettingsChangedEventArgs Args)
        {
            if (Args.NewSettings.GlobalOptions.ContainsKey(LogEnableKey))
            {
                Log = Args.NewSettings.GlobalOptions[LogEnableKey].IsTruePhrase();
                Application?.Logger.Log(LogLevel, $"Event Logger {(Log ? "Enabled" : "Disabled" )}", EventLoggerSenderName);
            }

            if (!Log)
                return;
            Application?.Logger.Log(LogLevel, "Settings Changed", EventLoggerSenderName);
        }

        private void SelectedProfileChangedEvent(object? Sender, SelectedProfileChangedEventArgs Args)
        {
            if (!Log)
                return;
            Application?.Logger.Log(LogLevel, $"Profile changed to {Args.NewProfile?.Name}.", EventLoggerSenderName);
        }

        private void ProfileListChangedEvent(object? Sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs Args)
        {
            if (!Log)
                return;
            Application?.Logger.Log(LogLevel, $"Profile list action -> {Args.Action}.", EventLoggerSenderName);
        }

        private void CommunicationsStoppedEvent(object? sender, EventArgs e)
        {
            if (!Log)
                return;
            Application?.Logger.Log(LogLevel, $"Communications Stopped.", EventLoggerSenderName);
        }

        private void CommunicationsStartEvent(object? sender, EventArgs e)
        {
            if (!Log)
                return;
            Application?.Logger.Log(LogLevel, $"Communications Started.", EventLoggerSenderName);
        }

        private void ExitEvent(object? sender, EventArgs e)
        {
            if (!Log)
                return;
            Application?.Logger.Log(LogLevel, $"Exiting.", EventLoggerSenderName);
        }

        ~EventLogger()
        {
            Dispose();
        }
    }
}