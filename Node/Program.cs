using System.Runtime.CompilerServices;
using Avalonia;
using FileFlows.Node.Ui;
using FileFlows.ServerShared.Helpers;
using FileFlows.ServerShared.Services;

namespace FileFlows.Node;
public class Program
{
    /// <summary>
    /// Gets an instance of a node manager
    /// </summary>
    internal static NodeManager? Manager { get; private set; }

    private static bool Exiting = false;
    private static Mutex appMutex = null;
    const string appName = "FileFlowsNode";
    public static void Main(string[] args)
    {
        args ??= new string[] { };
        if (args.Any(x => x.ToLower() == "--help" || x.ToLower() == "-?" || x.ToLower() == "/?" || x.ToLower() == "/help" || x.ToLower() == "-help"))
        {
            CommandLineOptions.PrintHelp();
            return;
        }

        var options = CommandLineOptions.Parse(args);
        if (Globals.IsLinux && options.InstallService)
        {
            Utils.SystemdService.Install(options.DotNet);
            return;
        }
        Globals.IsDocker = options.Docker;
        ServerShared.Globals.IsDocker = options.Docker;
        
        DirectoryHelper.Init(options.Docker, true);
        
        appMutex = new Mutex(true, appName, out bool createdNew);
        if (createdNew == false)
        {
            // app is already running
            if (options.NoGui)
            {
                Console.WriteLine("An instance of FileFlows Node is already running");
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


            if (string.IsNullOrEmpty(options.Server) == false)
                AppSettings.ForcedServerUrl = options.Server;
            if (string.IsNullOrEmpty(options.Temp) == false)
                AppSettings.ForcedTempPath = options.Temp;
            if (string.IsNullOrEmpty(options.Name) == false)
                AppSettings.ForcedHostName = options.Name;


            Logger.Instance = new ServerShared.FileLogger(DirectoryHelper.LoggingDirectory, "FileFlows-Node");
            ServerShared.Logger.Instance = Logger.Instance;
            Logger.Instance?.ILog("FileFlows Node version: " + Globals.Version);

            AppSettings.Init();


            bool showUi = options.Docker == false && options.NoGui == false;

            Manager = new ();
            Shared.Helpers.HttpHelper.Client = new HttpClient();
            
            
            if(File.Exists(Path.Combine(DirectoryHelper.BaseDirectory, "node-upgrade.bat")))
                File.Delete(Path.Combine(DirectoryHelper.BaseDirectory, "node-upgrade.bat"));
            if(File.Exists(Path.Combine(DirectoryHelper.BaseDirectory, "node-upgrade.sh")))
                File.Delete(Path.Combine(DirectoryHelper.BaseDirectory, "node-upgrade.sh"));

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

                if (Globals.IsDocker == false)
                {
                    Shared.Logger.Instance?.ILog("Press Esc to quit");

                    try
                    {
                        Task.WhenAny(new[]
                        {
                            new Task(() =>
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
                }
                else
                {
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


    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp(bool messagebox = false)
        => (messagebox ? AppBuilder.Configure<MessageApp>() : AppBuilder.Configure<App>())
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
