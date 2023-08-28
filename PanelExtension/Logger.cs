using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PanelExtension
{
    public interface ILogger
    {
        public void Info(string Message);
        public void Warn(string Message);
        public void Error(string Message);
        public void OnLogChange(EventHandler Handler);
        public void RemoveOnLogChange(EventHandler Handler);
        public string GetLog();
    }
}