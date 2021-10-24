using System;

namespace ViWatcher.Client {
    public class Logger
    {
        static Logger _Instance;
        public static Logger Instance
        {
            get
            {
                if (_Instance == null)
                    _Instance = new Logger();
                return _Instance;
            }
            set { _Instance = value; }
        }

        public void ILog(string message) => Log(message);
        public void ELog(string message) => Log(message);
        public void WLog(string message) => Log(message);
        public void DLog(string message) => Log(message);

        private void Log(string message) {
            Console.WriteLine(message);
        }
    }
}