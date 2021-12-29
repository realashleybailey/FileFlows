using FileFlows.Shared;

namespace FileFlows.Server
{
    public class Program
    {

        public static bool Docker { get; private set; }

        public static void Main(string[] args)
        {
            try
            {
                if (args.FirstOrDefault() == "--windows")
                    StartWindows(args.Skip(1).ToArray());
                else
                {
                    Console.WriteLine("Starting FileFlows Server...");
                    
                    Docker = args?.Any(x => x == "--docker") == true;

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

        internal static string GetAppDirectory()
        {
            var dir = Directory.GetCurrentDirectory();
            if (Docker)
            {
                // docker we move this to the Data diretory which is configured outside of the docker image
                // this is so the database and any plugins that are downloaded will be kept if the docker
                // image is updated/redownloaded.
                dir = Path.Combine(dir, "Data");
            }
            return dir;
        }

        private static string GetLogFile()
        {
            string dir = Path.Combine(GetAppDirectory(), "Logs");
            if (Directory.Exists(dir) == false)
                Directory.CreateDirectory(dir);
            return Path.Combine(dir, "FileFlows.log");
        }
    }
}