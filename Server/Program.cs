using Avalonia;
using FileFlows.Server.Ui;

namespace FileFlows.Server;

public class Program
{
    public static bool Docker { get; private set; }
    internal static bool WindowsGui { get; private set; }
    private static Mutex appMutex = null;
    const string appName = "FileFlowsServer";

    public static void Main(string[] args)
    {
        try
        {
            if (args.Any(x =>
                    x.ToLower() == "--help" || x.ToLower() == "-?" || x.ToLower() == "/?" || x.ToLower() == "/help" ||
                    x.ToLower() == "-help"))
            {
                Console.WriteLine("FileFlows v" + Globals.Version);
                Console.WriteLine("--no-gui: To hide the GUI");
                return;
            }
            
            Docker = args?.Any(x => x == "--docker") == true;
            var noGui = args?.Any((x => x.ToLower() == "--no-gui")) == true || Docker;
            DirectoryHelper.Init(Docker, false);
            Logger.Instance = new Server.Logger();
            
            Logger.Instance.ILog(new string('=', 50));
            Logger.Instance.ILog("Starting FileFlows " + Globals.Version);
            if(Docker)
                Logger.Instance.ILog("Running inside docker container");
            Logger.Instance.DLog("Arguments: " + (args?.Any() == true ? string.Join(" ", args) : "No arguments"));

            Logger.Instance.ILog(new string('=', 50));
            InitEncryptionKey();

            if (Docker == false)
            {
                appMutex = new Mutex(true, appName, out bool createdNew);
                if (createdNew == false)
                {
                    // app is already running
                    if (noGui)
                    {
                        Console.WriteLine("An instance of FileFlows is already running");
                    }
                    else
                    {
                        try
                        {
                            var appBuilder = BuildAvaloniaApp(true);
                            appBuilder.StartWithClassicDesktopLifetime(args);
                        }
                        catch (Exception) { }
                    }
            
                    return;
                }
            }

            if (Docker || noGui)
            {
                Console.WriteLine("Starting FileFlows Server...");
                WebServer.Start(args);
            }
            else
            {
                if(Globals.IsWindows)
                    Utils.WindowsConsoleManager.Hide();
                
                Task.Run(() =>
                {
                    Console.WriteLine("Starting FileFlows Server...");
                    WebServer.Start(args);
                });

                try
                {
                    var appBuilder = BuildAvaloniaApp();
                    appBuilder.StartWithClassicDesktopLifetime(args);
                }
                catch (Exception) { }
            }

            _ = WebServer.Stop();
            Console.WriteLine("Exiting FileFlows Server...");
        }
        catch (Exception ex)
        {
            try
            {
                Logger.Instance.ELog("Error: " + ex.Message + Environment.NewLine + ex.StackTrace);
            }
            catch (Exception) { }
            Console.WriteLine("Error: " + ex.Message + Environment.NewLine + ex.StackTrace);
        }
    }

    /// <summary>
    /// Init the encryption key, by making this a file in app data, it wont be included with the database if provided to for support
    /// It wont be lost if inside a docker container and updated
    /// </summary>
    private static void InitEncryptionKey()
    {
        string encryptionFile = DirectoryHelper.EncryptionKeyFile;
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


    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp(bool messagebox = false)
        => (messagebox ? AppBuilder.Configure<MessageApp>() : AppBuilder.Configure<App>())
            .UsePlatformDetect();
}
