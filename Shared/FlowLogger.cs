using System;
using System.Linq;
using System.Text;

namespace ViWatcher.Shared 
{
    public interface IFlowLogger {
        void ILog(params object[] args);
        void DLog(params object[] args);
        void WLog(params object[] args);
        void ELog(params object[] args);
    }

    public class FlowLogger : IFlowLogger
    {
        StringBuilder log = new StringBuilder();
        public void DLog(params object[] args)=> Log(LogType.Debug, args);
        public void ELog(params object[] args)=> Log(LogType.Error, args);
        public void ILog(params object[] args)=> Log(LogType.Info, args);

        public void WLog(params object[] args) => Log(LogType.Warning, args);
        private enum LogType {
            Error, Warning, Info, Debug
        }

        private void Log(LogType type, params object[] args)
        {
            if(args == null || args.Length == 0)
                return;
            string message = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss.ffff") + " - " + type + " -> " +
                string.Join(", ", args.Select(x =>
                x == null ? "null" :
                x.GetType().IsPrimitive || x is string ? x.ToString() :
                Newtonsoft.Json.JsonConvert.SerializeObject(x)));
            log.AppendLine(message);
            Console.WriteLine(message);
        }

        public override string ToString() => log.ToString();
    }
}