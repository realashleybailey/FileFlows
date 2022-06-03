using System.Collections;
using Avalonia;
using FileFlows.Server.Database;
using FileFlows.Server.Database.Managers;
using FileFlows.Server.Helpers;
using FileFlows.Server.Ui;
using FileFlows.Shared.Models;

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


            InitializeLogger();

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

            CleanDefaultTempDirectory();

            if (Docker == false)
            {
                appMutex = new Mutex(true, appName, out bool createdNew);
                if (createdNew == false)
                {
                    // app is already running;
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

            if (PrepareDatabase() == false)
                return;

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

    private static void InitializeLogger()
    {
        Logger.Instance = new Server.Logger();
        Shared.Logger.Instance = Logger.Instance;
        ServerShared.Logger.Instance = Logger.Instance;
    }

    private static bool PrepareDatabase()
    {
        if (string.IsNullOrEmpty(AppSettings.Instance.DatabaseMigrateConnection) == false)
        {
            if (AppSettings.Instance.DatabaseConnection == AppSettings.Instance.DatabaseMigrateConnection)
            {
                AppSettings.Instance.DatabaseMigrateConnection = null;
                AppSettings.Instance.Save();
            }
            else
            {
                Console.WriteLine("Database migration starting");
                bool migrated = DbMigrater.Migrate(AppSettings.Instance.DatabaseConnection,
                    AppSettings.Instance.DatabaseMigrateConnection);
                if (migrated)
                    AppSettings.Instance.DatabaseConnection = AppSettings.Instance.DatabaseMigrateConnection;
                else
                    Console.WriteLine("Database migration failed, reverting to previous database settings");
                AppSettings.Instance.DatabaseMigrateConnection = null;
                AppSettings.Instance.Save();
            }
        }
            
        // initialize the database
        if (DbHelper.Initialize().Result == false)
        {
            Logger.Instance.ELog("Failed initializing database");
            return false;
        }
        Logger.Instance.ILog("Database initialized");
            
        // run any upgrade code that may need to be run
        var settings = DbHelper.Single<Settings>().Result;
        new Upgrade.Upgrader().Run(settings);
        
        return true;
    }

    /// <summary>
    /// Clean the default temp directory on startup
    /// </summary>
    private static void CleanDefaultTempDirectory()
    {
        string tempDir = Docker ? Path.Combine(DirectoryHelper.DataDirectory, "temp") : Path.Combine(DirectoryHelper.BaseDirectory, "Temp");
        DirectoryHelper.CleanDirectory(tempDir);
    }


    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp(bool messagebox = false)
        => (messagebox ? AppBuilder.Configure<MessageApp>() : AppBuilder.Configure<App>())
            .UsePlatformDetect();
}
