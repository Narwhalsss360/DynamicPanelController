namespace Extensibility
{
    public class ControllerActionDescriptor : Attribute
    {
        public readonly string ActionName;

        public ControllerActionDescriptor(string ActionName = "")
        {
            this.ActionName = ActionName;
        }
    }


    public interface ControllerAction
    {
        string GetActionName();

        public void Do();
    }
}