using FileFlows.Node.Workers;
using FileFlows.Server.Workers;
using FileFlows.ServerShared.Models;
using FileFlows.ServerShared.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FileFlows.Node;

public class NodeManager
{
    public void Start()
    {
        StartWorkers();
    }

    public void Stop()
    {
        WorkerManager.StopWorkers();
    }

    private void StartWorkers()
    {
        Shared.Logger.Instance?.ILog("Starting workers");
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
    }

    public async Task<bool> Register()
    {
        string dll = Assembly.GetExecutingAssembly().Location;
        string path = new FileInfo(dll).DirectoryName ?? string.Empty;

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
            result = await nodeService.Register(settings.ServerUrl, settings.HostName, settings.TempPath, settings.Runners, settings.Enabled, mappings);
            if (result == null)
                return false;
        }
        catch (Exception ex)
        {
            Shared.Logger.Instance?.ELog("Failed to register with server: " + ex.Message);
            return false;
        }

        Service.ServiceBaseUrl = settings.ServerUrl;
        if (Service.ServiceBaseUrl.EndsWith("/"))
            Service.ServiceBaseUrl = Service.ServiceBaseUrl.Substring(0, Service.ServiceBaseUrl.Length - 1);

        Shared.Logger.Instance?.ILog("Successfully registered node");
        settings.Enabled = result.Enabled;
        settings.Runners = result.FlowRunners;
        settings.Save();
        return true;
    }
}
