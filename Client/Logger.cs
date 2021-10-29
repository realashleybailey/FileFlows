using System;
using Microsoft.JSInterop;
using FileFlow.Plugin;

namespace FileFlow.Client
{
    public class Logger : ILogger
    {
        public static IJSRuntime jsRuntime { get; set; }
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

        public void ELog(params object[] args) => Log(1, args);
        public void WLog(params object[] args) => Log(2, args);
        public void DLog(params object[] args) => Log(3, args);
        public void ILog(params object[] args) => Log(4, args);

        private void Log(int level, object[] args)
        {
            _ = jsRuntime.InvokeVoidAsync("ff.log", new object[] { level, args });
        }
    }
}