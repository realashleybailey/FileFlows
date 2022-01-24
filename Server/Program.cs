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
                Docker = args?.Any(x => x == "--docker") == true;
                InitEncryptionKey();

                if (args.FirstOrDefault() == "--windows")
                    StartWindows(args.Skip(1).ToArray());
                else
                {
                    Console.WriteLine("Starting FileFlows Server...");
                    MoveOldLogFiles();
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
            MoveOldLogFiles();
            WebServer.Start(args);
            Console.WriteLine("Exiting FileFlows Server...");


            Console.SetOut(oldOut);
            writer.Close();
            ostream.Close();
        }

        /// <summary>
        /// Init the encryption key, by making this a file in app data, it wont be included with the database if provided to for support
        /// It wont be lost if inside a docker container and updated
        /// </summary>
        private static void InitEncryptionKey()
        {
            string encryptionFile = Path.Combine(GetAppDirectory(), "encryptionkey.txt");
            if (File.Exists(encryptionFile))
            {
                string key = File.ReadAllText(encryptionFile);
                if (string.IsNullOrEmpty(key) == false)
                {
                    Helpers.Decrypter.EncryptionKey = key;
                    return;
                }
            }
            else
            {
                string key = Guid.NewGuid().ToString();
                File.WriteAllText(encryptionFile, key);
                Helpers.Decrypter.EncryptionKey = key;
            }
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

        private static void MoveOldLogFiles()
        {
            try
            {
                string source = Path.Combine(GetAppDirectory(), "Logs");
                string dest = Path.Combine(source, "LibraryFiles");
                if (Directory.Exists(dest) == false)
                    Directory.CreateDirectory(dest);

                foreach (var file in new DirectoryInfo(source).GetFiles("*.log"))
                {
                    if (file.Name.Contains("FileFlows"))
                        continue;
                    Logger.Instance.ILog("Test log file to be moved: " + file.Name);
                    if (file.Name.Length != 40)
                        continue; // not a guid name
                    try
                    {
                        file.MoveTo(Path.Combine(dest, file.Name), true);
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.ELog("Failed moving file: " + file.Name + " => " + ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Instance?.ELog("Failed moving old log files: " + ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }
    }
}