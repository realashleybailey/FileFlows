using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FileFlows.Node.Workers;
using FileFlows.Server.Workers;

namespace FileFlows.Node
{
    public class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                Shared.Logger.Instance = new ServerShared.ConsoleLogger();

                AppSettings.Init();

                if (AppSettings.IsConfigured() == false)
                {
                    Console.WriteLine("Configuration not set");
                    return;
                }

                Shared.Helpers.HttpHelper.Client = new HttpClient();

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
    }
}
