using PanelExtension;
using Profiling.ProfilingTypes.PanelItems;
using System.Data.SqlTypes;
using vJoyInterfaceWrap;

namespace IncludedExtensions.Inputs.Joy
{
    [PanelActionDescriptor("Joy Reset")]
    public class JoyReset : Extension, IPanelAction
    {
        private const string vJoyIDKey = "vJoy ID";

        Dictionary<string, string?> Options = new();

        public JoyReset()
        : base()
        {
            if (!new vJoy().vJoyEnabled())
                throw new Exception("vJoy driver must be installed to use action.");
        }

        public string?[]?[]? ValidOptions()
        {
            return new string?[]?[]
            {
                new string?[]
                {
                    vJoyIDKey,
                    null
                }
            };
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

        public object? Do(object? Arguments = null)
        {
            if (!Options.ContainsKey(vJoyIDKey))
                return $"{vJoyIDKey} was not specified.";

            if (!uint.TryParse(Options[vJoyIDKey], out uint vJoyID))
                return $"{Options[vJoyIDKey]} is not a number.";

            if (vJoyID is not > 0 and <= 16)
                return $"{vJoyIDKey} must be between 1->16.";

            if (!Inputs.AcquiredFeeders.ContainsKey(vJoyID))
                return $"vJoy {vJoyID} has not been acquired, cannot reset.";

            Inputs.AcquiredFeeders[vJoyID].ResetAll();

            return null;
        }
    }
}