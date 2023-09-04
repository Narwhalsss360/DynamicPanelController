using Panel.Communication;

namespace Profiling.ProfilingTypes
{
    public class ActionMapping : Mapping
    {
        public ButtonUpdateStates UpdateState { get; set; }
        public IPanelAction Action { get; set; }
    }
}