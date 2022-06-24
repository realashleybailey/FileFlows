namespace FileFlows.ServerShared.Models;

/// <summary>
/// Statistics for a Node
/// </summary>
public class NodeSystemStatistics
{
    /// <summary>
    /// Gets or sets the node UID
    /// </summary>
    public Guid Uid { get; set; }

    /// <summary>
    /// Gets or sets the size of the temporary directory
    /// </summary>
    public long TemporaryDirectorySize { get; set; }

    /// <summary>
    /// Gets or sets the size of the log directory
    /// </summary>
    public long LogDirectorySize { get; set; }

    /// <summary>
    /// Gets or sets when this was recorded
    /// </summary>
    public DateTime RecordedAt { get; set; }
}