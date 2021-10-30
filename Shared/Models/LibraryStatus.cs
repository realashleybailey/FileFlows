namespace FileFlow.Shared.Models
{
    public class LibraryStatus
    {
        public string Name { get; set; }
        public FileStatus Status { get; set; }
        public int Count { get; set; }
    }
}