using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileFlows.WindowsServer
{
    internal class Logger
    {
        private static string LogFile;
        public static void DLog(params object[] args) => Log(LogType.Debug, args);
        public static void ELog(params object[] args) => Log(LogType.Error, args);
        public static void ILog(params object[] args) => Log(LogType.Info, args);
        public static void WLog(params object[] args) => Log(LogType.Warning, args);

        private enum LogType
        {
            Error, Warning, Info, Debug
        }

        public static void MoveOldLog()
        {
            LogFile = GetLogFile();
            if (File.Exists(LogFile))
            {
                File.Move(LogFile, LogFile.Replace(".log", ".old.log"), true);
            }
        }

        private static void Log(LogType type, params object[] args)
        {
            if (args == null || args.Length == 0)
                return;
            string message = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss.ffff") + " - " + type + " -> WindowsServer: " +
                string.Join(", ", args.Select(x =>
                x == null ? "null" :
                x.GetType().IsPrimitive || x is string ? x.ToString() :
                System.Text.Json.JsonSerializer.Serialize(x)));

            if (string.IsNullOrEmpty(LogFile))
                LogFile = GetLogFile();

            File.AppendAllText(LogFile, message + Environment.NewLine);
        }
        internal static string GetAppDirectory()
        {
            var dir = Directory.GetCurrentDirectory();
            return dir;
        }

        private static string GetLogFile()
        {
            string dir = Path.Combine(GetAppDirectory(), "Logs");
            if (Directory.Exists(dir) == false)
                Directory.CreateDirectory(dir);
            return Path.Combine(dir, "FileFlowsWindows.log");
        }
    }
}
