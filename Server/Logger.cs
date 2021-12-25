namespace FileFlows.Server
{
    public class Logger : FileFlows.Plugin.ILogger
    {
        Queue<string> LogTail = new Queue<string>(1000);

        public string GetTail()
        {
            return string.Join(Environment.NewLine , LogTail.ToArray());    
        }

        private enum LogType { Error, Warning, Debug, Info }
        private void Log(LogType type, object[] args)
        {
            string prefix = type switch
            {
                LogType.Info => "INFO",
                LogType.Error => "ERRR",
                LogType.Warning => "WARN",
                LogType.Debug => "DBUG",
                _ => ""
            };

            var now = Helpers.TimeHelper.UserNow();

            string message = now.ToString("yyyy-MM-dd HH:mm:ss.fffff") + " [" + prefix + "] -> " + string.Join(", ", args.Select(x =>
                x == null ? "null" :
                x.GetType().IsPrimitive ? x.ToString() :
                x is string ? x.ToString() :
                System.Text.Json.JsonSerializer.Serialize(x)));
            Console.WriteLine(message);
            LogTail.Enqueue(message);
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
            set
            {
                if (value != null)
                    _Instance = value;
            }
        }
    }
}