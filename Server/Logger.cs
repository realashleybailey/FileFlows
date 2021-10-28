namespace ViWatcher.Server
{
    public class Logger
    {
        private enum LogType { Error, Warning, Debug, Info }
        public void ELog(string message) => Log(LogType.Error, message);
        public void WLog(string message) => Log(LogType.Warning, message);
        public void DLog(string message) => Log(LogType.Debug, message);
        public void ILog(string message) => Log(LogType.Info, message);

        private void Log(LogType type, string message)
        {
            Console.WriteLine(type + " -> " + message);
        }

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