using System.Diagnostics;

namespace FileFlows.WindowsServer
{
    internal static class Program
    {
        internal static string Url = "http://localhost:5151/";
        const string appGuid = "f77f5093-4d04-48b5-9824-cb8cf91ffff1";
        readonly static string LogFile;
        
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            bool silent = args?.Any(x => x.ToLower() == "--silent") == true;
            bool installer = args?.Any(x => x.ToLower() == "--installer") == true;

            Logger.MoveOldLog();

            if (installer)
            {
                Logger.ILog("Starting from installer");
            }

            using (Mutex mutex = new Mutex(false, "Global\\" + appGuid))
            {
                if (mutex.WaitOne(0, false) == false)
                {
                    if(silent == false)
                        LaunchBrowser();
                    return;
                }
                else
                {
                    var timer = new System.Timers.Timer();
                    timer.Interval = 30_000;
                    timer.Elapsed += Timer_Elapsed;
                    timer.Start();

                    WebServerHelper.Start();
                    ApplicationConfiguration.Initialize();
                    if (silent == false)
                    {
                        Thread.Sleep(5_000);
                        LaunchBrowser();
                    }
                    Application.Run(new Form1());
                    timer.Stop();
                    WebServerHelper.Stop();
                }
            }
        }

        private static void Timer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            var process = Process.GetProcessesByName("FileFlows.Server");
            if (process == null)
            {
                Logger.ILog("FileFlows.Server is not running, restarting it");
                // it stopped, restart it
                WebServerHelper.Start();
            }
        }

        internal static void LaunchBrowser()
        {
            Process.Start(new ProcessStartInfo("cmd", $"/c start {Url}") { CreateNoWindow = true });
        }
    }
}