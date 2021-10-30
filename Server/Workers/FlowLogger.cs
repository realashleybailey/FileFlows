namespace FileFlow.Server.Workers
{
    using System;
    using System.Linq;
    using System.Text;
    using FileFlow.Plugin;
    using FileFlow.Server.Helpers;
    using FileFlow.Shared.Models;

    public class FlowLogger : ILogger
    {
        StringBuilder log = new StringBuilder();
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
            log.AppendLine(message);
            Console.WriteLine(message);

            if (File != null)
            {
                File.Log = log.ToString();
                DbHelper.Update(File);
            }
        }

        public override string ToString() => log.ToString();
    }
}