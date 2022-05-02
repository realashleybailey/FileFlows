using System.Text;
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
                Console.WriteLine("FileFlows Node Version:" + Globals.Version);
                Console.WriteLine("");
                Console.WriteLine("--server [serveraddress]");
                Console.WriteLine("\teg --server http://tower:5000/");
                Console.WriteLine("--name [nodename]");
                Console.WriteLine("\teg --name " + Environment.MachineName);
                Console.WriteLine("--temp [tempdir]");
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    Console.WriteLine("\teg --temp C:\\fileflows\\temp");
                else
                    Console.WriteLine("\teg --temp /mnt/temp");
                Console.WriteLine("--no-gui does not show the GUI");
            }

            string server = GetArgument(args, "--server");
            if (string.IsNullOrEmpty(server) == false)
                AppSettings.ForcedServerUrl = server;
            string temp = GetArgument(args, "--temp");
            if (string.IsNullOrEmpty(temp) == false)
                AppSettings.ForcedTempPath = temp;
            string name = GetArgument(args, "--name");
            if (string.IsNullOrEmpty(name) == false)
                AppSettings.ForcedHostName = name;

            LoggingDirectory = GetLoggingDirectory();

            Logger.Instance = new ServerShared.FileLogger(LoggingDirectory, "FileFlows-Node");
            Logger.Instance?.ILog("FileFlows Node version: " + Globals.Version);

            AppSettings.Init();


            bool docker = args?.Any(x => x == "--docker") == true;
            bool noGui = args?.Any(x => x == "--no-gui") == true;
            bool showUi = docker == false && noGui == false;

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

    static string GetArgument(string[] args, string name)
    {
        int index = args.Select(x => x.ToLower()).ToList().IndexOf(name.ToLower());
        if (index < 0)
            return string.Empty;
        if (index >= args.Length - 1)
            return string.Empty;
        return args[index + 1];
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
    }
    
}
