namespace FileFlows.Shared.Models;

/// <summary>
/// A task that runs at a configured schedule
/// </summary>
public class ScheduledTask : FileFlowObject
{
    /// <summary>
    /// Gets or sets the script this task will execute
    /// </summary>
    public string Script { get; set; }
    
    /// <summary>
    /// Gets or sets the schedule this script runs at
    /// </summary>
    public string Schedule { get; set; }

    /// <summary>
    /// Gets or sets when this script was last run
    /// </summary>
    public DateTime LastRunAt { get; set; }
}