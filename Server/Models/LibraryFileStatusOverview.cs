namespace FileFlows.Server.Models;

/// <summary>
/// The overview of the library files
/// </summary>
public class LibraryFileStatusOverview
{
    /// <summary>
    /// Gets the number of disabled files there are
    /// </summary>
    public int Disabled { get; set; }
    
    /// <summary>
    /// Gets the number of out of schedule files there are
    /// </summary>
    public int OutOfSchedule { get; set; }
    
    /// <summary>
    /// Gets the number of unprocessed files there are
    /// </summary>
    public int Unprocessed { get; set; }
    
    /// <summary>
    /// Gets the number of processed files there are
    /// </summary>
    public int Processed { get; set; }
    
    /// <summary>
    /// Gets the number of processing files there are
    /// </summary>
    public int Processing { get; set; }
    
    /// <summary>
    /// Gets the number of flow not found files there are
    /// </summary>
    public int FlowNotFound { get; set; }
    
    /// <summary>
    /// Gets the number of processing failed files there are
    /// </summary>
    public int ProcessingFailed { get; set; }
    
    /// <summary>
    /// Gets the number of duplicate files there are
    /// </summary>
    public int Duplicate { get; set; }
    
    /// <summary>
    /// Gets the number of mapping issue files there are
    /// </summary>
    public int MappingIssue { get; set; }
}