using PanelExtension;
using Profiling.ProfilingTypes.PanelItems;
using WindowsInput;
using WindowsInput.Native;

namespace IncludedExtensions
{
    [PanelActionDescriptor("Key Stroke")]
    public class KeyStroke : Extension, IPanelAction
    {
        private const string InputTypeKey = "InputType";
        private const string TextInputTypeKey = "Text";
        private const string StrokeInputTypeKey = "Key";
        private const string EntryKey = "Entry";
        private Dictionary<string, string?> Options = new();
        private readonly InputSimulator Input = new();

        public string?[]?[]? ValidOptions()
        {
            if (Options.ContainsKey(InputTypeKey))
            {
                if (Options[InputTypeKey] == TextInputTypeKey)
                {
                    goto Default;
                }
                else
                {
                    List<string> EntryKeysArray = new() { EntryKey };
                    EntryKeysArray.AddRange(Enum.GetNames(typeof(VirtualKeyCode)));
                    return new string?[]?[]
                    {
                        new string?[]
                        {
                            InputTypeKey,
                            TextInputTypeKey,
                            StrokeInputTypeKey,
                        },
                        EntryKeysArray.ToArray()
                    };
                }
            }
        Default:
            return new string?[]?[]
            {
                new string?[]
                {
                    InputTypeKey,
                    TextInputTypeKey,
                    StrokeInputTypeKey,
                },
                new string?[]
                {
                    EntryKey,
                    null
                }
            };
        }

        public string? SetOptions(Dictionary<string, string?> Options)
        {
            if (Options.ContainsKey(InputTypeKey) && Options[InputTypeKey] == TextInputTypeKey)
                Options[EntryKey] = "";
            this.Options = Options;
            return null;
        }

        public Dictionary<string, string?>? GetOptions()
        {
            return Options;
        }

        public object? Do(object? Arguments = null)
        {
            if (!Options.ContainsKey(InputTypeKey))
                return "No specified input type.";
            if (!Options.ContainsKey(EntryKey))
                return "No Entry Key";

            if (Options[InputTypeKey] == TextInputTypeKey)
            {
                _ = Input.Keyboard.TextEntry(Options[EntryKey]);
            }
            else
            {
                bool Success = Enum.TryParse(Options[EntryKey], out VirtualKeyCode KeyCode);
                if (!Success)
                    return $"Couldn't parse keycode-string {Options[EntryKey]} to VirtualKeyCode.";
                _ = Input.Keyboard.KeyPress(KeyCode);
            }

            return null;
        }
    }
}