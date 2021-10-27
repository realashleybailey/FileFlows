using System;
using Microsoft.JSInterop;

namespace ViWatcher.Client {
    public class Logger
    {
        public static IJSRuntime jsRuntime{ get; set; }
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

        public void ELog(object message, object parameters = null) => Log(1, message, parameters);
        public void WLog(object message, object parameters = null) => Log(2, message, parameters);
        public void DLog(object message, object parameters = null) => Log(3, message, parameters);
        public void ILog(object message, object parameters = null) => Log(4, message, parameters);

        private void Log(int level, object message, object parameters) {
            _ = jsRuntime.InvokeVoidAsync("Vi.log", new object[] { level, message, parameters });
        }
    }
}