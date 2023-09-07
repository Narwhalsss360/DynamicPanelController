using PanelExtension;
using Profiling.ProfilingTypes.PanelItems;

namespace IncludedExtensions
{
    [PanelActionDescriptor("Profile Switcher")]
    public class ProfileSwitcher : Extension, IPanelAction
    {
        public static readonly string ProfileNameKey = "ProfileName";
        public const string CycleNextValue = "++";
        public const string CyclePreviousValue = "--";
        Dictionary<string, string?> Options = new();

        public ProfileSwitcher()
            : base()
        {
            ProfilesListChanged += ProfilesListUpdated;
        }

        public void ProfilesListUpdated(object? sender, EventArgs Args)
        {
            if (!Options.ContainsKey(ProfileNameKey))
                return;
            if (Options[ProfileNameKey] is CycleNextValue or CyclePreviousValue)
                return;
            if (Application is null)
                return;
            if (Application.Profiles.Any(Profile => Profile.Name == Options[ProfileNameKey]))
                return;
            Application?.Logger.Log(ILogger.Levels.Error, $"{Options[ProfileNameKey]} was not found in the profiles list, removing option.", "Profile Switcher");
            Options.Remove(ProfileNameKey);
        }

        public string?[]?[]? ValidOptions()
        {
            return new string?[]?[] { new string?[] { ProfileNameKey, null } };
        }

        public string? SetOptions(Dictionary<string, string?> Options)
        {
            if (!Options.ContainsKey(ProfileNameKey))
                return "Must specify a profile name.";
            if (Application is null)
                return "Application is null";
            if (!(Options[ProfileNameKey] is CycleNextValue or CyclePreviousValue))
                if (!Application.Profiles.Any(Profile => Profile.Name == Options[ProfileNameKey]))
                    return $"Profile {Options[ProfileNameKey]} not found.";
            this.Options = Options;
            return null;
        }

        public Dictionary<string, string?>? GetOptions()
        {
            return Options;
        }

        public object? Do(object? Arguments = null)
        {
            if (!Options.ContainsKey(ProfileNameKey))
                return "Profile Name not specifed.";

            if (Application is null)
                return "Application is null.";

            string? ProfileName = Options[ProfileNameKey];
            if (ProfileName == CycleNextValue)
            {
                int NewIndex = (Application.CurrentProfileIndex + 1) % Application.Profiles.Length;
                SelectIndex(NewIndex);
                ProfileName = Application.Profiles[NewIndex].Name;
            }
            else if (ProfileName == CyclePreviousValue)
            {
                int NewIndex = (Application.CurrentProfileIndex + 1) % Application.Profiles.Length;
                SelectIndex(NewIndex);
                ProfileName = Application.Profiles[NewIndex].Name;
            }
            else
            {
                int ProfileIndex = Array.FindIndex(Application.Profiles, Profile => Profile.Name == Options[ProfileNameKey]);
                if (ProfileIndex < 0)
                {
                    Options.Remove(ProfileNameKey);
                    return $"{Options[ProfileNameKey]} was not found in the profiles list, removing option.";
                }

                SelectIndex(ProfileIndex);
            }

            Application.Logger.Log(ILogger.Levels.Warning, $"Switching to profile {ProfileName}.", "Profile Switcher");

            return null;
        }
    }
}