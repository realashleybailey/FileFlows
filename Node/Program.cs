using Avalonia;
using FileFlows.Node.Ui;
using FileFlows.ServerShared.Services;

namespace FileFlows.Node;
public class Program
{
    internal static NodeManager? Manager { get; private set; }

    internal static string LoggingDirectory = String.Empty;
    private static bool Exiting = false;
    public static void Main(string[] args)
    {
        if(File.Exists("node-upgrade.bat"))
            File.Delete("node-upgrade.bat");
        if(File.Exists("node-upgrade.sh"))
            File.Delete("node-upgrade.sh");
        try
        {
            AppSettings.ForcedServerUrl = Environment.GetEnvironmentVariable("ServerUrl");
            AppSettings.ForcedTempPath = Environment.GetEnvironmentVariable("TempPath");
            AppSettings.ForcedHostName = Environment.GetEnvironmentVariable("NodeName");
            
            Service.ServiceBaseUrl = AppSettings.Load().ServerUrl;
            #if(DEBUG)
            if (string.IsNullOrEmpty(Service.ServiceBaseUrl))
                Service.ServiceBaseUrl = "http://localhost:6868/";
            #endif

            args ??= new string[] { };
            if (args.Any(x => x.ToLower() == "--help" || x.ToLower() == "-?" || x.ToLower() == "/?" || x.ToLower() == "/help" || x.ToLower() == "-help"))
            {
                CommandLineOptions.PrintHelp();
                return;
            }

            var options = CommandLineOptions.Parse(args);

            if (string.IsNullOrEmpty(options.Server) == false)
                AppSettings.ForcedServerUrl = options.Server;
            if (string.IsNullOrEmpty(options.Temp) == false)
                AppSettings.ForcedTempPath = options.Temp;
            if (string.IsNullOrEmpty(options.Name) == false)
                AppSettings.ForcedHostName = options.Name;

            LoggingDirectory = GetLoggingDirectory();

            Logger.Instance = new ServerShared.FileLogger(LoggingDirectory, "FileFlows-Node");
            Logger.Instance?.ILog("FileFlows Node version: " + Globals.Version);

            AppSettings.Init();


            bool showUi = options.Docker == false && options.NoGui == false;

            Manager = new ();
            Shared.Helpers.HttpHelper.Client = new HttpClient();

            if (showUi)
            {
                if(Globals.IsWindows)
                    Utils.WindowsConsoleManager.Hide();
                
                Logger.Instance?.ILog("Launching GUI");
                Task.Run(async () =>
                {
                    await Manager.Register();
                    Manager.Start();
                });
                try
                {
                    var appBuilder = BuildAvaloniaApp();
                    appBuilder.StartWithClassicDesktopLifetime(args);
                }
                catch (Exception) { }

            }
            else
            {
                if (AppSettings.IsConfigured() == false)
                {
                    Shared.Logger.Instance?.ELog("Configuration not set");
                    return;
                }

                Shared.Logger.Instance?.ILog("Registering FileFlow Node");

                if (Manager.Register().Result == false)
                    return;

                Shared.Logger.Instance?.ILog("FileFlows node starting");
                
                Manager.Start();

                Shared.Logger.Instance?.ILog("Press Esc to quit");

                try
                {
                    Task.WhenAny(new []
                    {
                        new Task( () =>
                        {
                            while (true)
                            {
                                var key = Console.ReadKey();
                                if (key.Key == ConsoleKey.Escape)
                                    break;
                            }
                        }),
                        new Task(() =>
                        {
                            while (Exiting == false)
                                Thread.Sleep(100);
                        })
                    });
                }
                catch (Exception)
                {
                    // can throw an exception if not run from console
                    Thread.Sleep(-1);
                }

                Shared.Logger.Instance?.ILog("Stopping workers");

                Manager.Stop();

                Shared.Logger.Instance?.ILog("Exiting");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message + Environment.NewLine + ex.StackTrace);
        }
    }

    private static string GetLoggingDirectory()
    {
        string current = Directory.GetCurrentDirectory();
        if (current.Replace("\\", "/").ToLower().EndsWith("fileflows/node"))
            current = new DirectoryInfo(Directory.GetCurrentDirectory())?.Parent?.FullName ?? current;
        
        string dir = Path.Combine(current, "Logs");
        if (Directory.Exists(dir) == false)
            Directory.CreateDirectory(dir);
        return dir;
    }


    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect();

    /// <summary>
    /// Quits the node application
    /// </summary>
    internal static void Quit()
    {
        Exiting = true;
        MainWindow.Instance?.ForceQuit();
        Environment.Exit(0);
    }
    
}
