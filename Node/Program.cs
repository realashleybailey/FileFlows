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

                Shared.Logger.Instance = new ServerShared.ConsoleLogger();

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
                WorkerManager.StartWorkers(new FlowWorker()
                {
                    IsEnabledCheck = () =>
                    {
                        if (AppSettings.IsConfigured() == false)
                            return false;


                        var nodeService = new ServerShared.Services.NodeService();
                        try
                        {
                            var settings = nodeService.GetByAddress(Environment.MachineName).Result;

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

                while (true)
                {
                    var key = System.Console.ReadKey();
                    if (key.Key == ConsoleKey.Escape)
                        break;
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
            var result = nodeService.Register(settings.ServerUrl, Environment.MachineName, settings.TempPath, settings.Runners, settings.Enabled, mappings).Result;
            if (result == null)
                return false;

            settings.Enabled = result.Enabled;
            settings.Runners = result.FlowRunners;
            settings.Save();
            return true;
        }
    }
}
