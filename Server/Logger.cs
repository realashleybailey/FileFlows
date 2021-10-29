namespace FileFlow.Server
{
    public class Logger : FileFlow.Plugins.ILogger
    {
        private enum LogType { Error, Warning, Debug, Info }
        private void Log(LogType type, object[] args)
        {
            Console.WriteLine(type + " -> " + string.Join(", ", args.Select(x =>
                x == null ? "null" :
                x.GetType().IsPrimitive ? x.ToString() :
                x is string ? x.ToString() :
                Newtonsoft.Json.JsonConvert.SerializeObject(x)))
            );
        }

        public void ILog(params object[] args) => Log(LogType.Info, args);
        public void DLog(params object[] args) => Log(LogType.Debug, args);
        public void WLog(params object[] args) => Log(LogType.Warning, args);
        public void ELog(params object[] args) => Log(LogType.Error, args);

        static Logger _Instance;
        public static Logger Instance
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