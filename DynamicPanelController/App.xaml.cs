using NStreamCom;
using Panel;
using Panel.Communication;
using PanelExtension;
using Profiling;
using Profiling.ProfilingTypes.Mappings;
using Profiling.ProfilingTypes.PanelItems;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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
    public delegate void EmulatorDisplayOut(byte ID, string Data);

    public partial class App : Application
    {
        public ObservableCollection<PanelProfile> Profiles = new();
        public int SelectedProfileIndexContainer;

        public int SelectedProfileIndex
        {
            get => SelectedProfileIndexContainer;
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
        private static readonly int SendSourceThreadMessageInterval = 1000 / 25;
        public bool Communicating { get; private set; } = false;
        public bool AllowEmulator
        {
            get
            {
                if (Settings.GlobalOptions.ContainsKey("EmulatorEnabled"))
                {
                    return Settings.GlobalOptions["EmulatorEnabled"].ToLower() == "true";
                }
                else
                {
                    return false;
                }
            }
        }
        private bool Emulating = false;
        public EmulatorDisplayOut? EmulatorDisplay = null;

        public struct AppSettings
        {
            public string FilePath = $"{Environment.CurrentDirectory}\\Settings.json";
            public string ExtensionsDirectory { get; set; } = $"{Environment.CurrentDirectory}\\Extensions";
            public string ProfilesDirectory { get; set; } = $"{Environment.CurrentDirectory}\\Profiles";
            public string LogDirectory { get; set; } = $"{Environment.CurrentDirectory}\\Log.txt";
            public PanelDescriptor? GlobalPanelDescriptor = null;
            public Dictionary<string, string> GlobalOptions = new();
            public Dictionary<string, string> GlobalSettingsValidOptions = new();
            public ILogger.Levels LogLevel = ILogger.Levels.Verbose;

            public AppSettings()
            {
            }

            public AppSettings(Serializable Serialized)
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
                public Dictionary<string, string>? GlobalSettingsValidOptions { set; get; } = new();
                public ILogger.Levels LogLevel { set; get; } = ILogger.Levels.Verbose;

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
                    GlobalSettings = Settings.GlobalOptions;
                    GlobalSettingsValidOptions = Settings.GlobalSettingsValidOptions;
                    LogLevel = Settings.LogLevel;
                }
            }
        }

        public AppSettings Settings = new();

        private class ApplicationLogger : ILogger
        {
            public string CurrentLog { get; private set; } = string.Empty;
            public event LogChangedEventHanlder? LogChanged;

            public string FormatMessage(ILogger.Levels? Level, object? Sender, string Message)
            {
                return $"{DateTime.Now:HH:mm} {(Sender is null ? "" : $"[{Sender}]")}{(Level is null ? "" : $"[{Level}]")}: {Message}";
            }

            public void Verbose(string Message, object? Sender = null)
            {
                if (CurrentLog.Length > 0)
                    if (CurrentLog.Last() is not ('\r' or '\n'))
                        CurrentLog += '\n';
                string? Appendage = FormatMessage(ILogger.Levels.Verbose, Sender, Message);
                CurrentLog += Appendage;
                LogChanged?.Invoke(this, new LogChangedEventArgs(Appendage));
            }

            public void Info(string Message, object? Sender = null)
            {
                if (CurrentLog.Length > 0)
                    if (CurrentLog.Last() is not ('\r' or '\n'))
                        CurrentLog += '\n';
                string Appendage = FormatMessage(ILogger.Levels.Info, Sender, Message);
                CurrentLog += Appendage;
                LogChanged?.Invoke(this, new LogChangedEventArgs(Appendage));
            }

            public void Warn(string Message, object? Sender = null)
            {
                if (CurrentLog.Length > 0)
                    if (CurrentLog.Last() is not ('\r' or '\n'))
                        CurrentLog += '\n';
                string Appendage = FormatMessage(ILogger.Levels.Warning, Sender, Message);
                CurrentLog += Appendage;
                LogChanged?.Invoke(this, new LogChangedEventArgs(Appendage));
            }

            public void Error(string Message, object? Sender = null)
            {
                if (CurrentLog.Length > 0)
                    if (CurrentLog.Last() is not ('\r' or '\n'))
                        CurrentLog += '\n';
                string Appendage = FormatMessage(ILogger.Levels.Error, Sender, Message);
                CurrentLog += Appendage;
                LogChanged?.Invoke(this, new LogChangedEventArgs(Appendage));
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
            Logger.LogChanged += LogChanged;
            SendSourceMappingsThread = new Thread(SendSourceMappings);
        }

        private void ProfilesListChanged(object? Sender, NotifyCollectionChangedEventArgs Args)
        {
            if (Sender is not ObservableCollection<PanelProfile> Collection)
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

                        for (int iName = 1; Collection.Any(Profile => !ReferenceEquals(Profile, NewProfile) && Profile.Name == NewProfile.Name); iName++)
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

                    for (int iSearch = Args.NewItems.Count - 1; iSearch >= 0; iSearch--)
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

                        for (int iName = 1; Collection.Any(Profile => !ReferenceEquals(Profile, NewProfile) && Profile.Name == NewProfile.Name); iName++)
                            NewProfile.Name = $"{Collection[iItems].Name}({iName})";
                    }
                }
            }

            PanelExtensions.ForEach(E => InvokeMethod<Extension>("ProfilesChangedWrapper", new object?[] { null, Args }, E));
        }

        private void ApplicationStarting(object Sender, EventArgs Args)
        {
            Logger.Log(ILogger.Levels.Verbose, "Program starting", "Program");
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

        private string? TestInstantiation(Type Type)
        {
            try
            {
                _ = Activator.CreateInstance(Type);
            }
            catch (Exception E)
            {
                return E.Message;
            }
            return null;
        }

        private int LoadExtension(Assembly AssemblyToLoad, string? ModuleName)
        {
            if (!AssemblyToLoad.CustomAttributes.Any(Attr => Attr.AttributeType == typeof(PanelAssemblyAttribute)))
            {
                Logger.Log(ILogger.Levels.Verbose, $"{AssemblyToLoad.FullName} did not contain required {typeof(PanelAssemblyAttribute)}.", "Program");
                return -3;
            }

            Type[]? Types;
            if (ModuleName is null)
            {
                try
                {
                    Types = AssemblyToLoad.GetTypes();

                }
                catch (ReflectionTypeLoadException E)
                {
                    Logger.Log(ILogger.Levels.Error, $"Couldn't load {AssemblyToLoad.FullName}. {E.Message}", "Program");
                    return -1;
                }
            }
            else
            {
                Module? Module = AssemblyToLoad.GetModule(ModuleName);
                if (Module is null)
                {
                    Logger.Log(ILogger.Levels.Error, $"Could not get module {ModuleName} from {AssemblyToLoad.FullName}.", "Program");
                    return -2;
                }
                try
                {
                    Types = Module.GetTypes();

                }
                catch (ReflectionTypeLoadException E)
                {
                    Logger.Log(ILogger.Levels.Error, $"Couldn't load {Module.Name}. {E.Message}", "Program");
                    return -1;
                }
            }

            if (Types is null)
            {
                Logger.Log(ILogger.Levels.Error, $"Couldn't load assembly {AssemblyToLoad.FullName}", "Program");
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
                            Logger.Log(ILogger.Levels.Error, $"Type {Type.FullName} from assembly {AssemblyToLoad.FullName} does not have PanelAbsoluteActionDescriptorAttribute.", "Program");
                            continue;
                        }
                        if (TestInstantiation(Type) is string ExceptionMessage)
                        {
                            Logger.Log(ILogger.Levels.Error, $"Cannot instantiate extension {Type.Name}.\n{ExceptionMessage}", "Program");
                            return -4;
                        }

                        AbsoluteActions.Add(Type);
                        ExtensionsLoaded++;
                        Logger.Log(ILogger.Levels.Verbose, $"Loaded Absolute Action {Type.FullName}", "Program");
                    }
                    else if (Interface == typeof(IPanelAction))
                    {
                        if (Type.GetCustomAttribute<PanelActionDescriptorAttribute>() is null)
                        {
                            Logger.Log(ILogger.Levels.Error, $"Type {Type.FullName} from assembly {AssemblyToLoad.FullName} does not have PanelActionDescriptorAttribute.", "Program");
                            continue;
                        }
                        if (TestInstantiation(Type) is string ExceptionMessage)
                        {
                            Logger.Log(ILogger.Levels.Error, $"Cannot instantiate extension {Type.Name}.\n{ExceptionMessage}", "Program");
                            return -4;
                        }

                        Actions.Add(Type);
                        ExtensionsLoaded++;
                        Logger.Log(ILogger.Levels.Verbose, $"Loaded Action {Type.FullName}", "Program");
                    }
                    else if (Interface == typeof(IPanelSource))
                    {
                        if (Type.GetCustomAttribute<PanelSourceDescriptorAttribute>() is null)
                        {
                            Logger.Log(ILogger.Levels.Error, $"Type {Type.FullName} from assembly {AssemblyToLoad.FullName} does not have PanelSourceDescriptorAttribute.", "Program");
                            continue;
                        }
                        if (TestInstantiation(Type) is string ExceptionMessage)
                        {
                            Logger.Log(ILogger.Levels.Error, $"Cannot instantiate extension {Type.Name}.\n{ExceptionMessage}", "Program");
                            return -4;
                        }

                        Sources.Add(Type);
                        ExtensionsLoaded++;
                        Logger.Log(ILogger.Levels.Verbose, $"Loaded Source {Type.FullName}", "Program");
                    }
                }
            }
            if (ExtensionsLoaded > 0)
                Logger.Log(ILogger.Levels.Info, $"Loaded {ExtensionsLoaded} from {AssemblyToLoad.FullName}", "Program");
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
                goto SkipLoad;
            foreach (var Profile in new DirectoryInfo(Settings.ProfilesDirectory).GetFiles())
            {
                if (Profile.Extension != ".json")
                    continue;
                using var ProfileStream = Profile.Open(FileMode.Open);
                byte[] FileBytes = new byte[ProfileStream.Length];
                _ = ProfileStream.Read(FileBytes, 0, FileBytes.Length);
                LoadProfile(FileBytes);
            }
        SkipLoad:
            if (Profiles.Count == 0)
                Profiles.Add(new PanelProfile() { Name = "New Profile" });
        }

        private void LoadSettings()
        {
            if (!File.Exists(Settings.FilePath))
            {
                Logger.Log(ILogger.Levels.Error, $"Could not find settings file @ {Settings.FilePath}", "Program");
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
                Logger.Log(ILogger.Levels.Error, $"Exception occured while loading settings {Ex.Message}. Loading default settings.", "Program");
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
            SetProperty<Extension.ApplicationVariables>("CurrentProfileIndex", SelectedProfileIndex, Variables);
            SetProperty<Extension.ApplicationVariables>("GlobalOptions", Settings.GlobalOptions, Variables);
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
                Logger.Log(ILogger.Levels.Info, $"Starting emulation.", "Program");
            }
            else
            {
                Logger.Log(ILogger.Levels.Info, $"Starting communications on port {Port.PortName}.", "Program");
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
                        object? Value = SourceMapping.Source.GetSourceValue();
                        if (Value is string OutString)
                        {
                            if (EmulatorDisplay is null)
                            {
                                StopPortCommunication();
                                break;
                            }
                            EmulatorDisplay(SourceMapping.ID, OutString);

                            continue;
                        }
                        else if (Value is Exception Error)
                        {
                            Logger.Log(ILogger.Levels.Error, $"{Error.Message}", $"Program | {SourceMapping.Source.GetPanelItemDescriptor()?.Name}");
                        }
                    }
                }
                else
                {
                    if (!Port.IsOpen)
                        continue;
                    if (SelectedProfileIndex == -1 || Profiles.Count == 0)
                        continue;

                    try
                    {
                        foreach (var SourceMapping in Profiles[SelectedProfileIndex].SourceMappings)
                        {
                            object? Value = SourceMapping.Source.GetSourceValue();

                            if (Value is string OutString)
                            {
                                Thread.Sleep(SendSourceThreadMessageInterval);
                                List<byte> Bytes = new() { SourceMapping.ID };
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
                                    Logger.Log(ILogger.Levels.Error, $"Device on port {Port.PortName} disconnected.", "Program");
                                    StopPortCommunication();
                                    break;
                                }
                            }
                            else if (Value is Exception Error)
                            {
                                Logger.Log(ILogger.Levels.Error, $"{Error.Message}", $"Program | {SourceMapping.Source.GetPanelItemDescriptor()?.Name}");
                            }
                        }
                    }
                    catch (InvalidOperationException)
                    {
                        continue;
                    }
                }
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
                Logger.Log(ILogger.Levels.Info, $"Stopping emulation.", "Program");
            }
            else
            {
                Logger.Log(ILogger.Levels.Info, $"Stopping communications on port {Port.PortName}", "Program");
                if (!Port.IsOpen)
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
                    Logger.Log(ILogger.Levels.Verbose, $"Button {ID} was {UpdateState}", "Program");

                    if (SelectedProfileIndex == -1)
                        return;
                    if (Profiles[SelectedProfileIndex].ActionMappings.Find(Mapping => Mapping.ID == ID && Mapping.UpdateState == UpdateState) is not ActionMapping ActionMapping)
                        return;

                    Logger.Log(ILogger.Levels.Verbose, $"Doing action {ActionMapping.Action.GetDescriptorAttribute()?.Name}.", "Program");
                    object? ActionResult = ActionMapping.Action.Do();
                    if (ActionResult is string ResultString)
                        Logger.Log(ILogger.Levels.Warning, $"{ActionMapping.Action.GetDescriptorAttribute()?.Name} Do -> {ResultString}", "Program");
                    else if (ActionResult is Exception ActionResultException)
                        Logger.Log(ILogger.Levels.Error, $"{ActionMapping.Action.GetDescriptorAttribute()?.Name} Do -> {ActionResultException.Message}", "Program");
                    else if (ActionResult is not null)
                        Logger.Log(ILogger.Levels.Warning, $"{ActionMapping.Action.GetDescriptorAttribute()?.Name} Do -> {ActionResult}", "Program");
                    break;
                case MessageReceiveIDs.AbsolutePosition:
                    float Value;
                    if (State is float or byte or ushort or uint or double)
                    {
                        Value = (float)State;
                    }
                    else
                    {
                        Logger.Log(ILogger.Levels.Error, $"State was not a valid type from ID {ID}.", "Program");
                        return;
                    }

                    if (Value is < 0 or > 100)
                    {
                        Logger.Log(ILogger.Levels.Error, $"Value {Value} was not in valid range from ID {ID}.", "Program");
                        return;
                    }

                    if (SelectedProfileIndex == -1)
                        return;
                    if (Profiles[SelectedProfileIndex].AbsoluteActionMappings.Find(Mapping => Mapping.ID == ID) is not AbsoluteActionMapping AbsoluteActionMapping)
                        return;

                    Logger.Log(ILogger.Levels.Verbose, $"Setting {AbsoluteActionMapping.AbsoluteAction.GetDescriptorAttribute()?.Name} -> {Value}", "Program");
                    object? AbsoluteActionResult = AbsoluteActionMapping.AbsoluteAction.Set(Value);
                    if (AbsoluteActionResult is string AbsoluteResultString)
                        Logger.Log(ILogger.Levels.Warning, $"{AbsoluteActionMapping.AbsoluteAction.GetDescriptorAttribute()?.Name} Set -> {AbsoluteResultString}", "Program");
                    else if (AbsoluteActionResult is Exception AbsoluteResultException)
                        Logger.Log(ILogger.Levels.Error, $"{AbsoluteActionMapping.AbsoluteAction.GetDescriptorAttribute()?.Name} Set -> {AbsoluteResultException.Message}", "Program");
                    else if (AbsoluteActionResult is not null)
                        Logger.Log(ILogger.Levels.Warning, $"{AbsoluteActionMapping.AbsoluteAction.GetDescriptorAttribute()?.Name} Set -> {AbsoluteActionResult}", "Program");
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
                        Logger.Log(ILogger.Levels.Error, "Did not receive a full message.", "Program");
                        break;
                    }

                    byte ButtonInputID = ReceivedMessage.Data[InputIDIndex];
                    ButtonUpdateStates ButtonState = ReceivedMessage.Data[ButtonStateIndex].ToButtonUpdateState();
                    RouteUpdate(MessageReceiveIDs.ButtonStateUpdate, ButtonInputID, ButtonState);
                    break;
                case MessageReceiveIDs.AbsolutePosition:
                    if (ReceivedMessage.MessageSize < 2)
                    {
                        Logger.Log(ILogger.Levels.Error, "Did not receive a full message.", "Program");
                        return;
                    }

                    byte AbsoluteInputID = ReceivedMessage.Data[InputIDIndex];
                    int SizeOfData = ReceivedMessage.Data.Length - 1;

                    switch (SizeOfData)
                    {
                        case 1:
                            RouteUpdate(MessageReceiveIDs.AbsolutePosition, AbsoluteInputID, ReceivedMessage.Data[1]);
                            break;
                        case 2:
                            RouteUpdate(MessageReceiveIDs.AbsolutePosition, AbsoluteInputID, BitConverter.ToUInt16(ReceivedMessage.Data, 1));
                            break;
                        case 4:
                            if (BitConverter.ToUInt32(ReceivedMessage.Data, 1) <= 100)
                                RouteUpdate(MessageReceiveIDs.AbsolutePosition, AbsoluteInputID, BitConverter.ToUInt32(ReceivedMessage.Data, 1));
                            else
                                RouteUpdate(MessageReceiveIDs.AbsolutePosition, AbsoluteInputID, BitConverter.ToSingle(ReceivedMessage.Data, 1));
                            break;
                        case 8:
                            RouteUpdate(MessageReceiveIDs.AbsolutePosition, AbsoluteInputID, BitConverter.ToDouble(ReceivedMessage.Data, 1));
                            break;
                        default:
                            Logger.Log(ILogger.Levels.Error, $"Received Unkown Data from ID {ReceivedMessage.MessageID}", "Program");
                            return;
                    }
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

        private void LogChanged(object? Sender, LogChangedEventArgs Args)
        {
            if (Args.AppendedText is null)
                return;
            using var LogFile = new StreamWriter(Settings.LogDirectory, true);
            LogFile.Write($"${(Args.AppendedText[0] is '\n' or '\r' ? "" : "\n")}{Args.AppendedText}");
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
            DirectoryInfo DirectoryInfo = new(Settings.ProfilesDirectory);
            if (!DirectoryInfo.Exists)
            {
                Settings.ProfilesDirectory = new AppSettings().ProfilesDirectory;
                DirectoryInfo = new(Settings.ProfilesDirectory);
                if (!DirectoryInfo.Exists)
                    Directory.CreateDirectory(Settings.ProfilesDirectory);
            }

            foreach (var FileInfo in DirectoryInfo.GetFiles())
            {
                if (FileInfo.Extension != ".json")
                    continue;

                byte[] FileBytes = new byte[FileInfo.Length];

                using (var ProfileStream = FileInfo.Open(FileMode.Open))
                    _ = ProfileStream.Read(FileBytes, 0, FileBytes.Length);
                PanelProfile Profile = new(Encoding.UTF8.GetString(FileBytes), Actions.ToArray(), AbsoluteActions.ToArray(), Sources.ToArray(), out bool Result);
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

                using var ProfileFile = new StreamWriter($"{Settings.ProfilesDirectory}\\{Profile.Name}.json");
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
            SuspendSendThread = true;
            if (SendSourceMappingsThread.ThreadState != ThreadState.Unstarted)
                SendSourceMappingsThread.Join();
            PanelExtensions.ForEach(E => InvokeMethod<Extension>("ApplicationExiting", new object[] { Sender, Args }, E));
            Logger.Log(ILogger.Levels.Verbose, "Program exiting...", "Program");
            SaveSettings();
            SaveProfiles();
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