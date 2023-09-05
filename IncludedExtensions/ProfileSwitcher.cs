using PanelExtension;
using Profiling.ProfilingTypes;

namespace IncludedExtensions
{
    [PanelActionDescriptor("Profile Switcher")]
    public class ProfileSwitcher : Extension, IPanelAction
    {
        public static readonly string ProfileNameKey = "ProfileName";
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
            if (Application is null)
                return;
            if (Application.Profiles.Any(Profile => Profile.Name == Options[ProfileNameKey]))
                return;
            Application?.Logger.Error($"{Options[ProfileNameKey]} was not found in the profiles list, removing option.");
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
            if (!Application.Profiles.Any(Profile => Profile.Name == Options[ProfileNameKey]))
                return $"Profile {Options[ProfileNameKey]} not found.";
            this.Options = Options;
            return null;
        }

        public Dictionary<string, string?>? GetOptions()
        {
            return Options;
        }

        object? Do(object? Arguments = null)
        {
            if (!Options.ContainsKey(ProfileNameKey))
            {
                return "Profile Name not specifed.";
            }

            if (Application is null)
                return "Application is not subscribed.";
            if (!Application.Profiles.Any(Profile => Profile.Name == Options[ProfileNameKey]))
            {
                Options.Remove(ProfileNameKey);
                return $"{Options[ProfileNameKey]} was not found in the profiles list, removing option.";
            }



            return null;
        }
    }
}