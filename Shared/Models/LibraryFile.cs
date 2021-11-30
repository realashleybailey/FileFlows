namespace FileFlows.Shared.Models
{
    using System;

    public class LibraryFile : ViObject
    {
        public string RelativePath { get; set; }

        public string OutputPath { get; set; }

        public ObjectReference Flow { get; set; }

        public ObjectReference Library { get; set; }

        public long OriginalSize { get; set; }
        public long FinalSize { get; set; }

        public DateTime ProcessingStarted { get; set; }
        public DateTime ProcessingEnded { get; set; }

        public FileStatus Status { get; set; }
        public int Order { get; set; }

        public TimeSpan ProcessingTime
        {
            get
            {
                if (Status == FileStatus.Unprocessed)
                    return new TimeSpan();
                if (Status == FileStatus.Processing)
                    return DateTime.Now.Subtract(ProcessingStarted);
                return ProcessingEnded.Subtract(ProcessingStarted);
            }
        }
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