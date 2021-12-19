namespace FileFlows.Server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                if (args.FirstOrDefault() == "--windows")
                    StartWindows(args.Skip(1).ToArray());
                else
                {
                    Console.WriteLine("Starting FileFlows Server...");
                    WebServer.Start(args);
                    Console.WriteLine("Exiting FileFlows Server...");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }

        public static void StartWindows(string[] args)
        {

            string logfile = GetLogFile();
            if (File.Exists(logfile))
            {
                File.Move(logfile, logfile.Replace(".log", ".old.log"), true);
            }

            FileStream ostream;
            StreamWriter writer;
            TextWriter oldOut = Console.Out;
            ostream = new FileStream(logfile, FileMode.Create, FileAccess.Write);
            writer = new StreamWriter(ostream);
            writer.AutoFlush = true;
            Console.SetOut(writer);

            Console.WriteLine("Starting FileFlows Server...");
            WebServer.Start(args);
            Console.WriteLine("Exiting FileFlows Server...");

            Console.SetOut(oldOut);
            writer.Close();
            ostream.Close();
        }



        internal static string GetAppDataDirectory()
        {
            var dir = Directory.GetCurrentDirectory();
            //string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            //string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            //if (dir.ToLower().Contains(programFiles.ToLower()))
            //{
            //    string path = Path.Combine(appData, "FileFlows");
            //    if (Directory.Exists(path) == false)
            //        Directory.CreateDirectory(path);
            //    return path;
            //}
            )
            return dir;
        }

        private static string GetLogFile()
        {
            string dir = Path.Combine(GetAppDataDirectory(), "Logs");
            if (Directory.Exists(dir) == false)
                Directory.CreateDirectory(dir);
            return Path.Combine(dir, "FileFlows.log");
        }
    }
}