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
            using (Mutex mutex = new Mutex(false, "Global\\" + appGuid))
            {
                if (mutex.WaitOne(0, false) == false)
                {
                    MessageBox.Show("Instance already running", "FileFlows");
                    return;
                }
                bool minimize = args?.FirstOrDefault() == "-minimized";
                ApplicationConfiguration.Initialize();

                HttpHelper.Client = new HttpClient();

                FileFlows.Shared.Logger.Instance = new ServerShared.FileLogger(Application.ExecutablePath + ".log");

                AppSettings.Init();

                Application.Run(new Form1(minimize));
            }
        }
    }
}