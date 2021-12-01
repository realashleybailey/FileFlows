namespace FileFlows.Server.Workers
{
    using System;
    using System.Linq;
    using System.Text;
    using FileFlows.Plugin;
    using FileFlows.Server.Helpers;
    using FileFlows.Shared.Models;

    public class FlowLogger : ILogger
    {
        public string LogFile { get; set; }
        List<string> log = new List<string>();
        public void DLog(params object[] args) => Log(LogType.Debug, args);
        public void ELog(params object[] args) => Log(LogType.Error, args);
        public void ILog(params object[] args) => Log(LogType.Info, args);
        public void WLog(params object[] args) => Log(LogType.Warning, args);

        public LibraryFile File { get; set; }
        private enum LogType
        {
            Error, Warning, Info, Debug
        }

        private void Log(LogType type, params object[] args)
        {
            if (args == null || args.Length == 0)
                return;
            string message = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss.ffff") + " - " + type + " -> " +
                string.Join(", ", args.Select(x =>
                x == null ? "null" :
                x.GetType().IsPrimitive || x is string ? x.ToString() :
                System.Text.Json.JsonSerializer.Serialize(x)));
            log.Add(message);
            Console.WriteLine(message);
            if (string.IsNullOrEmpty(LogFile) == false)
                System.IO.File.AppendAllText(LogFile, message + Environment.NewLine);
        }

        internal string GetPreview(int lineCount = 50)
        {
            lock (log)
            {
                if(lineCount > 0 && log.Count < lineCount)
                    return String.Join(Environment.NewLine, log.Skip(log.Count - lineCount));
                return String.Join(Environment.NewLine, log);
            }
        }

        public override string ToString() => String.Join(Environment.NewLine, log);
    }
}