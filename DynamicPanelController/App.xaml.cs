using NStreamCom;
using Panel;
using Panel.Communication;
using PanelExtension;
using Profiling;
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
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Profiling.ProfilingTypes.Mappings;
using Profiling.ProfilingTypes.PanelItems;

namespace DynamicPanelController
{
    public delegate void EmulatorDisplayOut(byte ID, string Data);

    public partial class App : Application
    {
        public ObservableCollection<PanelProfile> Profiles = new();
        public int SelectedProfileIndexContainer;

        public int SelectedProfileIndex
        {
            get
            {
                return SelectedProfileIndexContainer;
            }
            set
            {
                if (value >= Profiles.Count)
                    return;
                SelectedProfileIndexContainer = value;
                SelectedProfileChanged?.Invoke(this, new EventArgs());
                PanelExtensions.ForEach(E => InvokeMethod<Extension>("SelectedProfileChangedWrapper", new object[] { this, new EventArgs() }, E));
            }
        }

        public event EventHandler? SelectedProfileChanged;

        public List<Type> Actions = new();
        public List<Type> AbsoluteActions = new();
        public List<Type> Sources = new();
        public static readonly ushort PacketSize = 16;
        public SerialPort Port { get; private set; } = new SerialPort() { BaudRate = 115200 };

        private Thread SendSourceMappingsThread;
        private bool SuspendSendThread = false;
        private readonly PacketCollector Collector = new();
        public static readonly int InputIDIndex = 0;
        public static readonly int ButtonStateIndex = 1;
        public bool Communicating { get; private set; } = false;
        public bool AllowEmulator { get
            {
                if (Settings.GlobalSettings.ContainsKey("EmulatorEnabled"))
                {
                    return Settings.GlobalSettings["EmulatorEnabled"].ToLower() == "true";
                }
                else
                {
                    return false;
                }
            } }
        private bool Emulating = false;
        public EmulatorDisplayOut? EmulatorDisplay = null;

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

        private class ApplicationLogger : ILogger
        {
            public string CurrentLog { get; private set; } = string.Empty;
            public event EventHandler? LogChanged;

            public string FormatMessage(ILogger.LogLevels? Level, object? Sender, string Message)
            {
                return $"{DateTime.Now:HH:mm} {(Sender is null ? "" : $"[{Level}]")}{(Sender is null ? "" : $"[{Level}]")}: {Message}";
            }

            public void Verbose(string Message, object? Sender = null)
            {
                if (CurrentLog.Length > 0)
                    if (CurrentLog.Last() is not ('\r' or '\n'))
                        CurrentLog += '\n';
                CurrentLog += FormatMessage(ILogger.LogLevels.Verbose, Sender, Message);
                LogChanged?.Invoke(this, new EventArgs());
            }

            public void Info(string Message, object? Sender = null)
            {
                if (CurrentLog.Length > 0)
                    if (CurrentLog.Last() is not ('\r' or '\n'))
                        CurrentLog += '\n';
                CurrentLog += FormatMessage(ILogger.LogLevels.Info, Sender, Message);
                LogChanged?.Invoke(this, new EventArgs());
            }

            public void Warn(string Message, object? Sender = null)
            {
                if (CurrentLog.Length > 0)
                    if (CurrentLog.Last() is not ('\r' or '\n'))
                        CurrentLog += '\n';
                CurrentLog += FormatMessage(ILogger.LogLevels.Warning, Sender, Message);
                LogChanged?.Invoke(this, new EventArgs());
            }

            public void Error(string Message, object? Sender = null)
            {
                if (CurrentLog.Length > 0)
                    if (CurrentLog.Last() is not ('\r' or '\n'))
                        CurrentLog += '\n';
                CurrentLog += FormatMessage(ILogger.LogLevels.Error, Sender, Message);
                LogChanged?.Invoke(this, new EventArgs());
            }

            public string GetLog()
            {
                return CurrentLog;
            }
        }

        public static readonly ILogger Logger = new ApplicationLogger();

        public event EventHandler? CommunicationsStarted;
        public event EventHandler? CommunicationsStopped;

        private readonly List<Extension> PanelExtensions = new();

        private App()
        {
            SelectedProfileIndex = -1;
            Startup += ApplicationStarting;
            Port.DataReceived += PortDataReceived;
            Collector.PacketsReady += PacketsCollected;
            Exit += Exiting;
            Profiles.CollectionChanged += ProfilesListChanged;
            SendSourceMappingsThread = new Thread(SendSourceMappings);
        }

        void ProfilesListChanged(object? Sender, NotifyCollectionChangedEventArgs Args)
        {
            var Collection = Sender as ObservableCollection<PanelProfile>;
            if (Collection is null)
                return;

            if (Args.Action == NotifyCollectionChangedAction.Remove)
            {
                if (Collection.Count == 0)
                    Collection.Add(new PanelProfile() { Name = "New Profile" });

                if (SelectedProfileIndex >= Collection.Count)
                    SelectIndex(0);
            }
            else if (Args.Action == NotifyCollectionChangedAction.Add)
            {
                if (Collection.Count == 1)
                    return;

                if (Args.NewItems is null)
                    return;

                for (int iItems = 0; iItems < Collection.Count; iItems++)
                {
                    if (iItems >= Args.NewStartingIndex)
                        continue;

                    foreach (var NewItem in Args.NewItems)
                    {
                        if (NewItem is not PanelProfile NewProfile)
                            continue;

                        for (int iName = 1; Collection.Any(Profile => ReferenceEquals(Profile, NewProfile) ? false : Profile.Name == NewProfile.Name); iName++)
                            NewProfile.Name = $"{Collection[iItems].Name}({iName})";
                    }
                }
            }
            else if (Args.Action == NotifyCollectionChangedAction.Replace)
            {
                if (Args.NewItems is null)
                    return;

                if (Args.OldItems is null)
                    return;

                int iNewItems = 0;
                foreach (var NewObject in Args.NewItems)
                {
                    if (NewObject is not PanelProfile NewProfile)
                        goto EndLoop;

                    for (int iSearch = Args.NewItems.Count - 1; iSearch >= 0 ; iSearch--)
                    {
                        if (iSearch == iNewItems)
                            continue;

                        if (Args.NewItems[iSearch] is not PanelProfile CheckProfile)
                            continue;

                        for (int iName = 1; CheckProfile.Name == NewProfile.Name; iName++)
                            NewProfile.Name = $"{CheckProfile.Name}({iName})";
                    }
                EndLoop:
                    iNewItems++;
                }

                foreach (var NewObject in Args.NewItems)
                {
                    if (NewObject is not PanelProfile NewProfile)
                        continue;
                    for (int iItems = 0; iItems < Collection.Count; iItems++)
                    {
                        if (ReferenceEquals(NewProfile, Collection[iItems]))
                            continue;

                        for (int iName = 1; Collection.Any(Profile => ReferenceEquals(Profile, NewProfile) ? false : Profile.Name == NewProfile.Name); iName++)
                                NewProfile.Name = $"{Collection[iItems].Name}({iName})";
                    }
                }
            }

            PanelExtensions.ForEach(E => InvokeMethod<Extension>("ProfilesChangedWrapper", new object?[] { null, Args }, E));
        }

        private void ApplicationStarting(object Sender, EventArgs Args)
        {
            Logger.Info("Program starting", "Program");
            LoadSettings();
            SetProperty<Extension>("Subscriber", (PanelExtensionSubscriber)SubscribePanelExtension);
            SetProperty<Extension>("Unsubscriber", (PanelExtensionUnsubscriber)UnsubscribePanelExtension);

            LoadExtensionsFromDirectory();
            SetProperty<Extension>("ExtensionLoader", (ExtensionLoader)LoadExtension);
            SetProperty<Extension>("Refresher", (ExtensionRefresher)RefreshPanelExtension);
            SetProperty<Extension>("SelectProfileIndex", (ProfileIndexSelector)SelectIndex);
            LoadProfiles();
        }

        private void LoadExtensionsFromDirectory()
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
                _ = LoadExtension(Extension, File.Name);
            }
        }

        private int LoadExtension(Assembly AssemblyToLoad, string? ModuleName)
        {
            Type[]? Types;
            if (ModuleName is null)
            {
                try
                {
                    Types = AssemblyToLoad.GetTypes();

                }
                catch (ReflectionTypeLoadException E)
                {
                    Logger.Error($"Couldn't load {AssemblyToLoad.FullName}. {E.Message}", "Program");
                    return -1;
                }
            }
            else
            {
                Module? Module = AssemblyToLoad.GetModule(ModuleName);
                if (Module is null)
                    return -2;
                try
                {
                    Types = Module.GetTypes();

                }
                catch (ReflectionTypeLoadException E)
                {
                    Logger.Error($"Couldn't load {Module.Name}. {E.Message}", "Program");
                    return -1;
                }
            }

            if (Types is null)
            {
                Logger.Error($"Couldn't load assembly {AssemblyToLoad.FullName}", "Program");
                return -3;
            }

            int ExtensionsLoaded = 0;

            foreach (Type Type in Types)
            {
                foreach (Type Interface in Type.GetInterfaces())
                {
                    if (Interface == typeof(IAbsolutePanelAction))
                    {
                        if (Type.GetCustomAttribute<PanelAbsoluteActionDescriptorAttribute>() is null)
                        {
                            Logger.Error($"Type {Type.FullName} from assembly {AssemblyToLoad.FullName} does not have PanelAbsoluteActionDescriptorAttribute.", "Program");
                            continue;
                        }
                        AbsoluteActions.Add(Type);
                        ExtensionsLoaded++;
                        Logger.Info($"Loaded Absolute Action {Type.FullName}", "Program");
                    }
                    else if (Interface == typeof(IPanelAction))
                    {
                        if (Type.GetCustomAttribute<PanelActionDescriptorAttribute>() is null)
                        {
                            Logger.Error($"Type {Type.FullName} from assembly {AssemblyToLoad.FullName} does not have PanelActionDescriptorAttribute.", "Program");
                            continue;
                        }
                        Actions.Add(Type);
                        ExtensionsLoaded++;
                        Logger.Info($"Loaded Action {Type.FullName}", "Program");
                    }
                    else if (Interface == typeof(IPanelSource))
                    {
                        if (Type.GetCustomAttribute<PanelSourceDescriptorAttribute>() is null)
                        {
                            Logger.Error($"Type {Type.FullName} from assembly {AssemblyToLoad.FullName} does not have PanelSourceDescriptorAttribute.", "Program");
                            continue;
                        }
                        Sources.Add(Type);
                        ExtensionsLoaded++;
                        Logger.Info($"Loaded Source {Type.FullName}", "Program");
                    }
                }
            }
            if (ExtensionsLoaded > 0)
                Logger.Info($"Loaded {ExtensionsLoaded} from {AssemblyToLoad.FullName}", "Program");
            return ExtensionsLoaded;
        }

        public void LoadProfile(string Json)
        {
            bool Result;
            PanelProfile LoadedProfile = new(Json, Actions.ToArray(), AbsoluteActions.ToArray(), Sources.ToArray(), out Result);
            if (Result)
                Profiles.Add(LoadedProfile);
            SortProfiles();
        }

        public void LoadProfile(byte[] Bytes)
        {
            LoadProfile(Encoding.UTF8.GetString(Bytes));
        }

        public void LoadProfiles()
        {
            if (!Directory.Exists(Settings.ProfilesDirectory))
                return;
            foreach (var Profile in new DirectoryInfo(Settings.ProfilesDirectory).GetFiles())
            {
                if (Profile.Extension != ".json")
                    continue;
                using var ProfileStream = Profile.Open(FileMode.Open);
                byte[] FileBytes = new byte[ProfileStream.Length];
                _ = ProfileStream.Read(FileBytes, 0, FileBytes.Length);
                LoadProfile(FileBytes);
            }
            if (Profiles.Count == 0)
                Profiles.Add(new PanelProfile() { Name = "New Profile" });
        }

        private void LoadSettings()
        {
            if (!File.Exists(Settings.FilePath))
            {
                Logger.Error($"Could not find settings file @ {Settings.FilePath}", "Program");
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
                Logger.Error($"Exception occured while loading settings {Ex.Message}. Loading default settings.", "Program");
            }
            if (Deserialized is not null)
                Settings = new(Deserialized);
        }

        private object? SubscribePanelExtension(Extension Extension)
        {
            if (PanelExtensions.Contains(Extension))
                return "Already subscribed.";
            PanelExtensions.Add(Extension);
            SetPanelExtensionVariables(Extension);
            return null;
        }

        private void SetPanelExtensionVariables(Extension Extension)
        {
            Extension.ApplicationVariables Variables = new();
            SetProperty<Extension.ApplicationVariables>("LastLoad", DateTime.Now, Variables);
            SetProperty<Extension.ApplicationVariables>("Logger", Logger, Variables);
            SetProperty<Extension.ApplicationVariables>("CurrentProfile", SelectedProfileIndex == -1 ? null : Profiles[SelectedProfileIndex], Variables);
            SetProperty<Extension.ApplicationVariables>("Profiles", Profiles.ToArray(), Variables);
            SetProperty<Extension.ApplicationVariables>("Actions", Actions.ToArray(), Variables);
            SetProperty<Extension.ApplicationVariables>("AbsoluteActions", AbsoluteActions.ToArray(), Variables);
            SetProperty<Extension.ApplicationVariables>("Sources", Sources.ToArray(), Variables);
            SetProperty<Extension>("Application", Variables, Extension);
        }

        private void RefreshPanelExtension(Extension? Extension)
        {
            if (Extension is null)
                PanelExtensions.ForEach(E => SetPanelExtensionVariables(E));
            else
                SetPanelExtensionVariables(Extension);
        }

        private object? UnsubscribePanelExtension(Extension Extension)
        {
            if (PanelExtensions.Contains(Extension))
                return "Not subscribed.";
            _ = PanelExtensions.Remove(Extension);
            return null;
        }

        public void StartPortCommunication(bool Emulate = false)
        {
            if (Communicating)
                return;
            Communicating = true;

            Emulating = Emulate;
            if (Emulate)
            {
                Logger.Info($"Starting emulation.", "Program");
            }
            else
            {
                Logger.Info($"Starting communications on port {Port.PortName}.", "Program");
                Port.Open();
            }
            SuspendSendThread = false;
            if (SendSourceMappingsThread.ThreadState == ThreadState.Unstarted)
                SendSourceMappingsThread.Start();
            CommunicationsStarted?.Invoke(this, new EventArgs());
            PanelExtensions.ForEach(E => InvokeMethod<Extension>("CommunicationsStartedWrapper", new object?[] { null, new EventArgs() }, E));
        }

        private void SendSourceMappings()
        {
            while (!SuspendSendThread)
            {
                if (Emulating)
                {
                    if (SelectedProfileIndex == -1 || Profiles.Count == 0)
                        continue;

                    foreach (var SourceMapping in Profiles[SelectedProfileIndex].SourceMappings)
                    {
                        if (SourceMapping.Source.GetSourceValue() is string OutString)
                        {
                            if (EmulatorDisplay is null)
                            {
                                StopPortCommunication();
                                break;
                            }
                            EmulatorDisplay(SourceMapping.ID, OutString);
                                
                            continue;
                        }
                    }
                }
                else
                {
                    if (!Port.IsOpen)
                        continue;
                    if (SelectedProfileIndex == -1 || Profiles.Count == 0)
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
                            Logger.Error($"Device on port {Port.PortName} disconnected.", "Program");
                            StopPortCommunication();
                            break;
                        }
                    }
                }
                Thread.Sleep(15);
            }
            SendSourceMappingsThread = new Thread(SendSourceMappings);
        }

        public void StopPortCommunication()
        {
            if (!Communicating)
                return;
            Communicating = false;

            SuspendSendThread = true;
            if (Emulating)
            {
                Logger.Info($"Stopping emulation.", "Program");
            }
            else
            {
                Logger.Info($"Stopping communications on port {Port.PortName}", "Program");
                Port.Close();
            }
            CommunicationsStopped?.Invoke(this, new EventArgs());
            PanelExtensions.ForEach(E => InvokeMethod<Extension>("CommunicationsStoppedWrapper", new object?[] { null, new EventArgs() }, E));
        }

        public void RouteUpdate(MessageReceiveIDs UpdateType, byte ID, object? State)
        {
            switch (UpdateType)
            {
                case MessageReceiveIDs.Reserved:
                    break;
                case MessageReceiveIDs.ButtonStateUpdate:
                    if (State is not ButtonUpdateStates UpdateState)
                        return;
                    Logger.Info($"Button {ID} was {UpdateState}", "Program");

                    if (SelectedProfileIndex == -1)
                        return;
                    if (Profiles[SelectedProfileIndex].ActionMappings.Find(Mapping => Mapping.ID == ID && Mapping.UpdateState == UpdateState) is not ActionMapping ActionMapping)
                        return;

                    Logger.Info($"Doing action {ActionMapping.Action.GetDescriptorAttribute()?.Name}.", "Program");
                    object? ActionResult = ActionMapping.Action.Do();
                    if (ActionResult is string ResultString)
                        Logger.Warn($"{ActionMapping.Action.GetDescriptorAttribute()?.Name} -> {ResultString}", "Program");
                    else if (ActionResult is Exception ActionResultException)
                        Logger.Error($"{ActionMapping.Action.GetDescriptorAttribute()?.Name} -> {ActionResultException.Message}", "Program");
                    else if (ActionResult is not null)
                        Logger.Warn($"{ActionMapping.Action.GetDescriptorAttribute()?.Name} -> {ActionResult}", "Program");
                    break;
                case MessageReceiveIDs.AbsolutePosition:
                    if (State is not double)
                        return;

                    double StateDouble = (double)State;

                    if (SelectedProfileIndex == -1)
                        return;
                    if (Profiles[SelectedProfileIndex].AbsoluteActionMappings.Find(Mapping => Mapping.ID == ID) is not AbsoluteActionMapping AbsoluteActionMapping)
                        return;

                    Logger.Info($"Setting {AbsoluteActionMapping.AbsoluteAction.GetDescriptorAttribute()?.Name} -> {StateDouble}", "Program");
                    object? AbsoluteActionResult = AbsoluteActionMapping.AbsoluteAction.Set(StateDouble);
                    if (AbsoluteActionResult is string AbsoluteResultString)
                        Logger.Warn($"{AbsoluteActionMapping.AbsoluteAction.GetDescriptorAttribute()?.Name} -> {AbsoluteResultString}", "Program");
                    else if (AbsoluteActionResult is Exception AbsoluteResultException)
                        Logger.Error($"{AbsoluteActionMapping.AbsoluteAction.GetDescriptorAttribute()?.Name} -> {AbsoluteResultException.Message}", "Program");
                    else if (AbsoluteActionResult is not null)
                        Logger.Warn($"{AbsoluteActionMapping.AbsoluteAction.GetDescriptorAttribute()?.Name} -> {AbsoluteActionResult}", "Program");
                    break;
                default:
                    break;
            }
        }

        private void PacketsCollected(object? Sender, PacketsReadyEventArgs Args)
        {
            Message ReceivedMessage = new(Args.Packets);

            switch (ReceivedMessage.MessageID.ToReceiveID())
            {
                case MessageReceiveIDs.Reserved:
                    break;
                case MessageReceiveIDs.ButtonStateUpdate:
                    if (ReceivedMessage.MessageSize < 2)
                    {
                        Logger.Error("Did not receive button state, only button ID.", "Program");
                        break;
                    }

                    byte InputID = ReceivedMessage.Data[InputIDIndex];
                    ButtonUpdateStates ButtonState = ReceivedMessage.Data[ButtonStateIndex].ToButtonUpdateState();
                    RouteUpdate(MessageReceiveIDs.ButtonStateUpdate, InputID, ButtonState);
                    break;
                case MessageReceiveIDs.AbsolutePosition:
                    break;
                default:
                    break;
            }
        }

        private void PortDataReceived(object Sender, SerialDataReceivedEventArgs Args)
        {
            if (Sender is not SerialPort Port)
                return;
            Collector.Collect(Encoding.UTF8.GetBytes(Port.ReadExisting()));
        }

        private void SaveSettings()
        {
            using var SettingsFile = new StreamWriter(Settings.FilePath);
            SettingsFile.Write(JsonSerializer.Serialize(new AppSettings.Serializable(Settings), options: new JsonSerializerOptions() { WriteIndented = true }));
        }

        public void SortProfiles()
        {
            PanelProfile? Selected = null;
            if (SelectedProfileIndex != -1)
                Selected = Profiles[SelectedProfileIndex];

            Profiles = new(Profiles.OrderBy(Profile => Profile.Name));
            Profiles.CollectionChanged += ProfilesListChanged;
            for (int i = 0; i < Profiles.Count; i++)
            {
                if (ReferenceEquals(Selected, Profiles[i]))
                {
                    SelectIndex(i);
                    break;
                }    
            }
        }

        public void RefreshProfiles()
        {
            DirectoryInfo DirectoryInfo = new DirectoryInfo(Settings.ProfilesDirectory);
            foreach (var FileInfo in DirectoryInfo.GetFiles())
            {
                if (FileInfo.Extension != ".json")
                    continue;

                byte[] FileBytes = new byte[FileInfo.Length];

                using (var ProfileStream = FileInfo.Open(FileMode.Open))
                    _ = ProfileStream.Read(FileBytes, 0, FileBytes.Length);
                PanelProfile Profile = new PanelProfile(Encoding.UTF8.GetString(FileBytes), Actions.ToArray(), AbsoluteActions.ToArray(), Sources.ToArray(), out bool Result);
                if (!Result)
                    continue;

                int FindIndex = -1;
                for (int i = 0; i < Profiles.Count; i++)
                {
                    if (Profiles[i].Name == Profile.Name)
                    {
                        FindIndex = i;
                        break;
                    }    
                }
                FileInfo.Delete();
                if (FindIndex == -1)
                    break;

                using (var ProfileFile = new StreamWriter($"{Settings.ProfilesDirectory}\\{Profile.Name}.json"))
                    ProfileFile.Write(Profile.Serialize());
            }
            SortProfiles();
        }

        public void SaveProfiles()
        {
            SortProfiles();
            foreach (var Profile in Profiles)
                using (var ProfileFile = new StreamWriter($"{Settings.ProfilesDirectory}\\{Profile.Name}.json"))
                    ProfileFile.Write(Profile.Serialize());
        }

        public void SelectIndex(int Index)
        {
            SelectedProfileIndex = Index;
        }

        private void Exiting(object Sender, EventArgs Args)
        {
            if (SendSourceMappingsThread.ThreadState != ThreadState.Unstarted)
                SendSourceMappingsThread.Join();
            PanelExtensions.ForEach(E => InvokeMethod<Extension>("ApplicationExiting", new object[] { Sender, Args }, E));
            Logger.Info("Program exiting...", "Program");
            SaveSettings();
            SaveProfiles();
            using var LogFile = new StreamWriter(Settings.LogDirectory, true);
            LogFile.Write(Logger.GetLog());
        }

        private static object? InvokeMethod<ClassType>(string MethodName, object?[]? Parameters, object? Instance, Type[]? Types = null)
        {
            Type t = typeof(ClassType);
            MethodInfo? m = Types is not null
                ? t.GetMethod(MethodName, BindingFlags.NonPublic | BindingFlags.Public | (Instance is null ? BindingFlags.Static : BindingFlags.Instance), Types)
                : t.GetMethod(MethodName, BindingFlags.NonPublic | BindingFlags.Public | (Instance is null ? BindingFlags.Static : BindingFlags.Instance));
            return m?.Invoke(Instance, Parameters);
        }

        private static void SetProperty<ClassType>(string PropertyName, object? Value, object? Instance = null)
        {
            typeof(ClassType).GetProperty(PropertyName, BindingFlags.NonPublic | BindingFlags.Public | (Instance is null ? BindingFlags.Static : BindingFlags.Instance))?.SetValue(Instance, Value);
        }
    }
}