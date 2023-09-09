using PanelExtension;
using Profiling.ProfilingTypes.PanelItems;
using WindowsInput.Native;
using static IncludedExtensions.Inputs.Inputs;

namespace IncludedExtensions.Inputs.Mouse
{
    [PanelActionDescriptor("Mouse Button")]
    public class MouseButton : Extension, IPanelAction
    {
        private const string ButtonKey = "Button";
        private Dictionary<string, string?> Options = new();

        public string?[]?[]? ValidOptions()
        {
            List<string> ValidButtonOptions = new() { ButtonKey };
            ValidButtonOptions.AddRange(Enum.GetNames<Inputs.MouseButtons>());
            return new string?[]?[]
            {
                ValidButtonOptions.ToArray()
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
            if (!Options.ContainsKey(ButtonKey))
                return $"{ButtonKey} not specified.";

            if (!Enum.TryParse<Inputs.MouseButtons>(Options[ButtonKey], out var Button))
                return $"{Options[ButtonKey]} was not a button.";

            Inputs.GetMouseAction(Button)();

            return null;
        }
    }
}