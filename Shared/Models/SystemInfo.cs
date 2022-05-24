namespace FileFlows.Shared.Models;

/// <summary>
/// Gets the system information about FileFlows
/// </summary>
public class SystemInfo
{
    /// <summary>
    /// Gets the amount of memory used by FileFlows
    /// </summary>
    public long MemoryUsage { get; set; }
    
    /// <summary>
    /// Gets the how much CPU is used by FileFlows
    /// </summary>
    public float CpuUsage { get; set; }
}