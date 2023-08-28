using Panel;
using Panel.Communication;
using Profiling.ProfilingTypes;
using System.Text.Json;

namespace Profiling
{
    public class PanelProfile
    {
        public string Name { get; set; } = string.Empty;
        public PanelDescriptor? PanelDescription { get; set; } = null;
        public Dictionary<byte, Tuple<ButtonUpdateStates, IPanelAction>> ActionMappings { get; set; } = new();
        public Dictionary<byte, IAbsolutePanelAction> AbsoluteActionMappings { get; set; } = new();
        public Dictionary<byte, IPanelSource> SourceMappings { get; set; } = new();

        public PanelProfile()
        {
        }

        public PanelProfile(string Json, Type[] AvailableActions, Type[] AvailableSources)
        {
            Serializable? Serialized = JsonSerializer.Deserialize<Serializable?>(Json) ?? throw new PanelProfileException("Couldn't serialize.");
            if (Serialized.Name is not null)
                Name = Serialized.Name;

            if (Serialized.ActionMappings is not null)
            {
                foreach (var IDAction in Serialized.ActionMappings)
                {
                    Type? ExtensionType = Array.Find(AvailableActions, Extension => Extension.FullName == IDAction.Value[1]);
                    if (ExtensionType is null)
                        continue;
                    IPanelAction? Instance = Activator.CreateInstance(ExtensionType) as IPanelAction ?? throw new PanelProfileException($"Couldn't create instance of type {ExtensionType.FullName}");
                    ActionMappings.Add(IDAction.Key, new Tuple<ButtonUpdateStates, IPanelAction>(IDAction.Value[0].ToButtonUpdateState(), Instance));
                }
            }
            if (Serialized.AbsoluteActionMappings is not null)
            {
                foreach (var IDAction in Serialized.AbsoluteActionMappings)
                {
                    Type? ExtensionType = Array.Find(AvailableActions, Extension => Extension.FullName == IDAction.Value);
                    if (ExtensionType is null)
                        continue;
                    IAbsolutePanelAction? Instance = Activator.CreateInstance(ExtensionType) as IAbsolutePanelAction ?? throw new PanelProfileException($"Couldn't create instance of type {ExtensionType.FullName}");
                    AbsoluteActionMappings.Add(IDAction.Key, Instance);
                }
            }
            if (Serialized.SourceMappings is not null)
            {
                foreach (var IDSource in Serialized.SourceMappings)
                {
                    Type? ExtensionType = Array.Find(AvailableSources, Extension => Extension.FullName == IDSource.Value);
                    if (ExtensionType is null)
                        continue;
                    IPanelSource? Instance = Activator.CreateInstance(ExtensionType) as IPanelSource ?? throw new PanelProfileException($"Couldn't create instance of type {ExtensionType.FullName}");
                    SourceMappings.Add(IDSource.Key, Instance);
                }
            }
        }

        public string Serialize() => JsonSerializer.Serialize(new Serializable(this), options: new JsonSerializerOptions() {  WriteIndented = true });

        class Serializable
        {
            public string? Name { get; set; } = null;
            public Dictionary<byte, string[]>? ActionMappings { get; set; } = null;
            public Dictionary<byte, string>? AbsoluteActionMappings { get; set; } = null;
            public Dictionary<byte, string>? SourceMappings { get; set; } = null;

            public Serializable()
            {
            }

            public Serializable(PanelProfile Source)
            {
                Name = Source.Name;

                ActionMappings = new();
                foreach (var IDAction in Source.ActionMappings)
                {
                    string? ActionTypeName = (IDAction.Value.Item2.GetType()?.FullName) ?? throw new PanelProfileException("Null ActionTypeName.");
                    ActionMappings.Add(IDAction.Key, new string[] { $"{IDAction.Value.Item1}", ActionTypeName });
                }

                AbsoluteActionMappings = new();
                foreach (var IDAction in Source.AbsoluteActionMappings)
                {
                    string? ActionTypeName = (IDAction.Value.GetType()?.FullName) ?? throw new PanelProfileException("Null ActionTypeName.");
                    AbsoluteActionMappings.Add(IDAction.Key, ActionTypeName);
                }

                SourceMappings = new();
                foreach (var IDSource in Source.SourceMappings)
                {
                    string? ActionTypeName = (IDSource.Value.GetType()?.FullName) ?? throw new PanelProfileException("Null ActionTypeName.");
                    SourceMappings.Add(IDSource.Key, ActionTypeName);
                }
            }
        }
    }
}