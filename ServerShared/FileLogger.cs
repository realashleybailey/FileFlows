using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileFlows.ServerShared
{
    public class FileLogger : FileFlows.Plugin.ILogger
    {
        private string logFile;
        public FileLogger(string logFile)
        {
            this.logFile = logFile;
        }

        private enum LogType { Error, Warning, Debug, Info }
        private void Log(LogType type, object[] args)
        {
            string message = type + " -> " + string.Join(", ", args.Select(x =>
                x == null ? "null" :
                x.GetType().IsPrimitive ? x.ToString() :
                x is string ? x.ToString() :
                System.Text.Json.JsonSerializer.Serialize(x)));
            Console.WriteLine(message);
            System.IO.File.AppendAllText(logFile, message);
        }

        public void ILog(params object[] args) => Log(LogType.Info, args);
        public void DLog(params object[] args) => Log(LogType.Debug, args);
        public void WLog(params object[] args) => Log(LogType.Warning, args);
        public void ELog(params object[] args) => Log(LogType.Error, args);

        static FileFlows.Plugin.ILogger _Instance;
        public static FileFlows.Plugin.ILogger Instance
        {
            get
            {
                if (_Instance == null)
                    _Instance = new Logger();
                return _Instance;
            }
        }
    }
}
