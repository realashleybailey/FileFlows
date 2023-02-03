namespace FileFlows.Shared.Models;

/// <summary>
/// A task that runs at a configured schedule
/// </summary>
public class FileFlowsTask : FileFlowObject
{
    /// <summary>
    /// Gets or sets the script this task will execute
    /// </summary>
    public string Script { get; set; }

    /// <summary>
    /// Gets or sets the type of task
    /// </summary>
    public TaskType Type { get; set; }

    /// <summary>
    /// Gets or sets the schedule this script runs at
    /// </summary>
    public string Schedule { get; set; }
    
    /// <summary>
    /// Gets or sets when the task was last run
    /// </summary>
    public DateTime LastRun { get; set; }
}