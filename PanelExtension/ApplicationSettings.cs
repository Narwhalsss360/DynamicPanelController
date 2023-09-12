using Panel;
using System.Text.Json.Serialization;

namespace PanelExtension
{
    public class ApplicationSettings
    {
        public string FilePath = $"{Environment.CurrentDirectory}\\Settings.json";
        public string ExtensionsDirectory { get; set; } = $"{Environment.CurrentDirectory}\\Extensions";
        public string ProfilesDirectory { get; set; } = $"{Environment.CurrentDirectory}\\Profiles";
        public string LogDirectory { get; set; } = $"{Environment.CurrentDirectory}\\Log.txt";
        public ILogger.Levels LogLevel = ILogger.Levels.Verbose;
        public PanelDescriptor? GlobalPanelDescriptor = null;
        public Dictionary<string, string> GlobalOptions = new();
        public Dictionary<string, string[]> GlobalSettingsValidOptions = new();

        public ApplicationSettings()
        {
        }

        public ApplicationSettings(ApplicationSettings Other)
        {
            FilePath = new(Other.FilePath);
            ExtensionsDirectory = new(Other.ExtensionsDirectory);
            ProfilesDirectory = new(Other.ProfilesDirectory);
            LogDirectory = new(Other.LogDirectory);
            LogLevel = Other.LogLevel;
            if (Other.GlobalPanelDescriptor is not null)
                GlobalPanelDescriptor = new(Other.GlobalPanelDescriptor);
            GlobalOptions = new(Other.GlobalOptions);
            GlobalSettingsValidOptions = new(Other.GlobalSettingsValidOptions);
        }

        public ApplicationSettings(Serializable Serialized)
        {
            if (Directory.Exists(Serialized.ExtensionsDirectory))
                ExtensionsDirectory = Serialized.ExtensionsDirectory;
            if (Directory.Exists(Serialized.ProfilesDirectory))
                ProfilesDirectory = Serialized.ProfilesDirectory;
            if (Directory.Exists(Serialized.LogDirectory))
                LogDirectory = Serialized.LogDirectory;
            LogLevel = Serialized.LogLevel;

            if (Serialized.GlobalPanelDescriptor is not null)
                GlobalPanelDescriptor = new PanelDescriptor(Serialized.GlobalPanelDescriptor);
            if (Serialized.GlobalSettings is not null)
                GlobalOptions = Serialized.GlobalSettings;
            if (Serialized.GlobalSettingsValidOptions is not null)
                GlobalSettingsValidOptions = Serialized.GlobalSettingsValidOptions;
        }

        public class Serializable
        {
            public string ExtensionsDirectory { get; set; } = $"{Environment.CurrentDirectory}\\Extensions";
            public string ProfilesDirectory { get; set; } = $"{Environment.CurrentDirectory}\\Profiles";
            public string LogDirectory { get; set; } = $"{Environment.CurrentDirectory}\\Log.txt";
            public PanelDescriptor.Serializable? GlobalPanelDescriptor { set; get; } = null;
            public Dictionary<string, string>? GlobalSettings { set; get; } = new();
            public Dictionary<string, string[]>? GlobalSettingsValidOptions { set; get; } = new();
            public ILogger.Levels LogLevel { set; get; } = ILogger.Levels.Verbose;

            [JsonConstructor]
            public Serializable()
            {
            }

            public Serializable(ApplicationSettings Settings)
            {
                ExtensionsDirectory = Settings.ExtensionsDirectory;
                ProfilesDirectory = Settings.ProfilesDirectory;
                LogDirectory = Settings.LogDirectory;
                if (Settings.GlobalPanelDescriptor is not null)
                    GlobalPanelDescriptor = new PanelDescriptor.Serializable(Settings.GlobalPanelDescriptor);
                GlobalSettings = Settings.GlobalOptions;
                GlobalSettingsValidOptions = Settings.GlobalSettingsValidOptions;
                LogLevel = Settings.LogLevel;
            }
        }

        public static bool operator ==(ApplicationSettings OperandA, ApplicationSettings OperandB)
        {
            return OperandA.Equals(OperandB);
        }

        public static bool operator !=(ApplicationSettings OperandA, ApplicationSettings OperandB)
        {
            return !OperandA.Equals(OperandB);
        }

        public override bool Equals(object? OtherObject)
        {
            if (OtherObject is not ApplicationSettings OtherSettings)
                return false;
            
            if (FilePath != OtherSettings.FilePath)
                return false;
            if (ExtensionsDirectory != OtherSettings.ExtensionsDirectory)
                return false;
            if (ProfilesDirectory != OtherSettings.ProfilesDirectory)
                return false;
            if (LogDirectory != OtherSettings.LogDirectory)
                return false;
            if (LogLevel != OtherSettings.LogLevel)
                return false;
            if (GlobalPanelDescriptor != OtherSettings.GlobalPanelDescriptor)
                return false;
            if (!GlobalOptions.CompareKVPs(OtherSettings.GlobalOptions))
                return false;

            return true;
        }
    }
}