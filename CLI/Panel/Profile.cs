using Extensibility;
using Panel;
using System.Text.Json;

namespace DynmaicPanelControllerCLI
{
    public class Profile
    {
        public static Profile? CurrentProfile { get; private set; } = null;
        public static Profile[]? Profiles { get; private set; } = null;

        public string Name { get; set; } = string.Empty;
        public PanelDescriptor Panel { get; set; } = new PanelDescriptor();
        public Dictionary<byte, Tuple<ButtonUpdate, ControllerAction>> ButtonActions { get; set; } = new Dictionary<byte, Tuple<ButtonUpdate, ControllerAction>>();
        public Dictionary<byte, AbsoluteControllerAction> AbsoluteActions { get; set; } = new Dictionary<byte, AbsoluteControllerAction>();
        public Dictionary<byte, StringSource> StringMappings { get; set; } = new Dictionary<byte, StringSource>();

        class Serializable
        {
            public string? Name { get; set; } = null;
            public Dictionary<byte, string[]>? ButtonActions { get; set; } = null;
            public Dictionary<byte, string>? AbsoluteActions { get; set; } = null;
            public Dictionary<byte, string>? StringMappings { get; set; } = null;

            public Serializable()
            {
            }

            public Serializable(Profile? Source = null)
            {
                if (Source is null)
                    return;

                Name = Source.Name;

                ButtonActions = new Dictionary<byte, string[]>();
                foreach (var KVP in Source.ButtonActions)
                {
                    ButtonActions.Add(KVP.Key, new string[] { $"{KVP.Value.Item1}" , KVP.Value.Item2.GetType()?.FullName });
                }

                AbsoluteActions = new Dictionary<byte, string>();
                foreach (var KVP in Source.AbsoluteActions)
                {
                    AbsoluteActions.Add(KVP.Key, KVP.Value.GetType()?.FullName);
                }

                StringMappings = new Dictionary<byte, string>();
                foreach (var KVP in Source.StringMappings)
                {
                    StringMappings.Add(KVP.Key, KVP.Value.GetType()?.FullName);
                }
            }
        }

        public Profile()
        {
        }

        public Profile(Stream JsonStream)
        {
            Serializable? Serialized = JsonSerializer.Deserialize<Serializable>(JsonStream);
            if (Serialized is null)
                return;

            if (Serialized.Name is not null)
                Name = Serialized.Name;
            if (Serialized.ButtonActions is not null)
                foreach (var KVP in Serialized.ButtonActions)
                {
                    if (LoadedExtensions.ControllerActions is null)
                        break;
                    Type? ExtensionType = null;
                    foreach (var item in LoadedExtensions.ControllerActions)
                    {
                        if (item.FullName == KVP.Value[1])
                        {
                            ExtensionType = item;
                            break;
                        }
                    }
                    if (ExtensionType is null)
                        continue;
                    ButtonActions.Add(KVP.Key, new Tuple<ButtonUpdate, ControllerAction>(KVP.Value[0] == "Pushed" ? ButtonUpdate.Pushed : ButtonUpdate.Released, (ControllerAction)Activator.CreateInstance(ExtensionType)));
                }
            if (Serialized.AbsoluteActions is not null)
                foreach (var KVP in Serialized.AbsoluteActions)
                {
                    if (LoadedExtensions.ControllerActions is null)
                        break;
                    Type? ExtensionType = null;
                    foreach (var item in LoadedExtensions.ControllerActions)
                    {
                        if (item.FullName == KVP.Value)
                        {
                            ExtensionType = item;
                            break;
                        }
                    }
                    if (ExtensionType is null)
                        continue;
                    AbsoluteActions.Add(KVP.Key, (AbsoluteControllerAction)Activator.CreateInstance(ExtensionType));
                }
            if (Serialized.StringMappings is not null)
                foreach (var KVP in Serialized.StringMappings)
                {
                    if (LoadedExtensions.StringSources is null)
                        break;
                    Type? ExtensionType = null;
                    foreach (var item in LoadedExtensions.StringSources)
                    {
                        if (item.FullName == KVP.Value)
                        {
                            ExtensionType = item;
                            break;
                        }
                    }
                    if (ExtensionType is null)
                        continue;
                    StringMappings.Add(KVP.Key, (StringSource)Activator.CreateInstance(ExtensionType));
                }
        }

        public byte[] DeserializeJson()
        {
            byte[] Bytes;
            using (var M = new MemoryStream())
            {
                JsonSerializer.Serialize(M, new Serializable(this), options: new JsonSerializerOptions() { WriteIndented = true });
                Bytes = M.ToArray();
            }
            return Bytes;
        }
    }
}