namespace FileFlows.Shared.Models;

/// <summary>
/// Data for System Info
/// </summary>
public class SystemInfoData
{
    /// <summary>
    /// Gets or sets the time from the server
    /// </summary>
    public DateTime SystemDateTime { get; set; }
    /// <summary>
    /// Gets or sets the CPU Usage
    /// </summary>
    public IEnumerable<SystemValue<float>> CpuUsage { get; set; }
    
    /// <summary>
    /// Gets or sets the Memory Usage
    /// </summary>
    public IEnumerable<SystemValue<float>> MemoryUsage { get; set; }
    /// <summary>
    /// Gets or sets the CPU Usage
    /// </summary>
    public IEnumerable<SystemValue<long>> TempStorageUsage { get; set; }
}


public class SystemValue<T>
{
    public DateTime Time { get; set; } = DateTime.Now;
    public T Value { get; set; }
}