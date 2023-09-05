using NStreamCom;
using Panel;
using Panel.Communication;
using PanelExtension;
using Profiling;
using Profiling.ProfilingTypes;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Windows;

namespace DynamicPanelController
{
    public partial class App : Application
    {
        public List<PanelProfile> Profiles = new();
        public int SelectedProfileIndex = -1;

        public List<Type> Actions = new();
        public List<Type> AbsoluteActions = new();
        public List<Type> Sources = new();

        public static readonly ushort PacketSize = 16;
        public SerialPort Port { get; private set; } = new SerialPort() { BaudRate = 115200 };
        readonly Thread SendSourceMappingsThread;
        bool SuspendSendThread = false;
        readonly PacketCollector Collector = new();
        public static readonly int InputIDIndex = 0;
        public static readonly int ButtonStateIndex = 1;
        public bool Communicating { get; private set; } = false;

        public struct AppSettings
        {
            public string FilePath = $"{Environment.CurrentDirectory}\\Settings.json";
            public string ExtensionsDirectory { get; set; } = $"{Environment.CurrentDirectory}\\Extensions";
            public string ProfilesDirectory { get; set; } = $"{Environment.CurrentDirectory}\\Profiles";
            public string LogDirectory { get; set; } = $"{Environment.CurrentDirectory}\\Log.txt";
            public PanelDescriptor? GlobalPanelDescriptor = null;
            public Dictionary<string, string> GlobalSettings = new();
            public Dictionary<string, string> GlobalSettingsValidOptions = new();

            public AppSettings()
            {
            }


            public AppSettings(Serializable Serialized)
            {
                ExtensionsDirectory = Serialized.ExtensionsDirectory;
                ProfilesDirectory = Serialized.ProfilesDirectory;
                LogDirectory = Serialized.LogDirectory;
                if (Serialized.GlobalPanelDescriptor is not null)
                    GlobalPanelDescriptor = new PanelDescriptor(Serialized.GlobalPanelDescriptor);
                if (Serialized.GlobalSettings is not null)
                    GlobalSettings = Serialized.GlobalSettings;
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
                public Dictionary<string, string>? GlobalSettingsValidOptions { set; get; } = new();

                [JsonConstructor]
                public Serializable()
                {
                }

                public Serializable(AppSettings Settings)
                {
                    ExtensionsDirectory = Settings.ExtensionsDirectory;
                    ProfilesDirectory = Settings.ProfilesDirectory;
                    LogDirectory = Settings.LogDirectory;
                    if (Settings.GlobalPanelDescriptor is not null)
                        GlobalPanelDescriptor = new PanelDescriptor.Serializable(Settings.GlobalPanelDescriptor);
                    GlobalSettings = Settings.GlobalSettings;
                    GlobalSettingsValidOptions = Settings.GlobalSettingsValidOptions;
                }
            }
        }

        public AppSettings Settings = new();

        class ApplicationLogger : ILogger
        {
            public string CurrentLog { get; private set; } = string.Empty;
            public event EventHandler? LogChanged;

            public void Info(string Message)
            {
                if (CurrentLog.Length > 0)
                    if (!(CurrentLog.Last() == '\r' || CurrentLog.Last() == '\n'))
                        CurrentLog += '\n';
                CurrentLog += $"{DateTime.Now} Info:{Message}";
                LogChanged?.Invoke(this, new EventArgs());
            }

            public void Warn(string Message)
            {
                if (CurrentLog.Length > 0)
                    if (!(CurrentLog.Last() == '\r' || CurrentLog.Last() == '\n'))
                        CurrentLog += '\n';
                CurrentLog += $"{DateTime.Now} Warning:{Message}";
                LogChanged?.Invoke(this, new EventArgs());
            }

            public void Error(string Message)
            {
                if (CurrentLog.Length > 0)
                    if (!(CurrentLog.Last() == '\r' || CurrentLog.Last() == '\n'))
                        CurrentLog += '\n';
                CurrentLog += $"{DateTime.Now} Error:{Message}";
                LogChanged?.Invoke(this, new EventArgs());
            }

            public string GetLog() => CurrentLog;
        }

        public static readonly ILogger Logger = new ApplicationLogger();

        public event EventHandler? CommunicationsStarted;
        public event EventHandler? CommunicationsStopped;

        readonly List<Extension> PanelExtensions = new();

        App()
        {
            Startup += ApplicationStarting;
            Port.DataReceived += PortDataReceived;
            Collector.PacketsReady += PacketsCollected;
            Exit += Exiting;
            SendSourceMappingsThread = new Thread(SendSourceMappings);
        }

        void ApplicationStarting(object Sender, EventArgs Args)
        {
            Logger.Info("Program starting");
            LoadSettings();
            SetProperty<PanelExtension.Extension>("Subscriber", (PanelExtensionSubscriber)SubscribePanelExtension);
            SetProperty<PanelExtension.Extension>("Unsubscriber", (PanelExtensionUnsubscriber)UnsubscribePanelExtension);

            LoadExtensionsFromDirectory();
            SetProperty<PanelExtension.Extension>("ExtensionLoader", (ExtensionLoader)LoadExtension);
            SetProperty<PanelExtension.Extension>("Refresher", (ExtensionRefresher)RefreshPanelExtension);
            LoadProfiles();
        }

        void LoadExtensionsFromDirectory()
        {
            if (!Directory.Exists(Settings.ExtensionsDirectory))
                return;
            foreach (FileInfo File in new DirectoryInfo(Settings.ExtensionsDirectory).GetFiles())
            {
                if (File.Extension != ".dll")
                    continue;

                Assembly? Extension;
                try
                {
                    Extension = Assembly.LoadFrom(File.FullName);
                }
                catch (BadImageFormatException)
                {
                    continue;
                }
                LoadExtension(Extension, File.Name);
            }
        }

        int LoadExtension(Assembly AssemblyToLoad, string? ModuleName)
        {
            Type[]? Types;
            if (ModuleName is null)
            {
                Types = AssemblyToLoad.GetTypes();
            }
            else
            {
                Module? module = AssemblyToLoad.GetModule(ModuleName);
                if (module is null)
                    return -1;
                Types = module.GetTypes();
            }

            if (Types is null)
            {
                Logger.Error($"Couldn't load assembly {AssemblyToLoad.FullName}");
                return -2;
            }

            int ExtensionsLoaded = 0;

            foreach (Type Type in Types)
            {
                foreach (Type Interface in Type.GetInterfaces())
                {
                    if (Interface == typeof(IPanelAction))
                    {
                        if (Type.GetCustomAttribute<PanelActionDescriptorAttribute>() is null)
                        {
                            Logger.Error($"Type {Type.FullName} from assembly {AssemblyToLoad.FullName} does not have PanelActionDescriptorAttribute.");
                            continue;
                        }
                        Actions.Add(Type);
                        ExtensionsLoaded++;
                        Logger.Info($"Loaded {Type.FullName}");
                    }
                    else if (Interface == typeof(IPanelSource))
                    {
                        if (Type.GetCustomAttribute<PanelSourceDescriptorAttribute>() is null)
                        {
                            Logger.Error($"Type {Type.FullName} from assembly {AssemblyToLoad.FullName} does not have PanelSourceDescriptorAttribute.");
                            continue;
                        }
                        Sources.Add(Type);
                        ExtensionsLoaded++;
                        Logger.Info($"Loaded {Type.FullName}");
                    }
                }
            }
            if (ExtensionsLoaded > 0)
                Logger.Info($"Loaded {ExtensionsLoaded} from {AssemblyToLoad.FullName}");
            return ExtensionsLoaded;
        }

        void LoadProfile(string Json) => Profiles.Add(new PanelProfile(Json, Actions.ToArray(), Sources.ToArray()));

        void LoadProfile(byte[] Bytes) => LoadProfile(Encoding.UTF8.GetString(Bytes));

        void LoadProfiles()
        {
            if (!Directory.Exists(Settings.ProfilesDirectory))
                return;
            foreach (var Profile in new DirectoryInfo(Settings.ProfilesDirectory).GetFiles())
            {
                if (Profile.Extension != ".json")
                    continue;
                using var ProfileStream = Profile.Open(FileMode.Open);
                byte[] FileBytes = new byte[ProfileStream.Length];
                ProfileStream.Read(FileBytes, 0, FileBytes.Length);
                LoadProfile(FileBytes);
            }
            if (Profiles.Count == 0)
                Profiles.Add(new PanelProfile() { Name = "Empty" });
        }

        void LoadSettings()
        {
            if (!File.Exists(Settings.FilePath))
            {
                Logger.Error($"Could not find settings file @ {Settings.FilePath}");
                return;
            }

            using var SettingsFile = new StreamReader(Settings.FilePath);
            AppSettings.Serializable? Deserialized = null;
            try
            {
                Deserialized = JsonSerializer.Deserialize<AppSettings.Serializable>(SettingsFile.ReadToEnd());
            }
            catch (JsonException Ex)
            {
                Logger.Error($"Exception occured while loading settings {Ex.Message}. Loading default settings.");
            }
            if (Deserialized is not null)
                Settings = new(Deserialized);
        }

        object? SubscribePanelExtension(PanelExtension.Extension Extension)
        {
            if (PanelExtensions.Contains(Extension))
                return "Already subscribed.";
            PanelExtensions.Add(Extension);
            return null;
        }

        void SetPanelExtensionVariables(PanelExtension.Extension Extension)
        {
            PanelExtension.Extension.ApplicationVariables Variables = new();
            SetProperty<PanelExtension.Extension.ApplicationVariables>("LastLoad", DateTime.Now, Variables);
            SetProperty<PanelExtension.Extension.ApplicationVariables>("Logger", Logger, Variables);
            SetProperty<PanelExtension.Extension.ApplicationVariables>("CurrentProfile", (SelectedProfileIndex == -1 ? null : Profiles[SelectedProfileIndex]), Variables);
            SetProperty<PanelExtension.Extension.ApplicationVariables>("Profiles", (SelectedProfileIndex == -1 ? null : Profiles[SelectedProfileIndex]), Variables);
            SetProperty<PanelExtension.Extension.ApplicationVariables>("Actions", Actions.ToArray(), Variables);
            SetProperty<PanelExtension.Extension.ApplicationVariables>("AbsoluteActions", AbsoluteActions.ToArray(), Variables);
            SetProperty<PanelExtension.Extension.ApplicationVariables>("Sources", Sources.ToArray(), Variables);
            SetProperty<PanelExtension.Extension>("Application", Variables, Extension);
        }

        void RefreshPanelExtension(PanelExtension.Extension? Extension)
        {
            if (Extension is null)
                PanelExtensions.ForEach(E => SetPanelExtensionVariables(E));
            else
                SetPanelExtensionVariables(Extension);
        }

        object? UnsubscribePanelExtension(PanelExtension.Extension Extension)
        {
            if (PanelExtensions.Contains(Extension))
                return "Not subscribed.";
            PanelExtensions.Remove(Extension);
            return null;
        }

        public void StartPortCommunication()
        {
            Logger.Info($"Starting communications on port {Port.PortName}");
            Port.Open();
            SuspendSendThread = false;
            if (SendSourceMappingsThread.ThreadState == ThreadState.Unstarted)
                SendSourceMappingsThread.Start();
            Communicating = true;
            CommunicationsStarted?.Invoke(this, new EventArgs());
        }

        void SendSourceMappings()
        {
            while (!SuspendSendThread)
            {
                if (!Port.IsOpen)
                    continue;
                if (SelectedProfileIndex == -1 || Profiles.Count == 0)
                    continue;
                if (Profiles[SelectedProfileIndex].SourceMappings.Count == 0)
                    continue;

                foreach (var SourceMapping in Profiles[SelectedProfileIndex].SourceMappings)
                {
                    List<byte> Bytes = new() { SourceMapping.ID };
                    if (SourceMapping.Source.GetSourceValue() is not string OutString)
                        continue;
                    Bytes.AddRange(Encoding.UTF8.GetBytes(OutString));
                    Bytes.Add(0);

                    try
                    {
                        if (SuspendSendThread)
                            continue;
                        Message.FastWrite(1, Bytes.ToArray(), PacketSize, Port.BaseStream);
                    }
                    catch (Exception)
                    {
                        Logger.Error($"Device on port {Port.PortName} disconnected.");
                        StopPortCommunication();
                    }
                }
            }
        }

        public void StopPortCommunication()
        {
            Logger.Info($"Stopping communications on port {Port.PortName}");
            SuspendSendThread = true;
            Port.Close();
            Communicating = false;
            CommunicationsStopped?.Invoke(this, new EventArgs());
        }

        void PacketsCollected(object? Sender, PacketsReadyEventArgs Args)
        {
            Message ReceivedMessage = new(Args.Packets);

            switch (ReceivedMessage.MessageID.ToReceiveID())
            {
                case MessageReceiveIDs.Reserved:
                    break;
                case MessageReceiveIDs.ButtonStateUpdate:
                    if (ReceivedMessage.MessageSize < 2)
                    {
                        Logger.Error("Did not receive button state, only button ID.");
                        break;
                    }

                    byte InputID = ReceivedMessage.Data[InputIDIndex];
                    ButtonUpdateStates ButtonState = ReceivedMessage.Data[ButtonStateIndex].ToButtonUpdateState();

                    Logger.Info($"Button {InputID} has been {ButtonState}");

                    if (SelectedProfileIndex == -1)
                        break;

                    PanelProfile SelectedProfile = Profiles[SelectedProfileIndex];
                    if (SelectedProfile.ActionMappings.Find(A => A.ID == InputID && A.UpdateState == ButtonState) is not ActionMapping Mapping)
                        return;
                    Logger.Info($"Doing action {Mapping.Action.GetDescriptorAttribute()?.Name}");
                    Mapping.Action.Do();
                    break;
                case MessageReceiveIDs.AbsolutePosition:
                    break;
                default:
                    break;
            }
        }

        void PortDataReceived(object Sender, SerialDataReceivedEventArgs Args)
        {
            if (Sender is not SerialPort Port)
                return;
            Collector.Collect(Encoding.UTF8.GetBytes(Port.ReadExisting()));
        }

        void SaveSettings()
        {
            using var SettingsFile = new StreamWriter(Settings.FilePath);
            SettingsFile.Write(JsonSerializer.Serialize(new AppSettings.Serializable(Settings), options: new JsonSerializerOptions() { WriteIndented = true }));
        }

        void SaveProfiles()
        {
            foreach (var Profile in Profiles)
                using (var ProfileFile = new StreamWriter($"{Settings.ProfilesDirectory}\\{Profile.Name}.json"))
                    ProfileFile.Write(Profile.Serialize());
        }

        void Exiting(object Sender, EventArgs Args)
        {
            if (SendSourceMappingsThread.ThreadState != ThreadState.Unstarted)
                SendSourceMappingsThread.Join();
            PanelExtensions.ForEach(E => InvokeMethod<PanelExtension.Extension>("ApplicationExiting", new object[] { Sender, Args }, E));
            Logger.Info("Program exiting...");
            SaveSettings();
            SaveProfiles();
            new StreamWriter(Settings.LogDirectory, true).Write(Logger.GetLog());
        }

        static object? InvokeMethod<ClassType>(string MethodName, object?[]? Parameters, object? Instance, Type[]? Types = null)
        {
            Type t = typeof(ClassType);
            MethodInfo? m;
            if (Types is not null)
                m = t.GetMethod(MethodName, BindingFlags.NonPublic | BindingFlags.Public | (Instance is null ? BindingFlags.Static : BindingFlags.Instance), Types);
            else
                m = t.GetMethod(MethodName, BindingFlags.NonPublic | BindingFlags.Public | (Instance is null ? BindingFlags.Static : BindingFlags.Instance));
            return m?.Invoke(Instance, Parameters);
        }

        static void SetProperty<ClassType>(string PropertyName, object? Value, object? Instance = null) => typeof(ClassType).GetProperty(PropertyName, BindingFlags.NonPublic | BindingFlags.Public | (Instance is null ? BindingFlags.Static : 0))?.SetValue(Instance, Value);
    }
}