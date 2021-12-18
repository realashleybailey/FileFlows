using FileFlows.ServerShared;

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


                string logfile = Application.ExecutablePath + ".log";
                if (File.Exists(logfile))
                {
                    File.Move(logfile, Application.ExecutablePath + ".old.log", true);
                }

                Server.Logger.Instance = new ConsoleLogger();

                FileStream ostream;
                StreamWriter writer;
                TextWriter oldOut = Console.Out;
                ostream = new FileStream(Application.ExecutablePath + ".log", FileMode.Create, FileAccess.Write);
                writer = new StreamWriter(ostream);
                writer.AutoFlush = true;
                Console.SetOut(writer);

                Console.WriteLine("Starting app");


                ApplicationConfiguration.Initialize();
                Application.Run(new Form1());

                Console.SetOut(oldOut);
                writer.Close();
                ostream.Close();

            }
        }
    }
}