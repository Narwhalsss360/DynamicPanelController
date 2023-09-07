using Panel;
using Panel.Communication;
using Profiling.ProfilingTypes.Mappings;
using Profiling.ProfilingTypes.PanelItems;
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

        public PanelProfile(string Json, Type[] AvailableActions, Type[] AvailableAbsoluteActions, Type[] AvailableSources, out bool Result)
        {
            Serializable? Serialized = null;
            try
            {
                Serialized = JsonSerializer.Deserialize<Serializable?>(Json);
            }
            catch (JsonException)
            {
            }

            if (Serialized is null)
            {
                Name = "New Profile";
                Result = false;
                return;
            }

            if (Serialized.Name is null)
            {
                Name = "New Profile";
                Result = false;
                return;
            }

            Name = Serialized.Name;

            if (Serialized.PanelDescriptor is not null)
                PanelDescriptor = new(Serialized.PanelDescriptor);

            if (Serialized.PushedActionMappings is not null)
            {
                foreach (var IDAction in Serialized.PushedActionMappings)
                {
                    Type? ExtensionType = Array.Find(AvailableActions, Extension => Extension.FullName == IDAction.Value);
                    if (ExtensionType is null)
                        continue;
                    IPanelAction? Instance = Activator.CreateInstance(ExtensionType) as IPanelAction ?? throw new PanelProfileException($"Couldn't create instance of type {ExtensionType.FullName}");
                    ActionMappings.Add(new ActionMapping() { ID = IDAction.Key, UpdateState = ButtonUpdateStates.Pushed, Action = Instance });

                    if (Serialized.PushedActionMappingsOptions is not null)
                        if (Serialized.PushedActionMappingsOptions.ContainsKey(IDAction.Key))
                            if (Serialized.PushedActionMappingsOptions[IDAction.Key] is Dictionary<string, string?> Options)
                                _ = ActionMappings.Last().Action.SetOptions(Options);
                }
            }

            if (Serialized.ReleasedActionMappings is not null)
            {
                foreach (var IDAction in Serialized.ReleasedActionMappings)
                {
                    Type? ExtensionType = Array.Find(AvailableActions, Extension => Extension.FullName == IDAction.Value);
                    if (ExtensionType is null)
                        continue;
                    IPanelAction? Instance = Activator.CreateInstance(ExtensionType) as IPanelAction ?? throw new PanelProfileException($"Couldn't create instance of type {ExtensionType.FullName}");
                    ActionMappings.Add(new ActionMapping() { ID = IDAction.Key, UpdateState = ButtonUpdateStates.Released, Action = Instance });

                    if (Serialized.ReleasedActionMappingsOptions is not null)
                        if (Serialized.ReleasedActionMappingsOptions.ContainsKey(IDAction.Key))
                            if (Serialized.ReleasedActionMappingsOptions[IDAction.Key] is Dictionary<string, string?> Options)
                                _ = ActionMappings.Last().Action.SetOptions(Options);
                }
            }

            if (Serialized.AbsoluteActionMappings is not null)
            {
                foreach (var IDAbsoluteAction in Serialized.AbsoluteActionMappings)
                {
                    Type? ExtensionType = Array.Find(AvailableAbsoluteActions, Extension => Extension.FullName == IDAbsoluteAction.Value);
                    if (ExtensionType is null)
                        continue;
                    IAbsolutePanelAction? Instance = Activator.CreateInstance(ExtensionType) as IAbsolutePanelAction ?? throw new PanelProfileException($"Couldn't create instance of type {ExtensionType.FullName}");
                    AbsoluteActionMappings.Add(new AbsoluteActionMapping() { ID = IDAbsoluteAction.Key, AbsoluteAction = Instance });

                    if (Serialized.AbsoluteActionMappingsOptions is not null)
                        if (Serialized.AbsoluteActionMappingsOptions.ContainsKey(IDAbsoluteAction.Key))
                            if (Serialized.AbsoluteActionMappingsOptions[IDAbsoluteAction.Key] is Dictionary<string, string?> Options)
                                _ = AbsoluteActionMappings.Last().AbsoluteAction.SetOptions(Options);
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

                    if (Serialized.SourceMappingsOptions is not null)
                        if (Serialized.SourceMappingsOptions.ContainsKey(IDSource.Key))
                            if (Serialized.SourceMappingsOptions[IDSource.Key] is Dictionary<string, string?> Options)
                                _ = SourceMappings.Last().Source.SetOptions(Options);
                }
            }

            Result = true;
        }

        public string Serialize()
        {
            return JsonSerializer.Serialize(new Serializable(this), options: new JsonSerializerOptions() { WriteIndented = true });
        }

        private class Serializable
        {
            public string? Name { get; set; } = null;
            public Dictionary<byte, string>? PushedActionMappings { get; set; } = null;
            public Dictionary<byte, string>? ReleasedActionMappings { get; set; } = null;
            public Dictionary<byte, string>? AbsoluteActionMappings { get; set; } = null;
            public Dictionary<byte, string>? SourceMappings { get; set; } = null;
            public PanelDescriptor.Serializable? PanelDescriptor { get; set; } = null;
            public Dictionary<byte, Dictionary<string, string?>?>? PushedActionMappingsOptions { get; set; } = null;
            public Dictionary<byte, Dictionary<string, string?>?>? ReleasedActionMappingsOptions { get; set; } = null;
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

                PushedActionMappings = new();
                ReleasedActionMappings = new();
                PushedActionMappingsOptions = new();
                ReleasedActionMappingsOptions = new();
                foreach (var IDAction in Source.ActionMappings)
                {
                    string? ActionTypeName = (IDAction.Action.GetType()?.FullName) ?? throw new PanelProfileException("Null ActionTypeName.");
                    if (IDAction.UpdateState == ButtonUpdateStates.Pushed)
                    {
                        PushedActionMappings.Add(IDAction.ID, ActionTypeName);
                        PushedActionMappingsOptions.Add(IDAction.ID, IDAction.Action.GetOptions());
                    }
                    else
                    {
                        ReleasedActionMappings.Add(IDAction.ID, ActionTypeName);
                        ReleasedActionMappingsOptions.Add(IDAction.ID, IDAction.Action.GetOptions());
                    }
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
                    SourceMappingsOptions.Add(IDSource.ID, IDSource.Source.GetOptions());
                }
            }
        }
    }
}