using FileFlows.Node;
using FileFlows.Shared.Helpers;

namespace FileFlows.WindowsNode
{
    internal static class Program
    {
        const string appGuid = "f77f5093-4d04-48b5-9824-cb8cf91ffff2";

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            bool silent = args?.FirstOrDefault() == "--silent";
            using (Mutex mutex = new Mutex(false, "Global\\" + appGuid))
            {
                if (mutex.WaitOne(0, false) == false)
                {
                    if(silent == false)
                        MessageBox.Show("Instance already running", "FileFlows Node");
                    return;
                }

                HttpHelper.Client = new HttpClient();

                string logPath = Path.Combine(new FileInfo(Application.ExecutablePath).DirectoryName, "Logs");
                if (Directory.Exists(logPath) == false)
                    Directory.CreateDirectory(logPath);

                FileFlows.Shared.Logger.Instance = new ServerShared.FileLogger(logPath, "FileFlowsNode");

                string message = "=    Starting FileFlows Node v" + Globals.Version + "    =";
                FileFlows.Shared.Logger.Instance.ILog(new string('=', message.Length));
                FileFlows.Shared.Logger.Instance.ILog(message);
                FileFlows.Shared.Logger.Instance.ILog(new string('=', message.Length));

                AppSettings.Init();

                bool minimize = AppSettings.IsConfigured();
                ApplicationConfiguration.Initialize();
                Application.Run(new Form1(minimize));
            }
        }
    }
}