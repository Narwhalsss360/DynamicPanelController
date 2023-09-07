using Panel.Communication;
using Profiling.ProfilingTypes.PanelItems;

namespace Profiling.ProfilingTypes.Mappings
{
    public class ActionMapping : Mapping
    {
        public ButtonUpdateStates UpdateState { get; set; }
        public IPanelAction Action { get; set; }
    }
}