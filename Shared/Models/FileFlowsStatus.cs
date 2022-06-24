namespace FileFlows.Shared.Models;

/// <summary>
/// The system status of FileFlows
/// </summary>
public class FileFlowsStatus
{
    /// <summary>
    /// Gets or sets the configuration status of FileFLows
    /// </summary>
    public ConfigurationStatus ConfigurationStatus { get; set; }

    /// <summary>
    /// Gets or sets if FileFlows is using an external database
    /// </summary>
    public bool ExternalDatabase { get; set; }
    
    /// <summary>
    /// Gets or sets if FileFlows is licensed
    /// </summary>
    public bool Licensed { get; set; }
}

/// <summary>
/// The configuration status of the system
/// </summary>
[Flags]
public enum ConfigurationStatus
{
    /// <summary>
    /// Flows are configured
    /// </summary>
    Flows = 1,
    /// <summary>
    /// Libraries are configured
    /// </summary>
    Libraries = 2
}