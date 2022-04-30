using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using FileFlows.Node.Ui;
using FileFlows.Node.Workers;
using FileFlows.Server.Workers;
using FileFlows.ServerShared.Models;
using FileFlows.ServerShared.Services;

namespace FileFlows.Node;
public class Program
{
    internal static NodeManager? Manager { get; private set; }
    public static void Main(string[] args)
    {
        try
        {
            AppSettings.ForcedServerUrl = Environment.GetEnvironmentVariable("ServerUrl");
            AppSettings.ForcedTempPath = Environment.GetEnvironmentVariable("TempPath");
            AppSettings.ForcedHostName = Environment.GetEnvironmentVariable("NodeName");

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

            string logPath = Path.Combine(Directory.GetCurrentDirectory(), "Logs");
            if (Directory.Exists(logPath) == false)
                Directory.CreateDirectory(logPath);

            Shared.Logger.Instance = new ServerShared.FileLogger(logPath, "FileFlows-Node");
            Shared.Logger.Instance?.ILog("FileFlows Node version: " + Globals.Version);

            AppSettings.Init();


            bool docker = args?.Any(x => x == "--docker") == true;
            bool noGui = args?.Any(x => x == "--no-gui") == true;
            bool showUi = docker == false && noGui == false;

            Manager = new ();
            Shared.Helpers.HttpHelper.Client = new HttpClient();

            if (showUi)
            {
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
                    while (true)
                    {
                        var key = Console.ReadKey();
                        if (key.Key == ConsoleKey.Escape)
                            break;
                    }
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
}
