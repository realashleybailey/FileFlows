using FileFlows.Plugin;

namespace FileFlows.Shared.Models;

/// <summary>
/// A library that FileFlows will monitor for files to process
/// </summary>
public class Library : FileFlowObject
{
    /// <summary>
    /// Gets or sets if this library is enabled
    /// </summary>
    public bool Enabled { get; set; }
    
    /// <summary>
    /// Gets or sets the path of the library
    /// </summary>
    public string Path { get; set; }
    
    /// <summary>
    /// Gets or sets the filter used to determine what files to add ot the library files
    /// </summary>
    public string Filter { get; set; }

    /// <summary>
    /// Gets or sets filter to determine if a file should be excluded
    /// </summary>
    public string ExclusionFilter { get; set; }

    /// <summary>
    /// Gets or sets the template this library is based on
    /// </summary>
    public string Template { get; set; }
    
    /// <summary>
    /// Gets or sets the description of the library
    /// </summary>
    public string Description { get; set; }
    
    /// <summary>
    /// Gets or sets the flow this library uses
    /// </summary>
    public ObjectReference Flow { get; set; }

    /// <summary>
    /// Gets or sets if this library should be routinely scanned,
    /// or if false, will listen for file system events.
    /// If off the library will still be fully scanned every other hour
    /// </summary>
    public bool Scan { get; set; }

    /// <summary>
    /// If this library monitors for folders or files
    /// </summary>
    public bool Folders { get; set; }

    /// <summary>
    /// Gets or sets if this library will use fingerprinting to determine if a file already is known
    /// </summary>
    public bool UseFingerprinting { get; set; }
    
    /// <summary>
    /// Gets or sets the number of seconds that have to pass between changes to the folder for it to be scanned into the library
    /// </summary>
    public int WaitTimeSeconds { get; set; }

    /// <summary>
    /// Gets or sets if hidden files and folders should be excluded from the library
    /// </summary>
    public bool ExcludeHidden { get; set; }

    /// <summary>
    /// Gets or sets the schedule for this library
    /// </summary>
    public string Schedule { get; set; }

    /// <summary>
    /// When the library was last scanned
    /// </summary>
    public DateTime LastScanned { get; set; }

    
    /// <summary>
    /// Gets or sets if recreated files (files with a different creation time) should be automatically reprocessed
    /// This is helpful if you download the same file multiple times and want to reprocess it again
    /// </summary>
    public bool ReprocessRecreatedFiles { get; set; }


    /// <summary>
    /// The timespan of when this was last scanned
    /// </summary>
    public TimeSpan LastScannedAgo => DateTime.Now - LastScanned;

    /// <summary>
    /// Gets or sets the number of seconds to scan files
    /// </summary>
    public int ScanInterval { get; set; }

    /// <summary>
    /// Gets or sets the number of seconds to wait before checking for file size changes when scanning the library
    /// </summary>
    public int FileSizeDetectionInterval { get; set; }

    /// <summary>
    /// Gets or sets the processing priority of this library
    /// </summary>
    public ProcessingPriority Priority { get; set; }

    /// <summary>
    /// Gets or sets the order this library will process its files
    /// </summary>
    public ProcessingOrder ProcessingOrder { get; set; }
}

/// <summary>
/// Processing priority, used to prioritize library files for processing
/// </summary>
public enum ProcessingPriority
{
    /// <summary>
    /// Lowest priority
    /// </summary>
    Lowest = -10,
    /// <summary>
    /// Low priority
    /// </summary>
    Low = -5,
    /// <summary>
    /// Normal priority
    /// </summary>
    Normal = 0,
    /// <summary>
    /// High priority
    /// </summary>
    High = 5,
    /// <summary>
    /// Highest priority
    /// </summary>
    Highest = 10
}

public enum ProcessingOrder
{
    /// <summary>
    /// Default order, as they are found
    /// </summary>
    AsFound = 0,
    /// <summary>
    /// Randomly
    /// </summary>
    Random = 1,
    /// <summary>
    /// Smallest files first
    /// </summary>
    SmallestFirst = 2,
    /// <summary>
    /// Largest files first
    /// </summary>
    LargestFirst = 3,
    /// <summary>
    /// Newest files first
    /// </summary>
    NewestFirst = 4,
}