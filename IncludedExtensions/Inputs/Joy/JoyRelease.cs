using PanelExtension;
using Profiling.ProfilingTypes.PanelItems;
using vJoyInterfaceWrap;

namespace IncludedExtensions.Inputs.Joy
{
    [PanelActionDescriptor("Joy Release")]
    public class JoyRelease : Extension, IPanelAction
    {
        private const string vJoyIDKey = "vJoy ID";
        private const string ButtonNumberKey = "Button";

        Dictionary<string, string?> Options = new();

        JoyRelease()
        : base()
        {
            if (!new vJoy().vJoyEnabled())
                throw new Exception("vJoy driver must be installed to use action.");
        }

        public string?[]?[]? ValidOptions()
        {
            if (Options.ContainsKey(vJoyIDKey))
            {
                if (uint.TryParse(Options[vJoyIDKey], out uint vJoyID))
                {
                    vJoy Joystick = new();
                    if (Joystick.GetVJDStatus(vJoyID) <= VjdStat.VJD_STAT_FREE)
                    {
                        int ButtonCount = Joystick.GetVJDButtonNumber(vJoyID);
                        List<string> ButtonNumbersValidOptions = new() { ButtonNumberKey };
                        for (int i = 1; i < ButtonCount + 1; i++)
                            ButtonNumbersValidOptions.Add($"{i}");
                        return new string?[]?[]
                        {
                            new string?[]
                            {
                                vJoyIDKey,
                                null
                            },
                            ButtonNumbersValidOptions.ToArray()
                        };
                    }
                }
            }
            return new string?[]?[]
            {
                new string?[]
                {
                    vJoyIDKey,
                    null
                },
                new string?[]
                {
                    ButtonNumberKey,
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

            vJoy Joystick;

            if (Inputs.AcquiredFeeders.ContainsKey(vJoyID))
            {
                Joystick = Inputs.AcquiredFeeders[vJoyID];
            }
            else
            {
                Joystick = new();
                VjdStat Status = Joystick.GetVJDStatus(vJoyID);

                switch (Status)
                {
                    case VjdStat.VJD_STAT_OWN:
                        goto SkipAcquisition; //Should be impossible.
                    case VjdStat.VJD_STAT_FREE:
                        Application?.Logger.Log(ILogger.Levels.Info, $"{vJoyID} was free, trying to acquire.", "Joy Push");
                        break;
                    case VjdStat.VJD_STAT_BUSY:
                        return $"vJoy {vJoyID} is being used by another feeder.";
                    case VjdStat.VJD_STAT_MISS:
                        return $"vJoy {vJoyID} is not installed or disabled.";
                    case VjdStat.VJD_STAT_UNKN:
                    default:
                        return $"vJoy {vJoyID} cannot be acquired due to an unkown error.";
                }

                if (!Joystick.AcquireVJD(vJoyID))
                    return $"Could not acquire {vJoyID}.";

                SkipAcquisition:
                Inputs.AcquiredFeeders.Add(vJoyID, Joystick);
            }

            if (!Options.ContainsKey(ButtonNumberKey))
                return $"{ButtonNumberKey} was not specified.";

            if (!uint.TryParse(Options[ButtonNumberKey], out uint ButtonNumber))
                return $"{ButtonNumberKey} was not a number.";

            if (ButtonNumber is not > 0 and <= 128)
                return $"{ButtonNumberKey} must be between 1->128";

            if (ButtonNumber > Joystick.GetVJDButtonNumber(vJoyID))
                return $"{ButtonNumberKey} was greater than number of buttons on vJoy {vJoyID}.";

            if (!Joystick.SetBtn(false, vJoyID, ButtonNumber))
                return $"Setting button {Options[ButtonNumberKey]} of vJoy {vJoyID} was unsuccessful.";

            return null;
        }
    }
}