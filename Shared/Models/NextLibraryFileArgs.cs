namespace FileFlows.Shared.Models;

/// <summary>
/// Arguments for hte Next Library File request
/// </summary>
public class NextLibraryFileArgs
{
    /// <summary>
    /// Gets or sets the name of the node
    /// </summary>
    public string NodeName { get; set; }
    /// <summary>
    /// Gets or sets the Uid of the node
    /// </summary>
    public Guid NodeUid { get; set; }
    /// <summary>
    /// Gets or sets the Version of the node
    /// </summary>
    public string NodeVersion { get; set; }
    /// <summary>
    /// Gets or sets the worker UID
    /// </summary>
    public Guid WorkerUid { get; set; }
}