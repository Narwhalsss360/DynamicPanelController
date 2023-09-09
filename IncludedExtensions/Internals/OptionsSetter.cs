using Panel.Communication;
using PanelExtension;
using Profiling;
using Profiling.ProfilingTypes.Mappings;
using Profiling.ProfilingTypes.PanelItems;

namespace IncludedExtensions.Internals
{
    [PanelActionDescriptor("Options Setter")]
    internal class OptionsSetter : Extension, IPanelAction
    {
        public const string ProfileNameKey = "ProfileName";
        public const string IOTypeKey = "IOType";
        public const string IDKey = "ID";


        public const string ButtonUpdateStateKey = "ButtonUpdateState";
        public const string ButtonUpdateStatePushed = "Pushed";
        public const string ButtonUpdateStateReleased = "Released";

        public const string IOTypeButton = "Button";
        public const string IOTypeAbsolute = "Absolute";
        public const string IOTypeSource = "Source";
        private Dictionary<string, string?> Options = new();

        public string?[]?[]? ValidOptions()
        {
            return new string?[]?[]
            {
                new string?[]
                {
                    ProfileNameKey,
                    null
                },
                new string?[]
                {
                    IDKey,
                    null
                },
                new string?[]
                {
                    IOTypeKey,
                    IOTypeButton,
                    IOTypeAbsolute,
                    IOTypeSource
                },
                new string?[]
                {
                    ButtonUpdateStateKey,
                    ButtonUpdateStatePushed,
                    ButtonUpdateStateReleased
                },
                null
            };
        }

        public string? ValidateOptions(Dictionary<string, string?> Options)
        {
            if (!Options.ContainsKey(ProfileNameKey))
                return "Options did not contain a profile name.";
            if (!Options.ContainsKey(IOTypeKey))
                return "Options did not contain an IO Type.";
            if (!Options.ContainsKey(IDKey))
                return "Options did not contain ID.";
            if (Options[IOTypeKey] == IOTypeButton)
            {
                if (!Options.ContainsKey(ButtonUpdateStateKey))
                    return $"IOType button must contain {ButtonUpdateStateKey} key.";

                if (Options[ButtonUpdateStateKey] is not (ButtonUpdateStatePushed or ButtonUpdateStateReleased))
                    return "Unknown button updat state entered.";
            }
            else if (Options[IOTypeKey] is not (IOTypeAbsolute or IOTypeSource))
                return "Unknown IOType entered.";

            return null;
        }

        public string? SetOptions(Dictionary<string, string?> Options)
        {
            this.Options = Options;
            return null;
        }

        public Dictionary<string, string?>? GetOptions()
        {
            return Options;
        }

        private IPanelItem? GetPanelItemOfMapping(Mapping Mapping)
        {
            if (Mapping is ActionMapping ActionMapping)
                return ActionMapping.Action;
            else if (Mapping is AbsoluteActionMapping AbsoluteActionMapping)
                return AbsoluteActionMapping.AbsoluteAction;
            else if (Mapping is SourceMapping SourceMapping)
                return SourceMapping.Source;
            else
                return null;
        }

        public object? Do(object? Arguments = null)
        {
            if (ValidateOptions(Options) is string ErrorMessage)
                return ErrorMessage;

            if (Application is null)
                return "Application is null";

            if (Array.Find(Application.Profiles, Profile => Profile.Name == Options[ProfileNameKey]) is not PanelProfile Profile)
                return $"Did not find profile with name {Options[ProfileNameKey]}.";

            Mapping[] MappingSource;
            Predicate<Mapping> FindingPredicate;

            if (Options[IOTypeKey] == IOTypeButton)
            {
                MappingSource = Profile.ActionMappings.ToArray();
                FindingPredicate = (Action) => $"{Action.ID}" == Options[IDKey] && (Action as ActionMapping)?.UpdateState == Options[ButtonUpdateStateKey]?.ToButtonUpdateState();
            }
            else if (Options[IOTypeKey] == IOTypeAbsolute)
            {
                MappingSource = Profile.AbsoluteActionMappings.ToArray();
                FindingPredicate = (AbsoluteAction) => $"{AbsoluteAction.ID}" == Options[IDKey];
            }
            else
            {
                MappingSource = Profile.SourceMappings.ToArray();
                FindingPredicate = (Source) => $"{Source.ID}" == Options[IDKey];
            }

            if (Array.Find(MappingSource, FindingPredicate) is not Mapping Mapping)
                return $"Could not find mapping with ID {Options[IDKey]}.";

            if (GetPanelItemOfMapping(Mapping) is not IPanelItem PanelItemToSet)
                return "Couldn't get panel item.";

            Dictionary<string, string?> SettingsToSet = new(Options);

            if (SettingsToSet.ContainsKey(ProfileNameKey))
                _ = SettingsToSet.Remove(ProfileNameKey);
            if (SettingsToSet.ContainsKey(IOTypeKey))
                _ = SettingsToSet.Remove(IOTypeKey);
            if (SettingsToSet.ContainsKey(IDKey))
                _ = SettingsToSet.Remove(IDKey);
            if (SettingsToSet.ContainsKey(ButtonUpdateStateKey))
                _ = SettingsToSet.Remove(ButtonUpdateStateKey);

            string? Message = PanelItemToSet.SetOptions(SettingsToSet);

            if (Message is not null)
                Application.Logger.Log(ILogger.Levels.Error, Message, $"Options Setter->{PanelItemToSet.GetPanelItemDescriptor()?.Name}");

            return Message is null ? null : "There was a problem setting the options";
        }
    }
}