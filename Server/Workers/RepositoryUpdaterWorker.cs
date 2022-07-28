using FileFlows.Server.Controllers;
using FileFlows.Server.Services;
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
        var service = new RepositoryService();
        service.Init().Wait();
        service.DownloadFlowTemplates().Wait();
        service.DownloadFunctionScripts().Wait();
        service.DownloadLibraryTemplates().Wait();
    }
}