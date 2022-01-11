using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using FileFlows.Node.Workers;
using FileFlows.Server.Workers;
using FileFlows.ServerShared.Models;
using FileFlows.ServerShared.Services;

namespace FileFlows.Node
{
    public class Program
    {
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
                

                Shared.Logger.Instance = new ServerShared.ConsoleLogger();
                Shared.Logger.Instance?.ILog("FileFlows Node version: " + Globals.Version);

                AppSettings.Init();

                if (AppSettings.IsConfigured() == false)
                {
                    Console.WriteLine("Configuration not set");
                    return;
                }

                Shared.Helpers.HttpHelper.Client = new HttpClient();

                Shared.Logger.Instance.ILog("Registering FileFlow Node");

                if (Register() == false)
                    return;

                Shared.Logger.Instance.ILog("FileFlows node starting");


                Shared.Logger.Instance.ILog("Press Esc to quit");


                Shared.Logger.Instance.ILog("Starting workers");
                WorkerManager.StartWorkers(new FlowWorker(AppSettings.Instance.HostName)
                {
                    IsEnabledCheck = () =>
                    {
                        if (AppSettings.IsConfigured() == false)
                            return false;


                        var nodeService = new ServerShared.Services.NodeService();
                        try
                        {
                            var settings = nodeService.GetByAddress(AppSettings.Instance.HostName).Result;

                            AppSettings.Instance.Enabled = settings.Enabled;
                            AppSettings.Instance.Runners = settings.FlowRunners;
                            AppSettings.Instance.TempPath = settings.TempPath;
                            AppSettings.Instance.Save();

                            return AppSettings.Instance.Enabled;
                        }
                        catch (Exception ex)
                        {
                            Shared.Logger.Instance?.ELog("Failed checking enabled: " + ex.Message + Environment.NewLine + ex.StackTrace);
                        }
                        return false;
                    }
                });

                try
                {
                    while (true)
                    {
                        var key = System.Console.ReadKey();
                        if (key.Key == ConsoleKey.Escape)
                            break;
                    }
                }catch (Exception)
                {
                    // can throw an exception if not run from console
                    Thread.Sleep(-1);
                    
                }

                Shared.Logger.Instance.ILog("Stopping workers");

                WorkerManager.StopWorkers();

                Shared.Logger.Instance.ILog("Exiting");
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

        private static bool Register()
        {
            string dll = Assembly.GetExecutingAssembly().Location;
            string path = new FileInfo(dll).DirectoryName;

            bool windows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);


            List<RegisterModelMapping> mappings = new List<RegisterModelMapping>
            {
                new RegisterModelMapping
                {
                    Server = "ffmpeg",
                    Local = Path.Combine(path, "Tools", windows ? "ffmpeg.exe" : "ffmpeg")
                }
            };

            var settings = AppSettings.Instance;
            var nodeService = new NodeService();
            Shared.Models.ProcessingNode result;
            try
            {
                result = nodeService.Register(settings.ServerUrl, settings.HostName, settings.TempPath, settings.Runners, settings.Enabled, mappings).Result;
                if (result == null)
                    return false;
            }
            catch(Exception ex)
            {
                Shared.Logger.Instance?.ELog("Failed to register with server: " + ex.Message);
                return false;
            }

            Service.ServiceBaseUrl = settings.ServerUrl;
            if(Service.ServiceBaseUrl.EndsWith("/"))
                Service.ServiceBaseUrl = Service.ServiceBaseUrl.Substring(0, Service.ServiceBaseUrl.Length - 1);    

            Shared.Logger.Instance?.ILog("Successfully registered node");
            settings.Enabled = result.Enabled;
            settings.Runners = result.FlowRunners;
            settings.Save();
            return true;
        }
    }
}
