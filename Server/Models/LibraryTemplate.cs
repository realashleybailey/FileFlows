using FileFlows.Shared.Models;
using System.Dynamic;

namespace FileFlows.Server.Models
{
    class LibraryTemplate
    {
        public string Name { get; set; }
        public string Group { get; set; }
        public string Description { get; set; }
        public string Filter { get; set; }
        public string Path { get; set; }
        public int ScanInterval { get; set; }

        public ProcessingPriority Priority { get; set; }

        public int FileSizeDetectionInterval { get; set; }
    }
}
