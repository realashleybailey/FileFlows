using System.Collections;
using Avalonia;
using FileFlows.Server.Ui;

namespace FileFlows.Server;

public class Program
{
    /// <summary>
    /// Gets if this is running inside a docker container
    /// </summary>
    public static bool Docker => ServerShared.Globals.IsDocker;
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
            
            ServerShared.Globals.IsDocker = args?.Any(x => x == "--docker") == true;
            var noGui = args?.Any((x => x.ToLower() == "--no-gui")) == true || Docker;
            DirectoryHelper.Init(Docker, false);
            
            
            if(File.Exists(Path.Combine(DirectoryHelper.BaseDirectory, "server-upgrade.bat")))
                File.Delete(Path.Combine(DirectoryHelper.BaseDirectory, "server-upgrade.bat"));
            if(File.Exists(Path.Combine(DirectoryHelper.BaseDirectory, "server-upgrade.sh")))
                File.Delete(Path.Combine(DirectoryHelper.BaseDirectory, "server-upgrade.sh"));
            
            Logger.Instance = new Server.Logger();
            ServerShared.Logger.Instance = Logger.Instance;

            Logger.Instance.ILog(new string('=', 50));
            Logger.Instance.ILog("Starting FileFlows " + Globals.Version);
            if(Docker)
                Logger.Instance.ILog("Running inside docker container");
            Logger.Instance.DLog("Arguments: " + (args?.Any() == true ? string.Join(" ", args) : "No arguments"));
            foreach (DictionaryEntry var in Environment.GetEnvironmentVariables())
            {
                Logger.Instance.DLog($"ENV.{var.Key} = {var.Value}");
            }
            
            Logger.Instance.ILog(new string('=', 50));

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


    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp(bool messagebox = false)
        => (messagebox ? AppBuilder.Configure<MessageApp>() : AppBuilder.Configure<App>())
            .UsePlatformDetect();
}
