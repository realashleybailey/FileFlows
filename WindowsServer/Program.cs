namespace FileFlows.WindowsServer
{
    internal static class Program
    {
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
                    MessageBox.Show("Instance already running", "FileFlows");
                    return;
                }

                WebServerHelper.Start();

                ApplicationConfiguration.Initialize();
                Application.Run(new Form1());

            }
        }
    }
}