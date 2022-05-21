using FileFlows.Server.Controllers;
using FileFlows.ServerShared.Workers;

namespace FileFlows.Server.Workers;

/// <summary>
/// A worker that monitors FlowRunners and will cancel
/// any "dead" runners
/// </summary>
public class FlowRunnerMonitor:Worker
{
    private WorkerController Controller;
    
    /// <summary>
    /// Constructs a Flow Runner Monitor worker
    /// </summary>
    public FlowRunnerMonitor() : base(ScheduleType.Second, 10)
    {
        Controller = new(null);
    }

    protected override void Execute()
    {
        Controller.AbortDisconnectedRunners();
    }
}