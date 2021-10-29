namespace FileFlow.Shared.Models
{
    public class FileBrowserItem
    {
        public string Name { get; set; }
        public string FullName { get; set; }
        public bool IsPath { get; set; }
        public bool IsParent { get; set; }
        public bool IsDrive { get; set; }

    }
}