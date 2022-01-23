using System.Diagnostics;

namespace FileFlows.WindowsServer
{
    internal static class Program
    {
        internal static string Url = "http://localhost:5151/";
        const string appGuid = "f77f5093-4d04-48b5-9824-cb8cf91ffff1";
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            bool silent = args?.FirstOrDefault() == "--silent";
            bool upgraded = args?.FirstOrDefault() == "--upgraded";

            if (upgraded)
            {
                Console.WriteLine("Starting FileFlows after upgrade");
                KillOtherProcesses();
                silent = true;
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

                    WebServerHelper.Start(upgraded);
                    ApplicationConfiguration.Initialize();
                    if(silent == false)
                        LaunchBrowser();
                    Application.Run(new Form1());
                    timer.Stop();
                    WebServerHelper.Stop();
                }
            }
        }

        private static void KillOtherProcesses()
        {
            var current = Process.GetCurrentProcess();


            foreach (var process in Process.GetProcesses())
            {
                if (process.Id == current.Id) 
                    continue;
                string pname = process.ProcessName?.ToLower() ?? string.Empty;
                if (pname.Contains("fileflows") == false)
                    continue;
                if (pname.Contains("node"))
                    continue;

                Console.WriteLine("Killing process: " + process.ProcessName);
                process.Kill();
            }
        }

        private static void Timer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            var process = Process.GetProcessesByName("FileFlows.Server");
            if (process == null)
            {
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