using Extensibility;
using System.Text;

namespace DynmaicPanelControllerCLI.DefaultExtensions
{
    public class TestAction : ControllerAction
    {
        public string GetActionName()
        {
            return "Test Action";
        }

        public void Do()
        {
            using (var F = File.Open($"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}\\TestAction.txt", FileMode.Create))
            {
                string Str = $"{GetActionName()} ran at {DateTime.Now}. Current Profile: {Profile.CurrentProfile?.ButtonActions[0]?.Item2.GetActionName()}";
                byte[] Bytes = Encoding.ASCII.GetBytes(Str);
                F.Write(Bytes, 0, Bytes.Length);
            }
        }
    }
}