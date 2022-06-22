using System.Text.Json.Serialization;
using FileFlows.Shared.Json;

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

    /// <summary>
    /// Gets or sets the processing times for libraries
    /// </summary>
    public IEnumerable<BoxPlotData> LibraryProcessingTimes { get; set; }
}


public class SystemValue<T>
{
    [JsonPropertyName("x")]
    [JsonConverter(typeof(DateTimeStringConverter))]
    public DateTime Time { get; set; } = DateTime.Now;
    
    [JsonPropertyName("y")]
    public T Value { get; set; }
}

public class BoxPlotData
{
    public string Name { get; set; }
    public int Minimum { get; set; }
    public int LowQuartile { get; set; }
    public int Median { get; set; }
    public int HighQuartile { get; set; }
    public int Maximum { get; set; }
}