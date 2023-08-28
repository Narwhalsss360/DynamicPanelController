using DynmaicPanelControllerCLI;
using DynmaicPanelControllerCLI.DefaultExtensions;
using Extensibility;
using NStreamCom;
using Panel;
using System.IO;
using System.IO.Ports;
using System.Reflection;
using System.Text;

const string ApplicationName = "DynamicPanelController";
const string EntryMarker = "->";

PacketCollector Collector = new PacketCollector();

SerialPort Port = new SerialPort();
Port.BaudRate = 115200;
Port.DataReceived += DataReceived;

Reenter:
Console.WriteLine("Enter extension directory:");
string? ExtensionsDirectory = Console.ReadLine();

if (string.IsNullOrEmpty(ExtensionsDirectory))
{
    Console.WriteLine("Enter a path");
    goto Reenter;
}

List<Type> ControllerActionExtensions = new List<Type>()
{
    typeof(TestAction)
};
List<Type> StringSourceExtensions = new List<Type>();

Profile? CurrentProfile = null;
List<Profile> Profiles = new List<Profile>();

Thread RWPropertiesThread = new Thread(ReadWriteExtensibleProperites);

CLIApplication Application = new CLIApplication()
{
    Name = ApplicationName,
    EntryMarker = EntryMarker,
    Commands = new List<Command>
        {
            new Command("exit", Exit, "Exits program"),
            new Command("stop", Exit, "Exits program"),
            new Command("quit", Exit, "Exits program"),
            new Command("q", Exit, "Exits program"),
            new Command("show-extensions", ShowExtensions, "Shows all loaded extensions"),
            new Command("save-profiles", SaveProfilesCommand, "Saves all profiles"),
            new Command("show-profiles", ShowProfiles, "Shows all profiles"),
            new Command("run", RunAction, "Run an action", new ArgumentDefinition[]
            {
                new ArgumentDefinition(typeof(string), true, "action-name"),
                new ArgumentDefinition(typeof(string), false, "type"),
                new ArgumentDefinition(typeof(string), false, "value")
            }),
            new Command("create-profile", CreateProfile, "Create a profile", new ArgumentDefinition[]
            {
                new ArgumentDefinition(typeof(string), true, "name")
            }),
            new Command("load-profile", LoadProfile, "Load a profile from a specified path", new ArgumentDefinition[]
            {
                new ArgumentDefinition(typeof(string), true, "path")
            }),
            new Command("select-profile", SelectProfile, "Select a profile", new ArgumentDefinition[]
            {
                new ArgumentDefinition(typeof(string), true, "name")
            }),
            new Command("set", Set, "Set a variable of the current profile", new ArgumentDefinition[]
            {
                new ArgumentDefinition(typeof(string), true, "bind-type"),
                new ArgumentDefinition(typeof(string), true, "type-name"),
                new ArgumentDefinition(typeof(byte), true, "ID"),
                new ArgumentDefinition(typeof(string), false, "button-state"),
            }),
        }
};

LoadExtensions();

System.Timers.Timer WriteTimer = new System.Timers.Timer(35);
WriteTimer.AutoReset = true;
WriteTimer.Start();

Console.WriteLine("Select Port Number");
foreach (var item in SerialPort.GetPortNames())
    Console.WriteLine(item);
Port.PortName = $"COM{Console.ReadLine()}";

bool PortClosing = false;

WriteTimer.Elapsed += WriteToDisplays;
Collector.PacketsReady += PacketsReceived;

try
{
    Port.Open();
}
catch (Exception E)
{
    if (E is FileNotFoundException)
        Cleanup("Could not connect to port");
    Environment.Exit(-1);
}

RWPropertiesThread.Start();

bool Run = true;

while (Run)
{
    Application.Get();
}

#region Functions
void ParseMessage(Message message)
{
    if (CurrentProfile is null)
        return;

    if (message.MessageID == 0)
    {
        return;
    }
    if (message.MessageSize < 1)
        return;

    byte ID = message.Data[0];
    if (message.MessageID == 1)
    {
        ShowConsoleMessage($"Button {ID} {(message.Data[1] == 0 ? "Pressed" : "Released")}");
        if (CurrentProfile.ButtonActions.ContainsKey(ID))
        {
            if ((byte)CurrentProfile.ButtonActions[ID].Item1 == message.Data[1])
                CurrentProfile.ButtonActions[ID].Item2.Do();
        }
    }
    else if (message.MessageID == 2)
    {
        if (CurrentProfile.AbsoluteActions.ContainsKey(ID))
        {    
            ShowConsoleMessage($"Potentiometer {ID} at {BitConverter.ToInt64(message.Data, 1)}");
            AbsoluteControllerAction? absoluteAction = CurrentProfile.AbsoluteActions[ID] as AbsoluteControllerAction;
            if (absoluteAction == null)
                (absoluteAction as ControllerAction)?.Do();
            else
                absoluteAction.Set(BitConverter.ToInt64(message.Data, 1));
        }
}
}

void WriteToDisplays(object? Sender, EventArgs Args)
{
    if (CurrentProfile is null)
        return;
    foreach (var DisplayStringMapping in CurrentProfile.StringMappings)
    {
        List<byte> ASCIIOut = new() { DisplayStringMapping.Key };
        ASCIIOut.AddRange(Encoding.ASCII.GetBytes(DisplayStringMapping.Value.GetString()));
        ASCIIOut.Add(0);
        try
        {
            if (PortClosing)
                return;
            Message.FastWrite(1, ASCIIOut.ToArray(), 16, Port.BaseStream);
        }
        catch (Exception)
        {
            ShowConsoleMessage("Device disconnected. Exitting.");
            Cleanup();
            Run = false;
            while (true);
        }
    }
}

void PacketsReceived(object? Sender, PacketsReadyEventArgs Args)
{
    ParseMessage(new Message(Args.Packets));
}

void DataReceived(object Sender, EventArgs Args)
{
    SerialPort? SenderPort = Sender as SerialPort;
    if (SenderPort is null)
        return;

    Collector.Collect(Encoding.ASCII.GetBytes(SenderPort.ReadExisting()));
}

void LoadExtension(Assembly ExtensionAssembly, string ModuleName)
{
    Module? module = ExtensionAssembly.GetModule(ModuleName);
    if (module is null)
        return;
    Type[]? ExtensionTypes = module?.GetTypes();
    if (ExtensionTypes is null)
        return;

    foreach (Type ExtensionType in ExtensionTypes)
    {
        foreach (Type InterfaceType in ExtensionType.GetInterfaces())
        {
            if (InterfaceType == typeof(ControllerAction))
            {
                ControllerAction? TestInstance = (ControllerAction?)Activator.CreateInstance(ExtensionType);
                if (TestInstance is null)
                {
                    Console.WriteLine($"Cannot instantiate { ExtensionType.Name } from {ExtensionAssembly.FullName}. Not loading.");
                }
                else
                {
                    Console.WriteLine($"Loading Action { TestInstance.GetActionName() }");
                    ControllerActionExtensions.Add(ExtensionType);
                }
            }
            if (InterfaceType == typeof(StringSource))
            {
                StringSource? TestInstance = (StringSource?)Activator.CreateInstance(ExtensionType);
                if (TestInstance is null)
                {
                    ShowConsoleMessage($"Cannot instantiate { ExtensionType.Name } from {ExtensionAssembly.FullName}. Not loading.");
                }
                else
                {
                    ShowConsoleMessage($"Loading Source { TestInstance.GetSourceName() }");
                    StringSourceExtensions.Add(ExtensionType);
                }
            }
        }
    }
}

void LoadExtensions()
{
    DirectoryInfo ExtensionsDirectoryInfo = new DirectoryInfo(ExtensionsDirectory);

    foreach (FileInfo File in ExtensionsDirectoryInfo.GetFiles())
    {
        if (File.Extension != ".dll")
            continue;
        Assembly? ExtensionAssembly = null;
        try
        {
            ExtensionAssembly = Assembly.LoadFrom(File.FullName);
        }
        catch (BadImageFormatException E)
        {
            Console.WriteLine($"File was wrong format.\n{ E.FileName }n{ E.Message }\n{ E.StackTrace }");
            continue;
        }
        LoadExtension(ExtensionAssembly, File.Name);
    }
}

void QuickSetProperty<TClass, TProperty>(string Name, TProperty? Value)
{
    typeof(TClass).GetProperty(Name)?.SetValue(null, Value);
}

void ReadWriteExtensibleProperites()
{
    if (LoadedExtensions.LoadRequest is not null)
    {
        LoadExtension(LoadedExtensions.LoadRequest.Item1, LoadedExtensions.LoadRequest.Item2);
        QuickSetProperty<LoadedExtensions, Tuple<Assembly, string>?>("LoadRequest", null);
    }
    QuickSetProperty<Profile, Profile>("CurrentProfile", CurrentProfile);
    QuickSetProperty<Profile, Profile[]>("Profiles", Profiles.ToArray());
    QuickSetProperty<LoadedExtensions, Type[]?>("ControllerActions", ControllerActionExtensions.ToArray());
    QuickSetProperty<LoadedExtensions, Type[]?>("StringSources", StringSourceExtensions.ToArray());
}

void SaveProfiles()
{
    string ProfilesDirectory = $"{Environment.CurrentDirectory}\\Profiles";
    if (!Directory.Exists(ProfilesDirectory))
        Directory.CreateDirectory(ProfilesDirectory);
    foreach (var Profile in Profiles)
    {
        byte[] Bytes = Profile.DeserializeJson();
        using (var F = File.Create($"{ProfilesDirectory}\\{Profile.Name}.json"))
            F.Write(Bytes);
    }
}

void Cleanup(string? Message = null)
{
    if (Message != null)
        Console.WriteLine(Message);

    Console.WriteLine("Program is exiting.");

    PortClosing = true;
    if (RWPropertiesThread.ThreadState == ThreadState.Running)
        RWPropertiesThread.Join();
    if (Port.IsOpen)
        Port.Close();

    SaveProfiles();
}

void ShowConsoleMessage(string Message)
{
    if (Console.CursorTop != 0)
        Console.WriteLine();
    Console.WriteLine(Message);
    Console.Write($"{ ApplicationName }{ EntryMarker }");
}
#endregion

#region Commands
void CreateProfile(Dictionary<object, object?> Arguments, string[]? Flags, object? Caller)
{
    CLIApplication? App = Caller as CLIApplication;
    if (App is null)
        return;

    if (Arguments is null)
        return;

    if (!Arguments.ContainsKey("name"))
    {
        App.Out.WriteLine("Must enter a name.");
        return;
    }

    string Name = (string)Arguments["name"];

    if (Profiles.Find(P => P.Name == Name) is null)
    {
        Profiles.Add(new Profile() { Name = Name });

        if (Flags is not null)
            if (Flags.Contains("--select"))
                SelectProfile(new Dictionary<object, object?> { { "name", Name } }, null, App);
    }
    else
    {
        App.Out.WriteLine("Profile with name already exists");
    }
}

void SelectProfile(Dictionary<object, object?> Arguments, string[]? Flags, object? Caller)
{
    CLIApplication? App = Caller as CLIApplication;
    if (App is null)
        return;

    if (Arguments is null)
        return;

    if (!Arguments.ContainsKey("name"))
    {
        App.Out.WriteLine("Must enter a name.");
        return;
    }

    string Name = (string)Arguments["name"];

    Profile? Found = Profiles.Find(P => P.Name == Name);
    if (Found is null)
        App.Out.WriteLine("Profile does not exist");
    else
        CurrentProfile = Found;
}

void Set(Dictionary<object, object?> Arguments, string[]? Flags, object? Caller)
{
    CLIApplication? App = Caller as CLIApplication;
    if (App is null)
        return;

    if (CurrentProfile is null)
    {
        App.Out.WriteLine("No current profile, select one.");
        return;
    }

    if (Arguments is null)
        return;

    if (!Arguments.ContainsKey("bind-type"))
    {
        App.Out.WriteLine("Must specify a bind-type");
        return;
    }

    if (!Arguments.ContainsKey("type-name"))
    {
        App.Out.WriteLine("Must specify a type-name");
        return;
    }

    if (!Arguments.ContainsKey("ID"))
    {
        App.Out.WriteLine("Must specify an ID");
        return;
    }

    string BindType = (string)Arguments["bind-type"];
    string TypeName = (string)Arguments["type-name"];
    byte ID = (byte)Arguments["ID"];

    Type? Type = null;
    if (BindType.ToLower() == "source")
    {
        Type = StringSourceExtensions.Find(E => E.FullName == TypeName);
        if (Type == null)
        {
            App.Out.WriteLine("Could not find source.");
            return;
        }
        CurrentProfile.StringMappings.Add(ID, (StringSource)Activator.CreateInstance(Type));
    }
    else if (BindType.ToLower() == "button" || BindType.ToLower() == "absolute")
    {
        Type = ControllerActionExtensions.Find(E => E.FullName == TypeName);
        if (Type == null)
        {
            App.Out.WriteLine("Could not find action.");
            return;
        }

        if (BindType.ToLower() == "absolute")
        {
            if (Type.GetInterface(typeof(AbsoluteControllerAction).Name) is null)
            {
                App.Out.WriteLine($"{ TypeName } is not an absolute action.");
                return;
            }

            CurrentProfile.AbsoluteActions.Add(ID, (AbsoluteControllerAction)Activator.CreateInstance(Type));
        }
        else
        {
            if (!Arguments.ContainsKey("button-state"))
            {
                App.Out.WriteLine("Must specify a button-state");
                return;
            }

            string ButtonState = (string)Arguments["button-state"];
            if (ButtonState.ToLower() == "pushed")
            {
                CurrentProfile.ButtonActions.Add(ID, new Tuple<ButtonUpdate, ControllerAction>(ButtonUpdate.Pushed, (ControllerAction)Activator.CreateInstance(Type)));
            }
            else if (ButtonState.ToLower() == "released")
            {

            }
            else
            {
                App.Out.WriteLine("Unkown button state.");
            }
        }
    }
    else
    {
        App.Out.WriteLine("Unkown bind-type");
    }
}

void LoadProfile(Dictionary<object, object?> Arguments, string[]? Flags, object? Caller)
{
    CLIApplication? App = Caller as CLIApplication;
    if (App is null)
        return;

    if (Arguments.Count  == 0)
    {
        App.Out.WriteLine("Must specify a path");
        return;
    }

    string Path = (string)Arguments["path"];

    if (!File.Exists(Path))
    {
        App.Out.WriteLine("File does not exist.");
        return;
    }
    Profiles.Add(new Profile(File.Open(Path, FileMode.Open)));
}

void ShowProfiles(Dictionary<object, object?> Arguments, string[]? Flags, object? Caller)
{
    CLIApplication? App = Caller as CLIApplication;
    if (App is null)
        return;

    if (Profiles.Count == 0)
    {
        App.Out.WriteLine("None");
        return;
    }

    foreach (Profile P in Profiles)
    {
        Console.Write($"    {P.Name}");
        Console.WriteLine($"{ (P.Name == CurrentProfile?.Name ? "<-" : "") }");
    }
}

void SaveProfilesCommand(Dictionary<object, object?> Arguments, string[]? Flags, object? Caller)
{
    CLIApplication? App = Caller as CLIApplication;
    if (App is null)
        return;

    SaveProfiles();
}

void RunAction(Dictionary<object, object?> Arguments, string[]? Flags, object? Caller)
{
    CLIApplication? App = Caller as CLIApplication;
    if (App is null)
        return;

    if (Arguments is null)
        return;

    if (!Arguments.ContainsKey("action-name"))
    {
        App.Out.WriteLine("Must specify an action.");
        return;
    }

    string ActionName = (string)Arguments["action-name"];

    Type? ActionType = ControllerActionExtensions.Find(Action => Action.FullName == ActionName);
    if (ActionType is null)
    {
        App.Out.WriteLine("Couldn't find action.");
        return;
    }

    ControllerAction? Action = (ControllerAction?)Activator.CreateInstance(ActionType);

    if (ActionType.GetInterface(typeof(AbsoluteControllerAction).Name) is not null)
    {
        if (Arguments.Count() > 2)
        {
            if (!Arguments.ContainsKey("type"))
                goto NotEnoughArguments;
            if (!Arguments.ContainsKey("value"))
                goto NotEnoughArguments;

            string Type = (string)Arguments["type"];

            object? Value = null;
            if (Type == "int")
            {
                int outInt;
                if (!int.TryParse((string)Arguments["value"], out outInt))
                    goto CouldntParse;
                Value = outInt;
            }
            else if (Type == "float")
            {
                float outFloat;
                if (!float.TryParse((string)Arguments["value"], out outFloat))
                    goto CouldntParse;
                Value = outFloat;
            }
            else if (Type == "string")
            {
                Value = (string)Arguments["value"];
            }
            else if (Type == "null")
            {
            }
            else
            {
                goto CouldntParse;
            }

            AbsoluteControllerAction? AbsoluteAction = Action as AbsoluteControllerAction;
            if (AbsoluteAction is null)
            {
                App.Out.WriteLine("Action is not absolute.");
                return;
            }

            AbsoluteAction.Set(Value);

            return;
            CouldntParse:
            App.Out.WriteLine("Could not parse argument.");
        }
        NotEnoughArguments:
        App.Out.WriteLine("Must enter type of argument.");
    }
    else
    {
        Action.Do();
    }
}

void ShowExtensions(Dictionary<object, object?> Arguments, string[]? Flags, object? Caller)
{
    CLIApplication? App = Caller as CLIApplication;
    if (App is null)
        return;

    App.Out.WriteLine("Sources:");
    foreach (var StringSourceExtension in StringSourceExtensions)
        Console.WriteLine($"   {StringSourceExtension.FullName}: {((StringSource?)Activator.CreateInstance(StringSourceExtension))?.GetSourceName()}");
    App.Out.WriteLine("Actions:");
    foreach (var ControllerActionExtension in ControllerActionExtensions)
        Console.WriteLine($"   {ControllerActionExtension.FullName}: {((ControllerAction?)Activator.CreateInstance(ControllerActionExtension))?.GetActionName()}{ (ControllerActionExtension.GetInterface(typeof(AbsoluteControllerAction).Name) == null ? "" : " | Absolute") }");
}

void Exit(Dictionary<object, object?> Arguments, string[]? Flags, object? Caller)
{
    Cleanup();
    Run = false;
}
#endregion