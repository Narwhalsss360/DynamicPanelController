using Profiling;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using PanelExtension;
using Profiling.ProfilingTypes;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Text;
using NStreamCom;
using System.Text.Json;
using Panel.Communication;
using System.ComponentModel;
using System.Linq;

namespace DynamicPanelController
{
    public partial class App : Application, ILogger
    {
        public static readonly ushort PacketSize = 16;

        public List<PanelProfile> Profiles = new();
        public int SelectedProfileIndex = -1;
        public List<Type> Actions = new();
        public List<Type> Sources = new();

        public SerialPort Port { get; private set; } = new SerialPort() { BaudRate = 115200 };
        Thread SendSourceMappingsThread;
        bool SuspendSendThread = false;
        PacketCollector Collector = new();
        public static readonly int InputIDIndex = 0;
        public static readonly int ButtonStateIndex = 1;
        public bool Communicating { get; private set; } = false;

        public struct AppSettings
        {
            public readonly string FilePath = $"";
            public string ExtensionsDirectory { get; set; } = $"{ Environment.CurrentDirectory }\\Extensions";
            public string ProfilesDirectory { get; set; } = $"{ Environment.CurrentDirectory }\\Profiles";
            public string LogPath {   get; set; } = $"{Environment.CurrentDirectory}\\Log.txt";

            public AppSettings()
            {
            }
        }

        AppSettings Settings = new();

        public string CurrentLog { get; private set; } = string.Empty;

        public event EventHandler? LogChangedHandlers;
        public event EventHandler? CommunicationsStarted;
        public event EventHandler? CommunicationsStopped;

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
            LoadSettings();
            SetProperty<Extension>("ExtensionLoader", (ExtensionLoader)LoadExtension);
            SetProperty<Extension>("Refresher", (Refresher)RefreshPanelExtensionProperties);
            LoadExtensionsFromDirectory();
            LoadProfiles();
            Info("Program starting");
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

        public int LoadExtension(Assembly AssemblyToLoad, string? ModuleName)
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
                Error($"Couldn't load assembly { AssemblyToLoad.FullName }");
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
                            Error($"Type { Type.FullName } from assembly { AssemblyToLoad.FullName } does not have PanelActionDescriptorAttribute.");
                            continue;
                        }
                        Actions.Add(Type);
                        ExtensionsLoaded++;
                        Info($"Loaded { Type.FullName }");
                    }
                    else if (Interface == typeof(IPanelSource))
                    {
                        if (Type.GetCustomAttribute<PanelSourceDescriptorAttribute>() is null)
                        {
                            Error($"Type { Type.FullName } from assembly { AssemblyToLoad.FullName } does not have PanelSourceDescriptorAttribute.");
                            continue;
                        }
                        Sources.Add(Type);
                        ExtensionsLoaded++;
                        Info($"Loaded { Type.FullName }");
                    }
                }
            }
            Info($"Loaded { ExtensionsLoaded } from { AssemblyToLoad.FullName }");
            return ExtensionsLoaded;
        }

        public void LoadProfile(string Json) => Profiles.Add(new PanelProfile(Json, Actions.ToArray(), Sources.ToArray()));

        public void LoadProfile(byte[] Bytes) => LoadProfile(Encoding.UTF8.GetString(Bytes));

        public void LoadProfiles()
        {
            if (!Directory.Exists(Settings.ProfilesDirectory))
                return;
            foreach (var Profile in new DirectoryInfo(Settings.ProfilesDirectory).GetFiles())
            {
                if (Profile.Extension != ".json")
                    continue;
                using (var ProfileStream = Profile.Open(FileMode.Open))
                {
                    byte[] FileBytes = new byte[ProfileStream.Length];
                    ProfileStream.Read(FileBytes, 0, FileBytes.Length);
                    LoadProfile(FileBytes);
                }
            }
        }

        public void LoadSettings()
        {
            if (!File.Exists(Settings.FilePath))
                return;

            using (var SettingsFile = new StreamReader(Settings.FilePath))
            {
                AppSettings? Deserialized = JsonSerializer.Deserialize<AppSettings>(SettingsFile.ReadToEnd());
                if (Deserialized is not null)
                    Settings = (AppSettings)Deserialized;
            }
        }

        public void RefreshPanelExtensionProperties()
        {
            SetProperty<Extension>("PanelActions", Actions.ToArray());
            SetProperty<Extension>("PanelSources", Sources.ToArray());
            SetProperty<Extension>("Profiles", Profiles.ToArray());
            SetProperty<Extension>("CurrentProfile", SelectedProfileIndex == -1 ? null : Profiles[SelectedProfileIndex]);
            SetProperty<Extension>("Logger", this);
        }

        public void StartPortCommunication()
        {
            Info($"Starting communications on port { Port.PortName }");
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
                    List<byte> Bytes = new() { SourceMapping.Key };
                    if (SourceMapping.Value.GetSourceValue() is not string OutString)
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
                        Error($"Device on port { Port.PortName } disconnected.");
                        StopPortCommunication();
                    }
                }
            }
        }

        public void StopPortCommunication()
        {
            Info($"Stopping communications on port {Port.PortName}");
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
                        Error("Did not receive button state, only button ID.");
                        break;
                    }

                    byte InputID = ReceivedMessage.Data[InputIDIndex];
                    ButtonUpdateStates ButtonState = ReceivedMessage.Data[ButtonStateIndex].ToButtonUpdateState();

                    Info($"Button { InputID } has been { ButtonState }");

                    if (SelectedProfileIndex == -1)
                        break;

                    PanelProfile SelectedProfile = Profiles[SelectedProfileIndex];
                    if (!SelectedProfile.ActionMappings.ContainsKey(InputID))
                        break;
                    var MappingTuple = SelectedProfile.ActionMappings[InputID];
                    if (MappingTuple is not null)
                    {
                        if (MappingTuple.Item1 == ButtonState)
                        {
                            Info($"Doing action { MappingTuple.Item2.GetDescriptorAttribute()?.Name }");
                            MappingTuple.Item2.Do();
                        }
                    }

                    break;
                case MessageReceiveIDs.AbsolutePosition:
                    break;
                default:
                    break;
            }
        }

        void PortDataReceived(object Sender, SerialDataReceivedEventArgs Args)
        {
            SerialPort? Port = Sender as SerialPort;
            if (Port is null)
                return;
            Collector.Collect(Encoding.UTF8.GetBytes(Port.ReadExisting()));
        }

        public void SaveSettings()
        {
            using (var SettingsFile = new StreamWriter($"{ Environment.CurrentDirectory }\\Settings.json"))
                SettingsFile.Write(JsonSerializer.Serialize(Settings, options: new JsonSerializerOptions() { WriteIndented = true }));
        }

        public void SaveProfiles()
        {
            foreach (var Profile in Profiles)
                using (var ProfileFile = new StreamWriter(Settings.FilePath))
                    ProfileFile.Write(Profile.Serialize());
        }

        void Exiting(object Sender, EventArgs Args)
        {
            if (SendSourceMappingsThread.ThreadState != ThreadState.Unstarted)
                SendSourceMappingsThread.Join();
            List<EventHandler>? Handlers = GetProperty<Extension, List<EventHandler>>("ExitingHandlers");
            Handlers?.ForEach(Handler => Handler.Invoke(this, new EventArgs()));
            Info("Program exiting");
            using (var LogStream = new StreamWriter(Settings.LogPath, true))
                LogStream.Write(CurrentLog);
        }

        static void SetProperty<ClassType>(string PropertyName, object? Value, object? Instance = null) => typeof(ClassType).GetProperty(PropertyName)?.SetValue(Instance, Value);

        static PropertyType? GetProperty<ClassType, PropertyType>(string PropertyName, object? Instance = null) => (PropertyType?)(typeof(ClassType).GetProperty(PropertyName)?.GetValue(Instance));
    
        public void Info(string Message)
        {
            if (CurrentLog.Length > 0)
                if (!(CurrentLog.Last() == '\r' || CurrentLog.Last() == '\n'))
                    CurrentLog += '\n';
            CurrentLog += $"{ DateTime.Now } Info:{ Message }";
            LogChangedHandlers?.Invoke(this, new EventArgs());
        }

        public void Warn(string Message)
        {
            if (CurrentLog.Length > 0)
                if (!(CurrentLog.Last() == '\r' || CurrentLog.Last() == '\n'))
                    CurrentLog += '\n';
            CurrentLog += $"{ DateTime.Now } Warning:{ Message }";
            LogChangedHandlers?.Invoke(this, new EventArgs());
        }

        public void Error(string Message)
        {
            if (CurrentLog.Length > 0)
                if (!(CurrentLog.Last() == '\r' || CurrentLog.Last() == '\n'))
                    CurrentLog += '\n';
            CurrentLog += $"{ DateTime.Now } Error:{ Message }";
            LogChangedHandlers?.Invoke(this, new EventArgs());
        }

        public void OnLogChange(EventHandler Handler) => LogChangedHandlers += Handler;   

        public void RemoveOnLogChange(EventHandler Handler) => LogChangedHandlers -= Handler;

        public string GetLog() => CurrentLog;
    }
}