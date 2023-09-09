using WindowsInput;

namespace IncludedExtensions.Inputs
{
    internal static class Inputs
    {
        public delegate IMouseSimulator MouseSimulatorFunction();

        public enum MouseButtons
        {
            Left,
            Right,
            ScrollUp,
            ScrollDown
        }

        public static readonly InputSimulator Input = new();
        public static int ScrollClicks = 1;

        public static MouseSimulatorFunction GetMouseAction(MouseButtons Button, IMouseSimulator? MS = null)
        {
            if (MS is null)
                MS = Input.Mouse;

            switch (Button)
            {
                case MouseButtons.Left:
                    return MS.LeftButtonClick;
                case MouseButtons.Right:
                    return MS.RightButtonClick;
                case MouseButtons.ScrollUp:
                    return () => { return MS.VerticalScroll(ScrollClicks); };
                case MouseButtons.ScrollDown:
                    return () => { return MS.VerticalScroll(-1 * ScrollClicks); };
                default:
                    return MS.LeftButtonClick;
            }
        }
    }
}