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
    /// <summary>
    /// Gets or sets if this node is registered
    /// </summary>
    public bool Registered { get; private set; }

    /// <summary>
    /// Starts the node processing
    /// </summary>
    public void Start()
    {
        StartWorkers();
    }

    /// <summary>
    /// Stops the node processing
    /// </summary>
    public void Stop()
    {
        WorkerManager.StopWorkers();
    }

    /// <summary>
    /// Starts the node workers
    /// </summary>
    private void StartWorkers()
    {
        Shared.Logger.Instance?.ILog("Starting workers");
        var updater = new NodeUpdater();
        if (updater.RunCheck())
            return;
        
        WorkerManager.StartWorkers(new FlowWorker(AppSettings.Instance.HostName)
        {
            IsEnabledCheck = () =>
            {
                if (this.Registered == false)
                    return false;
                
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
                    if(ex.Message?.Contains("502 Bad Gateway") == true)
                        Logger.Instance?.ELog("Failed checking enabled: Unable to reach FileFlows Server.");
                    else
                        Logger.Instance?.ELog("Failed checking enabled: " + ex.Message + Environment.NewLine + ex.StackTrace);
                }
                return false;
            }
        }, updater);
    }

    
    /// <summary>
    /// Registers the node with the server
    /// </summary>
    /// <returns>whether or not it was registered</returns>
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
            {
                this.Registered = false;
                return false;
            }
        }
        catch (Exception ex)
        {
            Shared.Logger.Instance?.ELog("Failed to register with server: " + ex.Message);
            this.Registered = false;
            return false;
        }

        Service.ServiceBaseUrl = settings.ServerUrl;
        if (Service.ServiceBaseUrl.EndsWith("/"))
            Service.ServiceBaseUrl = Service.ServiceBaseUrl.Substring(0, Service.ServiceBaseUrl.Length - 1);

        Shared.Logger.Instance?.ILog("Successfully registered node");
        settings.Enabled = result.Enabled;
        settings.Runners = result.FlowRunners;
        settings.Save();
        this.Registered = true;
        return true;
    }
}
