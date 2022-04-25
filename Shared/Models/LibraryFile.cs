namespace FileFlows.Shared.Models
{
    using FileFlows.Plugin;
    using System;
    using System.Collections.Generic;

    public class LibraryFile : FileFlowObject
    {
        public string RelativePath { get; set; }

        public string OutputPath { get; set; }

        public ObjectReference Flow { get; set; }

        /// <summary>
        /// Gets or sets a list of nodes that were executed against this library file
        /// </summary>
        public List<ExecutedNode> ExecutedNodes { get; set; }

        public ObjectReference Library { get; set; }
        public ObjectReference Duplicate { get; set; }

        public long OriginalSize { get; set; }
        public long FinalSize { get; set; }
        public string Fingerprint { get; set; }
        public ObjectReference Node { get; set; }

        public Guid WorkerUid { get; set; }

        public DateTime ProcessingStarted { get; set; }
        public DateTime ProcessingEnded { get; set; }

        public FileStatus Status { get; set; }
        public int Order { get; set; }

        public bool IsDirectory { get; set; }

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

    public enum FileStatus
    {
        Disabled = -2,
        OutOfSchedule = -1,
        Unprocessed = 0,
        Processed = 1,
        Processing = 2,
        FlowNotFound = 3,
        ProcessingFailed = 4,
        Duplicate = 5,
        MappingIssue = 6
    }

    public class ExecutedNode
    {
        public string NodeName { get; set; }
        public string NodeUid { get; set; }
        public TimeSpan ProcessingTime { get; set; }
        public int Output { get; set; }
    }
}