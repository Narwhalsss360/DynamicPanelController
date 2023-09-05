using Panel;
using Panel.Communication;
using Profiling.ProfilingTypes;
using System.Text.Json;

namespace Profiling
{
    public class PanelProfile
    {
        public string Name { get; set; } = string.Empty;
        public PanelDescriptor? PanelDescriptor
        {
            get;
            set;
        } = null;
        public List<ActionMapping> ActionMappings { get; set; } = new();
        public List<AbsoluteActionMapping> AbsoluteActionMappings { get; set; } = new();
        public List<SourceMapping> SourceMappings { get; set; } = new();

        public PanelProfile()
        {
        }

        public PanelProfile(string Json, Type[] AvailableActions, Type[] AvailableAbsoluteActions, Type[] AvailableSources)
        {
            Serializable? Serialized = JsonSerializer.Deserialize<Serializable?>(Json) ?? throw new PanelProfileException("Couldn't serialize.");
            if (Serialized.Name is not null)
                Name = Serialized.Name;

            if (Serialized.PanelDescriptor is not null)
                PanelDescriptor = new(Serialized.PanelDescriptor);

            if (Serialized.ActionMappings is not null)
            {
                foreach (var IDAction in Serialized.ActionMappings)
                {
                    Type? ExtensionType = Array.Find(AvailableActions, Extension => Extension.FullName == IDAction.Value[1]);
                    if (ExtensionType is null)
                        continue;
                    IPanelAction? Instance = Activator.CreateInstance(ExtensionType) as IPanelAction ?? throw new PanelProfileException($"Couldn't create instance of type {ExtensionType.FullName}");
                    ActionMappings.Add(new ActionMapping() { ID = IDAction.Key, UpdateState = IDAction.Value[0].ToButtonUpdateState(), Action = Instance });
                }
            }
            if (Serialized.AbsoluteActionMappings is not null)
            {
                foreach (var IDAction in Serialized.AbsoluteActionMappings)
                {
                    Type? ExtensionType = Array.Find(AvailableAbsoluteActions, Extension => Extension.FullName == IDAction.Value);
                    if (ExtensionType is null)
                        continue;
                    IAbsolutePanelAction? Instance = Activator.CreateInstance(ExtensionType) as IAbsolutePanelAction ?? throw new PanelProfileException($"Couldn't create instance of type {ExtensionType.FullName}");
                    AbsoluteActionMappings.Add(new AbsoluteActionMapping() { ID = IDAction.Key, AbsoluteAction = Instance });
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
                    SourceMappings.Add(new SourceMapping() { ID = IDSource.Key, Source = Instance });
                }
            }
        }

        public string Serialize()
        {
            return JsonSerializer.Serialize(new Serializable(this), options: new JsonSerializerOptions() { WriteIndented = true });
        }

        private class Serializable
        {
            public string? Name { get; set; } = null;
            public Dictionary<byte, string[]>? ActionMappings { get; set; } = null;
            public Dictionary<byte, string>? AbsoluteActionMappings { get; set; } = null;
            public Dictionary<byte, string>? SourceMappings { get; set; } = null;
            public PanelDescriptor.Serializable? PanelDescriptor { get; set; } = null;
            public Dictionary<byte, Dictionary<string, string?>?>? ActionMappingsOptions { get; set; } = null;
            public Dictionary<byte, Dictionary<string, string?>?>? AbsoluteActionMappingsOptions { get; set; } = null;
            public Dictionary<byte, Dictionary<string, string?>?>? SourceMappingsOptions { get; set; } = null;

            public Serializable()
            {
            }

            public Serializable(PanelProfile Source)
            {
                Name = Source.Name;

                if (Source.PanelDescriptor is not null)
                    PanelDescriptor = new PanelDescriptor.Serializable(Source.PanelDescriptor);

                ActionMappings = new();
                ActionMappingsOptions = new();
                foreach (var IDAction in Source.ActionMappings)
                {
                    string? ActionTypeName = (IDAction.Action.GetType()?.FullName) ?? throw new PanelProfileException("Null ActionTypeName.");
                    ActionMappings.Add(IDAction.ID, new string[] { $"{IDAction.UpdateState}", ActionTypeName });
                    ActionMappingsOptions.Add(IDAction.ID, IDAction.Action.GetOptions());
                }

                AbsoluteActionMappings = new();
                AbsoluteActionMappingsOptions = new();
                foreach (var IDAction in Source.AbsoluteActionMappings)
                {
                    string? ActionTypeName = (IDAction.AbsoluteAction.GetType()?.FullName) ?? throw new PanelProfileException("Null ActionTypeName.");
                    AbsoluteActionMappings.Add(IDAction.ID, ActionTypeName);
                    AbsoluteActionMappingsOptions.Add(IDAction.ID, IDAction.AbsoluteAction.GetOptions());
                }

                SourceMappings = new();
                SourceMappingsOptions = new();
                foreach (var IDSource in Source.SourceMappings)
                {
                    string? ActionTypeName = (IDSource.Source.GetType()?.FullName) ?? throw new PanelProfileException("Null ActionTypeName.");
                    SourceMappings.Add(IDSource.ID, ActionTypeName);
                    AbsoluteActionMappingsOptions.Add(IDSource.ID, IDSource.Source.GetOptions());
                }
            }
        }
    }
}