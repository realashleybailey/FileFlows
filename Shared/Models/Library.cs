namespace FileFlows.Shared.Models
{
    using System;

    public class Library : ViObject
    {
        public bool Enabled { get; set; }
        public string Path { get; set; }
        public string Filter { get; set; }
        public ObjectReference Flow { get; set; }

        /// <summary>
        /// When the library was last scanned
        /// </summary>
        public DateTime LastScanned { get; set; }


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
    }
}