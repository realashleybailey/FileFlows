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
            using (Mutex mutex = new Mutex(false, "Global\\" + appGuid))
            {
                if (mutex.WaitOne(0, false) == false)
                {
                    LaunchBrowser();// MessageBox.Show("Instance already running", "FileFlows");
                    return;
                }
                else
                {
                    WebServerHelper.Start();
                    ApplicationConfiguration.Initialize();
                    LaunchBrowser();
                    Application.Run(new Form1());
                }
            }
        }
        internal static void LaunchBrowser()
        {
            Process.Start(new ProcessStartInfo("cmd", $"/c start {Url}") { CreateNoWindow = true });

        }
    }