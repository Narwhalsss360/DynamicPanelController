using PanelExtension;
using Profiling.ProfilingTypes.PanelItems;
using WindowsInput.Native;

namespace IncludedExtensions.Inputs.Keyboard
{
    [PanelActionDescriptor("Macro")]
    internal class Macro : Extension, IPanelAction
    {
        private Dictionary<string, string?> Options = new();

        public string?[]?[]? ValidOptions()
        {
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

        public object? Do()
        {
            Thread.Sleep(2000);
            Queue<VirtualKeyCode> UpQueue = new();
            foreach (var MacroPair in Options)
            {
                if (!Enum.TryParse(MacroPair.Value, out VirtualKeyCode KeyCode))
                {
                    Application?.Logger.Log(ILogger.Levels.Error, $"{MacroPair.Value} is not a valid key code.", "Macro");
                    continue;
                }
                _ = Inputs.Input.Keyboard.KeyDown(KeyCode);
                UpQueue.Enqueue(KeyCode);
            }
            while (UpQueue.Count != 0)
                Inputs.Input.Keyboard.KeyUp(UpQueue.Dequeue());

            return null;
        }
    }
}