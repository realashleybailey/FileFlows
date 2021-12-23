namespace FileFlows.Shared.Models
{
    public class Library : FileFlowObject
    {
        public bool Enabled { get; set; }
        public string Path { get; set; }
        public string Filter { get; set; }
        public string Template { get; set; }
        public string Description { get; set; }
        public ObjectReference Flow { get; set; }

        /// <summary>
        /// If this library monitors for directories or files
        /// </summary>
        public bool Directories { get; set; }

        public string Schedule { get; set; }

        /// <summary>
        /// When the library was last scanned
        /// </summary>
        public DateTime LastScanned { get; set; }


        /// <summary>
        /// The timespan of when this was last scanned
        /// </summary>
        public TimeSpan LastScannedAgo => DateTime.UtcNow - LastScanned;

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
    }

    public enum ProcessingPriority
    {
        Lowest = -10,
        Low = -5,
        Normal = 0,
        High = 5,
        Highest = 10

    }
}