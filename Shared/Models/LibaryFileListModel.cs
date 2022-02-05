using FileFlows.Plugin;

namespace FileFlows.Shared.Models
{
    public class LibaryFileListModel: IUniqueObject
    {
        public Guid Uid { get; set; }
        public string Name { get; set; }
        public string RelativePath { get; set; }
        public string? Duplicate { get; set; }
        public long? FinalSize { get; set; }
        public string? Flow { get; set; }
        public string? Library { get; set; }
        public string? Node { get; set; }
        public long? OriginalSize { get; set; }
        public string? OutputPath { get; set; }
        public TimeSpan? ProcessingTime { get; set; }
    }
}
