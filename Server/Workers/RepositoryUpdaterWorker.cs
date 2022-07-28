using FileFlows.Server.Controllers;
using FileFlows.ServerShared.Workers;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Workers;

/// <summary>
/// A worker that runs FileFlows Tasks
/// </summary>
public class RepositoryUpdaterWorker: Worker
{
    private static RepositoryUpdaterWorker Instance;
    
    
    /// <summary>
    /// Creates a new instance of the Scheduled Task Worker
    /// </summary>
    public RepositoryUpdaterWorker() : base(ScheduleType.Daily, 5)
    {
        Instance = this;
        Execute();
    }
    
    /// <summary>
    /// Executes any tasks
    /// </summary>
    protected override void Execute()
    {
        var repo = GetRepository();
        UpdateFunctionScripts(repo);
        UpdateFlowTemplates(repo);
        UpdateLibraryTemplates(repo);
    }

    private FileFlowsRepository GetRepository()
    {
        var controller = new ScriptRepositoryController();
        return controller.GetRepository().Result;
    }

    private void UpdateFunctionScripts(FileFlowsRepository repo = null)
    {
        repo ??= GetRepository();
        Logger.Instance.ILog("Downloading Function Templates");
        var controller = new ScriptRepositoryController();
        controller.DownloadTemplateScripts(repo).Wait();
        Logger.Instance.ILog("Downloaded Function Templates");
    }

    private void UpdateFlowTemplates(FileFlowsRepository repo = null)
    {
        repo ??= GetRepository();
        Logger.Instance.ILog("Downloading Flow Templates");
        var controller = new ScriptRepositoryController();
        controller.DownloadFlowTemplates(repo).Wait();
        Logger.Instance.ILog("Downloaded Flow Templates");
    }

    private void UpdateLibraryTemplates(FileFlowsRepository repo = null)
    {
        repo ??= GetRepository();
        Logger.Instance.ILog("Downloading Library Templates");
        var controller = new ScriptRepositoryController();
        controller.DownloadLibraryTemplates(repo).Wait();
        Logger.Instance.ILog("Downloaded Library Templates");
    }
}