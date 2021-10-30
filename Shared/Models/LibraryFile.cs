namespace FileFlow.Shared.Models
{
    using System;

    public class LibraryFile : ViObject
    {
        public string Log { get; set; }

        public string RelativePath { get; set; }

        public ObjectReference Flow { get; set; }

        public ObjectReference Library { get; set; }

        public FileStatus Status { get; set; }
        public int Order { get; set; }
    }

    public enum FileStatus
    {
        Unprocessed = 0,
        Processed = 1,
        Processing = 2,
        FlowNotFound = 3,
        ProcessingFailed = 4
    }
}