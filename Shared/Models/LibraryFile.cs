namespace FileFlows.Shared.Models;

using FileFlows.Plugin;
using System;
using System.Collections.Generic;

/// <summary>
/// A library file is a file that FileFlows will process
/// </summary>
public class LibraryFile : FileFlowObject
{
    /// <summary>
    /// Gets or sets the relative path of the library file.
    /// This is the path relative to the library
    /// </summary>
    public string RelativePath { get; set; }

    /// <summary>
    /// Gets or sets the path of the final output file
    /// </summary>
    public string OutputPath { get; set; }

    /// <summary>
    /// Gets or sets the flow that executed this file
    /// </summary>
    public ObjectReference Flow { get; set; }

    /// <summary>
    /// Gets or sets a list of nodes that were executed against this library file
    /// </summary>
    public List<ExecutedNode> ExecutedNodes { get; set; }

    /// <summary>
    /// Gets or sets the library this library files belongs to
    /// </summary>
    public ObjectReference Library { get; set; }
    
    /// <summary>
    /// Gets or sets an object reference to an existing
    /// library file that this file is a duplicate of
    /// </summary>
    public ObjectReference Duplicate { get; set; }

    /// <summary>
    /// Gets or sets the size of the original library file
    /// </summary>
    public long OriginalSize { get; set; }
    
    /// <summary>
    /// Gets or sets the size of the final file after processing
    /// </summary>
    public long FinalSize { get; set; }
    
    /// <summary>
    /// Gets or sets the fingerprint of the file
    /// </summary>
    public string Fingerprint { get; set; }
    
    /// <summary>
    /// Gets or sets the node tha this processing/has processed the file
    /// </summary>
    public ObjectReference Node { get; set; }

    /// <summary>
    /// Gets or sets the UID of the worker that is executing this library file
    /// </summary>
    public Guid WorkerUid { get; set; }
    
    /// <summary>
    /// Gets or sets when the file began processing
    /// </summary>
    public DateTime ProcessingStarted { get; set; }
    
    /// <summary>
    /// Gets or sets when the file finished processing
    /// </summary>
    public DateTime ProcessingEnded { get; set; }

    /// <summary>
    /// Gets or sets the processing status of the file
    /// </summary>
    public FileStatus Status { get; set; }
    
    /// <summary>
    /// Gets or sets the order of the file when the file should be processed
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Gets or sets if this library file is a directory
    /// </summary>
    public bool IsDirectory { get; set; }

    /// <summary>
    /// Gets the total processing time of the library file
    /// </summary>
    public TimeSpan ProcessingTime
    {
        get
        {
            if (Status == FileStatus.Unprocessed)
                return new TimeSpan();
            if (Status == FileStatus.Processing)
                return DateTime.Now.Subtract(ProcessingStarted);
            if (ProcessingEnded < new DateTime(2000, 1, 1))
                return new TimeSpan();
            return ProcessingEnded.Subtract(ProcessingStarted);
        }
    }
}

/// <summary>
/// Possible status of library files
/// </summary>
public enum FileStatus
{
    /// <summary>
    /// The library is disabled and the file will not be processed
    /// </summary>
    Disabled = -2,
    /// <summary>
    /// The library is out of schedule and will not process until it is in the processing schedule
    /// </summary>
    OutOfSchedule = -1,
    /// <summary>
    /// The file has not been processed
    /// </summary>
    Unprocessed = 0,
    /// <summary>
    /// The file has been successfully processed
    /// </summary>
    Processed = 1,
    /// <summary>
    /// The file is currently processing
    /// </summary>
    Processing = 2,
    /// <summary>
    /// The file cannot be processed as the flow configured for the library can not be found
    /// </summary>
    FlowNotFound = 3,
    /// <summary>
    /// THe file was processed, but exited with a failure
    /// </summary>
    ProcessingFailed = 4,
    /// <summary>
    /// The file is a duplicate of an existing library file
    /// </summary>
    Duplicate = 5,
    /// <summary>
    /// The file could not be processed due to a mapping issue
    /// </summary>
    MappingIssue = 6
}

/// <summary>
/// A node/flow part that has been executed
/// </summary>
public class ExecutedNode
{
    /// <summary>
    /// Gets or sets the name of the node part
    /// </summary>
    public string NodeName { get; set; }
    
    /// <summary>
    /// Gets or sets the UID of the node part
    /// </summary>
    public string NodeUid { get; set; }
    
    /// <summary>
    /// Gets or sets the time it took to process this node 
    /// </summary>
    public TimeSpan ProcessingTime { get; set; }
    
    /// <summary>
    /// Gets or sets the output from this node
    /// </summary>
    public int Output { get; set; }
}